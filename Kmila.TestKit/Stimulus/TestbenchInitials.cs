using System.Text.RegularExpressions;

using Kmila.Shared.Services.Api;

namespace Kmila.TestKit.Stimulus;

/// <summary>
/// Initial values of the testbench signals that drive the DUT's inputs
/// (<c>signal clk, rst : std_logic := '0';</c>). GHDL starts each input port at its
/// actual's initial value, so a <c>clk &lt;= not clk</c> at t=0 is a real rising edge;
/// the golden VCD only shows the settled value at <c>#0</c>, so the bench passes these
/// to Kmila as port initial values (before time 0, no event).
/// </summary>
public static class TestbenchInitials
{
    private static readonly Regex SignalDecl = new(
        @"\bsignal\s+(?<names>[A-Za-z_][\w\s,]*?)\s*:\s*[^;:]+?:=\s*(?<value>[^;]+);",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PortMap = new(@"\bport\s+map\s*\((?<assocs>[^;]*?)\)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>(port, value) for every input port whose testbench signal of the same name has an initializer.</summary>
    public static List<ApiPortInitial> Read(IEnumerable<string> sources, IEnumerable<(string Name, int Width)> inputs)
    {
        string text = string.Concat(sources.Where(File.Exists).Select(f => Parser.Models.VhdlText.StripLineComments(File.ReadAllText(f)) + "\n"));
        // Testbench signal → initial value.
        var initials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in SignalDecl.Matches(text))
            foreach (var name in m.Groups["names"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                initials.TryAdd(name, m.Groups["value"].Value.Trim());
        // DUT port → testbench actual (`clk_i => clk_t`); unmapped ports match by name.
        var actuals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in PortMap.Matches(text))
            foreach (var assoc in m.Groups["assocs"].Value.Split(','))
                if (assoc.Split("=>") is [var formal, var actual])
                    actuals.TryAdd(formal.Trim(), actual.Trim());
        var result = new List<ApiPortInitial>();
        foreach (var port in inputs)
        {
            string signal = actuals.TryGetValue(port.Name, out var a) ? a : port.Name;
            if (initials.TryGetValue(signal, out var text0) && ValueOf(text0, port.Width) is { } value)
                result.Add(new ApiPortInitial { Port = port.Name, Value = value });
        }
        return result;
    }

    /// <summary><c>'0'</c>, <c>"0101"</c> or <c>(others =&gt; '0')</c> as bits; null for anything else.</summary>
    private static string? ValueOf(string text, int width)
    {
        if (Regex.Match(text, @"^'([01UXZWLH-])'$", RegexOptions.IgnoreCase) is { Success: true } c) return c.Groups[1].Value.ToUpperInvariant();
        if (Regex.Match(text, @"^""([01UXZWLH-]+)""$", RegexOptions.IgnoreCase) is { Success: true } v) return v.Groups[1].Value.ToUpperInvariant();
        if (Regex.Match(text, @"^\(\s*others\s*=>\s*'([01UXZWLH-])'\s*\)$", RegexOptions.IgnoreCase) is { Success: true } o)
            return new string(char.ToUpperInvariant(o.Groups[1].Value[0]), Math.Max(1, width));
        return null;
    }
}
