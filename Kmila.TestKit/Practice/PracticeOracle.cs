using Kmila.Frontend;
using Kmila.Shared.Models.Practice;
using Kmila.Shared.Services.Api;
using Kmila.Shared.Services.Vcd;
using Kmila.TestKit.Harness;
using Kmila.TestKit.Reference;

namespace Kmila.TestKit.Practice;

/// <summary>One practice test checked against the reference simulator: equal at every graded instant, or why not.</summary>
public sealed record OracleResult(string TestId, bool Pass, List<SignalDiff>? Diff, string? Error);

/// <summary>
/// The external oracle of the practices: each test simulates the canonical
/// solution in Kmila (the headless pipeline with the app's stimulus policy:
/// proposed clock, LRM initial values) and, under a generated testbench that
/// applies the very same <c>SimulationConfig</c>, in the language's reference
/// simulator; the output ports must agree at the instants the app grades
/// (<see cref="PracticeExercise.GradedInstantsNs"/>), with the nine values exact.
/// The design is never modified.
/// </summary>
public static class PracticeOracle
{
    /// <summary>Checks every test of <paramref name="exercise"/>'s <paramref name="language"/> solution.</summary>
    public static async Task<List<OracleResult>> CheckAsync(PracticeExercise exercise, HdlLanguage language, CancellationToken ct = default)
    {
        var results = new List<OracleResult>();
        var reference = ReferenceSimulators.For(language);
        var harness = HarnessGenerators.For(language);
        if (reference is null || harness is null)
        {
            results.Add(new OracleResult("*", false, null, $"no reference simulator or harness generator for {language.DisplayName}"));
            return results;
        }
        string solution = exercise.Variant(language).Solution;
        foreach (var test in exercise.TestCases)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await CheckTestAsync(exercise, language, solution, test, reference, harness, ct));
        }
        return results;
    }

    private static async Task<OracleResult> CheckTestAsync(PracticeExercise exercise, HdlLanguage language, string solution,
        PracticeTestCase test, IReferenceSimulator reference, IHarnessGenerator harness, CancellationToken ct)
    {
        const int TickResNs = TimeMachine.Models.TimeClock.NS_PER_TICK;
        string dir = Path.Combine(Path.GetTempPath(), "kmila-oracle", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string design = Path.Combine(dir, "design" + language.PrimaryExtension);
            await File.WriteAllTextAsync(design, solution, ct);
            long totalNs = test.DurationNs;

            // Kmila: the headless pipeline with no declared stimulus options = the app's practice policy.
            var run = await HeadlessSimulator.RunProjectAsync(dir, new HeadlessRunOptions
            {
                Language = language,
                TotalNs = totalNs,
                TickResNs = TickResNs,
                SystemClockMhz = test.SystemClockMhz,
                PortMoments = test.Inputs,
            });
            if (run.EngineError != null) return new OracleResult(test.Id, false, null, "kmila: " + run.EngineError);

            // Reference: a testbench applying the configuration Kmila just executed.
            string bench = Path.Combine(dir, harness.TopName + language.PrimaryExtension);
            await File.WriteAllTextAsync(bench, harness.Generate(HarnessSpec.From(run.Entity.Name, run.Entity.Ports, run.Config, run.TickResNs)), ct);
            var golden = await reference.RunAsync(new ReferenceRequest([design, bench], harness.TopName, totalNs), ct);
            if (!golden.Ok) return new OracleResult(test.Id, false, null, $"reference exit={golden.ExitCode}: {Tail(golden.Log)}");

            var diff = WaveformComparer.Compare(
                VcdReader.Parse(golden.Vcd!),
                VcdReader.Parse(run.History.ExportToVcd(run.Entity.Name, timescaleNs: run.TickResNs)),
                CompareOptions.Strict(exercise.OutputPortsToCompare, ignoreT0: false) with
                {
                    SampleTimesFs = exercise.GradedInstantsNs(test).Select(ns => ns * Interpreter.Models.PhysicalTime.FsPerNs).ToList(),
                });
            return new OracleResult(test.Id, diff.All(d => d.Equal), diff, null);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or IOException)
        {
            return new OracleResult(test.Id, false, null, ex.Message);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    private static string Tail(string log) => log.Length <= 300 ? log.Trim() : "…" + log[^300..].Trim();
}
