using System.Diagnostics;
using Debugger.Repositories;
using Interpreter.Services;
using Kmila.Shared.Models;
using TimeMachine.Models;
using PModels = Parser.Models;
using IModels = Interpreter.Models;

namespace Kmila.Shared.Services.Api;

// Drives the simulator from a sandbox project without going through the
// UI-coupled ProgramBuilder. Parses the project's VHDL, builds a
// SimulationConfig via StimulusBuilder, runs SimulationRunner, and returns
// the VCD text. No events, no progress UI, no toast — strictly request →
// response.
public sealed class HeadlessSimulator
{
    private readonly SandboxProjectStore _store;

    public HeadlessSimulator(SandboxProjectStore store) { _store = store; }

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

        // Pick the file to parse. Prefer the entry file if the caller named
        // one (matches `<entity>.vhdl`), otherwise grab the first .vhdl/.vhd.
        string? vhdlPath = ResolveEntryFile(project, request.EntryPoint);
        if (vhdlPath == null)
            throw new InvalidOperationException("No .vhdl/.vhd file found in sandbox project.");

        var log = new List<string>();
        var sw = Stopwatch.StartNew();

        // F-G-08 pass 3 diag (2026-07-16): phase-boundary stderr trace so we
        // can see where a long-running sim is stuck. Env-var gated so it's
        // silent by default.
        bool phaseTrace = Environment.GetEnvironmentVariable("KMILA_SIM_PHASE_TRACE") == "1";
        void Phase(string tag)
        {
            if (phaseTrace) Console.Error.WriteLine($"[kmila-phase t={sw.ElapsedMilliseconds}ms] {tag}");
        }

        // F-G-08 pass 4 (2026-07-17): hoist the wall-timeout CTS up here so
        // it covers parse + elaborate + stimulus, not just the simulation
        // loop. Pass 3 put the CancelAfter inside SimulationRunner, which is
        // too late — the parser hangs before the runner is even constructed.
        // The runner reuses this CTS via its RunAsync(ct) overload wired below.
        using var requestCts = new CancellationTokenSource();
        var wallEnv = Environment.GetEnvironmentVariable("KMILA_SIM_WALL_TIMEOUT_MS");
        if (int.TryParse(wallEnv, out int wallMs) && wallMs > 0)
        {
            requestCts.CancelAfter(TimeSpan.FromMilliseconds(wallMs));
        }
        var ct = requestCts.Token;

