using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kmila.Shared.Services.VisualRun;

// .kviz schema v1. One sidecar per project canvas; stored alongside the
// design's .vhd via ProjectFiles. Component palette is intentionally tiny
// in Phase 0: led + button only. Add new types here as they ship.

/// <summary>Root of the .kviz sidecar schema (v1): one visualization canvas per project design.</summary>
public sealed class KvizDoc
{
    /// <summary>Schema version of this sidecar document.</summary>
    public int Version { get; set; } = 1;
    /// <summary>Relative path of the design (.vhd) file this canvas is bound to.</summary>
    public string DesignFile { get; set; } = "";
    /// <summary>Canvas dimensions and background settings.</summary>
    public KvizCanvas Canvas { get; set; } = new();
    /// <summary>Placed visualization components (widgets) on the canvas.</summary>
    public List<KvizComponent> Components { get; set; } = new();
    /// <summary>Playback and buffer settings for the visual run.</summary>
    public KvizSettings Settings { get; set; } = new();
}

/// <summary>Canvas dimensions and background style for a .kviz document.</summary>
public sealed class KvizCanvas
{
    /// <summary>Canvas width in pixels.</summary>
    public int Width { get; set; } = 1200;
    /// <summary>Canvas height in pixels.</summary>
    public int Height { get; set; } = 800;
    /// <summary>Background style identifier (e.g. "grid").</summary>
    public string Background { get; set; } = "grid";
}

/// <summary>A single visualization component (widget) placed on the canvas.</summary>
public sealed class KvizComponent
{
    /// <summary>Unique component identifier within the document.</summary>
    public string Id { get; set; } = "";
    /// <summary>Widget type name (e.g. "led", "button").</summary>
    public string Type { get; set; } = "";
    /// <summary>Position of the component on the canvas.</summary>
    public KvizPosition Position { get; set; } = new();
    /// <summary>Type-specific configuration values, kept as raw JSON for forward-compatibility.</summary>
    public Dictionary<string, JsonElement> Config { get; set; } = new();
    /// <summary>Maps each pin name to a bound design port (whole port name for Phase 0; slice syntax <c>name[hi:lo]</c> allowed in Phase 1).</summary>
    public Dictionary<string, string> Bindings { get; set; } = new();
}

/// <summary>Two-dimensional canvas position for a component.</summary>
public sealed class KvizPosition
{
    /// <summary>Horizontal coordinate.</summary>
    public double X { get; set; }
    /// <summary>Vertical coordinate.</summary>
    public double Y { get; set; }
}

/// <summary>Playback and buffer settings for a visual run.</summary>
public sealed class KvizSettings
{
    /// <summary>Number of ticks retained in the signal ring buffer.</summary>
    public int RingBufferTicks { get; set; } = 2000;
    /// <summary>Legacy enum member name (Half/Normal/Fast/Free). Kept for back-compat — older sidecars only carry this. Superseded by <see cref="DefaultTickDelayMs"/>.</summary>
    public string DefaultSpeed { get; set; } = "Normal";
    /// <summary>Playback pacing in ms/tick (0 = Free). Null when absent so old sidecars fall back to <see cref="DefaultSpeed"/>; source of truth once written.</summary>
    public int? DefaultTickDelayMs { get; set; }
}

/// <summary>JSON (de)serialization helpers for <see cref="KvizDoc"/> and typed config accessors.</summary>
public static class KvizSerializer
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serializes a <see cref="KvizDoc"/> to its JSON string form.</summary>
    /// <param name="doc">The document to serialize.</param>
    /// <returns>The JSON representation of the document.</returns>
    public static string Serialize(KvizDoc doc) => JsonSerializer.Serialize(doc, Opts);

    /// <summary>Deserializes a .kviz JSON string, returning null on empty or malformed input.</summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed document, or null if the input is empty or invalid.</returns>
    public static KvizDoc? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<KvizDoc>(json, Opts); }
        catch { return null; }
    }

    // Convenience helpers for typed config access. JsonElement keeps the
    // schema forward-compatible without losing unknown keys on round-trip.
    /// <summary>Reads a string value from a config dictionary, returning a fallback when absent or not a string.</summary>
    /// <param name="cfg">The config dictionary.</param>
    /// <param name="key">The key to read.</param>
    /// <param name="fallback">Value returned when the key is missing or not a string.</param>
    /// <returns>The stored string, or the fallback.</returns>
    public static string GetString(this Dictionary<string, JsonElement> cfg, string key, string fallback = "")
        => cfg.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? fallback) : fallback;

    /// <summary>Reads a boolean value from a config dictionary, returning a fallback when absent or not a boolean.</summary>
    /// <param name="cfg">The config dictionary.</param>
    /// <param name="key">The key to read.</param>
    /// <param name="fallback">Value returned when the key is missing or not a boolean.</param>
    /// <returns>The stored boolean, or the fallback.</returns>
    public static bool GetBool(this Dictionary<string, JsonElement> cfg, string key, bool fallback = false)
        => cfg.TryGetValue(key, out var el) && (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False)
            ? el.GetBoolean() : fallback;

    /// <summary>Reads an integer value from a config dictionary, returning a fallback when absent or not an integer.</summary>
    /// <param name="cfg">The config dictionary.</param>
    /// <param name="key">The key to read.</param>
    /// <param name="fallback">Value returned when the key is missing or not an integer.</param>
    /// <returns>The stored integer, or the fallback.</returns>
    public static int GetInt(this Dictionary<string, JsonElement> cfg, string key, int fallback = 0)
        => cfg.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)
            ? v : fallback;

    /// <summary>Stores a string value into a config dictionary.</summary>
    /// <param name="cfg">The config dictionary.</param>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value to store.</param>
    public static void Put(this Dictionary<string, JsonElement> cfg, string key, string value)
        => cfg[key] = JsonSerializer.SerializeToElement(value, Opts);

    /// <summary>Stores a boolean value into a config dictionary.</summary>
    /// <param name="cfg">The config dictionary.</param>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value to store.</param>
    public static void Put(this Dictionary<string, JsonElement> cfg, string key, bool value)
        => cfg[key] = JsonSerializer.SerializeToElement(value, Opts);

    /// <summary>Stores an integer value into a config dictionary.</summary>
    /// <param name="cfg">The config dictionary.</param>
    /// <param name="key">The key to write.</param>
    /// <param name="value">The value to store.</param>
    public static void Put(this Dictionary<string, JsonElement> cfg, string key, int value)
        => cfg[key] = JsonSerializer.SerializeToElement(value, Opts);
}
