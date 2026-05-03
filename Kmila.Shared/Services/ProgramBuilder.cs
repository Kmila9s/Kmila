using System.Text.RegularExpressions;
using Debugger;
using Interpreter.Models;
using Interpreter.Services;
using Kmila.Shared.Models;
using LibraryCompiler.Repositories;
using LibraryCompiler.Services;
using Parser.Models;
namespace Kmila.Shared.Services;

/// <summary>
/// Manages the building and simulation of a VHDL program.
/// </summary>
public class ProgramBuilder : IDisposable
{
    /// <summary>
    /// Occurs when the simulation has ended.
    /// </summary>
    public event Action<bool>? OnSimulationEnded;
    /// <summary>
    /// Occurs when the simulation progress has advanced.
    /// </summary>
    public event Action<double>? OnProgressAdvanced;
    /// <summary>
    /// Occurs when build progress is updated (progress 0-1, message).
    /// </summary>
    public event Action<float, string>? OnBuildProgressChanged;
    /// <summary>
    /// Occurs when a build error occurs.
    /// </summary>
    public event Action<string, string?>? OnBuildError;
    /// <summary>
    /// Occurs when build completes successfully.
    /// </summary>
    public event Action<string>? OnBuildCompleted;
    /// <summary>
    /// Occurs when build is cancelled by user.
    /// </summary>
    public event Action<string>? OnBuildCancelled;
    /// <summary>
    /// Occurs when the simulation completes with waveform data.
    /// </summary>
    public event Action<SignalHistory>? OnSimulationCompleted;
    /// <summary>
    /// Fires periodically during a running simulation with the live
    /// <see cref="SignalHistory"/> so the UI can grow the waveform in
    /// real time. Throttled by <see cref="ProgressUpdateIntervalMs"/>;
    /// stops firing once the run ends — the final state arrives via
    /// <see cref="OnSimulationCompleted"/>.
    /// </summary>
    public event Action<SignalHistory>? OnSimulationProgressUpdate;
    /// <summary>
    /// Tick rate (ms) for live waveform refreshes. 200 ms ≈ 5 Hz, which
    /// feels real-time without burning UI cycles on long sims; the merge
    /// is incremental so per-tick cost scales with new transitions, not
    /// total sim length.
    /// </summary>
    private const int ProgressUpdateIntervalMs = 200;
    /// <summary>
    /// Gets the parser used to parse the VHDL file.
    /// </summary>
    public Debugger.Repositories.Parser Parser { get; private set; } = default!;
    /// <summary>
    /// Gets whether a build is currently in progress.
    /// </summary>
    public bool IsBuilding { get; private set; } = false;
    /// <summary>
    /// Gets the signal history from the last simulation run.
    /// </summary>
    public SignalHistory? SimulationHistory => _simulationRunner?.Engine?.History;
    /// <summary>
    /// Effective ns-per-tick used by the most recent run. Persisted on the
    /// builder (not the config) so the waveform factory — which is called
    /// from an event handler on a later frame — can label time axes with
    /// the same scale the simulation actually ran at.
    /// Defaults to <see cref="TimeMachine.Models.TimeClock.NS_PER_TICK"/>
    /// (10) so call sites that haven't been updated still get sensible
    /// labels.
    /// </summary>
    public int LastTickResNs { get; private set; } = TimeMachine.Models.TimeClock.NS_PER_TICK;
    // Legacy timer for step-by-step simulation (TimeMachine path)
    private TimeMachine.Repositories.Timer _timeMachine = new();
    // SimulationRunner for fast full-simulation execution
    private SimulationRunner? _simulationRunner;
    // Cancellation token source for cooperative cancellation of async operations
    private readonly CancellationTokenSource _cts = new();

    // Library compiler (optional) — when present, ProgramBuilder pre-scans
    // the file being built for `use X.Y.all` clauses, asks the compiler to
    // resolve them, and seeds the symbol names into FunctionRegistry so the
    // parser sees library-defined functions as known. Null in test contexts
    // that construct ProgramBuilder without DI.
    private readonly LibraryCompiler.Services.LibraryCompiler? _libraryCompiler;

