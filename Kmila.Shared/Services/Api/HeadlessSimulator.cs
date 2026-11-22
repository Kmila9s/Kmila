using System.Diagnostics;
using Kmila.Frontend;
using Interpreter.Services;
using Kmila.Shared.Models;
using TimeMachine.Models;
using PModels = Parser.Models;
using IModels = Interpreter.Models;

namespace Kmila.Shared.Services.Api;

/// <summary>
/// Drives the simulator from a sandbox project without going through the
/// UI-coupled ProgramBuilder. Loads the design through the HDL front-end,
/// builds a SimulationConfig via StimulusBuilder, runs SimulationRunner, and
/// returns the VCD text. No events, no progress UI, no toast — strictly request →
/// response.
/// </summary>
public sealed class HeadlessSimulator
{
    private readonly SandboxProjectStore _store;

    /// <summary>Creates the headless simulator over the given sandbox project store.</summary>
    /// <param name="store">Store that resolves sandbox projects by id.</param>
    public HeadlessSimulator(SandboxProjectStore store) { _store = store; }

    /// <summary>Parses, elaborates, and simulates the requested sandbox project, returning the VCD result.</summary>
    /// <param name="request">The simulation request (project id, files, entry point, stimulus, and timing).</param>
    /// <returns>The simulation response including the VCD text, diagnostics, and run metadata.</returns>
    public async Task<SimulateResponse> RunAsync(SimulateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectId))
            throw new ArgumentException("projectId required.");

        var project = _store.Get(request.ProjectId)
            ?? throw new InvalidOperationException($"Unknown project '{request.ProjectId}'.");

        // Per-request inline file writes — lets callers create + simulate in
        // one round-trip instead of POSTing every file first.
        if (request.Files != null)
        {
            foreach (var f in request.Files)
                project.WriteFile(f.Name, f.Content);
        }

        int? wallMs = int.TryParse(Environment.GetEnvironmentVariable("KMILA_SIM_WALL_TIMEOUT_MS"), out int ms) && ms > 0
            ? ms : null;
        HdlLanguage? language = null;
        if (!string.IsNullOrWhiteSpace(request.Language))
            language = HdlLanguages.FromId(request.Language)
                ?? throw new NotSupportedException($"Unsupported HDL language '{request.Language}'.");

        var run = await RunProjectAsync(project.Path, new HeadlessRunOptions
        {
            Language       = language,
            EntryPoint     = request.EntryPoint,
            Files          = request.Files?.Select(f => f.Name).ToList(),
            TotalNs        = TotalNsFromRequest(request),
            TickResNs      = request.TickResNs,
            SystemClockMhz = request.SystemClockMhz,
            PortMoments    = MaterializePortMoments(request.PortMoments),
            WallTimeoutMs  = wallMs,
            Stimulus       = StimulusOptionsOf(request),
        });

        string vcd = run.History.ExportToVcd(run.Entity.Name, timescaleNs: run.TickResNs);

        return new SimulateResponse
        {
            RunId       = Guid.NewGuid().ToString("N").Substring(0, 12),
            Entity      = run.Entity.Name,
            DurationMs  = run.DurationMs,
            TotalTicks  = run.TotalTicks,
            TickResNs   = run.TickResNs,
            SignalCount = run.History.SignalCount,
            Vcd         = vcd,
            Log         = run.Log,
            Error       = run.EngineError,
        };
    }

    /// <summary>The request's clocks, port initial values and initial-value policy.</summary>
    private static StimulusOptions StimulusOptionsOf(SimulateRequest r) => StimulusOptions.Declared(
        StimulusOptions.ParsePolicy(r.InitialValues),
        r.Clocks?.Select(c => new ClockSpec(c.Port, c.PeriodNs, c.InitialHigh)).ToList(),
        r.PortInitials?.Select(p => KeyValuePair.Create(p.Port, p.Value)).ToList());

    /// <summary>
    /// The headless pipeline shared by the HTTP API and the CLI: load the
    /// design through the HDL front-end (scan → elaborate → parse → top),
    /// build the stimulus config, run the engine, and return the live
    /// <see cref="SignalHistory"/>.
    /// </summary>
    /// <param name="projectPath">Directory holding the design sources.</param>
    /// <param name="options">Entry point, timing and stimulus.</param>
    /// <param name="onProgress">Optional progress sink (0..100).</param>
    public static async Task<HeadlessRunResult> RunProjectAsync(
        string projectPath, HeadlessRunOptions options, Action<double>? onProgress = null)
    {
        var log = new List<string>();
        var sw = Stopwatch.StartNew();
        int tickResNs = options.TickResNs > 0 ? options.TickResNs : TimeClock.NS_PER_TICK;
        int systemClockMhz = options.SystemClockMhz > 0 ? options.SystemClockMhz : 50;

        // F-G-08 pass 3 diag (2026-07-16): phase-boundary stderr trace so we
        // can see where a long-running sim is stuck. Env-var gated so it's
        // silent by default.
        bool phaseTrace = Environment.GetEnvironmentVariable("KMILA_SIM_PHASE_TRACE") == "1";
        void Phase(string tag)
        {
            if (phaseTrace) Console.Error.WriteLine($"[kmila-phase t={sw.ElapsedMilliseconds}ms] {tag}");
        }

        // F-G-08 pass 4 (2026-07-17): the wall-timeout CTS covers load +
        // stimulus + simulation, not just the simulation loop.
        using var requestCts = new CancellationTokenSource();
        if (options.WallTimeoutMs is int wallMs && wallMs > 0)
            requestCts.CancelAfter(TimeSpan.FromMilliseconds(wallMs));
        var ct = requestCts.Token;

        Phase("load-begin");
        // std.textio files (`file f : text open read_mode is "data.txt"`) open from the project, as GHDL from its working directory.
        Interpreter.Models.TextioFiles.BaseDirectory = projectPath;
        var sources = SourceOrder.Sort(projectPath,
            Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories).Where(HdlLanguages.IsHdlSource),
            options.Files);
        var frontend = options.Language != null ? Frontends.For(options.Language) : Frontends.ForSources(sources);
        var design = frontend.LoadDesign(new DesignRequest
        {
            Root      = projectPath,
            Files     = sources,
            Entry     = options.EntryPoint,
            EntryIsExplicit = options.EntryIsExplicit,
            // Inline sub-component instances into one flat design when an
            // entry point is named: child architectures are not executed at
            // run time, so a modular design would otherwise produce no output.
            Elaborate = !string.IsNullOrEmpty(options.EntryPoint),
        }, ct);
        log.AddRange(design.Log);
        Phase("load-end");
        design.ThrowIfFailed();

        if (design.Entities.Count == 0)
            throw new InvalidOperationException("Parser produced no entities.");
        var entity = design.Top
            ?? throw new InvalidOperationException("No entity with architecture found.");

        Phase("stimulus-begin");
        var config = StimulusBuilder.BuildConfigNs(
            entity,
            options.PortMoments is { Count: > 0 } ? options.PortMoments : null,
            totalNs: options.TotalNs,
            tickResNs: tickResNs,
            systemClockMhz: systemClockMhz,
            options: options.Stimulus);
        Phase("stimulus-end");
        if (config.TickResNs != tickResNs)
            log.Add($"[sim] tick adjusted to {config.TickResNs} ns (static delays of the design)");

        log.Add($"[sim] entity={entity.Name} ports={entity.Ports.Count} totalTicks={config.TotalTicks} stimuli={config.Stimuli.Count} clocks={config.Clocks.Count}");

        using var runner = new SimulationRunner();
        // Surface runtime errors and VHDL `report` output into the log —
        // otherwise a failing run looks like an empty successful VCD.
        runner.OnError += msg => log.Add($"[engine] {msg}");
        runner.OnReport += msg => log.Add($"[report] {msg}");
        if (onProgress != null) runner.OnProgressChanged += p => onProgress(p);
        using var cancelReg = ct.Register(runner.Stop);
        Phase("runner-begin");
        await runner.RunAsync(config);
        Phase("runner-end");

        var history = runner.Engine?.History
            ?? throw new InvalidOperationException("Simulation produced no SignalHistory.");

        // Delta-loop counters: tells a stalled run apart from a fast one.
        if (runner.Engine != null)
            log.Add($"[diag] {runner.Engine.GetDiagnosticSummary()}");
        // F-G-08 pass 5 (2026-07-22): parse-side hot-path counters (env-gated).
        if (Parser.Services.ParseProfile.Enabled)
        {
            var profileLine = Parser.Services.ParseProfile.Summarize();
            log.Add($"[diag] {profileLine}");
            if (phaseTrace) Console.Error.WriteLine(profileLine);
        }

        sw.Stop();
        return new HeadlessRunResult
        {
            Entity     = entity,
            History    = history,
            TickResNs  = config.TickResNs,
            Config     = config,
            DurationMs = sw.ElapsedMilliseconds,
            Log        = log,
            EngineError = runner.Error,
        };
    }

    private static long TotalNsFromRequest(SimulateRequest r)
    {
        // Integer nanoseconds — avoids TimeSpan's 100ns floor that silently
        // truncates sub-100ns sims (e.g. a 20ns bench case rounded to 0).
        var unit = r.TimeScale?.ToUpperInvariant() ?? "NS";
        return unit switch
        {
            "NS" => r.Duration,
            "US" => (long)r.Duration * 1_000L,
            "MS" => (long)r.Duration * 1_000_000L,
            "S"  => (long)r.Duration * 1_000_000_000L,
            _    => (long)r.Duration * 1_000L,
        };
    }

    private static List<PortMomentConfiguration>? MaterializePortMoments(List<ApiPortMoment> raw)
    {
        if (raw == null || raw.Count == 0) return null;

        // Group flat (port, t, value) tuples into the nested shape the
        // StimulusBuilder expects.
        var grouped = raw
            .Where(m => !string.IsNullOrEmpty(m.Port))
            .GroupBy(m => m.Port, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PortMomentConfiguration
            {
                PortName = g.Key,
                Values = g.OrderBy(v => v.Time)
                          .Select(v => new PortMomentValue
                          {
                              Time      = v.Time,
                              TimeScale = string.IsNullOrEmpty(v.Scale) ? "NS" : v.Scale.ToUpperInvariant(),
                              Value     = v.Value ?? string.Empty,
                          })
                          .ToList()
            })
            .ToList();

        return grouped.Count > 0 ? grouped : null;
    }
}

