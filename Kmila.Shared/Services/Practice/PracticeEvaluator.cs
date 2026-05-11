using System.Diagnostics;
using Interpreter.Services;
using Kmila.Shared.Models;
using Kmila.Shared.Models.Practice;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Practice;

/// <summary>
/// Phase 0 grader. For each test case it (a) simulates the canonical
/// <see cref="PracticeExercise.SolutionVhdl"/> to compute the expected
/// waveform on first use (cached for the service lifetime), and (b)
/// simulates the candidate's code, then diffs the listed output ports at the
/// configured sample times.
/// </summary>
/// <remarks>
/// Each grade owns its own <see cref="ProgramBuilder"/> instance so the
/// student's open Editor doesn't have its in-flight simulation stomped on.
/// The Interpreter's <c>ProgressReporter</c> is a process-wide singleton
/// though, so we serialize evaluator runs through <see cref="_runGate"/> to
/// avoid the two pipelines tripping over each other.
/// </remarks>
public class PracticeEvaluator
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private readonly Dictionary<string, WaveformData> _expectedCache = new();

    public PracticeEvaluator(ILogger<PracticeEvaluator>? logger = null)
    {
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public async Task<PracticeAttemptVerdict> GradeAsync(PracticeExercise exercise, string candidateCode, CancellationToken ct = default)
    {
        var totalSw = Stopwatch.StartNew();
        long buildMs = 0;
        var results = new List<PracticeTestResult>();

        // Modality-aware code transformation. For FillGap exercises only the
        // text between -- BEGIN_EDIT and -- END_EDIT in the user's buffer is
        // accepted: we splice that region into the canonical starter and
        // grade the spliced version. Anything the user typed outside the
        // markers (paste, devtools tampering, accidental edits to the locked
        // scaffold) is silently discarded — the visual lock in Monaco is a
        // hint, this is the actual enforcement.
        var graderInput = exercise.Modality == PracticeModality.FillGap
            ? SpliceFillGap(exercise.StarterVhdl, candidateCode)
            : candidateCode;

        await _runGate.WaitAsync(ct);
        try
        {
            // ── 1. Run the candidate against every test case. We build once
            // (Parser-stage cost) but each test is its own RunSimulationAsync.
            // Build failures short-circuit with a BuildError verdict.
            string? buildErrorMsg = null;
            string? buildErrorDetail = null;

            foreach (var test in exercise.TestCases)
            {
                ct.ThrowIfCancellationRequested();

                // Generate (or fetch cached) expected waveform from the canonical solution.
                WaveformData? expected;
                try
                {
                    expected = await GetExpectedAsync(exercise, test, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Practice] Expected generation failed for {Ex}/{Tc}", exercise.Id, test.Id);
                    results.Add(new PracticeTestResult(
                        TestCaseId: test.Id,
                        Passed: false,
                        ErrorClass: "expected-generation",
                        FirstMismatchSignal: null,
                        FirstMismatchTimeNs: null,
                        ExpectedAtMismatch: null,
                        ActualAtMismatch: null,
                        MismatchCount: 0,
                        SimulationDurationMs: 0));
                    continue;
                }

                if (expected == null)
                {
                    results.Add(new PracticeTestResult(
                        test.Id, false, "expected-empty",
                        null, null, null, null, 0, 0));
                    continue;
                }

                // Simulate the candidate.
                var sw = Stopwatch.StartNew();
                WaveformData? actual = null;
                string? errorClass = null;
                try
                {
                    actual = await SimulateAsync(graderInput, test, ct);
                }
                catch (BuildPhaseException bex)
                {
                    buildErrorMsg = bex.Message;
                    buildErrorDetail = bex.Detail;
                    errorClass = "build";
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Practice] Candidate sim threw on {Ex}/{Tc}", exercise.Id, test.Id);
                    errorClass = "runtime";
                }
                sw.Stop();

                if (errorClass != null || actual == null)
                {
                    results.Add(new PracticeTestResult(
                        test.Id, false, errorClass ?? "no-output",
                        null, null, null, null, 0, sw.ElapsedMilliseconds));

                    // Build errors are fatal across all tests — short-circuit.
                    if (errorClass == "build") break;
                    continue;
                }

                results.Add(Compare(test, expected, actual, exercise.MatchKind, exercise.OutputPortsToCompare, exercise.SampleTimesNs, sw.ElapsedMilliseconds));
            }

            totalSw.Stop();

            PracticeVerdictKind kind;
            if (buildErrorMsg != null)              kind = PracticeVerdictKind.BuildError;
            else if (results.Count == 0)            kind = PracticeVerdictKind.RuntimeError;
            else if (results.All(r => r.Passed))    kind = PracticeVerdictKind.Solved;
            else if (results.Any(r => r.ErrorClass == "runtime")) kind = PracticeVerdictKind.RuntimeError;
            else                                    kind = PracticeVerdictKind.WrongAnswer;

            return new PracticeAttemptVerdict(
                Kind: kind,
                ExerciseId: exercise.Id,
                SubmittedAt: DateTime.UtcNow,
                BuildDurationMs: buildMs,
                TotalDurationMs: totalSw.ElapsedMilliseconds,
                BuildErrorMessage: buildErrorMsg,
                BuildErrorDetail: buildErrorDetail,
                TestResults: results);
        }
        finally
        {
            _runGate.Release();
        }
    }

    private async Task<WaveformData?> GetExpectedAsync(PracticeExercise ex, PracticeTestCase test, CancellationToken ct)
    {
        var key = $"{ex.Id}|{test.Id}";
        if (_expectedCache.TryGetValue(key, out var cached)) return cached;

        var produced = await SimulateAsync(ex.SolutionVhdl, test, ct);
        if (produced != null) _expectedCache[key] = produced;
        return produced;
    }

    /// <summary>
    /// Writes <paramref name="vhdlSource"/> to a temp file, builds it with a
    /// fresh <see cref="ProgramBuilder"/>, runs the simulation, and returns
    /// the resulting <see cref="WaveformData"/>. Throws
    /// <see cref="BuildPhaseException"/> if the parse/build phase fails so
    /// callers can distinguish those from runtime simulation errors.
    /// </summary>
    private async Task<WaveformData?> SimulateAsync(string vhdlSource, PracticeTestCase test, CancellationToken ct)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "kmila-practice", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var fileName = "design.vhd";
        var filePath = Path.Combine(tempDir, fileName);
        try
        {
            await File.WriteAllTextAsync(filePath, vhdlSource, ct);

            using var builder = new ProgramBuilder();
            var project = new Project { Name = "PracticeRun", BasePath = tempDir };
            var pf = new ProjectFile
            {
                FileName = fileName,
                DirectoryPath = string.Empty,
                Content = vhdlSource,
                Project = project,
            };

            // Build phase. Map any throw into a BuildPhaseException so we can
            // surface it as a build-error verdict rather than a generic runtime fail.
            string? buildError = null;
            string? buildDetail = null;
            void OnErr(string msg, string? det) { buildError ??= msg; buildDetail ??= det; }
            builder.OnBuildError += OnErr;
            try
            {
                if (!await builder.BuildAsync(pf, ct))
                    throw new BuildPhaseException("Build returned false", null);
            }
            catch (BuildPhaseException) { throw; }
            catch (Exception ex)
            {
                throw new BuildPhaseException(ex.Message, buildDetail ?? buildError);
            }
            finally
            {
                builder.OnBuildError -= OnErr;
            }

            // Runner completion. ProgramBuilder fires its events synchronously
            // from inside SimulationRunner.OnSimulationEnded — `OnSimulationEnded`
            // first, then `OnSimulationCompleted` (only if a history with
            // samples exists). We must NOT subscribe to `OnSimulationEnded` for
            // early completion: doing so races OnDone and would set the tcs to
            // null before the history ever arrives.
            //
            // Strategy: wait for RunSimulationAsync to return — by then both
            // events have fired. If tcs is set we have history; otherwise the
            // run produced none and we return null.
            var tcs = new TaskCompletionSource<SignalHistory?>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnDone(SignalHistory h) => tcs.TrySetResult(h);
            void OnRuntimeErr(string msg, string? det)
            {
                // Surface the runner's *detail* — it's the line that names the
                // VHDL construct or signal that actually failed. Without this
                // the verdict shows only the generic title "Simulation error",
                // which makes diagnosing impossible. Format as a single string
                // since the verdict UI takes only an error message.
                if (!tcs.Task.IsCompleted)
                {
                    var combined = string.IsNullOrEmpty(det) ? msg : $"{msg}: {det}";
                    tcs.TrySetException(new InvalidOperationException(combined));
                }
            }

            builder.OnSimulationCompleted += OnDone;
            builder.OnBuildError += OnRuntimeErr;

            try
            {
                using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

                // Wrap RunSimulationAsync in Task.Run so a synchronous portion
                // of the runner can't block this thread. Capture exceptions
                // into the tcs so they propagate to the awaiter.
                var runTask = Task.Run(async () =>
                {
                    try
                    {
                        await builder.RunSimulationAsync(test.Inputs, test.Duration, test.SystemClockMhz);
                    }
                    catch (Exception runEx)
                    {
                        if (!tcs.Task.IsCompleted) tcs.TrySetException(runEx);
                    }
                }, ct);

                // Wait for the runner to finish. ProgramBuilder's events fire
                // synchronously inside the runner before this await returns.
                await runTask;

                // No history was produced → report as null so the caller
                // surfaces a runtime/empty-output verdict instead of hanging.
                if (!tcs.Task.IsCompleted)
                {
                    // Diagnostic — the no-waveform case is the validator's
                    // dominant failure mode. Snapshot what the engine *did*
                    // record (signals tracked, sample count) before we
                    // forfeit the run, so /logs shows the actual gap.
                    var diag = builder.SimulationHistory;
                    var trackedCount = 0;
                    var totalSamples = 0L;
                    try
                    {
                        if (diag != null)
                        {
                            trackedCount = diag.GetTrackedSignals()?.Count() ?? 0;
                            totalSamples = diag.TotalSamples;
                        }
                    }
                    catch { /* defensive — diagnostic must not throw */ }
                    _logger.LogWarning(
                        "[Practice] Sim ended without history for test {Tc}: tracked-signals={Sigs}, total-samples={Samples}, history-null={NullHist}",
                        test.Id, trackedCount, totalSamples, diag == null);
                    tcs.TrySetResult(null);
                }

                var history = await tcs.Task;
                if (history == null) return null;
                return WaveformData.FromSignalHistory(history, builder.LastTickResNs);
            }
            finally
            {
                builder.OnSimulationCompleted -= OnDone;
                builder.OnBuildError -= OnRuntimeErr;
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort */ }
        }
    }

    /// <summary>
    /// Compares actual to expected on the listed output ports. Phase 0 only
    /// implements <see cref="PracticeMatchKind.ValueAtTimes"/>; other modes
    /// degrade to "first sample == final sample".
    /// </summary>
    private static PracticeTestResult Compare(
        PracticeTestCase test,
        WaveformData expected,
        WaveformData actual,
        PracticeMatchKind matchKind,
        IReadOnlyList<string> outputPorts,
        IReadOnlyList<long> sampleTimes,
        long simMs)
    {
        IReadOnlyList<long> times = matchKind switch
        {
            PracticeMatchKind.FinalValueOnly => new[] { (long)((double)test.Duration.Ticks / TimeSpan.TicksPerMillisecond * 1_000_000.0) },
            _ => sampleTimes.Count > 0 ? sampleTimes : new long[] { (long)((double)test.Duration.Ticks / TimeSpan.TicksPerMillisecond * 1_000_000.0) }
        };

        int mismatches = 0;
        string? firstSig = null;
        long? firstNs = null;
        string? expVal = null;
        string? actVal = null;

        foreach (var port in outputPorts)
        {
            var expSig = expected.Signals.FirstOrDefault(s =>
                string.Equals(s.Name, port, StringComparison.OrdinalIgnoreCase));
            var actSig = actual.Signals.FirstOrDefault(s =>
                string.Equals(s.Name, port, StringComparison.OrdinalIgnoreCase));

            if (expSig == null || actSig == null)
            {
                mismatches++;
                firstSig ??= port;
                firstNs ??= 0;
                expVal ??= expSig == null ? "(missing)" : "(present)";
                actVal ??= actSig == null ? "(missing)" : "(present)";
                continue;
            }

            foreach (var t in times)
            {
                var e = SampleAtNs(expSig, t);
                var a = SampleAtNs(actSig, t);
                if (!ValuesEqual(e, a))
                {
                    mismatches++;
                    if (firstSig == null)
                    {
                        firstSig = port;
                        firstNs = t;
                        expVal = FormatValue(e, expSig);
                        actVal = FormatValue(a, actSig);
                    }
                }
            }
        }

        return new PracticeTestResult(
            TestCaseId: test.Id,
            Passed: mismatches == 0,
            ErrorClass: mismatches == 0 ? null : "value-mismatch",
            FirstMismatchSignal: firstSig,
            FirstMismatchTimeNs: firstNs,
            ExpectedAtMismatch: expVal,
            ActualAtMismatch: actVal,
            MismatchCount: mismatches,
            SimulationDurationMs: simMs);
    }

    /// <summary>Step-function lookup: the value at <paramref name="ns"/> is the most recent transition at or before that time.</summary>
    private static double? SampleAtNs(WaveformSignal sig, long ns)
    {
        var ts = sig.Transitions;
        if (ts.Count == 0) return null;
        int lo = 0, hi = ts.Count - 1, best = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >>> 1;
            if (ts[mid].TimeNs <= ns) { best = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return best < 0 ? null : ts[best].Value;
    }

    private static bool ValuesEqual(double? a, double? b)
    {
        if (!a.HasValue && !b.HasValue) return true;
        if (a.HasValue != b.HasValue) return false;
        return Math.Abs(a!.Value - b!.Value) < 1e-9;
    }

    private static string FormatValue(double? v, WaveformSignal sig)
    {
        if (!v.HasValue) return "U";
        if (sig.DataType == "std_logic") return ((int)Math.Round(v.Value)).ToString();
        return $"{(long)Math.Round(v.Value)} (0x{(long)Math.Round(v.Value):X})";
    }

    /// <summary>
    /// Internal sentinel exception raised when the build phase of a practice grade
    /// fails. Carries optional <see cref="Detail"/> with a parser/synthesis trace
    /// so the caller can render a richer <see cref="PracticeVerdictKind.BuildError"/>.
    /// </summary>
    private class BuildPhaseException : Exception
    {
        /// <summary>Optional detailed error trace (parser output, stack snippet, …).</summary>
        public string? Detail { get; }
        /// <summary>Creates a build-phase exception with a summary message and optional detailed trace.</summary>
        public BuildPhaseException(string message, string? detail) : base(message) { Detail = detail; }
    }

    /// <summary>
    /// For FillGap exercises: extract the text between <c>-- BEGIN_EDIT</c>
    /// and <c>-- END_EDIT</c> markers in the user's submission and splice
    /// it into the canonical starter at the equivalent location. Anything
    /// the user wrote outside the markers is discarded.
    /// </summary>
    /// <remarks>
    /// Marker matching is case-insensitive and whitespace-tolerant — the
    /// student can have any indentation before the marker and any text
    /// after it on the same line. If either side is missing markers we
    /// fall back to the candidate as-is so the grader still produces a
    /// build/sim error with a meaningful diagnostic instead of silently
    /// erasing the user's work.
    /// </remarks>
    internal static string SpliceFillGap(string starter, string candidate)
    {
        var (sBegin, sEnd) = FindEditMarkers(starter);
        if (sBegin < 0 || sEnd < 0 || sEnd <= sBegin) return candidate;

        var (cBegin, cEnd) = FindEditMarkers(candidate);
        if (cBegin < 0 || cEnd < 0 || cEnd <= cBegin) return candidate;

        // Text BETWEEN the marker lines (exclusive) on each side.
        var userBlock = candidate.Substring(cBegin, cEnd - cBegin);

        return starter.Substring(0, sBegin)
             + userBlock
             + starter.Substring(sEnd);
    }

    /// <summary>
    /// Returns the (begin, end) char offsets representing the text BETWEEN
    /// the marker lines — i.e. begin = char-index just after the newline
    /// terminating the BEGIN_EDIT line, end = char-index of the first
    /// character of the END_EDIT line. Returns (-1, -1) if either marker
    /// is missing.
    /// </summary>
    private static (int begin, int end) FindEditMarkers(string source)
    {
        if (string.IsNullOrEmpty(source)) return (-1, -1);

        int beginIdx = IndexOfMarkerLine(source, "BEGIN_EDIT");
        if (beginIdx < 0) return (-1, -1);
        int afterBeginNewline = IndexOfNextLineStart(source, beginIdx);

        int endIdx = IndexOfMarkerLine(source, "END_EDIT", searchFrom: afterBeginNewline);
        if (endIdx < 0) return (-1, -1);

        return (afterBeginNewline, endIdx);
    }

    /// <summary>Find the start-of-line index for the line that contains the marker token. Case-insensitive.</summary>
    private static int IndexOfMarkerLine(string source, string token, int searchFrom = 0)
    {
        int idx = source.IndexOf(token, searchFrom, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return -1;
        // Walk back to the start of that line.
        int lineStart = idx;
        while (lineStart > 0 && source[lineStart - 1] != '\n') lineStart--;
        return lineStart;
    }

    private static int IndexOfNextLineStart(string source, int from)
    {
        int nl = source.IndexOf('\n', from);
        return nl < 0 ? source.Length : nl + 1;
    }
}
