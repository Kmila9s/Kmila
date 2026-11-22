using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kmila.TestKit.Cases;

/// <summary>A catalogued failure of one variant of a case: why Kmila does not match yet, and since when.</summary>
public sealed class KnownGap
{
    [JsonPropertyName("case")] public string Case { get; set; } = "";
    [JsonPropertyName("variant")] public string Variant { get; set; } = "";
    /// <summary><c>parser</c>, <c>package</c>, <c>elaboration</c>, <c>semantics</c> or <c>timing</c>.</summary>
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    /// <summary>Gap id (G84…) and summary.</summary>
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("since")] public string Since { get; set; } = "";
}

/// <summary>
/// The ratchet over <c>known-gaps.json</c>: the list may only shrink. A run fails
/// when a variant fails without being listed (a regression or a new gap) and when
/// a listed variant passes (fixed: remove the entry).
/// </summary>
public static class KnownGaps
{
    /// <summary>Reads the list (a missing file is an empty list).</summary>
    public static List<KnownGap> Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<List<KnownGap>>(File.ReadAllText(path)) ?? new() : new();

    /// <summary>
    /// Applies the ratchet to the variants that ran: the violations (empty means the
    /// run agrees with the list) and, per case, the listed reasons of its failing variants.
    /// </summary>
    public static (List<string> Violations, Dictionary<string, string> ReasonsByCase) Apply(
        IEnumerable<KnownGap> listed, IEnumerable<(string Case, string Variant, bool Pass)> results)
    {
        var gaps = listed.ToDictionary(g => (g.Case, g.Variant));
        var violations = new List<string>();
        var reasons = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (id, variant, pass) in results)
        {
            bool known = gaps.TryGetValue((id, variant), out var gap);
            if (!pass && !known) violations.Add($"{id}/{variant}: fails and is not in known-gaps.json");
            if (pass && known) violations.Add($"{id}/{variant}: passes but is still in known-gaps.json (remove it)");
            if (!pass && known) (reasons.TryGetValue(id, out var list) ? list : reasons[id] = new()).Add(gap!.Reason);
        }
        return (violations, reasons.ToDictionary(kv => kv.Key, kv => string.Join("; ", kv.Value)));
    }
}
