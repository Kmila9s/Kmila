using Interpreter.Services;
using Kmila.Shared.Models;
using Kmila.Shared.Services;
using Kmila.Shared.Services.Api;

namespace Kmila.CLI.Simulation;

/// <summary>
/// What a simulation run produced. Unlike the shared <c>HeadlessSimulator</c>
/// (which returns only VCD text), this keeps the engine's
/// <see cref="SignalHistory"/> so the CLI can emit every output format and feed
/// the ASCII waveform viewer from the same data.
/// </summary>
public sealed class SimRunResult
{
    /// <summary>Name of the entity that was actually simulated.</summary>
    public required string Entity { get; init; }
    /// <summary>The engine's recorded signal history — the source for every output format.</summary>
    public required SignalHistory History { get; init; }
    /// <summary>The run's effective tick, in femtoseconds (what <see cref="TotalTicks"/> counts).</summary>
    public long TickFs { get; init; }
    /// <summary>Total number of simulated ticks.</summary>
    public ulong TotalTicks { get; init; }
    /// <summary>Wall-clock time the run took, in milliseconds.</summary>
    public long DurationMs { get; init; }
    /// <summary>Number of distinct signals captured.</summary>
    public int SignalCount { get; init; }
    /// <summary>Structured per-stage diagnostics (scan/elaborate/parse/sim/report lines).</summary>
    public List<string> Log { get; init; } = new();
    /// <summary>Raw chatter the engine wrote to <see cref="Console.Out"/> during the run
    /// (captured so it can never corrupt piped stdout output). Shown only with --verbose.</summary>
    public string EngineConsole { get; init; } = string.Empty;
    /// <summary>Error that aborted the engine mid-run (the history is partial), or null.</summary>
    public string? EngineError { get; init; }
}

/// <summary>Inputs for one simulation run.</summary>
public sealed class SimRunRequest
{
    /// <summary>Absolute path to the primary VHDL file.</summary>
    public required string TopFilePath { get; init; }
    /// <summary>Additional source files (dependencies).</summary>
    public IReadOnlyList<string> ExtraFiles { get; init; } = Array.Empty<string>();
    /// <summary>Top entity name; inferred from the top file name when null.</summary>
    public string? EntryPoint { get; init; }
    /// <summary>Total simulated duration in nanoseconds.</summary>
    public long TotalNs { get; init; } = 1000;
    /// <summary>Requested tick in ns (narrowed to the design's static delays).</summary>
    public int TickResNs { get; init; } = 10;
    /// <summary>System clock in MHz.</summary>
    public int SystemClockMhz { get; init; } = 50;
    /// <summary>Optional FPGA model name (informational / for logging).</summary>
    public string? Fpga { get; init; }
    /// <summary>Manual input stimuli applied to ports over time.</summary>
    public List<PortMomentConfiguration> PortMoments { get; init; } = new();
    /// <summary>Optional wall-clock budget; aborts the run when exceeded.</summary>
    public int? WallTimeoutMs { get; init; }
    /// <summary>Clocks, port initial values and initial-value policy; null proposes the clock by name under LRM.</summary>
    public StimulusOptions? Stimulus { get; init; }
}

/// <summary>
/// Drives the Kmila engine headlessly for the CLI: stages the sources in a
/// throw-away sandbox, captures engine console chatter, and delegates the
/// pipeline to <see cref="HeadlessSimulator.RunProjectAsync"/> (the same
/// code path as the HTTP API), keeping the live <see cref="SignalHistory"/>.
/// </summary>
public sealed class KmilaSimulationService
{
    /// <summary>Optional progress sink (0..100). Wired to the engine runner.</summary>
    public Action<double>? OnProgress { get; set; }

    /// <summary>
    /// Runs the simulation in an isolated sandbox directory and returns the
    /// recorded history plus run metadata.
    /// </summary>
    public async Task<SimRunResult> RunAsync(SimRunRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TopFilePath) || !File.Exists(request.TopFilePath))
            throw new FileNotFoundException($"top file not found: {request.TopFilePath}");

        var store = new SandboxProjectStore();
        var project = store.Create("cli-run", retain: false);
        try
        {
            // Stage the top file + dependencies into the sandbox (analysis order).
            var staged = SourceStaging.Stage(project, request.TopFilePath, request.ExtraFiles, request.EntryPoint);
            return await RunInProjectAsync(project.Path, staged, request);
        }
        finally
        {
            store.Delete(project.Id);
        }
    }

    private async Task<SimRunResult> RunInProjectAsync(string projectPath, SourceStaging.Staged staged, SimRunRequest request)
    {
        // The VHDL engine (Interpreter) writes diagnostic chatter straight to
        // Console.Out. If the CLI is piping VCD/TXT/CSV to stdout, that chatter
        // would corrupt the data stream — so capture Console.Out for the whole
        // run and hand it back as EngineConsole (surfaced only under --verbose).
        var originalOut = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            return await RunPipelineAsync(projectPath, staged, request, captured);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private async Task<SimRunResult> RunPipelineAsync(
        string projectPath, SourceStaging.Staged staged, SimRunRequest request, StringWriter captured)
    {
        var run = await HeadlessSimulator.RunProjectAsync(projectPath, new HeadlessRunOptions
        {
            EntryPoint     = staged.Entry,
            EntryIsExplicit = staged.EntryIsExplicit,
            Files          = staged.Files,
            TotalNs        = request.TotalNs,
            TickResNs      = request.TickResNs,
            SystemClockMhz = request.SystemClockMhz,
            PortMoments    = request.PortMoments,
            WallTimeoutMs  = request.WallTimeoutMs,
            Stimulus       = request.Stimulus,
        }, OnProgress);

        if (!string.IsNullOrEmpty(request.Fpga))
            run.Log.Add($"[sim] fpga={request.Fpga}");

        return new SimRunResult
        {
            Entity = run.Entity.Name,
            History = run.History,
            TickFs = run.TickFs,
            TotalTicks = run.TotalTicks,
            DurationMs = run.DurationMs,
            SignalCount = run.History.SignalCount,
            Log = run.Log,
            EngineConsole = captured.ToString(),
            EngineError = run.EngineError,
        };
    }
}
