using Kmila.Shared.Data;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Lightweight "I finished this lesson" tracker backed by the existing
/// <c>AppSettings</c> key-value table. Keys are of the form
/// <c>learn.done.{moduleId}.{lessonId}</c> with value <c>"1"</c>.
/// </summary>
/// <remarks>
/// Chose AppSettings over a dedicated table because:
///   • completion is a single boolean per (module, lesson) pair,
///   • SQLite-net doesn't mind thousands of settings rows,
///   • keeps migrations out of this feature's critical path.
/// If per-user or per-attempt history is ever needed, swap this for a proper
/// table without changing the public API.
/// </remarks>
public class LessonProgressStore
{
    private const string KeyPrefix = "learn.done.";
    /// <summary>AppSettings key holding the last (module, lesson) the user viewed — as <c>moduleId/lessonId</c>.</summary>
    private const string LastOpenedKey = "learn.last";

    private readonly ApplicationDbContext _db;
    private readonly ILogger _logger;
    private readonly HashSet<string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _hydrated;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates the store with the database context and an optional logger.</summary>
    /// <param name="db">Database context backing lesson progress persistence.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public LessonProgressStore(ApplicationDbContext db,
                               ILogger<LessonProgressStore>? logger = null)
    {
        _db = db;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Fires whenever completion state changes so UIs can re-render.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Loads every "lesson done" row into memory once. Subsequent lookups are
    /// O(1). We hydrate lazily so pages that never touch the Learn routes
    /// don't pay the query cost.
    /// </summary>
    private async Task EnsureHydratedAsync()
    {
        if (_hydrated) return;
        await _gate.WaitAsync();
        try
        {
            if (_hydrated) return;
            await _db.InitDBAsync();
            var rows = await _db.Settings
                .Where(s => s.Key.StartsWith(KeyPrefix))
                .ToListAsync();
            foreach (var r in rows)
            {
                if (r.Value == "1") _cache.Add(r.Key);
            }
            _hydrated = true;
            _logger.LogInformation("[LessonProgressStore] hydrated {Count} completions", _cache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonProgressStore] hydration failed — starting empty");
            _hydrated = true; // don't thrash on repeated failures
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>True if the lesson has been marked complete.</summary>
    public async Task<bool> IsCompletedAsync(string moduleId, string lessonId)
    {
        await EnsureHydratedAsync();
        return _cache.Contains(BuildKey(moduleId, lessonId));
    }

    /// <summary>Sync counterpart once hydrated — for tight render loops.</summary>
    public bool IsCompletedCached(string moduleId, string lessonId)
        => _cache.Contains(BuildKey(moduleId, lessonId));

    /// <summary>Returns how many lessons in the module the user has finished.</summary>
    public async Task<int> CompletedCountAsync(string moduleId, IEnumerable<string> lessonIds)
    {
        await EnsureHydratedAsync();
        return lessonIds.Count(id => _cache.Contains(BuildKey(moduleId, id)));
    }

    /// <summary>Marks a lesson complete (idempotent).</summary>
    public async Task MarkCompletedAsync(string moduleId, string lessonId)
    {
        await EnsureHydratedAsync();
        var key = BuildKey(moduleId, lessonId);
        if (!_cache.Add(key)) return;
        try
        {
            await _db.SetSettingAsync(key, "1");
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            // Roll the cache back so the UI doesn't lie about persistence.
            _cache.Remove(key);
            _logger.LogError(ex, "[LessonProgressStore] failed to persist {Key}", key);
        }
    }

    /// <summary>Un-marks a lesson (e.g. "start over").</summary>
    public async Task MarkIncompleteAsync(string moduleId, string lessonId)
    {
        await EnsureHydratedAsync();
        var key = BuildKey(moduleId, lessonId);
        if (!_cache.Remove(key)) return;
        try
        {
            await _db.SetSettingAsync(key, "0");
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _cache.Add(key);
            _logger.LogError(ex, "[LessonProgressStore] failed to un-persist {Key}", key);
        }
    }

    /// <summary>Total completions across all modules.</summary>
    public async Task<int> TotalCompletedAsync()
    {
        await EnsureHydratedAsync();
        return _cache.Count;
    }

    /// <summary>
    /// Remembers "user just opened this lesson" so the Learn catalog can
    /// surface a "Continue where you left off" CTA on next visit. Called by
    /// <see cref="Pages.LessonView"/> on mount.
    /// </summary>
    public async Task TouchLastOpenedAsync(string moduleId, string lessonId)
    {
        if (string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(lessonId)) return;
        try
        {
            await _db.InitDBAsync();
            await _db.SetSettingAsync(LastOpenedKey, $"{moduleId}/{lessonId}");
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonProgressStore] failed to persist last-opened");
        }
    }

    /// <summary>
    /// Wipes every tracked completion plus the last-opened pointer.
    /// Destructive; the Settings page wraps it in a confirmation dialog.
    /// </summary>
    public async Task ResetAllAsync()
    {
        await EnsureHydratedAsync();
        try
        {
            await _db.InitDBAsync();
            var snapshot = _cache.ToList();
            _cache.Clear();
            foreach (var key in snapshot)
            {
                // key is already a full AppSettings key (we stored it with the prefix)
                await _db.SetSettingAsync(key, "0");
            }
            await _db.SetSettingAsync(LastOpenedKey, string.Empty);
            OnChange?.Invoke();
            _logger.LogInformation("[LessonProgressStore] reset {Count} completions", snapshot.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonProgressStore] reset failed");
        }
    }

    /// <summary>
    /// Returns <c>(moduleId, lessonId)</c> of the lesson most recently opened,
    /// or null if the user hasn't opened any. Used to render the catalog's
    /// "Continue" banner.
    /// </summary>
    public async Task<(string ModuleId, string LessonId)?> GetLastOpenedAsync()
    {
        try
        {
            await _db.InitDBAsync();
            var raw = await _db.GetSettingAsync(LastOpenedKey);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Split('/', 2);
            if (parts.Length != 2 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1])) return null;
            return (parts[0], parts[1]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonProgressStore] failed to read last-opened");
            return null;
        }
    }

    private static string BuildKey(string moduleId, string lessonId)
        => $"{KeyPrefix}{moduleId}.{lessonId}";
}