        Phase("start");
        // Reset cross-file registries so a previous run's entities don't
        // leak into this one — same hygiene the CLI does between invocations.
        Interpreter.Services.EntityRegistry.Instance.Clear();
        Interpreter.Services.PackageRegistry.Instance.Clear();
        Interpreter.Services.FunctionRegistry.Instance.Clear();
        // F-G-08 pass 5 (2026-07-22): zero the parse-profile counters so
        // each simulate call gets a fresh reading. No-op when the env var
        // isn't set (Reset is unconditional but the counters are also
        // untouched in that case).
        Parser.Services.ParseProfile.Reset();
        Phase("registries-cleared");
        foreach (var f in project.ListFiles())
        {
            if (f.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".vhd",  StringComparison.OrdinalIgnoreCase))
            {
                // F-G-10 (2026-07-18): skip our own elaborator artefacts.
                // A stale `_elaborated_<entry>.vhd` from a previous run would
                // otherwise re-register the same top-level entity in
                // EntityRegistry (with its arch body already flattened),
                // shadowing the fresh source and confusing downstream
                // component-instantiation lookups.
                if (Path.GetFileName(f).StartsWith("_elaborated_", StringComparison.OrdinalIgnoreCase))
                    continue;
                var fullPath = Path.Combine(project.Path, f);
                Phase($"scan-begin:{f}");
                try { Interpreter.Services.EntityRegistry.Instance.ScanFile(fullPath); }
                catch (Exception ex) { log.Add($"[scan] {f}: {ex.Message}"); }
                Phase($"scan-end:{f}");
                // F-G-04 follow-up (session 6, 2026-07-09): pick up
                // package-declared subtypes so downstream entities can
                // resolve `q16_t`, `keycode_t`, etc. to their base
                // (signed|unsigned|std_logic_vector) with the correct
                // range, instead of falling back to the STD_LOGIC_VECTOR
                // placeholder that VariableSyntetizer's F-G-04 branch
                // installs.
                Phase($"subscan-begin:{f}");
                try { Interpreter.Services.PackageSubtypeScanner.ScanFile(fullPath); }
                catch (Exception ex) { log.Add($"[subscan] {f}: {ex.Message}"); }
                Phase($"subscan-end:{f}");
            }
        }

        // Pre-elaboration: if the entry-point's architecture instantiates
        // any sub-components, inline them into a flat single-entity source.
        // Kmila's simulator parses component instantiations but doesn't yet
        // execute child architectures at run time, so without this step a
        // modular design would silently produce zero outputs. The user's
        // source files on disk are untouched; the elaborator writes a
        // sibling _elaborated_<entity>.vhd that the parser consumes.
        if (!string.IsNullOrEmpty(request.EntryPoint))
        {
            Phase("elaborate-begin");
            try
            {
                var flat = Interpreter.Services.SourceElaborator.ElaborateOrNull(project.Path, request.EntryPoint!);
                if (flat != null)
                {
                    var elaboratedPath = Path.Combine(project.Path, $"_elaborated_{request.EntryPoint}.vhd");
                    File.WriteAllText(elaboratedPath, flat);
                    vhdlPath = elaboratedPath;
                    log.Add($"[elab] inlined components for {request.EntryPoint}, flat source at {Path.GetFileName(elaboratedPath)}");
                }
            }
            catch (Exception ex)
            {
                log.Add($"[elab] elaboration failed, falling back to original source: {ex.Message}");
            }
            Phase("elaborate-end");
        }

        Phase("parse-begin");
        var parser = new Debugger.Repositories.Parser(vhdlPath);
        // F-G-08 pass 4: pass the request-scoped CTS so a wall-timeout can
        // actually interrupt a stuck parse. Prior to pass 4 ParseFile had no
        // cancellation surface.
        parser.ParseFile(ct);
        Phase("parse-end");
        if (parser.Entities.Count == 0)
            throw new InvalidOperationException("Parser produced no entities.");

        var entity = !string.IsNullOrEmpty(request.EntryPoint)
            ? parser.Entities.FirstOrDefault(e => string.Equals(e.Name, request.EntryPoint, StringComparison.OrdinalIgnoreCase) && e.Architecture != null)
              ?? parser.Entities.FirstOrDefault(e => e.Architecture != null)
            : parser.Entities.FirstOrDefault(e => e.Architecture != null);

        if (entity == null)
            throw new InvalidOperationException("No entity with architecture found.");

        long totalNs  = TotalNsFromRequest(request);
        var tickResNs = request.TickResNs > 0 ? request.TickResNs : TimeClock.NS_PER_TICK;

        var portMoments = MaterializePortMoments(request.PortMoments);

        Phase("stimulus-begin");
        var config = StimulusBuilder.BuildConfigNs(
            entity,
            portMoments,
            totalNs: totalNs,
            tickResNs: tickResNs,
            systemClockMhz: request.SystemClockMhz > 0 ? request.SystemClockMhz : 50);
        Phase("stimulus-end");

        log.Add($"[sim] entity={entity.Name} ports={entity.Ports.Count} totalTicks={config.TotalTicks} stimuli={config.Stimuli.Count} clocks={config.Clocks.Count}");

        using var runner = new SimulationRunner();
        // Hand the tick resolution to the runner via its engine so
        // `after N ns` clauses convert to the correct future-tick offset.
        // Bench bug #8.
        runner.OnEngineReady += eng => eng.TickResolutionNs = tickResNs;
        // Surface runtime errors (caught silently by RunAsync's outer
        // try/catch) into the API response — otherwise the user just sees
        // an empty VCD with a successful HTTP 200 and no clue what went
        // wrong inside the engine.
        runner.OnError += msg => log.Add($"[engine] {msg}");
        // F-G-11 (2026-07-18): surface VHDL `report` statements executed
        // during the run. Testbench self-checking sweeps use `report` as
        // their case-marker mechanism (e.g. Ring 5's
        // `report "======== A1 begin ========";`) so this is how the
        // headless caller reads back what happened.
        runner.OnReport += msg => log.Add($"[report] {msg}");
        // F-G-08 pass 4: propagate the request-scoped CTS into the runner so
        // a wall-timeout cancels sim (not just parse). SimulationRunner still
        // reads KMILA_SIM_WALL_TIMEOUT_MS itself as a fallback for direct
        // (non-HeadlessSimulator) callers; both paths end up calling Stop().
        using var cancelReg = ct.Register(runner.Stop);
        Phase("runner-begin");
        await runner.RunAsync(config);
        Phase("runner-end");

        var history = runner.Engine?.History
            ?? throw new InvalidOperationException("Simulation produced no SignalHistory.");

        // F-G-08 pass 3 diag (2026-07-16): surface the delta-loop counters
        // so a caller can see how many ticks/deltas the run touched and
        // whether the delta cap was hit — otherwise a stalled run looks
        // indistinguishable from a fast one via the response alone.
        if (runner.Engine != null)
        {
            log.Add($"[diag] {runner.Engine.GetDiagnosticSummary()}");
        }
        // F-G-08 pass 5 (2026-07-22): surface parse-side hot-path counters.
        // Gated on env var so the log stays terse by default.
        if (Parser.Services.ParseProfile.Enabled)
        {
            var profileLine = Parser.Services.ParseProfile.Summarize();
            log.Add($"[diag] {profileLine}");
            if (phaseTrace) Console.Error.WriteLine(profileLine);
        }

        string vcd = history.ExportToVcd(entity.Name, timescaleNs: tickResNs);
        sw.Stop();

        return new SimulateResponse
        {
            RunId       = Guid.NewGuid().ToString("N").Substring(0, 12),
            Entity      = entity.Name,
            DurationMs  = sw.ElapsedMilliseconds,
            TotalTicks  = config.TotalTicks,
            TickResNs   = tickResNs,
            SignalCount = history.SignalCount,
            Vcd         = vcd,
            Log         = log,
        };
    }

    private static string? ResolveEntryFile(SandboxProject project, string? entryPoint)
    {
        var vhdlFiles = project.ListFiles()
            .Where(f => f.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".vhd",  StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (vhdlFiles.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(entryPoint))
        {
            var match = vhdlFiles.FirstOrDefault(f =>
                string.Equals(Path.GetFileNameWithoutExtension(f), entryPoint, StringComparison.OrdinalIgnoreCase));
            if (match != null) return Path.Combine(project.Path, match);
        }
        return Path.Combine(project.Path, vhdlFiles[0]);
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
