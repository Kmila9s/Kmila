using Kmila.CLI.Cli;
using Kmila.CLI.I18n;
using Kmila.CLI.Simulation;
using Interpreter.Models;
using Kmila.Shared.Models;
using Kmila.Shared.Services;

namespace Kmila.CLI.Commands;

/// <summary>Implements <c>kmila sim</c> — a non-interactive simulation run.</summary>
public static class SimCommand
{
    /// <summary>Parses options, runs one simulation, and writes the chosen output format.</summary>
    /// <returns>0 on success, 1 on a run/IO failure, 64 on a usage error.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args,
            "no-breakpoints", "quiet", "q", "verbose", "v", "progress", "watch", "help", "h", "didactic");

        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Sim());
            return 0;
        }

        ConfigFile cfg;
        try { cfg = ConfigFile.Load(p.Get("config")); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila sim: {ex.Message}"); return 64; }

        var s = Strings.Load(p.Get("lang") ?? cfg.Lang);

        bool quiet   = p.Has("quiet", "q");
        bool verbose = p.Has("verbose", "v");
        bool progress = p.Has("progress");

        // --fpga list : enumerate known devices and exit.
        var fpga = p.Get("fpga") ?? cfg.Fpga;
        if (string.Equals(fpga, "list", StringComparison.OrdinalIgnoreCase))
        {
            var models = FpgaResolver.KnownModels();
            if (models.Count == 0) Console.Error.WriteLine("kmila: no device specs found.");
            else Console.WriteLine(string.Join(Environment.NewLine, models));
            return 0;
        }

        // Required: the top file.
        var topFile = p.Get("top-file", "main-file", "t");
        if (string.IsNullOrWhiteSpace(topFile))
        {
            Console.Error.WriteLine("kmila sim: missing --top-file. Try `kmila sim --help`.");
            return 64;
        }
        topFile = Path.GetFullPath(topFile);
        if (!File.Exists(topFile))
        {
            Console.Error.WriteLine($"kmila sim: top file not found: {topFile}");
            return 1;
        }

        // Extra sources: repeated --file plus any bare positional paths.
        var extras = p.GetAll("file").Concat(p.Positionals)
                      .Select(Path.GetFullPath)
                      .Where(f => !string.Equals(f, topFile, StringComparison.Ordinal))
                      .Distinct()
                      .ToList();
        foreach (var f in extras)
        {
            if (!File.Exists(f))
            {
                Console.Error.WriteLine($"kmila sim: source file not found: {f}");
                return 1;
            }
        }

        // Validate the FPGA (if any) and derive the default clock.
        if (!string.IsNullOrWhiteSpace(fpga) && FpgaResolver.Load(fpga) == null)
        {
            Console.Error.WriteLine($"kmila sim: unknown FPGA '{fpga}'. Use `--fpga list` to see options.");
            return 1;
        }

        int clock;
        var clockRaw = p.Get("clockMHZ", "clock-mhz", "clockmhz");
        if (int.TryParse(clockRaw, out var c) && c > 0)
            clock = c;
        else if (cfg.ClockMhz is > 0)
            clock = cfg.ClockMhz.Value;
        else if (!string.IsNullOrWhiteSpace(fpga))
            clock = FpgaResolver.DefaultClockMhz(fpga) ?? 50;
        else
            clock = 50;

        // Duration + tick resolution.
        long totalNs;
        try { totalNs = DurationParser.ParseToNs(p.Get("duration") ?? cfg.Duration ?? "1000ns"); }
        catch (FormatException ex) { Console.Error.WriteLine($"kmila sim: {ex.Message}"); return 64; }

        int tickRes = p.Has("tick-res-ns") ? p.GetInt(10, "tick-res-ns") : (cfg.TickResNs ?? 10);
        if (tickRes <= 0) tickRes = 10;

        // Stimuli.
        List<PortMomentConfiguration> portMoments;
        try { portMoments = BuildStimuli(p.GetAll("stimulus")); }
        catch (FormatException ex) { Console.Error.WriteLine($"kmila sim: {ex.Message}"); return 64; }

        // Initial values, declared clocks and port initial values.
        StimulusOptions stimulusOptions;
        try
        {
            var policy = p.Has("didactic") ? InitialValuePolicy.Didactic : StimulusOptions.ParsePolicy(cfg.InitialValues);
            var clockSpecs = p.GetAll("clock").Concat(cfg.Clocks ?? []).Select(ParseClock).ToList();
            var initials = p.GetAll("initial").Select(ParseInitial).ToList();
            stimulusOptions = StimulusOptions.Declared(policy, clockSpecs, initials);
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException)
        {
            Console.Error.WriteLine($"kmila sim: {ex.Message}");
            return 64;
        }

        // Output format.
        OutputFormat format;
        try { format = OutputWriter.ParseFormat(p.Get("output-format", "format") ?? cfg.Format); }
        catch (FormatException ex) { Console.Error.WriteLine($"kmila sim: {ex.Message}"); return 64; }

        int? wallTimeout = int.TryParse(p.Get("wall-timeout-ms"), out var w) && w > 0 ? w : null;

        var request = new SimRunRequest
        {
            TopFilePath = topFile,
            ExtraFiles = extras,
            EntryPoint = p.Get("entity", "top"),
            TotalNs = totalNs,
            TickResNs = tickRes,
            SystemClockMhz = clock,
            Fpga = fpga,
            PortMoments = portMoments,
            WallTimeoutMs = wallTimeout,
            Stimulus = stimulusOptions,
        };

        var testbench = p.Get("testbench", "tb");
        if (!string.IsNullOrWhiteSpace(testbench))
        {
            testbench = Path.GetFullPath(testbench);
            if (!File.Exists(testbench))
            {
                Console.Error.WriteLine($"kmila sim: testbench not found: {testbench}");
                return 1;
            }
        }

        async Task<int> ExecuteOnceAsync()
        {
        SimRunResult result;
        try
        {
            if (!string.IsNullOrWhiteSpace(testbench))
            {
                result = await TestbenchRunner.RunAsync(
                    topFile, extras, testbench, totalNs, tickRes, clock);
            }
            else
            {
                var service = new KmilaSimulationService();
                if (progress && !quiet)
                {
                    int last = -1;
                    service.OnProgress = pct =>
                    {
                        int step = (int)pct;
                        if (step != last) { last = step; Console.Error.Write($"\r[sim] {step,3}%"); }
                    };
                }
                result = await service.RunAsync(request);
            }
        }
        catch (Exception ex)
        {
            if (progress && !quiet) Console.Error.WriteLine();
            Console.Error.WriteLine($"kmila sim: {ex.Message}");
            return 1;
        }
        if (progress && !quiet) Console.Error.WriteLine("\r[sim] 100%");

        if (verbose && !quiet)
        {
            if (!string.IsNullOrWhiteSpace(result.EngineConsole))
                Console.Error.Write(result.EngineConsole);
            foreach (var line in result.Log)
                Console.Error.WriteLine(line);
        }

        // An engine abort is a failed run even though the partial waveform is
        // still written below; it is reported regardless of --quiet.
        bool aborted = result.EngineError != null;
        if (aborted)
            Console.Error.WriteLine($"kmila sim: engine error: {result.EngineError}");

        // Assertions (--assert signal@time=value, repeatable; --assert-file <f>).
        var assertTokens = new List<string>(p.GetAll("assert"));
        var assertFile = p.Get("assert-file");
        if (!string.IsNullOrWhiteSpace(assertFile))
        {
            if (!File.Exists(assertFile))
            {
                Console.Error.WriteLine($"kmila sim: assert file not found: {assertFile}");
                return 64;
            }
            assertTokens.AddRange(File.ReadAllLines(assertFile)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith('#')));
        }

        int assertExit = 0;
        if (assertTokens.Count > 0)
        {
            var model = Tui.WaveModel.FromVcd(result.History.ExportToVcd(result.Entity, result.TickFs));
            List<AssertionChecker.Result> checks;
            try { checks = AssertionChecker.Evaluate(model, assertTokens); }
            catch (FormatException ex) { Console.Error.WriteLine($"kmila sim: {ex.Message}"); return 64; }

            int passed = checks.Count(r => r.Passed);
            if (!quiet)
            {
                foreach (var r in checks)
                    Console.Error.WriteLine(
                        $"[assert] {(r.Passed ? "PASS" : "FAIL")} {r.Signal}@{r.TimeNs}ns expected={r.Expected} actual={r.Actual}");
                Console.Error.WriteLine($"[assert] {passed}/{checks.Count} passed");
            }
            if (passed != checks.Count) assertExit = 1;
        }

        // Render + write.
        string content = OutputWriter.Render(result, format);
        var outFile = p.Get("output-file", "o");
        if (string.IsNullOrWhiteSpace(outFile) && format == OutputFormat.Svg)
            outFile = OutputWriter.DefaultFileName(format);

        if (string.IsNullOrWhiteSpace(outFile))
        {
            Console.Out.Write(content);
            if (!content.EndsWith('\n')) Console.Out.WriteLine();
        }
        else
        {
            outFile = Path.GetFullPath(outFile);
            File.WriteAllText(outFile, content);
            if (!quiet)
                Console.Error.WriteLine(
                    $"[sim] entity={result.Entity} signals={result.SignalCount} " +
                    $"ticks={result.TotalTicks} {result.DurationMs}ms → {outFile}");
        }

        return aborted ? 1 : assertExit;
        }

        if (!p.Has("watch"))
            return await ExecuteOnceAsync();

        return await WatchLoopAsync(ExecuteOnceAsync, topFile, extras, quiet);
    }

    /// <summary>
    /// Re-runs <paramref name="runOnce"/> whenever the top file or a dependency
    /// changes on disk, until Ctrl+C. Backs <c>kmila sim --watch</c>.
    /// </summary>
    private static async Task<int> WatchLoopAsync(
        Func<Task<int>> runOnce, string topFile, IReadOnlyList<string> extras, bool quiet)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        if (!quiet) Console.Error.WriteLine("[watch] running… (Ctrl+C to stop)");
        try { await runOnce(); } catch { /* keep watching through failures */ }

        var dirs = new[] { topFile }.Concat(extras)
            .Select(f => Path.GetDirectoryName(Path.GetFullPath(f))!)
            .Where(d => !string.IsNullOrEmpty(d)).Distinct().ToList();
        var files = new[] { topFile }.Concat(extras)
            .Select(Path.GetFullPath).ToHashSet(StringComparer.Ordinal);

        var watchers = new List<FileSystemWatcher>();
        var pending = new SemaphoreSlim(0, 1);
        DateTime last = DateTime.MinValue;

        void OnChange(object _, FileSystemEventArgs e)
        {
            if (!files.Contains(Path.GetFullPath(e.FullPath))) return;
            // Debounce editor double-writes.
            var now = DateTime.UtcNow;
            if ((now - last).TotalMilliseconds < 200) return;
            last = now;
            try { if (pending.CurrentCount == 0) pending.Release(); } catch { }
        }

        foreach (var d in dirs)
        {
            var w = new FileSystemWatcher(d) { EnableRaisingEvents = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size };
            w.Changed += OnChange; w.Created += OnChange; w.Renamed += OnChange;
            watchers.Add(w);
        }

        try
        {
            while (!cts.IsCancellationRequested)
            {
                try { await pending.WaitAsync(cts.Token); }
                catch (OperationCanceledException) { break; }
                await Task.Delay(120, cts.Token).ContinueWith(_ => { }, TaskScheduler.Default);
                if (!quiet) Console.Error.WriteLine($"[watch] change detected — re-running {DateTime.Now:HH:mm:ss}");
                try { await runOnce(); } catch (Exception ex) { Console.Error.WriteLine($"kmila sim: {ex.Message}"); }
            }
        }
        finally
        {
            foreach (var w in watchers) { try { w.Dispose(); } catch { } }
        }
        if (!quiet) Console.Error.WriteLine("[watch] stopped.");
        return 0;
    }

    /// <summary>
    /// Parses <c>--clock port[:period]</c>: the period is a time (<c>20ns</c>) or a
    /// frequency (<c>50MHz</c>); without one the system clock (<c>--clockMHZ</c>) is used.
    /// </summary>
    private static ClockSpec ParseClock(string spec)
    {
        int colon = spec.IndexOf(':');
        if (colon < 0) return new ClockSpec(spec.Trim());
        string port = spec[..colon].Trim(), period = spec[(colon + 1)..].Trim();
        if (port.Length == 0) throw new FormatException($"invalid --clock '{spec}' (use port[:20ns|:50MHz])");
        if (period.EndsWith("mhz", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(period[..^3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double mhz) && mhz > 0)
            return new ClockSpec(port, 1_000.0 / mhz);
        return new ClockSpec(port, DurationParser.ParseToNs(period));
    }

    /// <summary>Parses <c>--initial port=value</c> (the port's value before time 0).</summary>
    private static KeyValuePair<string, string> ParseInitial(string token)
    {
        int eq = token.IndexOf('=');
        if (eq <= 0 || eq == token.Length - 1) throw new FormatException($"invalid --initial '{token}' (use port=value, e.g. clk=0)");
        return KeyValuePair.Create(token[..eq].Trim(), token[(eq + 1)..].Trim());
    }

    /// <summary>
    /// Turns <c>port@time=value</c> tokens into per-port, time-sorted
    /// <see cref="PortMomentConfiguration"/> entries for the stimulus builder.
    /// </summary>
    private static List<PortMomentConfiguration> BuildStimuli(IReadOnlyList<string> tokens)
    {
        var byPort = new Dictionary<string, PortMomentConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var tok in tokens)
        {
            var (port, timeNs, value) = DurationParser.ParseStimulus(tok);
            if (!byPort.TryGetValue(port, out var cfg))
                byPort[port] = cfg = new PortMomentConfiguration { PortName = port };
            cfg.Values.Add(new PortMomentValue { Time = timeNs, TimeScale = "NS", Value = value });
        }
        foreach (var cfg in byPort.Values)
            cfg.Values.Sort((a, b) => a.Time.CompareTo(b.Time));
        return byPort.Values.ToList();
    }
}
