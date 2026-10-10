using System.Globalization;
using Kmila.CLI.Tui;

namespace Kmila.CLI.Simulation;

/// <summary>
/// A compact VCD reader that turns VCD text into a <see cref="WaveModel"/> for
/// the terminal waveform viewer. Handles the subset Kmila and common tools
/// (GHDL/Surfer) emit: <c>$timescale</c>, <c>$scope/$upscope</c>, <c>$var</c>,
/// <c>$dumpvars</c>, <c>#time</c> markers, scalar changes (<c>0!</c>/<c>1!</c>/
/// <c>x!</c>/<c>z!</c>) and vector changes (<c>b1010 !</c>). Hierarchy in names
/// is flattened to the leaf; reals/strings are ignored.
/// </summary>
public static class VcdReader
{
    /// <summary>Reads a VCD file into a <see cref="WaveModel"/>.</summary>
    public static WaveModel ParseFile(string path) => Parse(File.ReadAllText(path));

    /// <summary>Parses VCD text into a <see cref="WaveModel"/>.</summary>
    public static WaveModel Parse(string text)
    {
        var model = new WaveModel();
        var byId = new Dictionary<string, WaveSignal>(StringComparer.Ordinal);
        long timescaleFs = 1000; // default 1ps when unspecified
        long currentFs = 0;
        bool inDefs = true;

        // Token stream across whitespace/newlines handles multi-line $timescale.
        var tokens = Tokenize(text);
        for (int i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];

            if (inDefs)
            {
                switch (tok)
                {
                    case "$timescale":
                    {
                        // Collect until $end, join (handles "1 ps" and "1ps").
                        var parts = new List<string>();
                        while (++i < tokens.Count && tokens[i] != "$end") parts.Add(tokens[i]);
                        timescaleFs = ParseTimescaleFs(string.Concat(parts));
                        break;
                    }
                    case "$scope":
                    {
                        // $scope module <name> $end — remember the first module.
                        if (i + 2 < tokens.Count && string.IsNullOrEmpty(model.Module))
                            model.Module = tokens[i + 2];
                        while (++i < tokens.Count && tokens[i] != "$end") { }
                        break;
                    }
                    case "$var":
                    {
                        // $var <type> <width> <id> <name> [<range>] $end
                        var fields = new List<string>();
                        while (++i < tokens.Count && tokens[i] != "$end") fields.Add(tokens[i]);
                        if (fields.Count >= 4)
                        {
                            int width = int.TryParse(fields[1], out var w) ? w : 1;
                            string id = fields[2];
                            string name = Leaf(fields[3]);
                            // A trailing [n:0] range token is part of the display name.
                            if (fields.Count >= 5 && fields[4].StartsWith('['))
                                name += fields[4];
                            if (!byId.ContainsKey(id))
                            {
                                var sig = new WaveSignal { Name = name, Width = width };
                                byId[id] = sig;
                                model.Signals.Add(sig);
                            }
                        }
                        break;
                    }
                    case "$enddefinitions":
                        while (++i < tokens.Count && tokens[i] != "$end") { }
                        inDefs = false;
                        break;
                    default:
                        // $version/$date/$comment/$dumpvars etc. — skip to $end if block-like.
                        if (tok.StartsWith('$') && tok != "$end")
                            while (++i < tokens.Count && tokens[i] != "$end") { }
                        break;
                }
                continue;
            }

            // Value-change section.
            if (tok.Length == 0) continue;
            char c0 = tok[0];
            if (c0 == '#')
            {
                if (long.TryParse(tok.AsSpan(1), out var t))
                    currentFs = t * timescaleFs;
            }
            else if (c0 == '0' || c0 == '1' || c0 == 'x' || c0 == 'X' || c0 == 'z' || c0 == 'Z' ||
                     c0 == 'u' || c0 == 'U' || c0 == 'l' || c0 == 'L' || c0 == 'h' || c0 == 'H')
            {
                // Scalar change: <value><id>
                string id = tok[1..];
                if (byId.TryGetValue(id, out var sig))
                    Append(sig, currentFs, char.ToLowerInvariant(c0).ToString());
            }
            else if (c0 == 'b' || c0 == 'B')
            {
                // Vector change: b<bits> <id> (id is the NEXT token).
                string bits = tok[1..];
                if (i + 1 < tokens.Count)
                {
                    string id = tokens[++i];
                    if (byId.TryGetValue(id, out var sig))
                        Append(sig, currentFs, bits);
                }
            }
            else if (c0 == 'r' || c0 == 'R')
            {
                // Real change: skip value + id.
                if (i + 1 < tokens.Count) i++;
            }
            // else: unknown token, ignore.
        }

        foreach (var s in model.Signals)
            if (s.Transitions.Count > 0)
                model.MaxTimeFs = Math.Max(model.MaxTimeFs, s.Transitions[^1].TimeFs);

        return model;
    }

    private static void Append(WaveSignal sig, long fs, string value)
    {
        // Collapse duplicate consecutive values at the same or later time.
        if (sig.Transitions.Count > 0)
        {
            var last = sig.Transitions[^1];
            if (last.Value == value) return;
            if (last.TimeFs == fs)
            {
                sig.Transitions[^1] = new WaveTransition(fs, value);
                return;
            }
        }
        sig.Transitions.Add(new WaveTransition(fs, value));
    }

    private static List<string> Tokenize(string text)
    {
        var list = new List<string>(text.Length / 4 + 16);
        int n = text.Length, i = 0;
        while (i < n)
        {
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            if (i >= n) break;
            int start = i;
            while (i < n && !char.IsWhiteSpace(text[i])) i++;
            list.Add(text[start..i]);
        }
        return list;
    }

    private static string Leaf(string name)
    {
        int slash = name.LastIndexOf('/');
        return slash >= 0 ? name[(slash + 1)..] : name;
    }

    private static long ParseTimescaleFs(string spec)
    {
        // spec like "1ps", "10ns", "1fs". Split digits from unit.
        int k = 0;
        while (k < spec.Length && (char.IsDigit(spec[k]) || spec[k] == '.')) k++;
        double mag = k > 0 && double.TryParse(spec[..k], NumberStyles.Float, CultureInfo.InvariantCulture, out var m) ? m : 1;
        string unit = spec[k..].Trim().ToLowerInvariant();
        long perUnitFs = unit switch
        {
            "s"  => 1_000_000_000_000_000L,
            "ms" => 1_000_000_000_000L,
            "us" => 1_000_000_000L,
            "ns" => 1_000_000L,
            "ps" => 1_000L,
            "fs" => 1L,
            _    => 1_000L,
        };
        return Math.Max(1, (long)(mag * perUnitFs));
    }
}
