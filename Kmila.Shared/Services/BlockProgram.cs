using System.Text;
using System.Text.Json;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Bridges the user's Blockly workspace JSON to a pre-baked execution
/// timeline. Phase 1 strategy: compute a canonical fingerprint of the
/// program shape, then look it up in the exercise's pre-baked catalog.
/// If no match, surface a friendly "not pre-baked yet" outcome so the
/// student doesn't hit a dead end.
/// </summary>
/// <remarks>
/// The fingerprint walks the Blockly serialization (produced by
/// <c>kmilaBlocklySerialize</c> on the JS side) in deterministic order
/// and emits one record per block of the form
/// <c>type:field1=value1,field2=value2</c>. Inputs are inlined with
/// brackets; <c>next</c> chains are joined with <c>|</c>. Coordinates
/// and ids are dropped so two students who built the same program in
/// different positions get the same fingerprint.
/// </remarks>
public class BlockProgram
{
    private readonly ILogger _logger;

    public BlockProgram(ILogger<BlockProgram>? logger = null)
    {
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Compute a canonical fingerprint for a Blockly workspace JSON
    /// string. Returns the empty string when the workspace has no blocks
    /// or the JSON is malformed — both cases the page treats as "nothing
    /// to run yet".
    /// </summary>
    public string Fingerprint(string? workspaceJson)
    {
        if (string.IsNullOrWhiteSpace(workspaceJson)) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(workspaceJson);
            if (!doc.RootElement.TryGetProperty("blocks", out var blocksRoot)) return string.Empty;
            if (!blocksRoot.TryGetProperty("blocks", out var topArray)) return string.Empty;
            if (topArray.ValueKind != JsonValueKind.Array || topArray.GetArrayLength() == 0)
                return string.Empty;

            // Top-level blocks: render each and join with double-pipe so a
            // workspace with two disconnected stacks ("recipe" + "garnish")
            // never collides with one continuous stack.
            var groups = new List<string>();
            foreach (var block in topArray.EnumerateArray())
            {
                groups.Add(RenderBlock(block));
            }
            // Top-level groups are NOT order-independent here on purpose:
            // E1 / E2 use only one top-level chain anyway, and for E3 the
            // assignment chain is the canonical container that holds the
            // gate tree as inputs. If we later need a true set semantics
            // we can sort groups before joining.
            return string.Join("||", groups);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BlockProgram] Fingerprint parse failed");
            return string.Empty;
        }
    }

    private static string RenderBlock(JsonElement block)
    {
        var sb = new StringBuilder();
        sb.Append(block.GetProperty("type").GetString() ?? "?");

        // Fields — emit in sorted key order so JSON key ordering can't
        // produce different fingerprints for the same program.
        if (block.TryGetProperty("fields", out var fields) &&
            fields.ValueKind == JsonValueKind.Object)
        {
            var pairs = new List<(string K, string V)>();
            foreach (var f in fields.EnumerateObject())
            {
                pairs.Add((f.Name, f.Value.ValueKind switch
                {
                    JsonValueKind.String => f.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => f.Value.GetRawText(),
                    JsonValueKind.True   => "true",
                    JsonValueKind.False  => "false",
                    _                    => f.Value.GetRawText()
                }));
            }
            pairs.Sort((a, b) => string.CompareOrdinal(a.K, b.K));
            if (pairs.Count > 0)
            {
                sb.Append(':');
                sb.Append(string.Join(',', pairs.Select(p => $"{p.K}={p.V}")));
            }
        }

        // Value / statement inputs — emit `(name=<rendered child>)` for each.
        // Sorted by input name for stability against Blockly's internal order.
        if (block.TryGetProperty("inputs", out var inputs) &&
            inputs.ValueKind == JsonValueKind.Object)
        {
            var inputList = inputs.EnumerateObject().ToList();
            inputList.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (var inp in inputList)
            {
                if (inp.Value.TryGetProperty("block", out var child))
                {
                    sb.Append('(');
                    sb.Append(inp.Name);
                    sb.Append('=');
                    sb.Append(RenderBlock(child));
                    sb.Append(')');
                }
            }
        }

        // Next block in the same stack — render after a pipe.
        if (block.TryGetProperty("next", out var next) &&
            next.TryGetProperty("block", out var nextBlock))
        {
            sb.Append('|');
            sb.Append(RenderBlock(nextBlock));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Look up the pre-baked composition that matches a workspace's
    /// fingerprint. Returns null when nothing in the catalog matches —
    /// callers should render a friendly "I don't know how to run this
    /// combo yet" outcome.
    /// </summary>
    public ExerciseComposition? FindMatch(ExercisePayload payload, string fingerprint)
    {
        if (payload == null || string.IsNullOrEmpty(fingerprint)) return null;
        return payload.Compositions.FirstOrDefault(c =>
            string.Equals(c.Fingerprint, fingerprint, StringComparison.Ordinal));
    }
}
