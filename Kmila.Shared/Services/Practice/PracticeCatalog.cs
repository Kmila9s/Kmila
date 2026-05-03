using System.Text.Json;
using System.Text.Json.Serialization;
using Kmila.Shared.Models;
using Kmila.Shared.Models.Practice;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Practice;

/// <summary>
/// Loads the Practice catalog manifest + per-exercise assets (solution VHDL,
/// starter VHDL, tests.json, statement.{lang}.md) from disk. Mirrors the
/// stream-delegate pattern used by <see cref="LessonCatalog"/> so the same
/// implementation works on the Blazor Web host (file-system path candidates)
/// and on MAUI Blazor Hybrid (<c>FileSystem.OpenAppPackageFileAsync</c>).
/// </summary>
/// <remarks>
/// Two-stage caching:
///   • manifest.json is loaded once and cached for the service lifetime.
///   • per-exercise full payloads are loaded on first <see cref="GetAsync"/>
///     and cached by id. Switching the UI language doesn't trigger a reload —
///     statements for every available language are pre-loaded into the
///     <c>Statements</c> dictionary on the first <see cref="GetAsync"/> call.
/// </remarks>
public class PracticeCatalog
{
    private static readonly string[] KnownLangs = { "en", "es", "fr", "de", "ar", "ch", "jp" };

    private readonly Func<string, Task<Stream>> _open;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _manifestGate = new(1, 1);
    private readonly SemaphoreSlim _exerciseGate = new(1, 1);
    private List<PracticeExerciseMeta>? _manifest;
    private Dictionary<string, ManifestEntry>? _manifestRaw;
    private readonly Dictionary<string, PracticeExercise> _exerciseCache = new();

    public PracticeCatalog(Func<string, Task<Stream>> openFileAsync,
                           ILogger<PracticeCatalog>? logger = null)
    {
        _open = openFileAsync;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Best-effort localized lookup with English fallback, then first non-empty value.</summary>
    public static string Localize(IDictionary<string, string> values, string lang, string fallback = "")
    {
        if (values == null || values.Count == 0) return fallback;
        if (values.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
        if (values.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en)) return en;
        var first = values.Values.FirstOrDefault(x => !string.IsNullOrEmpty(x));
        return first ?? fallback;
    }

    /// <summary>Returns the catalog metadata. Cheap after the first call.</summary>
    public async Task<List<PracticeExerciseMeta>> ListAsync()
    {
        if (_manifest != null) return _manifest;
        await _manifestGate.WaitAsync();
        try
        {
            if (_manifest != null) return _manifest;
            await LoadManifestAsync();
            return _manifest ?? new List<PracticeExerciseMeta>();
        }
        finally
        {
            _manifestGate.Release();
        }
    }

    /// <summary>
    /// Returns the full <see cref="PracticeExercise"/> for the given id —
    /// solution, starter, tests, and every available language's statement
    /// body. Cached for the service lifetime; null if the id is not in the
    /// manifest or assets failed to load.
    /// </summary>
    public async Task<PracticeExercise?> GetAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_exerciseCache.TryGetValue(id, out var cached)) return cached;

        await _exerciseGate.WaitAsync();
        try
        {
            if (_exerciseCache.TryGetValue(id, out cached)) return cached;
            // Make sure the manifest is loaded so we can resolve folders + metadata.
            if (_manifestRaw == null) await LoadManifestAsync();
            if (_manifestRaw == null || !_manifestRaw.TryGetValue(id, out var raw))
            {
                _logger.LogWarning("[PracticeCatalog] Unknown exercise id {Id}", id);
                return null;
            }

            var built = await BuildExerciseAsync(raw);
            if (built != null) _exerciseCache[id] = built;
            return built;
        }
        finally
        {
            _exerciseGate.Release();
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Loading internals
    // ────────────────────────────────────────────────────────────────────

    private async Task LoadManifestAsync()
    {
        try
        {
            using var stream = await _open("Practice/manifest.json");
            var doc = await JsonSerializer.DeserializeAsync<ManifestRoot>(stream, JsonOpts);
            if (doc?.Exercises == null)
            {
                _logger.LogWarning("[PracticeCatalog] manifest.json deserialized to empty");
                _manifest = new List<PracticeExerciseMeta>();
                _manifestRaw = new Dictionary<string, ManifestEntry>();
                return;
            }

            var meta = new List<PracticeExerciseMeta>();
            var raw = new Dictionary<string, ManifestEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in doc.Exercises)
            {
                if (string.IsNullOrEmpty(e.Id)) continue;
                if (!Enum.TryParse<PracticeModality>(e.Modality, ignoreCase: true, out var modality)) continue;
                if (!Enum.TryParse<PracticeDifficulty>(e.Difficulty, ignoreCase: true, out var difficulty)) continue;

                meta.Add(new PracticeExerciseMeta(
                    Id: e.Id,
                    Modality: modality,
                    Difficulty: difficulty,
                    Titles: e.Titles ?? new(),
                    Summaries: e.Summaries ?? new(),
                    TestCaseCount: 0)); // hydrated lazily on GetAsync; -1 sentinel would be misleading on the catalog card
                raw[e.Id] = e;
            }

            _manifest = meta;
            _manifestRaw = raw;
            _logger.LogInformation("[PracticeCatalog] Loaded manifest with {Count} exercises", meta.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeCatalog] Failed to load manifest.json");
            _manifest = new List<PracticeExerciseMeta>();
            _manifestRaw = new Dictionary<string, ManifestEntry>();
        }
    }