/// <summary>Inputs of <see cref="HeadlessSimulator.RunProjectAsync"/>.</summary>
public sealed class HeadlessRunOptions
{
    /// <summary>HDL language to load; null infers it from the project's sources.</summary>
    public HdlLanguage? Language { get; init; }
    /// <summary>Top unit (entity or configuration); when set, the design is elaborated (sub-components inlined).</summary>
    public string? EntryPoint { get; init; }
    /// <summary>False when <see cref="EntryPoint"/> was inferred (e.g. from a file name): a missing unit is then a warning, not an error.</summary>
    public bool EntryIsExplicit { get; init; } = true;
    /// <summary>Source names (relative to the project) in analysis order; the project's other sources follow sorted by path.</summary>
    public IReadOnlyList<string>? Files { get; init; }
    /// <summary>Simulated duration in nanoseconds.</summary>
    public long TotalNs { get; init; }
    /// <summary>Nanoseconds per engine tick; 0 or less uses <see cref="TimeClock.NS_PER_TICK"/>.</summary>
    public int TickResNs { get; init; }
    /// <summary>System clock frequency (MHz) driving clock ports; 0 or less uses 50.</summary>
    public int SystemClockMhz { get; init; }
    /// <summary>Port stimulus; null or empty for none.</summary>
    public List<PortMomentConfiguration>? PortMoments { get; init; }
    /// <summary>Clocks, port initial values and initial-value policy; null proposes the clock by name under LRM.</summary>
    public StimulusOptions? Stimulus { get; init; }
    /// <summary>Wall-clock budget for load + simulation, or null for none.</summary>
    public int? WallTimeoutMs { get; init; }
}

/// <summary>Output of <see cref="HeadlessSimulator.RunProjectAsync"/>.</summary>
public sealed class HeadlessRunResult
{
    public required Entity Entity { get; init; }
    public required SignalHistory History { get; init; }
    public int TickResNs { get; init; }
    /// <summary>The stimulus configuration the engine executed (clocks, timed inputs, initial values).</summary>
    public required IModels.SimulationConfig Config { get; init; }
    public ulong TotalTicks => Config.TotalTicks;
    public long DurationMs { get; init; }
    public List<string> Log { get; init; } = new();
    /// <summary>
    /// First error that aborted the engine mid-run, or null when the run
    /// completed. The history then holds only what was recorded before it.
    /// </summary>
    public string? EngineError { get; init; }
}
