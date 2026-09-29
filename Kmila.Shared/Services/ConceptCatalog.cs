using System.Text.Json;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Loads the Concepts curriculum manifest + per-lesson Markdown bodies +
/// pre-recorded execution timelines. Sibling to <see cref="LessonCatalog"/>;
/// kept as a separate class (rather than parameterizing Learn's catalog) so
/// the Concepts track can evolve its schema without risking a regression in
/// the Learn pipeline.
/// </summary>
/// <remarks>
/// File layout under <c>wwwroot/Concepts/</c>:
/// <code>
/// Concepts/
/// ├── manifest.json
/// ├── en/{moduleId}/{conceptId}.md
/// ├── en/{moduleId}/{conceptId}.timeline.json   (optional sidecar)
/// └── es/… fr/… de/… ch/… jp/… ar/…
/// </code>
/// Both the manifest and per-language assets are cached for the service
/// lifetime (registered as a singleton on both hosts).
/// </remarks>
public class ConceptCatalog
{
    private readonly Func<string, Task<Stream>> _open;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private ConceptManifest? _manifest;
    private readonly Dictionary<string, string> _bodyCache = new();
    private readonly Dictionary<string, ExecutionTimelineDoc?> _timelineCache = new();
    private readonly Dictionary<string, ExercisePayload?> _exerciseCache = new();

