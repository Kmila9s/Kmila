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
    /// Phase B (2026-05-19): forwarded from
    /// <see cref="Interpreter.Services.SimulationRunner.OnEngineReady"/> so
    /// scoped consumers (Editor's Replay tab) can attach to the live engine
    /// without taking a hard reference to the runner. Fires once per run,
    /// right after the engine is initialised and before the first tick.
    /// </summary>
    public event Action<Interpreter.Services.DeltaCycleEngine>? OnEngineReady;
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
    /// Fires for each VHDL <c>report</c> statement executed during a run.
    /// Workbench (testbench) mode surfaces these in the Output panel so the
    /// user can read a testbench's self-check messages; the port-moment path
    /// simply has no subscriber. Forwarded from
    /// <see cref="Interpreter.Services.ISimulationRunner.OnReport"/>.
    /// </summary>
    public event Action<string>? OnReport;
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
    /// The libraries compiled for the most recent build/run (packages and the
    /// functions/types they publish). Surfaced so the Editor's entity-scope
    /// viewer can list what the imported libraries expose. Empty until a build
    /// that resolves at least one `use` clause runs.
    /// </summary>
    public IReadOnlyList<CompiledLibrary> CompiledLibraries { get; private set; } = System.Array.Empty<CompiledLibrary>();
    /// <summary>
    /// Gets whether a build is currently in progress.
    /// </summary>
    public bool IsBuilding { get; private set; }
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
    // Active run driver: SimulationRunner (Visual mode) or FastSimulationRunner
    // (Fast mode). Both implement ISimulationRunner and produce identical
    // waveforms — see RunSimulationAsync's fastRun branch.
    private ISimulationRunner? _simulationRunner;
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
    /// Matches a VHDL <c>library X;</c> (or <c>library X, Y, Z;</c>)
    /// declaration. Captures the comma-separated list as group 1; the
    /// caller splits on commas. <c>STD</c> and <c>WORK</c> are implicit
    /// in VHDL and don't need a declaration.
    /// </summary>
    private static readonly Regex LibraryDeclRe = new(
        @"(?im)^\s*library\s+([a-z_][a-z0-9_]*(?:\s*,\s*[a-z_][a-z0-9_]*)*)\s*;",
        RegexOptions.Compiled);

    /// <summary>Strip line comments before pre-scanning; matches the LibraryBuilder rule.</summary>
    private static readonly Regex LineCommentRe = new(
        @"--[^\r\n]*",
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
    /// <param name="ct">Token to cooperatively cancel the build.</param>
    /// <returns>True if the build was successful, otherwise false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the build is cancelled by the user.</exception>
    /// <remarks>
    /// Async because the library-compile step awaits real I/O
    /// (FileSystemBlobStore reads/writes). Calling it via
    /// <c>.GetAwaiter().GetResult()</c> from the Blazor UI thread deadlocks
    /// when the inner awaits' continuations try to resume on the captured
    /// sync context — that produced a silent first-run freeze that only
    /// cleared on Blazor Server after F5 (singleton cache hit) and never
    /// cleared on MAUI BlazorWebView.
    /// </remarks>
    public async Task<bool> BuildAsync(ProjectFile file, CancellationToken ct = default)
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
                throw new InvalidOperationException($"File not valid: {Path.GetFileName(filePath)}");

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
            await EnsureLibrariesCompiledForBuildAsync(filePath, file.Project?.BasePath, ct)
                .ConfigureAwait(false);

            Parser = new(filePath);
            Parser.ParseFile(CancellationToken.None);

            // Check for cancellation after parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            if (Parser.Entities.Count == 0)
                throw new InvalidOperationException("File not valid");

            // Aggregate hierarchical resource usage so a structural top (logic
            // delegated to port-mapped children) reports the whole subtree's
            // FF/LUT/DSP/BRAM, not just its own I/O. Best-effort — never fail a
            // build over a resource estimate.
            try { ApplyHierarchicalResources(file); } catch { /* estimate is non-fatal */ }

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
    /// Populates <see cref="Entity.HierarchicalResourceUsage"/> on each just-parsed
    /// top-file entity that instantiates sub-components, so the Entities view and
    /// the utilization gauge show whole-subtree usage instead of I/O only.
    /// </summary>
    /// <remarks>
    /// <see cref="Debugger.Repositories.Parser.ParseFile()"/> only parses the one
    /// built file, so children defined in sibling files carry no self estimate yet.
    /// We re-parse every project <c>.vhd</c> through <see cref="HierarchyLoader"/>
    /// (which resets the registry so port-map edges are fresh, not doubled) to
    /// build the name→entity map that <see cref="Resources.ComputeHierarchical"/>
    /// walks. Flat / single-entity designs get no edges and keep self-usage.
    /// </remarks>
    private void ApplyHierarchicalResources(ProjectFile file)
    {
        var basePath = file.Project?.BasePath;
        if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
            return;

        var files = new List<string>();
        foreach (var f in Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(f);
            if (!ext.Equals(".vhd", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".vhdl", StringComparison.OrdinalIgnoreCase))
                continue;
            if (Path.GetFileName(f).StartsWith("_elaborated_", StringComparison.OrdinalIgnoreCase))
                continue;
            files.Add(f);
        }
        if (files.Count == 0) return;

        var byName = HierarchyLoader.ParseDesign(files, entryName: null, out _, out var specialize);

        // Set the aggregate only on entities that actually instantiate something;
        // leaf entities keep HierarchicalResourceUsage == null so consumers fall
        // back to their independent self-usage.
        foreach (var e in Parser.Entities)
        {
            if (e?.Name == null) continue;
            if (EntityRegistry.Instance.GetComponentInstances(e.Name).Count == 0)
                continue;
            e.HierarchicalResourceUsage = Resources.ComputeHierarchical(e, byName, specialize);
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
    /// <param name="tickResNs">Tick resolution in nanoseconds for the simulation clock.</param>
    /// <param name="fastRun">When true, runs without per-tick throttling for maximum speed.</param>
    public async Task RunSimulationAsync(
        List<PortMomentConfiguration>? portMoments,
        TimeSpan duration,
        int systemClockMhz = 50,
        int tickResNs = TimeMachine.Models.TimeClock.NS_PER_TICK,
        bool fastRun = false)
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
                    if (p is null) continue;
                    Console.WriteLine($"[Sim.Port] name={p.Name ?? "?"} dir={p.PortType} size={p.Size}");
                }
            }
        }
        catch (Exception diagEx)
        {
            Console.WriteLine($"[Sim.Diag] failed: {diagEx.Message}");
        }

        await RunConfigAsync(config!, fastRun);
    }

    /// <summary>
    /// Shared runner execution used by both the port-moment path
    /// (<see cref="RunSimulationAsync"/>) and the Workbench testbench path
    /// (<see cref="RunTestbenchAsync"/>): creates the runner (Fast/Visual),
    /// wires progress / engine-ready / report / error / completion events,
    /// runs the live-waveform timer, executes <paramref name="config"/>, and
    /// emits perf diagnostics. Extracted verbatim so both entry points share
    /// identical execution + eventing — the simulation behaviour is unchanged.
    /// </summary>
    private async Task RunConfigAsync(SimulationConfig config, bool fastRun)
    {
        // Dispose previous runner if any. Fast mode swaps in the event-driven
        // FastSimulationRunner (skips idle ticks); Visual mode keeps the
        // reference SimulationRunner (live-updating tick-by-tick).
        _simulationRunner?.Dispose();
        _simulationRunner = fastRun
            ? new FastSimulationRunner()
            : new SimulationRunner();

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
        _simulationRunner.OnEngineReady += engine => OnEngineReady?.Invoke(engine);
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
        // Surface VHDL `report` output (testbench self-checks) to subscribers.
        _simulationRunner.OnReport += msg => OnReport?.Invoke(msg);

        // Start the live-update timer just before the run kicks off. The
        // Interlocked guard drops timer fires that arrive while a previous
        // merge is still inflight on the consumer — keeps slow listeners
        // from queueing redundant work.
        //
        // Fast mode SKIPS this timer entirely: no per-200ms history merge / SVG
        // re-render (the "reduced visual effects" half of Fast mode). The cheap
        // OnProgressChanged→OnProgressAdvanced percent above still fires for a
        // progress bar, and the full waveform is built once at the end from
        // OnSimulationCompleted → Editor.OnSimulationDataReady.
        if (!fastRun)
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

        var perfSw = System.Diagnostics.Stopwatch.StartNew();
        int gen2Start = GC.CollectionCount(2);
        try
        {
            // Run on current async context (caller should be on background/UI thread)
            await _simulationRunner.RunAsync(config);
        }
        finally
        {
            // Perf diagnostic (surfaces in /logs). For a FAST run, visitedTicks
            // vs totalTicks shows how much idle time was skipped: a ratio near
            // 100% means the design has activity almost every tick (e.g. a clock
            // at the tick resolution — a 50 MHz clock toggles every 10 ns tick),
            // so there is nothing to skip and Fast's only gain is dropping the
            // live-UI overhead. A small ratio means Fast is paying off.
            perfSw.Stop();
            var visited = _simulationRunner is Interpreter.Services.FastSimulationRunner fsr
                ? fsr.VisitedTicks : (int?)null;
            var pct = visited.HasValue && config != null && config.TotalTicks > 0
                ? $" visitedTicks={visited} ({100.0 * visited.Value / config.TotalTicks:F1}% of ticks ran)"
                : string.Empty;
            // Memory signal: a long run that balloons the (uncapped) SignalHistory
            // triggers gen2 GCs — and, under memory pressure, a MacCatalyst WebView
            // reload — which reads as the UI "fully refreshing / crashing" every few
            // seconds. samples = total recorded transitions (the thing that grows).
            var samples = _simulationRunner?.Engine?.History?.TotalSamples ?? 0;
            var heapMb = GC.GetTotalMemory(false) / (1024 * 1024);
            var gen2 = GC.CollectionCount(2) - gen2Start;
            Console.WriteLine($"[Sim.Perf] mode={(fastRun ? "FAST" : "VISUAL")} " +
                $"elapsed={perfSw.ElapsedMilliseconds}ms totalTicks={config?.TotalTicks}{pct} " +
                $"samples={samples} heapMB={heapMb} gen2GCs={gen2}");

            // Belt-and-braces: if RunAsync threw before OnSimulationEnded
            // fired, the timer would otherwise leak.
            progressTimer?.Dispose();
        }
    }

    /// <summary>
    /// Runs a VHDL testbench (Workbench mode). Unlike the port-moment path,
    /// the testbench supplies its own clock/reset stimulus in VHDL, so no
    /// UI-authored port moments are used. Mirrors the proven headless bench
    /// path (<see cref="Api.HeadlessSimulator"/>): scan all project files,
    /// elaborate the testbench entry point into a flat single-entity source,
    /// parse it, and run it through the shared <see cref="RunConfigAsync"/>.
    /// The design's simulation behaviour is untouched — this only changes
    /// which entity is elaborated and that stimulus comes from VHDL.
    /// </summary>
    /// <param name="projectBasePath">Absolute project root (holds the .vhd files on disk).</param>
    /// <param name="testbenchRelativePath">Project-relative path of the chosen testbench file.</param>
    /// <param name="duration">Total simulation duration.</param>
    /// <param name="systemClockMhz">Effective system clock frequency (MHz) for this run.</param>
    /// <param name="tickResNs">Tick resolution in nanoseconds for the simulation clock.</param>
    /// <param name="fastRun">When true, runs without per-tick throttling for maximum speed.</param>
    public async Task RunTestbenchAsync(
        string projectBasePath,
        string testbenchRelativePath,
        TimeSpan duration,
        int systemClockMhz = 50,
        int tickResNs = TimeMachine.Models.TimeClock.NS_PER_TICK,
        bool fastRun = false)
    {
        if (string.IsNullOrWhiteSpace(projectBasePath) || !Directory.Exists(projectBasePath))
            throw new InvalidOperationException("Project directory not found for testbench run.");
        if (string.IsNullOrWhiteSpace(testbenchRelativePath))
            throw new InvalidOperationException("No testbench file selected.");

        var tbPath = Path.Combine(
            new[] { projectBasePath }
                .Concat(testbenchRelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .ToArray());
        if (!File.Exists(tbPath))
            throw new InvalidOperationException($"Testbench file not found: {testbenchRelativePath}");

        LastTickResNs = tickResNs > 0 ? tickResNs : TimeMachine.Models.TimeClock.NS_PER_TICK;

        // Cross-file hygiene — mirror the headless bench path so a previous
        // run's entities don't leak into this one.
        EntityRegistry.Instance.Clear();
        PackageRegistry.Instance.Clear();
        FunctionRegistry.Instance.Clear();
        global::Parser.Services.ParseProfile.Reset();

        foreach (var f in Directory.EnumerateFiles(projectBasePath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(f);
            if (!ext.Equals(".vhd", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".vhdl", StringComparison.OrdinalIgnoreCase))
                continue;
            if (Path.GetFileName(f).StartsWith("_elaborated_", StringComparison.OrdinalIgnoreCase))
                continue;
            try { EntityRegistry.Instance.ScanFile(f); } catch { /* keep scanning */ }
            try { PackageSubtypeScanner.ScanFile(f); } catch { /* keep scanning */ }
            // v1.17.0 (P3): record declared identifier names so the undeclared-
            // identifier check rejects only names that appear in no declaration
            // (never a local constant/function the scope-builder failed to
            // evaluate). Harmless here since this path fully elaborates scope.
            try { PackageRegistry.Instance.RegisterDeclaredNamesFromSource(File.ReadAllText(f)); } catch { /* keep scanning */ }
        }

        // Resolve library `use` clauses across the project (same as a build).
        await EnsureLibrariesCompiledForBuildAsync(tbPath, projectBasePath, _cts.Token)
            .ConfigureAwait(false);

        // Discover the testbench entity name from the chosen file so we don't
        // rely on the filename matching the entity.
        var probe = new Debugger.Repositories.Parser(tbPath);
        probe.ParseFile();
        var tbEntity = probe.Entities.FirstOrDefault(e => e.Architecture != null)
            ?? probe.Entities.FirstOrDefault();
        if (tbEntity == null)
            throw new InvalidOperationException("The selected testbench has no entity with an architecture.");
        var tbName = tbEntity.Name;

        // Elaborate: inline any DUT instantiations into a flat single-entity
        // source. Written to a TEMP file so the user's project tree stays
        // clean — the headless path writes into its sandbox, but the editor
        // must not add an _elaborated_ file to the visible project.
        string parsePath = tbPath;
        try
        {
            var flat = SourceElaborator.ElaborateOrNull(projectBasePath, tbName);
            if (!string.IsNullOrEmpty(flat))
            {
                var tmp = Path.Combine(Path.GetTempPath(), $"kmila_tb_{tbName}_{Guid.NewGuid():N}.vhd");
                await File.WriteAllTextAsync(tmp, flat).ConfigureAwait(false);
                parsePath = tmp;
            }
            // A null/empty flat result is normal for a genuine single-file
            // testbench (nothing to inline) — keep the raw source silently.
        }
        catch (Exception elabEx)
        {
            // Elaboration THREW — don't let it masquerade as a zero waveform.
            // We still fall back to the raw source so a single-file design runs,
            // but surface the failure so a multi-file design that can't resolve
            // its DUT is diagnosable instead of silently producing all zeros.
            Console.Error.WriteLine($"[Sim.TB] elaboration failed for '{tbName}': {elabEx.GetType().Name}: {elabEx.Message}");
        }

        Parser = new Debugger.Repositories.Parser(parsePath);
        Parser.ParseFile();
        if (Parser.Entities.Count == 0)
            throw new InvalidOperationException("Testbench produced no entities.");

        var entity = Parser.Entities.FirstOrDefault(e =>
                string.Equals(e.Name, tbName, StringComparison.OrdinalIgnoreCase) && e.Architecture != null)
            ?? Parser.Entities.FirstOrDefault(e => e.Architecture != null);
        if (entity == null)
            throw new InvalidOperationException("No testbench entity with architecture found.");

        // Testbench drives its own clock/reset in VHDL → no UI port moments.
        var totalNs = (long)Math.Round(duration.TotalSeconds * 1e9);
        var config = StimulusBuilder.BuildConfigNs(
            entity, portMoments: null,
            totalNs: totalNs,
            tickResNs: LastTickResNs,
            systemClockMhz: systemClockMhz);

        Console.WriteLine($"[Sim.TB] entity={entity.Name} totalTicks={config.TotalTicks} " +
            $"clocks={config.Clocks.Count} stimuli={config.Stimuli.Count}");

        await RunConfigAsync(config, fastRun);
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
    private async Task EnsureLibrariesCompiledForBuildAsync(string buildingFilePath, string? basePath, CancellationToken ct)
    {
        if (_libraryCompiler is null) return;

        // Collect every (library, package) pair we'll need to resolve.
        var requestedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaredLibs      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Library names the user actually declared via `library X;` —
        // we enforce that every non-trivial `use` clause has a matching
        // declaration. STD / WORK / IEEE are implicit in VHDL even
        // without explicit declarations, so we seed them here.
        var declaredViaLibraryClause = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "STD", "WORK", "IEEE"
        };
        // Track which library-clause referent appears in which file so
        // the diagnostic can point at the right offender when a `use`
        // is missing its `library` partner.
        var useClauseLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in EnumerateProjectVhdlFiles(buildingFilePath, basePath))
        {
            string source;
            try { source = File.ReadAllText(path); }
            catch { continue; }   // unreadable sibling — let the parser surface it later

            // Strip line comments so a "-- use foo.bar.all" inside docs
            // can't trip the package-validation logic below.
            source = LineCommentRe.Replace(source, string.Empty);

            // Pick up `library X, Y;` declarations.
            foreach (Match m in LibraryDeclRe.Matches(source))
            {
                foreach (var raw in m.Groups[1].Value.Split(','))
                {
                    var name = raw.Trim();
                    if (name.Length > 0) declaredViaLibraryClause.Add(name);
                }
            }

            foreach (Match m in UseClauseRe.Matches(source))
            {
                var lib = m.Groups[1].Value;
                var pkg = m.Groups[2].Value;
                useClauseLibraries.Add(lib);

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

        // Strict-mode check: every library mentioned in a `use` clause must
        // also have a matching `library X;` declaration in some project
        // file. Real toolchains require this; without the check a user can
        // accidentally `use my_lib.foo.all` while forgetting the
        // `library my_lib;` line and get cryptic diagnostics deep in the
        // parser.
        var missingLibraryDecl = useClauseLibraries
            .Where(l => !declaredViaLibraryClause.Contains(l))
            .OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingLibraryDecl.Count > 0)
        {
            var first = missingLibraryDecl[0];
            throw new InvalidOperationException(
                $"`use {first}.…` is missing its `library {first};` declaration. " +
                "Add the declaration before the `use` clause, or remove the " +
                "`use` clause if it isn't needed.");
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
            throw new InvalidOperationException(
                $"Library '{blame}' not found. Import it under " +
                $"Settings → Libraries before building, or remove the " +
                $"corresponding `use {blame}.…` clause.");
        }

        var compiled = await _libraryCompiler
            .CompileAsync(requestedPackages, BuildExecutionMode.Linear, ct)
            .ConfigureAwait(false);

        // Cache for the entity-scope viewer's "Libraries" scope.
        CompiledLibraries = compiled;

        // Wipe the type registry before re-seeding from this build's libraries.
        // Without this, a previous project's type names leak into the current
        // build's "did you mean" suggestions (polish-pass Q3, 2026-06-18).
        global::Parser.Services.UserTypeRegistry.Instance.Clear();

        foreach (var lib in compiled)
        {
            foreach (var sym in lib.Functions)
            {
                switch (sym.Kind)
                {
                    case LibrarySymbolKind.Function:
                    case LibrarySymbolKind.Procedure:
                        FunctionRegistry.Instance.Register(new FunctionDefinition
                        {
                            Name        = sym.Name,
                            PackageName = lib.LibraryName,
                            IsPure      = sym.Kind == LibrarySymbolKind.Function
                        });
                        break;
                    case LibrarySymbolKind.Type:
                        // Surface user-defined types to the parser's typo
                        // suggestion path so `coeficient_array_t` →
                        // `coefficient_array_t` when the library declared it.
                        global::Parser.Services.UserTypeRegistry.Instance.Register(sym.Name);
                        break;
                    // Constants don't participate in port-type suggestions.
                }
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
        _timeMachine.Dispose();
        GC.SuppressFinalize(this);
    }
}