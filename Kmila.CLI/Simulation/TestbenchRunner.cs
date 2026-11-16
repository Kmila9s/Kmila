using Interpreter.Services;
using Kmila.Shared.Services;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Runs a VHDL <b>testbench</b> against a design headlessly, capturing its
/// <c>report</c> output. Reuses the shared <see cref="ProgramBuilder"/> testbench
/// path (<c>RunTestbenchAsync</c> + <c>OnReport</c>/<c>OnSimulationCompleted</c>),
/// the same one the editor uses — the testbench supplies its own clock/reset, so
/// no port-moment stimulus is needed. Returns a <see cref="SimRunResult"/> so the
/// rest of the CLI (output formats, assertions) works unchanged.
/// </summary>
public static class TestbenchRunner
{
    /// <summary>
    /// Stages the design + testbench into a temp project and runs the testbench.
    /// </summary>
    /// <param name="topFile">The design under test (absolute path).</param>
    /// <param name="extraFiles">Additional design sources.</param>
    /// <param name="testbenchFile">The <c>*_tb.vhd(l)</c> testbench (absolute path).</param>
    /// <param name="totalNs">Simulated duration in ns.</param>
    /// <param name="tickResNs">Tick resolution in ns.</param>
    /// <param name="systemClockMhz">System clock in MHz (for the engine's clock math).</param>
    public static async Task<SimRunResult> RunAsync(
        string topFile, IReadOnlyList<string> extraFiles, string testbenchFile,
        long totalNs, int tickResNs, int systemClockMhz)
    {
        if (!File.Exists(testbenchFile))
            throw new FileNotFoundException($"testbench not found: {testbenchFile}");

        var dir = Path.Combine(Path.GetTempPath(), "kmila-cli-tb", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        // Engine chatter → capture so piped stdout stays clean.
        var originalOut = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            void Stage(string src)
            {
                if (!File.Exists(src)) throw new FileNotFoundException($"source file not found: {src}");
                File.Copy(src, Path.Combine(dir, Path.GetFileName(src)), overwrite: true);
            }
            Stage(topFile);
            foreach (var f in extraFiles) Stage(f);
            var tbName = Path.GetFileName(testbenchFile);
            Stage(testbenchFile);

            var log = new List<string>();
            SignalHistory? history = null;
            string? buildError = null;

            using var builder = new ProgramBuilder();
            builder.OnReport += msg => log.Add($"[report] {msg}");
            builder.OnBuildError += (msg, det) => buildError ??= string.IsNullOrEmpty(det) ? msg : $"{msg}: {det}";
            builder.OnSimulationError += msg => log.Add($"[engine] {msg}");
            builder.OnSimulationCompleted += h => history = h;

            int tick = tickResNs > 0 ? tickResNs : 10;
            var duration = TimeSpan.FromTicks(Math.Max(1, totalNs / 100)); // 1 TimeSpan tick = 100 ns

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await builder.RunTestbenchAsync(dir, tbName, duration,
                systemClockMhz > 0 ? systemClockMhz : 50, tick);
            sw.Stop();

            history ??= builder.SimulationHistory;
            if (history == null)
                throw new InvalidOperationException(
                    buildError ?? builder.LastRunError ?? "testbench produced no signal history (check the testbench/design).");

            return new SimRunResult
            {
                Entity = Path.GetFileNameWithoutExtension(tbName),
                History = history,
                TickResNs = tick,
                TotalTicks = 0,
                DurationMs = sw.ElapsedMilliseconds,
                SignalCount = history.SignalCount,
                Log = log,
                EngineConsole = captured.ToString(),
                EngineError = builder.LastRunError,
            };
        }
        finally
        {
            Console.SetOut(originalOut);
            try { Directory.Delete(dir, true); } catch { /* best-effort */ }
        }
    }
}