    /// <summary>
    /// Matches a VHDL <c>use</c> clause as <c>library.package.something</c>.
    /// Captures the library segment (group 1) and the package segment
    /// (group 2) so the validator can check the right one against the
    /// resolver. Multiline-anchored to avoid false hits inside string
    /// literals; case-insensitive so <c>USE</c> / <c>Use</c> / <c>use</c>
    /// all match.
    /// </summary>
    private static readonly Regex UseClauseRe = new(
        @"(?im)^\s*use\s+([a-z_][a-z0-9_]*)\s*\.\s*([a-z_][a-z0-9_]*)",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramBuilder"/> class.
    /// </summary>
    /// <param name="libraryCompiler">
    /// DI-supplied library compiler. Optional — when null the builder
    /// behaves exactly as it did before #22's library-compiler integration
    /// (relying on the Interpreter's runtime function lookup instead of
    /// a pre-seeded FunctionRegistry). Test harnesses that construct
    /// <see cref="ProgramBuilder"/> via <c>new()</c> get the legacy path.
    /// </param>
    public ProgramBuilder(LibraryCompiler.Services.LibraryCompiler? libraryCompiler = null)
    {
        _libraryCompiler = libraryCompiler;

        _timeMachine.OnSimulationEnded += OnSimulationEnd;
        _timeMachine.OnProgressAdvance += OnProgressAdvance;

        // Subscribe to ProgressReporter events from Interpreter module
        ProgressReporter.Instance.ProgressChanged += OnInterpreterProgressChanged;
        ProgressReporter.Instance.ErrorOccurred += OnInterpreterError;
        ProgressReporter.Instance.Completed += OnInterpreterCompleted;
        ProgressReporter.Instance.CancellationRequested += OnInterpreterCancellationRequested;
    }

    /// <summary>
    /// Handles progress updates from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterProgressChanged(object? sender, ProgressEventArgs e)
    {
        var (progress, message) = e.Update.ToTuple();
        OnBuildProgressChanged?.Invoke(progress, message);
    }

    /// <summary>
    /// Handles error events from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterError(object? sender, ProgressEventArgs e)
    {
        OnBuildError?.Invoke(e.Update.Message, e.Update.Detail);
    }

    /// <summary>
    /// Handles completion events from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterCompleted(object? sender, ProgressEventArgs e)
    {
        IsBuilding = false;
        OnBuildCompleted?.Invoke(e.Update.Message);
    }

    /// <summary>
    /// Handles cancellation request events from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterCancellationRequested(object? sender, ProgressEventArgs e)
    {
        IsBuilding = false;
        OnBuildCancelled?.Invoke(e.Update.Message);
    }

    /// <summary>
    /// Requests cancellation of the current build operation.
    /// </summary>
    /// <remarks>
    /// This method signals the Interpreter's ProgressReporter to cancel the current operation.
    /// The cancellation is cooperative and may not be immediate depending on the current phase.
    /// </remarks>
    public void CancelBuild()
    {
        if (!IsBuilding) return;
        ProgressReporter.Instance.RequestCancellation();
    }
    /// <summary>
    /// Builds the program from the specified project file.
    /// </summary>
    /// <param name="file">The project file to build.</param>
    /// <returns>True if the build was successful, otherwise false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the build is cancelled by the user.</exception>
    public bool Build(ProjectFile file)
    {
        IsBuilding = true;

        // Start a cancellable operation in the ProgressReporter
        ProgressReporter.Instance.Reset();
        ProgressReporter.Instance.StartCancellableOperation();

        try
        {
            // Honor sub-directory layout: files can live in BasePath/<DirectoryPath>/<FileName>
            // since the file-tree refactor. Empty DirectoryPath collapses to BasePath/<FileName>.
            string filePath = string.IsNullOrEmpty(file.DirectoryPath)
                ? Path.Join(file.Project.BasePath, file.FileName)
                : Path.Combine(new[] { file.Project.BasePath }
                    .Concat(file.DirectoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
                    .Concat(new[] { file.FileName }).ToArray());
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            // Accept both .vhd (IEEE 1076 traditional) and .vhdl (common on Linux
            // tooling). The Parser treats them identically — this is just an
            // input gate.
            if (ext != ".vhdl" && ext != ".vhd")
                throw new Exception($"File not valid: {Path.GetFileName(filePath)}");

            // Check for cancellation before starting parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            // Resolve every `use X.Y.all` clause across the whole project
            // through the LibraryCompiler before the parser walks the file
            // we're building. We scan all sibling .vhd files under
            // Project.BasePath so cross-file dependencies are captured in
            // a single compile pass, and we fail loudly when a clause
            // references a library that hasn't been imported — at that
            // point we genuinely don't know what symbols it would expose,
            // so continuing the build would just produce confusing
            // "unknown function" diagnostics deep in the parser.
            EnsureLibrariesCompiledForBuild(filePath, file.Project?.BasePath);

            Parser = new(filePath);
            Parser.ParseFile();

            // Check for cancellation after parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            if (!Parser.Entities.Any())
                throw new Exception("File not valid");

            return true;
        }
        catch (OperationCanceledException)
        {
            IsBuilding = false;
            throw;
        }
        catch
        {
            IsBuilding = false;
            throw;
        }
    }
    /// <summary>
    /// Runs a full simulation using SimulationRunner (bypasses TimeMachine for speed).
    /// </summary>
    /// <param name="portMoments">Port moment configurations from the UI.</param>
    /// <param name="duration">Total simulation duration.</param>
    /// <param name="systemClockMhz">
    /// Effective system clock frequency (MHz) for this run. Resolved by the
    /// caller from the selected FPGA's default and any per-project override.
    /// Defaults to 50 MHz when the caller didn't compute one (kept for
    /// call sites that predate the FPGA-sourced clock).
    /// </param>
    public async Task RunSimulationAsync(
        List<PortMomentConfiguration>? portMoments,
        TimeSpan duration,
        int systemClockMhz = 50,
        int tickResNs = TimeMachine.Models.TimeClock.NS_PER_TICK)
    {
        // Get first entity with architecture
        var entity = Parser.Entities.FirstOrDefault(e => e.Architecture != null);
        if (entity == null)
            throw new InvalidOperationException("No entity with architecture found.");

        // Remember the effective tick resolution for downstream waveform
        // labelling — see LastTickResNs doc comment.
        LastTickResNs = tickResNs > 0 ? tickResNs : TimeMachine.Models.TimeClock.NS_PER_TICK;

        // Build simulation config from UI parameters
        var config = StimulusBuilder.BuildConfig(entity, portMoments, duration,
            tickResNs: LastTickResNs,
            systemClockMhz: systemClockMhz);

        // Surface the config to stdout (and therefore /logs via the Console
        // redirector wired up in Program.cs) so the user can see exactly what
        // the auto-clock + stimulus injector produced. Wrapped in try/catch
        // because null values on a Variable's getters would otherwise crash
        // the entire Run click before the simulator gets a chance.
        try
        {
            Console.WriteLine($"[Sim] Entity={entity?.Name ?? "?"} ports={entity?.Ports?.Count ?? 0} " +
                $"totalTicks={config?.TotalTicks} clocks={config?.Clocks?.Count ?? 0} stimuli={config?.Stimuli?.Count ?? 0}");
            if (config?.Clocks != null)
            {
                foreach (var clk in config.Clocks)
                {
                    if (clk == null) continue;
                    Console.WriteLine($"[Sim.Clock] name={clk.Signal?.Name ?? "?"} " +
                        $"halfPeriodTicks={clk.HalfPeriodTicks} initialHigh={clk.InitialHigh}");
                }
            }
            if (entity?.Ports != null)
            {
                foreach (var p in entity.Ports)
                {
                    if (p == null) continue;
                    Console.WriteLine($"[Sim.Port] name={p.Name ?? "?"} dir={p.PortType} size={p.Size}");
                }
            }
        }
        catch (Exception diagEx)
        {
            Console.WriteLine($"[Sim.Diag] failed: {diagEx.Message}");
        }

        // Dispose previous runner if any
        _simulationRunner?.Dispose();
        _simulationRunner = new SimulationRunner();

        // Live-update timer: periodically pushes the running SignalHistory
        // out via OnSimulationProgressUpdate so the UI can grow the waveform
        // in real time. Created up here in the closure so OnSimulationEnded
        // can stop it before OnSimulationCompleted fires (keeps the final
        // frame from racing with a trailing tick).
        System.Threading.Timer? progressTimer = null;
        int liveTickInflight = 0;

        // Subscribe to runner events and forward them. Important: we fire
        // OnSimulationCompleted whenever *any* signal history exists — even
        // on user-stopped runs (completed=false). That way the user sees a
        // partial waveform of whatever ran, instead of a blank Waveforms tab
        // after they hit Stop mid-simulation.
        _simulationRunner.OnProgressChanged += progress => OnProgressAdvanced?.Invoke(progress);
        _simulationRunner.OnSimulationEnded += completed =>
        {
            // Stop the live-update timer first so its callback can't race
            // with the final OnSimulationCompleted fire below.
            progressTimer?.Dispose();
            progressTimer = null;

            OnSimulationEnded?.Invoke(completed);
            var history = _simulationRunner?.Engine?.History;
            if (history != null && history.TotalSamples > 0)
            {
                // SimulationRunner now records stimulus + clock transitions
                // live during its tick loop (via engine.History.RecordChanges
                // on the direct-write set), so the post-run synthesis we
                // used to run here is no longer needed.
                OnSimulationCompleted?.Invoke(history);
            }
        };
        _simulationRunner.OnError += msg => OnBuildError?.Invoke("Simulation error", msg);

        // Start the live-update timer just before the run kicks off. The
        // Interlocked guard drops timer fires that arrive while a previous
        // merge is still inflight on the consumer — keeps slow listeners
        // from queueing redundant work.
        progressTimer = new System.Threading.Timer(_ =>
        {
            if (Interlocked.Exchange(ref liveTickInflight, 1) == 1) return;
            try
            {
                // Skip the fire entirely while the runner is stopped or
                // paused. Without this the UI thread would still receive
                // (and render) one no-op merge every 200 ms during a long
                // pause, drowning out subsequent button clicks with render
                // work and making Resume / Stop appear unresponsive.
                var runner = _simulationRunner;
                if (runner == null || !runner.IsRunning || runner.IsPaused) return;

                var h = runner.Engine?.History;
                if (h != null && h.TotalSamples > 0)
                    OnSimulationProgressUpdate?.Invoke(h);
            }
            catch
            {
                // A throwing subscriber must not kill the timer thread —
                // swallow and keep ticking. The next interval will retry.
            }
            finally
            {
                Interlocked.Exchange(ref liveTickInflight, 0);
            }
        }, null, ProgressUpdateIntervalMs, ProgressUpdateIntervalMs);

        try
        {
            // Run on current async context (caller should be on background/UI thread)
            await _simulationRunner.RunAsync(config);
        }
        finally
        {
            // Belt-and-braces: if RunAsync threw before OnSimulationEnded
            // fired, the timer would otherwise leak.
            progressTimer?.Dispose();
        }
    }

    /// <summary>
    /// Restarts the simulation timer with the specified wait time.
    /// </summary>
    /// <param name="WaitTime">The wait time for the simulation.</param>
    public void RestartTimer(TimeSpan WaitTime) => _timeMachine.Restart(WaitTime);
    /// <summary>
    /// Pauses the simulation (SimulationRunner or TimeMachine).
    /// </summary>
    public void PauseTimer()
    {
        if (_simulationRunner?.IsRunning == true)
            _simulationRunner.Pause();
        else
            _timeMachine.Pause();
    }
    /// <summary>
    /// Resumes the simulation (SimulationRunner or TimeMachine).
    /// </summary>
    public void ContinueTimer()
    {
        if (_simulationRunner?.IsPaused == true)
            _simulationRunner.Resume();
        else
            _timeMachine.Continue();
    }
    /// <summary>
    /// Stops the simulation (SimulationRunner or TimeMachine).
    /// </summary>
    public void StopTimer()
    {
        if (_simulationRunner?.IsRunning == true)
            _simulationRunner.Stop();
        else
            _timeMachine.Stop();
    }
    /// <summary>
    /// Forwards simulation progress from the TimeMachine to subscribers.
    /// </summary>
    /// <param name="progress">The simulation progress value (0.0 to 1.0).</param>
    private void OnProgressAdvance(double progress)
    {
        OnProgressAdvanced?.Invoke(progress);
    }
    /// <summary>
    /// Forwards simulation end event from the TimeMachine to subscribers.
    /// </summary>
    /// <param name="ended">True if the simulation completed, false if it was interrupted.</param>
    private void OnSimulationEnd(bool ended)
    {
        OnSimulationEnded?.Invoke(ended);
    }
    /// <summary>
    /// Sets the simulation time (legacy TimeMachine path).
    /// </summary>
    /// <param name="WaitTime">The wait time for the simulation.</param>
    public void SetSimulationTime(TimeSpan WaitTime)
    {
        _timeMachine.Start(WaitTime);
    }
    /// <summary>
    /// Pre-scans every <c>.vhd</c> / <c>.vhdl</c> file under the project's
    /// <c>BasePath</c> for <c>use library.package.all</c> clauses, validates
    /// that every referenced library is known to the resolver (IEEE bundles
    /// or a user-imported plugin), and asks the LibraryCompiler to compile
    /// the union. Symbols from each compiled library land in
    /// <see cref="FunctionRegistry"/> before the parser sees them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Missing libraries throw a build exception — we deliberately don't
    /// degrade silently because we can't guess what the missing library
    /// would have contributed. The user has to import the plugin (Settings
    /// → Libraries) before the build can complete.
    /// </para>
    /// <para>
    /// No-op when <see cref="_libraryCompiler"/> is null (legacy / test
    /// contexts that constructed <see cref="ProgramBuilder"/> directly).
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown when a <c>use</c> clause references a library the resolver
    /// doesn't know — the build halts at that point.
    /// </exception>
    private void EnsureLibrariesCompiledForBuild(string buildingFilePath, string? basePath)
    {
        if (_libraryCompiler is null) return;

        // Collect every (library, package) pair we'll need to resolve.
        var requestedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaredLibs      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in EnumerateProjectVhdlFiles(buildingFilePath, basePath))
        {
            string source;
            try { source = File.ReadAllText(path); }
            catch { continue; }   // unreadable sibling — let the parser surface it later

            foreach (Match m in UseClauseRe.Matches(source))
            {
                var lib = m.Groups[1].Value;
                var pkg = m.Groups[2].Value;

                // STD is intrinsic — the parser handles std.standard / std.textio
                // without LibraryCompiler help.
                if (string.Equals(lib, "STD", StringComparison.OrdinalIgnoreCase)) continue;

                // WORK = current project. Sibling files are parsed directly,
                // so there's nothing to compile through LibraryCompiler.
                if (string.Equals(lib, "WORK", StringComparison.OrdinalIgnoreCase)) continue;

                if (string.Equals(lib, "IEEE", StringComparison.OrdinalIgnoreCase))
                {
                    // For IEEE.<pkg>.all the resolver entry is keyed on the
                    // package name (std_logic_1164 / numeric_std / …).
                    requestedPackages.Add(pkg);
                }
                else
                {
                    // Single-segment user library naming: the user's plugin
                    // file-stem becomes both the library and the package
                    // entry the resolver knows. Track the original library
                    // name so the diagnostic mentions what the user wrote.
                    declaredLibs.Add(lib);
                    requestedPackages.Add(lib);
                }
            }
        }

        if (requestedPackages.Count == 0) return;

        // Validate every requested package against the resolver before
        // dispatching the build. Anything unknown halts the build with a
        // user-targeted message — at that point we genuinely don't know
        // what symbols the library would expose.
        var resolver = _libraryCompiler.Resolver;
        var unknown  = requestedPackages
            .Where(name => resolver.Resolve(name) is null)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknown.Count > 0)
        {
            var first = unknown[0];
            // Phrase the message with the library segment when the user
            // wrote a non-IEEE clause; that's what they'll search for in
            // the editor when they go fix the import.
            var blame = declaredLibs.Contains(first) ? first : first;
            throw new Exception(
                $"Library '{blame}' not found. Import it under " +
                $"Settings → Libraries before building, or remove the " +
                $"corresponding `use {blame}.…` clause.");
        }

        var compiled = _libraryCompiler
            .CompileAsync(requestedPackages, BuildExecutionMode.Linear)
            .GetAwaiter().GetResult();

        foreach (var lib in compiled)
        {
            foreach (var sym in lib.Functions)
            {
                if (sym.Kind != LibrarySymbolKind.Function && sym.Kind != LibrarySymbolKind.Procedure)
                    continue;
                FunctionRegistry.Instance.Register(new FunctionDefinition
                {
                    Name        = sym.Name,
                    PackageName = lib.LibraryName,
                    IsPure      = sym.Kind == LibrarySymbolKind.Function
                });
            }
        }
    }

