using System.Diagnostics;
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
    /// <summary>Tick resolution used for the run, in ns (also the VCD timescale).</summary>
    public int TickResNs { get; init; }
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
    /// <summary>Tick resolution in ns.</summary>
    public int TickResNs { get; init; } = 10;
    /// <summary>System clock in MHz.</summary>
    public int SystemClockMhz { get; init; } = 50;
    /// <summary>Optional FPGA model name (informational / for logging).</summary>
    public string? Fpga { get; init; }
    /// <summary>Manual input stimuli applied to ports over time.</summary>
    public List<PortMomentConfiguration> PortMoments { get; init; } = new();
    /// <summary>Optional wall-clock budget; aborts the run when exceeded.</summary>
    public int? WallTimeoutMs { get; init; }
}

/// <summary>
/// Drives the Kmila VHDL engine headlessly. This deliberately mirrors the
/// proven pipeline in <c>Kmila.Shared/Services/Api/HeadlessSimulator.cs</c>
/// (scan → elaborate → parse → stimulus → run → history) — the one difference
/// is that it surfaces the live <see cref="SignalHistory"/> to the caller.
/// Keep the two in step if the shared pipeline changes.
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
            // Stage the top file + dependencies into the sandbox.
            var topName = Path.GetFileName(request.TopFilePath);
            project.WriteFile(topName, File.ReadAllText(request.TopFilePath));
            foreach (var extra in request.ExtraFiles)
            {
                if (!File.Exists(extra))
                    throw new FileNotFoundException($"source file not found: {extra}");
                project.WriteFile(Path.GetFileName(extra), File.ReadAllText(extra));
            }

            var entry = string.IsNullOrWhiteSpace(request.EntryPoint)
                ? Path.GetFileNameWithoutExtension(topName)
                : request.EntryPoint!;

            return await RunInProjectAsync(project.Path, entry, request);
        }
        finally
        {
            store.Delete(project.Id);
        }
    }

    private async Task<SimRunResult> RunInProjectAsync(string projectPath, string entry, SimRunRequest request)
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
            return await RunPipelineAsync(projectPath, entry, request, captured);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private async Task<SimRunResult> RunPipelineAsync(
        string projectPath, string entry, SimRunRequest request, StringWriter captured)
    {
        var log = new List<string>();
        var sw = Stopwatch.StartNew();

        using var cts = new CancellationTokenSource();
        if (request.WallTimeoutMs is int ms && ms > 0)
            cts.CancelAfter(TimeSpan.FromMilliseconds(ms));
        var ct = cts.Token;

        // --- Reset the engine's process-global registries (one run per process,
        //     but mirror the shared pipeline's hygiene exactly). ---
        EntityRegistry.Instance.Clear();
        PackageRegistry.Instance.Clear();
        FunctionRegistry.Instance.Clear();
        global::Parser.Services.ParseProfile.Reset();

        // --- Scan every source so cross-file entities / packages resolve. ---
        string? vhdlPath = null;
        foreach (var f in Directory.EnumerateFiles(projectPath))
        {
            if (!(f.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase) ||
                  f.EndsWith(".vhd", StringComparison.OrdinalIgnoreCase)))
                continue;
            if (Path.GetFileName(f).StartsWith("_elaborated_", StringComparison.OrdinalIgnoreCase))
                continue;

            vhdlPath ??= f;
            // Prefer the entry file as the parse target when we can see it.
            if (string.Equals(Path.GetFileNameWithoutExtension(f), entry, StringComparison.OrdinalIgnoreCase))
                vhdlPath = f;

            try { EntityRegistry.Instance.ScanFile(f); }
            catch (Exception ex) { log.Add($"[scan] {Path.GetFileName(f)}: {ex.Message}"); }
            try { PackageSubtypeScanner.ScanFile(f); }
            catch (Exception ex) { log.Add($"[subscan] {Path.GetFileName(f)}: {ex.Message}"); }
            // v1.17.0 (P3): record declared names for the undeclared-identifier check.
            try { PackageRegistry.Instance.RegisterDeclaredNamesFromSource(File.ReadAllText(f)); } catch { /* best-effort */ }
        }
        if (vhdlPath == null)
            throw new InvalidOperationException("no .vhdl/.vhd source found.");

        // --- Elaborate: flatten any component instantiations into one source. ---
        try
        {
            var flat = SourceElaborator.ElaborateOrNull(projectPath, entry);
            if (flat != null)
            {
                var elaborated = Path.Combine(projectPath, $"_elaborated_{entry}.vhd");
                File.WriteAllText(elaborated, flat);
                vhdlPath = elaborated;
                log.Add($"[elab] inlined components for {entry}");
            }
        }
        catch (Exception ex)
        {
            log.Add($"[elab] elaboration skipped: {ex.Message}");
        }

        // --- Parse. ---
        var parser = new Debugger.Repositories.Parser(vhdlPath);
        parser.ParseFile(ct);
        if (parser.Entities.Count == 0)
            throw new InvalidOperationException("parser produced no entities (check the VHDL syntax).");

        var entity = parser.Entities
                         .FirstOrDefault(e => string.Equals(e.Name, entry, StringComparison.OrdinalIgnoreCase) && e.Architecture != null)
                     ?? parser.Entities.FirstOrDefault(e => e.Architecture != null);
        if (entity == null)
            throw new InvalidOperationException("no entity with an architecture found.");

        // --- Build the run config (clocks + stimuli, ns → ticks). ---
        int tickResNs = request.TickResNs > 0 ? request.TickResNs : 10;
        var config = StimulusBuilder.BuildConfigNs(
            entity,
            request.PortMoments.Count > 0 ? request.PortMoments : null,
            totalNs: request.TotalNs,
            tickResNs: tickResNs,
            systemClockMhz: request.SystemClockMhz > 0 ? request.SystemClockMhz : 50);

        log.Add($"[sim] entity={entity.Name} ports={entity.Ports.Count} totalTicks={config.TotalTicks} " +
                $"clockMHz={(request.SystemClockMhz > 0 ? request.SystemClockMhz : 50)}" +
                (string.IsNullOrEmpty(request.Fpga) ? "" : $" fpga={request.Fpga}"));

        // --- Run. ---
        using var runner = new SimulationRunner();
        runner.OnEngineReady += eng => eng.TickResolutionNs = tickResNs;
        runner.OnError += msg => log.Add($"[engine] {msg}");
        runner.OnReport += msg => log.Add($"[report] {msg}");
        if (OnProgress != null) runner.OnProgressChanged += p => OnProgress(p);
        using var cancelReg = ct.Register(runner.Stop);

        await runner.RunAsync(config);

        var history = runner.Engine?.History
            ?? throw new InvalidOperationException("simulation produced no signal history.");
        if (runner.Engine != null)
            log.Add($"[diag] {runner.Engine.GetDiagnosticSummary()}");

        sw.Stop();
        return new SimRunResult
        {
            Entity = entity.Name,
            History = history,
            TickResNs = tickResNs,
            TotalTicks = config.TotalTicks,
            DurationMs = sw.ElapsedMilliseconds,
            SignalCount = history.SignalCount,
            Log = log,
            EngineConsole = captured.ToString(),
        };
    }
}
