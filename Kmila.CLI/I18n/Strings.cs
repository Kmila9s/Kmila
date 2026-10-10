using System.Text.Json;
using Kmila.CLI.Cli;

namespace Kmila.CLI.I18n;

/// <summary>
/// Minimal, framework-light localization for the CLI. Loads the same flat
/// <c>key → string</c> dictionaries the Blazor app ships
/// (<c>Kmila.Shared/wwwroot/Dictionaries/{es,en}.json</c>) so CLI strings stay
/// in sync with the app's existing keys. Defaults to Spanish (the user's
/// language); <c>--lang en</c> switches. Unknown keys fall back to the key
/// itself so a missing translation is visible but never crashes.
/// </summary>
public sealed class Strings
{
    private readonly Dictionary<string, string> _entries;

    /// <summary>The active language code (e.g. "es" or "en").</summary>
    public string Lang { get; }

    private Strings(string lang, Dictionary<string, string> entries)
    {
        Lang = lang;
        _entries = entries;
    }

    /// <summary>
    /// Builds a <see cref="Strings"/> for the requested language. Falls back to
    /// an empty table (keys echo through) when the dictionary files can't be
    /// located, so the CLI still runs on a trimmed deployment.
    /// </summary>
    /// <param name="lang">Language code; defaults to "es" when null/blank.</param>
    public static Strings Load(string? lang)
    {
        lang = string.IsNullOrWhiteSpace(lang) ? "es" : lang.Trim().ToLowerInvariant();
        var dir = ResourcePaths.Dictionaries();
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        if (dir != null)
        {
            // Load English first as a base, then overlay the target language so
            // any key missing a localized value still resolves to something.
            TryOverlay(entries, Path.Combine(dir, "en.json"));
            if (lang != "en")
                TryOverlay(entries, Path.Combine(dir, $"{lang}.json"));
        }
        return new Strings(lang, entries);
    }

    private static void TryOverlay(Dictionary<string, string> into, string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            using var fs = File.OpenRead(path);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(fs);
            if (map == null) return;
            foreach (var (k, v) in map) into[k] = v;
        }
        catch
        {
            // A malformed dictionary must not take the whole CLI down — keys
            // simply echo through until the file is fixed.
        }
    }

    /// <summary>Looks up a key, returning the key itself when it has no translation.</summary>
    public string T(string key) => _entries.TryGetValue(key, out var v) ? v : key;

    /// <summary>Looks up a key and formats it with <paramref name="args"/> (<see cref="string.Format(string,object?[])"/>).</summary>
    public string T(string key, params object[] args)
    {
        var fmt = T(key);
        try { return string.Format(fmt, args); }
        catch (FormatException) { return fmt; }
    }
}
