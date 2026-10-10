using Kmila.CLI.Cli;
using Kmila.CLI.Simulation;
using Kmila.CLI.Tui;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila diff a.vcd b.vcd</c> — compare two waveform dumps
/// signal-by-signal and report where they diverge. Pairs with the "fix-a-bug"
/// workflow (diff a student's run against a golden dump). Reuses the CLI's own
/// <see cref="VcdReader"/> + <see cref="WaveModel"/>; exits non-zero when they differ.
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

        WaveModel wa, wb;
        try { wa = VcdReader.ParseFile(a); wb = VcdReader.ParseFile(b); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila diff: {ex.Message}"); return Task.FromResult(1); }

        // Optional signal filter.
        var only = p.GetAll("signals", "signal")
                    .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var namesA = wa.Signals.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var namesB = wb.Signals.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int differences = 0;

        // Signals present in only one file.
        foreach (var n in namesA.Except(namesB))
            if (only.Count == 0 || only.Contains(n))
            { differences++; if (!quiet) Console.Out.WriteLine($"- {n}: only in {Path.GetFileName(a)}"); }
        foreach (var n in namesB.Except(namesA))
            if (only.Count == 0 || only.Contains(n))
            { differences++; if (!quiet) Console.Out.WriteLine($"+ {n}: only in {Path.GetFileName(b)}"); }

        // Common signals: compare value at every transition time of either side.
        foreach (var sa in wa.Signals)
        {
            if (only.Count > 0 && !only.Contains(sa.Name)) continue;
            var sb = wb.Signals.FirstOrDefault(s => string.Equals(s.Name, sa.Name, StringComparison.OrdinalIgnoreCase));
            if (sb == null) continue;

            var times = sa.Transitions.Select(t => t.TimeFs)
                        .Concat(sb.Transitions.Select(t => t.TimeFs))
                        .Distinct().OrderBy(t => t).ToList();

            bool reported = false;
            foreach (var fs in times)
            {
                var va = sa.ValueAtFs(fs);
                var vb = sb.ValueAtFs(fs);
                if (!ValuesEqual(va, vb, sa.Width, sb.Width))
                {
                    differences++;
                    if (!quiet)
                        Console.Out.WriteLine(
                            $"~ {sa.Name} @ {fs / WaveModel.FsPerNs}ns: {Show(va)} != {Show(vb)}");
                    reported = true;
                    if (!all) break;
                }
            }
            _ = reported;
        }

        if (!quiet)
            Console.Error.WriteLine(differences == 0
                ? "[diff] identical on compared signals"
                : $"[diff] {differences} difference(s)");

        return Task.FromResult(differences == 0 ? 0 : 1);
    }

    private static string Show(string? v) => v ?? "(none)";

    private static bool ValuesEqual(string? a, string? b, int widthA, int widthB)
    {
        if (a == null || b == null) return a == b;
        if (widthA <= 1 && widthB <= 1)
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        // Bus: compare numerically when both are clean binary, else textually.
        if (TryBin(a, out var na) && TryBin(b, out var nb)) return na == nb;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBin(string bits, out long value)
    {
        value = 0;
        if (string.IsNullOrEmpty(bits) || !bits.All(c => c is '0' or '1')) return false;
        foreach (var c in bits) value = (value << 1) | (c == '1' ? 1L : 0L);
        return true;
    }
}
