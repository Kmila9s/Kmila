using System.Text.Json;
using System.Text.Json.Serialization;
using Kmila.Frontend;
using Kmila.Shared.Models;
using Kmila.Shared.Models.Practice;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Practice;

/// <summary>
/// Loads the Practice catalog manifest + per-exercise assets (solution and
/// starter per HDL language variant, tests.json, statement.{lang}.md) from disk. Mirrors the
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

    /// <summary>Creates the catalog with a delegate for opening bundled files.</summary>
    /// <param name="openFileAsync">Opens a bundled asset by relative path.</param>
    /// <param name="logger">Optional logger.</param>
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
            if (doc != null && !ContentSchema.Accepts(doc.Version, "Practice/manifest.json", _logger)) doc = null;
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
                    TestCaseCount: 0, // hydrated lazily on GetAsync; -1 sentinel would be misleading on the catalog card
                    RequiresEngine: string.IsNullOrEmpty(e.RequiresEngine) ? null : e.RequiresEngine));
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

            // Required source files, one pair per language variant. A variant
            // with a missing file is dropped; no usable variant → exercise unusable.
            var loaded = new Dictionary<string, PracticeVariant>(StringComparer.OrdinalIgnoreCase);
            foreach (var (langId, files) in e.Variants ?? new())
            {
                var solution = files.Solution == null ? null : await ReadTextAsync($"Practice/{folder}/{files.Solution}");
                var starter  = files.Starter  == null ? null : await ReadTextAsync($"Practice/{folder}/{files.Starter}");
                if (solution == null || starter == null)
                {
                    _logger.LogWarning("[PracticeCatalog] Missing solution/starter for {Id} ({Lang})", e.Id, langId);
                    continue;
                }
                loaded[langId] = new PracticeVariant(starter, solution);
            }
            var variants = HdlVariants<PracticeVariant>.FromIds(loaded);
            if (variants.IsEmpty)
            {
                _logger.LogWarning("[PracticeCatalog] No usable language variant for {Id}", e.Id);
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

            // Tiered hints — optional. Missing/invalid file → no hints (the UI
            // hides the hint section entirely).
            var hintsRaw = await ReadJsonAsync<HintsJson>($"Practice/{folder}/hints.json");
            var hints = ConvertHints(hintsRaw);

            return new PracticeExercise(
                Id: e.Id!,
                Modality: modality,
                Difficulty: difficulty,
                Titles: e.Titles ?? new(),
                Summaries: e.Summaries ?? new(),
                Statements: statements,
                Variants: variants,
                MatchKind: matchKind,
                OutputPortsToCompare: e.OutputPorts ?? new(),
                SampleTimesNs: e.SampleTimesNs ?? new(),
                TestCases: tests,
                RequiresEngine: string.IsNullOrEmpty(e.RequiresEngine) ? null : e.RequiresEngine,
                Hints: hints);
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

    /// <summary>
    /// Projects the raw hints.json into the public model, sorted by level and
    /// dropping entries with no text. Returns an empty list when the file was
    /// absent or malformed.
    /// </summary>
    private static List<PracticeHint> ConvertHints(HintsJson? doc)
    {
        var result = new List<PracticeHint>();
        if (doc?.Hints == null) return result;
        foreach (var h in doc.Hints)
        {
            if (h.Text == null || h.Text.Count == 0) continue;
            result.Add(new PracticeHint(h.Level, h.Text));
        }
        return result.OrderBy(h => h.Level).ToList();
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

    /// <summary>Root document shape of <c>Practice/manifest.json</c>: schema version + exercise entries.</summary>
    private sealed class ManifestRoot
    {
        /// <summary>Manifest schema version number; bumped when the on-disk shape changes.</summary>
        public int Version { get; set; }
        /// <summary>List of exercises declared by the manifest.</summary>
        public List<ManifestEntry>? Exercises { get; set; }
    }

    /// <summary>One exercise entry inside <see cref="ManifestRoot.Exercises"/>; later projected into the public <c>PracticeExerciseMeta</c>.</summary>
    private sealed class ManifestEntry
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
        /// <summary>Optional engine-feature gate. See PracticeExerciseMeta.RequiresEngine.</summary>
        public string? RequiresEngine { get; set; }
        /// <summary>Source files per HDL language id (schema v2), relative to <see cref="Folder"/>.</summary>
        public Dictionary<string, VariantFiles>? Variants { get; set; }
    }

    /// <summary>File names of one language variant inside an exercise folder.</summary>
    private sealed class VariantFiles
    {
        public string? Starter { get; set; }
        public string? Solution { get; set; }
    }

    /// <summary>Root document shape of an exercise's <c>hints.json</c> file.</summary>
    private sealed class HintsJson
    {
        /// <summary>Hint schema version number; bumped when the on-disk shape changes.</summary>
        public int Version { get; set; }
        /// <summary>Tiered hints declared by the file.</summary>
        public List<HintJsonItem>? Hints { get; set; }
    }

    /// <summary>One tiered hint inside <see cref="HintsJson.Hints"/>: escalation level + localized Markdown bodies.</summary>
    private sealed class HintJsonItem
    {
        /// <summary>Escalation level, 1 (general) … 3 (specific).</summary>
        public int Level { get; set; }
        /// <summary>Localized Markdown bodies keyed by language code (en/es authored).</summary>
        public Dictionary<string, string>? Text { get; set; }
    }

    /// <summary>Root document shape of an exercise's <c>tests.json</c> file.</summary>
    private sealed class TestsJson
    {
        /// <summary>List of test cases declared by the file.</summary>
        public List<TestJsonItem>? Tests { get; set; }
    }

    /// <summary>One test case inside <see cref="TestsJson.Tests"/>: id, visibility, descriptions, duration/clock parameters, and inputs.</summary>
    private sealed class TestJsonItem
    {
        public string? Id { get; set; }
        public bool Visible { get; set; }
        public Dictionary<string, string>? Descriptions { get; set; }
        public int DurationMicros { get; set; }
        public int SystemClockMhz { get; set; }
        public List<PortInputJson>? Inputs { get; set; }
    }

    /// <summary>Time-stamped input sequence applied to a single port during a test case.</summary>
    private sealed class PortInputJson
    {
        public string? PortName { get; set; }
        public List<PortValueJson>? Values { get; set; }
    }

    /// <summary>One <c>(time, scale, value)</c> tuple inside <see cref="PortInputJson.Values"/>.</summary>
    private sealed class PortValueJson
    {
        /// <summary>Numeric time value (interpreted in the unit named by <see cref="Scale"/>).</summary>
        public int Time { get; set; }
        /// <summary>Time unit ("ns", "us", "ms"…); empty defaults to nanoseconds.</summary>
        public string? Scale { get; set; }
        /// <summary>Bit value to drive on the port at the given time.</summary>
        public string? Value { get; set; }
    }
}
