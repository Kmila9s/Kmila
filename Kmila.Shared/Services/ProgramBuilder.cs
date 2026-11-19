using Debugger;
using Interpreter.Models;
using Interpreter.Services;
using Kmila.Frontend;
using Kmila.Shared.Models;
using LibraryCompiler.Repositories;
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
    /// The engine aborted a running simulation (message from the runner).
    /// Raised on the simulator thread, before <see cref="OnSimulationEnded"/>(false).
    /// </summary>
    public event Action<string>? OnSimulationError;

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
    /// The design produced by the most recent build: entities, diagnostics
    /// (mutable, so the editor can append build errors) and the parsed text.
    /// Null until the first build.
    /// </summary>
    public DesignResult? Design { get; private set; }
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
    /// <summary>Error that aborted the last simulation run, or null when it completed or was stopped.</summary>
    public string? LastRunError => _simulationRunner?.Error;
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

    // Long-run Phase 2: per-run on-disk trace (null on hosts without a trace store,
    // e.g. WASM — those keep the Phase-1 rolling windows).
    private readonly Services.Trace.TraceStorage? _traceStorage;

    /// <summary>
    /// Trace of the most recent run (long-run Phase 2). After the run, when the
    /// in-memory history cap evicted samples, <see cref="Services.Trace.TraceSession.IsComplete"/>
    /// is true and <see cref="Services.Trace.TraceSession.AttachArchives"/> gives the
    /// post-run waveform its full on-disk history. Disposed (file deleted) when the
    /// next run starts.
    /// </summary>
    public Services.Trace.TraceSession? LastTrace { get; private set; }

    // Long-run safety net (null = no watchdog, e.g. test harnesses using new()).
    private readonly RunWatchdog? _watchdog;

    /// <summary>
    /// Why the most recent run was stopped by the <see cref="RunWatchdog"/>;
    /// <see cref="RunStopReason.None"/> when it ended normally or by the user.
    /// </summary>
    public RunStopReason LastRunStopReason { get; private set; }

    /// <summary>
    /// Raised (on a timer thread) when the <see cref="RunWatchdog"/> stops a run to
    /// protect disk or memory, just before the run is stopped. The usual
    /// <see cref="OnSimulationEnded"/>(false) / <see cref="OnSimulationCompleted"/>
    /// follow, so the partial waveform still renders.
    /// </summary>
    public event Action<RunStopReason>? OnRunWatchdogStopped;

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
    /// <param name="traceStorage">
    /// Long-run Phase 2 trace store (optional). When present, history the in-memory
    /// cap evicts is spilled to a per-run file instead of being dropped.
    /// </param>
    /// <param name="watchdog">
    /// Long-run safety net (optional). When present, every run is watched and stopped
    /// gracefully before it exhausts trace storage or memory.
    /// </param>
    public ProgramBuilder(LibraryCompiler.Services.LibraryCompiler? libraryCompiler = null,
                          Services.Trace.TraceStorage? traceStorage = null,
                          RunWatchdog? watchdog = null)
    {
        _libraryCompiler = libraryCompiler;
        _traceStorage = traceStorage;
        _watchdog = watchdog;

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
            string filePath = Repositories.ProjectFiles.GetDiskPath(file.Project.BasePath, file);
            var language = HdlLanguages.FromPath(filePath)
                ?? throw new InvalidOperationException($"File not valid: {Path.GetFileName(filePath)}");
            var frontend = Frontends.For(language);

            // Check for cancellation before starting parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            var basePath = file.Project?.BasePath;
            var sources = !string.IsNullOrEmpty(basePath) && Directory.Exists(basePath)
                ? frontend.DiscoverSources(basePath)
                : new[] { filePath };

            // Resolve the libraries the whole project imports before parsing,
            // failing loudly on an unknown/undeclared library (continuing would
            // only produce confusing "unknown function" diagnostics).
            var compiled = await frontend.PrepareLibrariesAsync(_libraryCompiler, sources, ct).ConfigureAwait(false);
            if (compiled != null) CompiledLibraries = compiled;

            // Clean engine state, scan every project source (cross-file
            // entities, package subtypes, declared names), then parse the file
            // being built.
            Design = frontend.LoadDesign(new DesignRequest
            {
                Files = sources.Contains(filePath) ? sources : sources.Append(filePath).ToList(),
                ParseTarget = filePath,
            });
            Design.ThrowIfFailed();

            // Check for cancellation after parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            if (Design.Entities.Count == 0)
                throw new InvalidOperationException("File not valid");

            // Aggregate hierarchical resource usage so a structural top (logic
            // delegated to port-mapped children) reports the whole subtree's
            // FF/LUT/DSP/BRAM, not just its own I/O. Best-effort — never fail a
            // build over a resource estimate.
            try { ApplyHierarchicalResources(sources); } catch { /* estimate is non-fatal */ }

            // ApplyHierarchicalResources resets the entity registry to rebuild
            // port-map edges; re-register every project entity declaration so
            // the schematic synthesizer can resolve components defined in other
            // files (ScanFile is additive and keeps the edges intact).
            try { frontend.ScanSources(sources); } catch { /* resolution is best-effort */ }

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
    /// The build parses only the one built file, so children defined in sibling
    /// files carry no self estimate yet. <see cref="HierarchyLoader"/> re-parses
    /// every project source (resetting the registry so port-map edges are fresh,
    /// not doubled) to build the name→entity map that
    /// <see cref="Resources.ComputeHierarchical"/> walks. Flat / single-entity
    /// designs get no edges and keep self-usage.
    /// </remarks>
    private void ApplyHierarchicalResources(IReadOnlyList<string> sources)
    {
        if (sources.Count == 0 || Design == null) return;

        var byName = HierarchyLoader.ParseDesign(sources, entryName: null, out _, out var specialize);

        // Set the aggregate only on entities that actually instantiate something;
        // leaf entities keep HierarchicalResourceUsage == null so consumers fall
        // back to their independent self-usage.
        foreach (var e in Design.Entities)
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
        var entity = Design?.Top;
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
        var runner = fastRun
            ? (ISimulationRunner)new FastSimulationRunner()
            : new SimulationRunner();
        _simulationRunner = runner;
        LastRunStopReason = RunStopReason.None;

        // Long-run Phase 2: the previous run's trace goes away with its run (any
        // waveform still holding its archive falls back to the in-memory window),
        // and this run gets a fresh one. Nothing touches disk unless the in-memory
        // history cap actually evicts.
        LastTrace?.Dispose();
        LastTrace = null;
        var trace = _traceStorage?.StartSession(LastTickResNs);
        LastTrace = trace;

        // Live-update timer: periodically pushes the running SignalHistory
        // out via OnSimulationProgressUpdate so the UI can grow the waveform
        // in real time. Created up here in the closure so OnSimulationEnded
        // can stop it before OnSimulationCompleted fires (keeps the final
        // frame from racing with a trailing tick).
        System.Threading.Timer? progressTimer = null;
        int liveTickInflight = 0;

        // Long-run safety net: stop gracefully (same path as the Stop button)
        // before the trace store or memory runs out. Disposed when the run ends.
        IDisposable? watch = _watchdog?.Watch(trace, reason =>
        {
            LastRunStopReason = reason;
            Console.WriteLine($"[Sim.Watchdog] stopping run: {reason} " +
                $"(heapMB={GC.GetTotalMemory(false) / (1024 * 1024)} traceBytes={trace?.BytesWritten ?? 0})");
            try { OnRunWatchdogStopped?.Invoke(reason); } catch { /* UI listener must not block the stop */ }
            runner.Stop();
        });

        // Subscribe to runner events and forward them. Important: we fire
        // OnSimulationCompleted whenever *any* signal history exists — even
        // on user-stopped runs (completed=false). That way the user sees a
        // partial waveform of whatever ran, instead of a blank Waveforms tab
        // after they hit Stop mid-simulation.
        _simulationRunner.OnProgressChanged += progress => OnProgressAdvanced?.Invoke(progress);
        _simulationRunner.OnEngineReady += engine =>
        {
            // Hook the spill sink before anything can be evicted.
            trace?.Attach(engine.History);
            OnEngineReady?.Invoke(engine);
        };
        _simulationRunner.OnSimulationEnded += completed =>
        {
            // Stop the live-update timer first so its callback can't race
            // with the final OnSimulationCompleted fire below.
            progressTimer?.Dispose();
            progressTimer = null;
            watch?.Dispose();
            watch = null;

            // Long-run Phase 2: if the history cap evicted anything, spill the
            // in-memory tail too and seal the trace so OnSimulationCompleted
            // consumers can attach the COMPLETE on-disk history. Blocking here (sim
            // thread, run already over) just drains the background writer.
            if (trace != null)
            {
                try { trace.CompleteAsync().GetAwaiter().GetResult(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Trace] complete failed: {ex.Message}"); }
            }

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
        _simulationRunner.OnError += msg => OnSimulationError?.Invoke(msg);
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
            watch?.Dispose();
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

        var frontend = Frontends.ForPath(tbPath);
        var sources = frontend.DiscoverSources(projectBasePath);

        // Resolve library `use` clauses across the project (same as a build).
        await frontend.PrepareLibrariesAsync(_libraryCompiler, sources, _cts.Token).ConfigureAwait(false);

        // Clean state, scan every project source, parse the chosen testbench to
        // discover its entity name (so we don't rely on the file name).
        var probe = frontend.LoadDesign(new DesignRequest { Files = sources, ParseTarget = tbPath });
        probe.ThrowIfFailed();
        var tbName = (probe.Top ?? probe.Entities.FirstOrDefault())?.Name
            ?? throw new InvalidOperationException("The selected testbench has no entity with an architecture.");

        // Elaborate: inline the DUT instantiations into one flat design (in
        // memory — nothing is written to the user's project tree). A null
        // result is normal for a single-file testbench (nothing to inline).
        var unit = probe.Unit!;
        try
        {
            var flat = frontend.ElaborateSource(sources, tbName);
            if (!string.IsNullOrEmpty(flat)) unit = frontend.ParseSource(flat);
        }
        catch (Exception elabEx)
        {
            // Elaboration THREW — don't let it masquerade as a zero waveform.
            // Fall back to the raw source so a single-file design runs, but
            // surface the failure so a multi-file design that can't resolve its
            // DUT is diagnosable instead of silently producing all zeros.
            Console.Error.WriteLine($"[Sim.TB] elaboration failed for '{tbName}': {elabEx.GetType().Name}: {elabEx.Message}");
        }
        unit.ThrowIfFailed();
        if (unit.Entities.Count == 0)
            throw new InvalidOperationException("Testbench produced no entities.");
        var entity = frontend.PickTop(unit.Entities, tbName)
            ?? throw new InvalidOperationException("No testbench entity with architecture found.");

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
        LastTrace?.Dispose(); // deletes the last run's trace file
        LastTrace = null;
        _cts.Cancel();
        _cts?.Dispose();
        _timeMachine.Dispose();
        GC.SuppressFinalize(this);
    }
}