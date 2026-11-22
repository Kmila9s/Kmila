using System.Globalization;
using Interpreter.Models;

namespace Kmila.Shared.Services.Vcd;

/// <summary>
/// The one VCD reader (bench, test kit, <c>kmila diff</c>, the terminal
/// waveform viewer). Reads the subset Kmila and GHDL emit — <c>$timescale</c>,
/// <c>$scope</c>/<c>$upscope</c>, <c>$var</c>, <c>$dumpvars</c>, <c>#time</c>,
/// scalar (<c>1!</c>) and vector (<c>b1010 !</c>) changes — on a whitespace
/// token stream, so multi-line directives and either line layout work. Reals and
/// strings are skipped. Values are kept as written; times are in femtoseconds.
/// </summary>
public static class VcdReader
{
    /// <summary>Reads a VCD file.</summary>
    public static VcdDocument ParseFile(string path) => Parse(File.ReadAllText(path));

    /// <summary>Parses VCD text.</summary>
    public static VcdDocument Parse(string text)
    {
        var tokens = Tokenize(text);
        int i = 0;
        var doc = new VcdDocument();
        var byId = new Dictionary<string, VcdSignal>(StringComparer.Ordinal);
        var scopes = new List<string>();
        // ByName: the shallowest declaration wins; among equally deep ones, the last.
        var nameDepth = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (; i < tokens.Count; i++)
        {
            string tok = tokens[i];
            if (tok == "$enddefinitions") { SkipToEnd(tokens, ref i); i++; break; }
            switch (tok)
            {
                case "$timescale":
                    doc.TimescaleFs = TimescaleFs(string.Concat(Block(tokens, ref i)));
                    break;
                case "$scope":
                {
                    var fields = Block(tokens, ref i);
                    scopes.Add(fields.Count >= 2 ? fields[1] : "");
                    break;
                }
                case "$upscope":
                    SkipToEnd(tokens, ref i);
                    if (scopes.Count > 0) scopes.RemoveAt(scopes.Count - 1);
                    break;
                case "$var":
                {
                    // $var <type> <width> <id> <name> [<range>] $end
                    var f = Block(tokens, ref i);
                    if (f.Count < 4) break;
                    string id = f[2];
                    string leaf = Leaf(f[3]);
                    string name = StripSubscript(leaf);
                    int depth = scopes.Count;
                    // The module is the outermost scope holding the first variable: GHDL's empty
                    // library scopes (standard, std_logic_1164, …) come and go before it.
                    if (doc.Module.Length == 0 && depth > 0) doc.Module = scopes[0];
                    if (!byId.TryGetValue(id, out var sig))
                    {
                        sig = new VcdSignal
                        {
                            Id = id,
                            Name = name,
                            DisplayName = f.Count >= 5 && f[4].StartsWith('[') ? leaf + f[4] : leaf,
                            Width = int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) ? w : 1,
                        };
                        byId[id] = sig;
                        doc.Signals.Add(sig);
                    }
                    if (!nameDepth.TryGetValue(name, out int best) || depth <= best)
                    {
                        nameDepth[name] = depth;
                        doc.ByName[name] = sig;
                    }
                    break;
                }
                default:
                    if (tok.StartsWith('$') && tok != "$end") SkipToEnd(tokens, ref i);
                    break;
            }
        }

        long now = 0;
        for (; i < tokens.Count; i++)
        {
            string tok = tokens[i];
            char c0 = tok[0];
            if (c0 == '#')
            {
                if (long.TryParse(tok.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out long t))
                    now = t * doc.TimescaleFs;
            }
            else if (c0 is 'b' or 'B')
            {
                if (i + 1 < tokens.Count && byId.TryGetValue(tokens[++i], out var sig))
                    sig.Transitions.Add(new VcdChange(now, tok[1..]));
            }
            else if (c0 is 'r' or 'R' or 's' or 'S')
            {
                i++; // real / string value: skip the identifier as well
            }
            else if (StdLogic.IsValue(c0))
            {
                if (byId.TryGetValue(tok[1..], out var sig))
                    sig.Transitions.Add(new VcdChange(now, c0.ToString()));
            }
            // $dumpvars / $end / $comment markers and anything unknown: ignored.
        }
        return doc;
    }

    private static List<string> Block(List<string> tokens, ref int i)
    {
        var fields = new List<string>();
        while (++i < tokens.Count && tokens[i] != "$end") fields.Add(tokens[i]);
        return fields;
    }

    private static void SkipToEnd(List<string> tokens, ref int i)
    {
        while (++i < tokens.Count && tokens[i] != "$end") { }
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

    private static string StripSubscript(string name)
    {
        int bracket = name.IndexOf('[');
        return bracket >= 0 ? name[..bracket] : name;
    }

    /// <summary><c>1ps</c>, <c>10 ns</c>, … in femtoseconds per VCD unit (the engine's unit table; VCD writes seconds as <c>s</c>).</summary>
    private static long TimescaleFs(string spec)
    {
        int k = 0;
        while (k < spec.Length && (char.IsDigit(spec[k]) || spec[k] == '.')) k++;
        double magnitude = k > 0 && double.TryParse(spec[..k], NumberStyles.Float, CultureInfo.InvariantCulture, out var m) ? m : 1;
        string unit = spec[k..].Trim();
        long unitFs = PhysicalTime.UnitFs.TryGetValue(unit.Equals("s", StringComparison.OrdinalIgnoreCase) ? "sec" : unit, out long u) ? u : PhysicalTime.UnitFs["ps"];
        return Math.Max(1, (long)(magnitude * unitFs));
    }
}