    /// <summary>Creates the catalog with its resource-open delegate and an optional logger.</summary>
    /// <param name="openFileAsync">Delegate that opens a bundled resource stream by path.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public ConceptCatalog(Func<string, Task<Stream>> openFileAsync,
                          ILogger<ConceptCatalog>? logger = null)
    {
        _open = openFileAsync;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Returns the loaded manifest. First call loads + caches; subsequent
    /// calls are free. Returns an empty manifest on failure so the Concepts
    /// catalog page renders an empty state instead of crashing.
    /// </summary>
    public async Task<ConceptManifest> GetManifestAsync()
    {
        if (_manifest != null) return _manifest;
        await _loadGate.WaitAsync();
        try
        {
            if (_manifest != null) return _manifest;
            _manifest = await LoadManifestAsync();
            return _manifest;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task<ConceptManifest> LoadManifestAsync()
    {
        try
        {
            using var stream = await _open("Concepts/manifest.json");
            var manifest = await JsonSerializer.DeserializeAsync<ConceptManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });
            if (manifest == null)
            {
                _logger.LogWarning("[ConceptCatalog] manifest.json deserialized to null — empty catalog");
                return new ConceptManifest();
            }
            _logger.LogInformation("[ConceptCatalog] Loaded manifest with {Count} modules",
                manifest.Modules.Count);
            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConceptCatalog] Failed to load manifest.json");
            return new ConceptManifest();
        }
    }

    /// <summary>Markdown body in <paramref name="lang"/>, with EN fallback.</summary>
    public async Task<string?> GetConceptBodyAsync(string moduleId, string conceptId, string lang)
    {
        var key = $"{lang}|{moduleId}|{conceptId}";
        if (_bodyCache.TryGetValue(key, out var cached)) return cached;

        var body = await TryReadBodyAsync(moduleId, conceptId, lang)
                ?? (lang != "en" ? await TryReadBodyAsync(moduleId, conceptId, "en") : null);

        if (body != null) _bodyCache[key] = body;
        return body;
    }

    /// <summary>
    /// Body + metadata about which language we actually served. When we fall
    /// back to English the caller surfaces a "translation missing" banner.
    /// </summary>
    public async Task<(string? Body, string ServedLang, bool FellBack)> GetConceptBodyWithSourceAsync(
        string moduleId, string conceptId, string lang)
    {
        var direct = await TryReadBodyAsync(moduleId, conceptId, lang);
        if (direct != null) return (direct, lang, FellBack: false);

        if (lang != "en")
        {
            var en = await TryReadBodyAsync(moduleId, conceptId, "en");
            if (en != null) return (en, "en", FellBack: true);
        }

        return (null, lang, FellBack: false);
    }

    private async Task<string?> TryReadBodyAsync(string moduleId, string conceptId, string lang)
    {
        var path = $"Concepts/{lang}/{moduleId}/{conceptId}.md";
        try
        {
            using var stream = await _open(path);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads the pre-recorded execution timeline for a concept, with EN
    /// fallback. Returns null when no timeline exists (Reading-kind lessons).
    /// </summary>
    public async Task<ExecutionTimelineDoc?> GetTimelineAsync(string moduleId, string conceptId, string lang)
    {
        var key = $"{lang}|{moduleId}|{conceptId}";
        if (_timelineCache.TryGetValue(key, out var cached)) return cached;

        var doc = await TryReadTimelineAsync(moduleId, conceptId, lang)
               ?? (lang != "en" ? await TryReadTimelineAsync(moduleId, conceptId, "en") : null);

        _timelineCache[key] = doc;
        return doc;
    }

    private async Task<ExecutionTimelineDoc?> TryReadTimelineAsync(string moduleId, string conceptId, string lang)
    {
        var path = $"Concepts/{lang}/{moduleId}/{conceptId}.timeline.json";
        try
        {
            using var stream = await _open(path);
            return await JsonSerializer.DeserializeAsync<ExecutionTimelineDoc>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Resolve a module by id (null if not in the manifest).</summary>
    public async Task<ConceptModule?> GetModuleAsync(string moduleId)
    {
        var manifest = await GetManifestAsync();
        return manifest.Modules.FirstOrDefault(m =>
            string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Resolve a (module, concept) pair; null if either is missing.</summary>
    public async Task<(ConceptModule Module, Concept Concept)?> GetConceptAsync(string moduleId, string conceptId)
    {
        var module = await GetModuleAsync(moduleId);
        if (module == null) return null;
        var concept = module.Lessons.FirstOrDefault(l =>
            string.Equals(l.Id, conceptId, StringComparison.OrdinalIgnoreCase));
        return concept == null ? null : (module, concept);
    }

    /// <summary>
    /// Loads the on-disk payload for one Scratch-style exercise: the
    /// Blockly toolbox + pre-baked composition catalog. Cached for the
    /// service lifetime. Returns null if the file is missing or malformed.
    /// </summary>
    public async Task<ExercisePayload?> GetExercisePayloadAsync(string exerciseId)
    {
        if (_exerciseCache.TryGetValue(exerciseId, out var cached)) return cached;
        var path = $"Concepts/exercises/{exerciseId}.exercise.json";
        try
        {
            using var stream = await _open(path);
            var payload = await JsonSerializer.DeserializeAsync<ExercisePayload>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });
            _exerciseCache[exerciseId] = payload;
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ConceptCatalog] Failed to load exercise payload {Id}", exerciseId);
            _exerciseCache[exerciseId] = null;
            return null;
        }
    }

    /// <summary>
    /// Resolve a (module, exercise) pair. Returns null if either is missing.
    /// </summary>
    public async Task<(ConceptModule Module, ConceptExercise Exercise)?> GetExerciseAsync(string exerciseId)
    {
        var manifest = await GetManifestAsync();
        foreach (var m in manifest.Modules)
        {
            var ex = m.Exercises.FirstOrDefault(e =>
                string.Equals(e.Id, exerciseId, StringComparison.OrdinalIgnoreCase));
            if (ex != null) return (m, ex);
        }
        return null;
    }

    /// <summary>
    /// Same i18n-pick semantics as <see cref="LessonCatalog.Localize"/>:
    /// requested lang → <c>en</c> → first non-empty value → fallback.
    /// </summary>
    public static string Localize(IDictionary<string, string> values, string lang, string fallback = "")
    {
        if (values == null || values.Count == 0) return fallback;
        if (values.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
        if (values.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en)) return en;
        var first = values.Values.FirstOrDefault(x => !string.IsNullOrEmpty(x));
        return first ?? fallback;
    }

    /// <summary>
    /// Same as <see cref="Localize"/> but prepends a ⚠ marker when the
    /// requested language wasn't available and we fell back to English
    /// (or another language). Matches the project-wide
    /// translation-pending convention so untranslated notes are visibly
    /// flagged in the UI instead of silently passing through.
    /// </summary>
    public static string LocalizeWithFallbackWarning(IDictionary<string, string> values, string lang, string fallback = "")
    {
        if (values == null || values.Count == 0) return fallback;
        if (values.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
        // Fall back to English (or first non-empty) and warn.
        if (values.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en))
            return lang == "en" ? en : "⚠ " + en;
        var first = values.Values.FirstOrDefault(x => !string.IsNullOrEmpty(x));
        if (string.IsNullOrEmpty(first)) return fallback;
        return lang == "en" ? first : "⚠ " + first;
    }
}