    private async Task<PracticeExercise?> BuildExerciseAsync(ManifestEntry e)
    {
        try
        {
            if (!Enum.TryParse<PracticeModality>(e.Modality, true, out var modality)) return null;
            if (!Enum.TryParse<PracticeDifficulty>(e.Difficulty, true, out var difficulty)) return null;
            var matchKind = Enum.TryParse<PracticeMatchKind>(e.MatchKind, true, out var mk)
                ? mk : PracticeMatchKind.ValueAtTimes;

            string folder = (e.Folder ?? string.Empty).Trim('/');

            // Required source files. Failure to load any of these → exercise unusable.
            var solution = await ReadTextAsync($"Practice/{folder}/solution.vhdl");
            var starter  = await ReadTextAsync($"Practice/{folder}/starter.vhdl");
            if (solution == null || starter == null)
            {
                _logger.LogWarning("[PracticeCatalog] Missing solution/starter for {Id}", e.Id);
                return null;
            }

            // Tests
            var testsRaw = await ReadJsonAsync<TestsJson>($"Practice/{folder}/tests.json");
            var tests = ConvertTests(testsRaw);

            // Statements: one per available language, silently skipping missing files.
            var statements = new Dictionary<string, string>();
            foreach (var lang in KnownLangs)
            {
                var body = await ReadTextAsync($"Practice/{folder}/statement.{lang}.md");
                if (!string.IsNullOrEmpty(body)) statements[lang] = body;
            }

            return new PracticeExercise(
                Id: e.Id!,
                Modality: modality,
                Difficulty: difficulty,
                Titles: e.Titles ?? new(),
                Summaries: e.Summaries ?? new(),
                Statements: statements,
                SolutionVhdl: solution,
                StarterVhdl: starter,
                MatchKind: matchKind,
                OutputPortsToCompare: e.OutputPorts ?? new(),
                SampleTimesNs: e.SampleTimesNs ?? new(),
                TestCases: tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeCatalog] Build failed for {Id}", e.Id);
            return null;
        }
    }

    private static List<PracticeTestCase> ConvertTests(TestsJson? doc)
    {
        var result = new List<PracticeTestCase>();
        if (doc?.Tests == null) return result;
        foreach (var t in doc.Tests)
        {
            if (string.IsNullOrEmpty(t.Id)) continue;

            var inputs = new List<PortMomentConfiguration>();
            if (t.Inputs != null)
            {
                foreach (var pi in t.Inputs)
                {
                    if (string.IsNullOrEmpty(pi.PortName)) continue;
                    var pmc = new PortMomentConfiguration { PortName = pi.PortName };
                    if (pi.Values != null)
                    {
                        foreach (var pv in pi.Values)
                        {
                            pmc.Values.Add(new PortMomentValue
                            {
                                Time = pv.Time,
                                TimeScale = string.IsNullOrEmpty(pv.Scale) ? "NS" : pv.Scale,
                                Value = pv.Value ?? string.Empty
                            });
                        }
                    }
                    inputs.Add(pmc);
                }
            }

            var duration = t.DurationMicros > 0
                ? TimeSpan.FromMicroseconds(t.DurationMicros)
                : TimeSpan.FromMicroseconds(2);

            result.Add(new PracticeTestCase(
                Id: t.Id,
                Descriptions: t.Descriptions ?? new(),
                Visible: t.Visible,
                Inputs: inputs,
                Duration: duration,
                SystemClockMhz: t.SystemClockMhz > 0 ? t.SystemClockMhz : 50));
        }
        return result;
    }

    private async Task<string?> ReadTextAsync(string path)
    {
        try
        {
            using var s = await _open(path);
            using var r = new StreamReader(s);
            return await r.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }

    private async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        try
        {
            using var s = await _open(path);
            return await JsonSerializer.DeserializeAsync<T>(s, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PracticeCatalog] JSON load failed for {Path}", path);
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ────────────────────────────────────────────────────────────────────
    // Manifest + tests.json DTOs (private — never escape the service)
    // ────────────────────────────────────────────────────────────────────

    private class ManifestRoot
    {
        public int Version { get; set; }
        public List<ManifestEntry>? Exercises { get; set; }
    }

    private class ManifestEntry
    {
        public string? Id { get; set; }
        public string? Modality { get; set; }
        public string? Difficulty { get; set; }
        public string? Folder { get; set; }
        public string? MatchKind { get; set; }
        public List<string>? OutputPorts { get; set; }
        public List<long>? SampleTimesNs { get; set; }
        public Dictionary<string, string>? Titles { get; set; }
        public Dictionary<string, string>? Summaries { get; set; }
    }

    private class TestsJson
    {
        public List<TestJsonItem>? Tests { get; set; }
    }

    private class TestJsonItem
    {
        public string? Id { get; set; }
        public bool Visible { get; set; }
        public Dictionary<string, string>? Descriptions { get; set; }
        public int DurationMicros { get; set; }
        public int SystemClockMhz { get; set; }
        public List<PortInputJson>? Inputs { get; set; }
    }

    private class PortInputJson
    {
        public string? PortName { get; set; }
        public List<PortValueJson>? Values { get; set; }
    }

    private class PortValueJson
    {
        public int Time { get; set; }
        public string? Scale { get; set; }
        public string? Value { get; set; }
    }
}
