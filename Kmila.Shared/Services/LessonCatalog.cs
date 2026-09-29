using System.Text.Json;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Loads the Learn curriculum manifest + per-lesson Markdown bodies.
/// Follows the same stream-delegate pattern as <see cref="Traductor"/> so a
/// single implementation works on both the Blazor Web host (file system under
/// <c>_content/Kmila.Shared/Learn</c>) and MAUI Blazor Hybrid
/// (<c>FileSystem.OpenAppPackageFileAsync</c>).
/// </summary>
/// <remarks>
/// The manifest is loaded once and cached; lesson bodies are fetched on demand
/// and cached by <c>(moduleId, lessonId, lang)</c> triple so switching
/// languages on a lesson page doesn't refetch. Both caches survive for the
/// service lifetime (registered as a singleton).
/// </remarks>
public class LessonCatalog
{
    private readonly Func<string, Task<Stream>> _open;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private LessonManifest? _manifest;
    private readonly Dictionary<string, string> _bodyCache = new();
    private readonly Dictionary<string, ExecutionTimelineDoc?> _timelineCache = new();
    private readonly Dictionary<string, GateAnimationDoc?> _gatesCache = new();
    private readonly Dictionary<string, FsmAnimationDoc?> _fsmCache = new();

    /// <summary>Creates the catalog with a delegate for opening bundled files.</summary>
    /// <param name="openFileAsync">Opens a bundled asset by relative path.</param>
    /// <param name="logger">Optional logger.</param>
    public LessonCatalog(Func<string, Task<Stream>> openFileAsync,
                         ILogger<LessonCatalog>? logger = null)
    {
        _open = openFileAsync;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Returns the loaded manifest. First call loads + caches; subsequent
    /// calls are free. Returns an empty manifest if the file can't be read
    /// (so the Learn page renders an empty state instead of crashing).
    /// </summary>
    public async Task<LessonManifest> GetManifestAsync()
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

    private async Task<LessonManifest> LoadManifestAsync()
    {
        try
        {
            using var stream = await _open("Learn/manifest.json");
            var manifest = await JsonSerializer.DeserializeAsync<LessonManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new JsonStringEnumConverter() }
                });
            if (manifest == null)
            {
                _logger.LogWarning("[LessonCatalog] manifest.json deserialized to null — empty catalog");
                return new LessonManifest();
            }
            _logger.LogInformation("[LessonCatalog] Loaded manifest with {Count} modules",
                manifest.Modules.Count);
            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonCatalog] Failed to load manifest.json");
            return new LessonManifest();
        }
    }

    /// <summary>
    /// Returns the Markdown body for a lesson in the requested language,
    /// falling back to English when the localized file is missing. Null if
    /// neither is available.
    /// </summary>
    public async Task<string?> GetLessonBodyAsync(string moduleId, string lessonId, string lang)
    {
        var key = $"{lang}|{moduleId}|{lessonId}";
        if (_bodyCache.TryGetValue(key, out var cached)) return cached;

        var body = await TryReadBodyAsync(moduleId, lessonId, lang)
                ?? (lang != "en" ? await TryReadBodyAsync(moduleId, lessonId, "en") : null);

        if (body != null) _bodyCache[key] = body;
        return body;
    }

    /// <summary>
    /// Fetches the body plus metadata about which language was actually
    /// served. When <paramref name="lang"/> had no file and we fell back to
    /// English, the caller knows to surface a "translation missing" banner
    /// instead of silently swapping the content out from under the user.
    /// </summary>
    public async Task<(string? Body, string ServedLang, bool FellBack)> GetLessonBodyWithSourceAsync(
        string moduleId, string lessonId, string lang)
    {
        // Direct hit
        var direct = await TryReadBodyAsync(moduleId, lessonId, lang);
        if (direct != null) return (direct, lang, FellBack: false);

        // Fallback to English
        if (lang != "en")
        {
            var en = await TryReadBodyAsync(moduleId, lessonId, "en");
            if (en != null) return (en, "en", FellBack: true);
        }

        return (null, lang, FellBack: false);
    }

    private async Task<string?> TryReadBodyAsync(string moduleId, string lessonId, string lang)
    {
        var path = $"Learn/{lang}/{moduleId}/{lessonId}.md";
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
    /// Loads the optional pre-recorded execution timeline for a lesson,
    /// with EN fallback. Returns null when no <c>.timeline.json</c> sits
    /// next to the lesson's Markdown body. Used by <c>LessonView</c> to
    /// render an inline <c>ExecutionPlayer</c> when the lesson opts in
    /// via <see cref="Lesson.InlinePlayer"/>.
    /// </summary>
    public async Task<ExecutionTimelineDoc?> GetTimelineAsync(string moduleId, string lessonId, string lang)
    {
        var key = $"{lang}|{moduleId}|{lessonId}";
        if (_timelineCache.TryGetValue(key, out var cached)) return cached;

        var doc = await TryReadTimelineAsync(moduleId, lessonId, lang)
               ?? (lang != "en" ? await TryReadTimelineAsync(moduleId, lessonId, "en") : null);

        _timelineCache[key] = doc;
        return doc;
    }

    private async Task<ExecutionTimelineDoc?> TryReadTimelineAsync(string moduleId, string lessonId, string lang)
    {
        var path = $"Learn/{lang}/{moduleId}/{lessonId}.timeline.json";
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

    /// <summary>
    /// Loads the optional gate-animation sidecar for a lesson, with EN
    /// fallback. Returns null when no <c>.gates.json</c> sits next to the
    /// lesson's Markdown body. Used by <c>LessonView</c> to render an inline
    /// <c>GateAnimator</c> when the lesson opts in via
    /// <see cref="Lesson.GateAnimation"/>.
    /// </summary>
    public async Task<GateAnimationDoc?> GetGateAnimationAsync(string moduleId, string lessonId, string lang)
    {
        var key = $"{lang}|{moduleId}|{lessonId}";
        if (_gatesCache.TryGetValue(key, out var cached)) return cached;

        var doc = await TryReadGatesAsync(moduleId, lessonId, lang)
               ?? (lang != "en" ? await TryReadGatesAsync(moduleId, lessonId, "en") : null);

        _gatesCache[key] = doc;
        return doc;
    }

    private async Task<GateAnimationDoc?> TryReadGatesAsync(string moduleId, string lessonId, string lang)
    {
        var path = $"Learn/{lang}/{moduleId}/{lessonId}.gates.json";
        try
        {
            using var stream = await _open(path);
            return await JsonSerializer.DeserializeAsync<GateAnimationDoc>(
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

    /// <summary>
    /// Loads the optional FSM-animation sidecar for a lesson, with EN
    /// fallback. Mirrors <see cref="GetGateAnimationAsync"/>'s shape so the
    /// loader semantics stay consistent across both player surfaces.
    /// </summary>
    public async Task<FsmAnimationDoc?> GetFsmAnimationAsync(string moduleId, string lessonId, string lang)
    {
        var key = $"{lang}|{moduleId}|{lessonId}";
        if (_fsmCache.TryGetValue(key, out var cached)) return cached;

        var doc = await TryReadFsmAsync(moduleId, lessonId, lang)
               ?? (lang != "en" ? await TryReadFsmAsync(moduleId, lessonId, "en") : null);

        _fsmCache[key] = doc;
        return doc;
    }

    private async Task<FsmAnimationDoc?> TryReadFsmAsync(string moduleId, string lessonId, string lang)
    {
        var path = $"Learn/{lang}/{moduleId}/{lessonId}.fsm.json";
        try
        {
            using var stream = await _open(path);
            return await JsonSerializer.DeserializeAsync<FsmAnimationDoc>(
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
    public async Task<LessonModule?> GetModuleAsync(string moduleId)
    {
        var manifest = await GetManifestAsync();
        return manifest.Modules.FirstOrDefault(m =>
            string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Resolve a (module, lesson) pair; null if either is missing.</summary>
    public async Task<(LessonModule Module, Lesson Lesson)?> GetLessonAsync(string moduleId, string lessonId)
    {
        var module = await GetModuleAsync(moduleId);
        if (module == null) return null;
        var lesson = module.Lessons.FirstOrDefault(l =>
            string.Equals(l.Id, lessonId, StringComparison.OrdinalIgnoreCase));
        return lesson == null ? null : (module, lesson);
    }

    /// <summary>
    /// Picks the best available localized string from a dictionary keyed by
    /// language code. Falls back to <c>en</c>, then to the first value, then
    /// to <paramref name="fallback"/>.
    /// </summary>
    public static string Localize(IDictionary<string, string> values, string lang, string fallback = "")
    {
        if (values == null || values.Count == 0) return fallback;
        if (values.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
        if (values.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en)) return en;
        var first = values.Values.FirstOrDefault(x => !string.IsNullOrEmpty(x));
        return first ?? fallback;
    }
}

/// <summary>
/// Local alias for <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>
/// so <see cref="LessonCatalog"/>'s manifest deserialisation reads tidily without
/// fully-qualifying the BCL converter name on every reference.
/// </summary>
internal sealed class JsonStringEnumConverter : System.Text.Json.Serialization.JsonStringEnumConverter { }
