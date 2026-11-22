using Kmila.CLI.Cli;
using Kmila.Shared.Services.Vcd;
using Kmila.CLI.Tui;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila diff a.vcd b.vcd</c> — compare two waveform dumps
/// signal-by-signal and report where they diverge. Pairs with the "fix-a-bug"
/// workflow (diff a student's run against a golden dump). Reads both dumps with the
/// shared <see cref="VcdReader"/> and compares them with <see cref="WaveformComparer"/>
/// (buses by numeric value); exits non-zero when they differ.
/// </summary>
public static class DiffCommand
{
    /// <summary>Compares the two VCDs and prints divergences.</summary>
    /// <returns>0 when identical on the compared signals, 1 when they differ, 64 on a usage error.</returns>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "all", "quiet", "q", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Diff());
            return Task.FromResult(0);
        }

        var positionals = p.Positionals;
        var a = p.Get("a") ?? (positionals.Count > 0 ? positionals[0] : null);
        var b = p.Get("b") ?? (positionals.Count > 1 ? positionals[1] : null);
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            Console.Error.WriteLine("kmila diff: need two VCD files. Usage: kmila diff <a.vcd> <b.vcd>");
            return Task.FromResult(64);
        }
        a = Path.GetFullPath(a); b = Path.GetFullPath(b);
        if (!File.Exists(a)) { Console.Error.WriteLine($"kmila diff: not found: {a}"); return Task.FromResult(1); }
        if (!File.Exists(b)) { Console.Error.WriteLine($"kmila diff: not found: {b}"); return Task.FromResult(1); }

        bool all = p.Has("all");
        bool quiet = p.Has("quiet", "q");

        VcdDocument da, db;
        try { da = VcdReader.ParseFile(a); db = VcdReader.ParseFile(b); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila diff: {ex.Message}"); return Task.FromResult(1); }

        // Optional signal filter.
        var only = p.GetAll("signals", "signal")
                    .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .ToList();

        var diffs = WaveformComparer.Compare(da, db, new CompareOptions
        {
            Signals = only.Count > 0 ? only : null,
            Equality = ValueEquality.NumericBuses,
            MaxExamples = all ? int.MaxValue : 1,
        });

        int differences = 0;

        // Signals present in only one file.
        foreach (var d in diffs.Where(d => d.Missing == MissingSide.Actual))
        { differences++; if (!quiet) Console.Out.WriteLine($"- {d.Signal}: only in {Path.GetFileName(a)}"); }
        foreach (var d in diffs.Where(d => d.Missing == MissingSide.Expected))
        { differences++; if (!quiet) Console.Out.WriteLine($"+ {d.Signal}: only in {Path.GetFileName(b)}"); }
        // A --signals name in neither file (a typo) is a difference, not a silent pass.
        foreach (var d in diffs.Where(d => d.Missing == MissingSide.Both))
        { differences++; if (!quiet) Console.Out.WriteLine($"? {d.Signal}: {d.MissingFrom(Path.GetFileName(a), Path.GetFileName(b))}"); }

        // Common signals: the first divergence of each (every one with --all).
        foreach (var d in diffs.Where(d => d.Missing == MissingSide.None))
            foreach (var m in d.Examples)
            {
                differences++;
                if (!quiet)
                    Console.Out.WriteLine(
                        $"~ {d.Signal} @ {m.TimeFs / WaveModel.FsPerNs}ns: {Show(m.Expected, d.Width)} != {Show(m.Actual, d.ActualWidth)}");
            }

        if (!quiet)
            Console.Error.WriteLine(differences == 0
                ? "[diff] identical on compared signals"
                : $"[diff] {differences} difference(s)");

        return Task.FromResult(differences == 0 ? 0 : 1);
    }

    private static string Show(string? v, int width) => v is null ? "(none)" : WaveModel.Display(v, width);
}