    /// <summary>
    /// Lists every VHDL source file under <paramref name="basePath"/> (recursive)
    /// or, when <paramref name="basePath"/> is null/missing, falls back to
    /// just the file currently being built. Returns absolute paths.
    /// </summary>
    private static IEnumerable<string> EnumerateProjectVhdlFiles(string buildingFilePath, string? basePath)
    {
        if (!string.IsNullOrEmpty(basePath) && Directory.Exists(basePath))
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories)
                                 .Where(p =>
                                     p.EndsWith(".vhd",  StringComparison.OrdinalIgnoreCase) ||
                                     p.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                files = new[] { buildingFilePath };
            }
            foreach (var f in files) yield return f;
        }
        else
        {
            yield return buildingFilePath;
        }
    }

    /// <summary>
    /// Releases the resources used by the <see cref="ProgramBuilder"/> class.
    /// Unsubscribes from all events and disposes managed resources.
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from TimeMachine events to prevent memory leaks
        _timeMachine.OnSimulationEnded -= OnSimulationEnd;
        _timeMachine.OnProgressAdvance -= OnProgressAdvance;

        // Unsubscribe from ProgressReporter events to prevent memory leaks
        ProgressReporter.Instance.ProgressChanged -= OnInterpreterProgressChanged;
        ProgressReporter.Instance.ErrorOccurred -= OnInterpreterError;
        ProgressReporter.Instance.Completed -= OnInterpreterCompleted;
        ProgressReporter.Instance.CancellationRequested -= OnInterpreterCancellationRequested;

        _simulationRunner?.Dispose();
        _cts.Cancel();
        _cts?.Dispose();
    }
}