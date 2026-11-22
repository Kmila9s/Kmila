using System.Security.Cryptography;
using System.Text;

namespace Kmila.TestKit;

/// <summary>
/// The golden master: SHA-256 of each case's Kmila VCD without its
/// <c>$date … $end</c> block, one <c>&lt;sha&gt;  &lt;case&gt;</c> line per case
/// (the <c>bench-kmila-vcd.sha256</c> files under <c>Tests/baseline/</c>).
/// </summary>
public static class GoldenHashes
{
    /// <summary>Hex SHA-256 of <paramref name="vcd"/> without its <c>$date</c> block.</summary>
    public static string Of(string vcd) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(WithoutDate(vcd))));

    /// <summary><paramref name="vcd"/> without a leading <c>$date … $end</c> block (the only part that changes between identical runs).</summary>
    public static string WithoutDate(string vcd)
    {
        if (!vcd.StartsWith("$date", StringComparison.Ordinal)) return vcd;
        int end = vcd.IndexOf("$end", StringComparison.Ordinal);
        if (end < 0) return vcd;
        int next = vcd.IndexOf('\n', end);
        return next >= 0 ? vcd[(next + 1)..] : "";
    }

    /// <summary>The hash file text for <paramref name="hashes"/> (case → hash), ordered by case.</summary>
    public static string Format(IEnumerable<KeyValuePair<string, string>> hashes) =>
        string.Concat(hashes.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Value}  {kv.Key}\n"));

    /// <summary>Reads a hash file into case → hash.</summary>
    public static Dictionary<string, string> Parse(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int sep = line.IndexOf("  ", StringComparison.Ordinal);
            if (sep > 0) map[line[(sep + 2)..].Trim()] = line[..sep];
        }
        return map;
    }

    /// <summary>
    /// Differences between a baseline and the current hashes: a changed hash,
    /// a case missing now, or a case not in the baseline. Empty means identical.
    /// </summary>
    public static List<string> Compare(IReadOnlyDictionary<string, string> baseline, IReadOnlyDictionary<string, string> current)
    {
        var problems = new List<string>();
        foreach (var (id, hash) in baseline.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (!current.TryGetValue(id, out var now)) problems.Add($"{id}: missing (no VCD)");
            else if (now != hash) problems.Add($"{id}: changed");
        }
        foreach (var id in current.Keys.Except(baseline.Keys).Order(StringComparer.Ordinal))
            problems.Add($"{id}: not in the baseline");
        return problems;
    }
}
