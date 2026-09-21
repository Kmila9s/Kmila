using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kmila.Shared.Services.VisualRun;

// .kviz schema v1. One sidecar per project canvas; stored alongside the
// design's .vhd via ProjectFiles. Component palette is intentionally tiny
// in Phase 0: led + button only. Add new types here as they ship.

public sealed class KvizDoc
{
    public int Version { get; set; } = 1;
    public string DesignFile { get; set; } = "";
    public KvizCanvas Canvas { get; set; } = new();
    public List<KvizComponent> Components { get; set; } = new();
    public KvizSettings Settings { get; set; } = new();
}

public sealed class KvizCanvas
{
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;
    public string Background { get; set; } = "grid";
}

public sealed class KvizComponent
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public KvizPosition Position { get; set; } = new();
    public Dictionary<string, JsonElement> Config { get; set; } = new();
    // Pin-name → bound design port (whole port name for Phase 0; slice
    // syntax `name[hi:lo]` allowed in Phase 1).
    public Dictionary<string, string> Bindings { get; set; } = new();
}

public sealed class KvizPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class KvizSettings
{
    public int RingBufferTicks { get; set; } = 2000;
    // Legacy: enum member name (Half/Normal/Fast/Free). Kept for back-compat —
    // older sidecars only carry this. Superseded by DefaultTickDelayMs.
    public string DefaultSpeed { get; set; } = "Normal";
    // Playback pacing in ms/tick (0 = Free). Nullable + WhenWritingNull so old
    // sidecars that lack the key deserialize to null and fall back to
    // DefaultSpeed. This is the source of truth once written.
    public int? DefaultTickDelayMs { get; set; }
}

public static class KvizSerializer
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(KvizDoc doc) => JsonSerializer.Serialize(doc, Opts);

    public static KvizDoc? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<KvizDoc>(json, Opts); }
        catch { return null; }
    }

    // Convenience helpers for typed config access. JsonElement keeps the
    // schema forward-compatible without losing unknown keys on round-trip.
    public static string GetString(this Dictionary<string, JsonElement> cfg, string key, string fallback = "")
        => cfg.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? fallback) : fallback;

    public static bool GetBool(this Dictionary<string, JsonElement> cfg, string key, bool fallback = false)
        => cfg.TryGetValue(key, out var el) && (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False)
            ? el.GetBoolean() : fallback;

    public static int GetInt(this Dictionary<string, JsonElement> cfg, string key, int fallback = 0)
        => cfg.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)
            ? v : fallback;

    public static void Put(this Dictionary<string, JsonElement> cfg, string key, string value)
        => cfg[key] = JsonSerializer.SerializeToElement(value, Opts);

    public static void Put(this Dictionary<string, JsonElement> cfg, string key, bool value)
        => cfg[key] = JsonSerializer.SerializeToElement(value, Opts);

    public static void Put(this Dictionary<string, JsonElement> cfg, string key, int value)
        => cfg[key] = JsonSerializer.SerializeToElement(value, Opts);
}
