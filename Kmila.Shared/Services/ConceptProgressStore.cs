using Kmila.Shared.Data;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Concepts-track completion tracker. Mirrors
/// <see cref="LessonProgressStore"/> but uses a distinct key prefix so the
/// two tracks never collide in the shared <c>AppSettings</c> KV table.
/// Keys: <c>concept.done.{moduleId}.{conceptId}</c>; last-opened pointer:
/// <c>concept.last</c>.
/// </summary>
public class ConceptProgressStore
{
    private const string KeyPrefix = "concept.done.";
    private const string LastOpenedKey = "concept.last";

    private readonly ApplicationDbContext _db;
    private readonly ILogger _logger;
    private readonly HashSet<string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _hydrated;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates a new <see cref="ConceptProgressStore"/>.</summary>
    /// <param name="db">Application database context backing the key-value store.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public ConceptProgressStore(ApplicationDbContext db,
                                ILogger<ConceptProgressStore>? logger = null)
    {
        _db = db;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Raised whenever a completion or the last-opened pointer changes.</summary>
    public event Action? OnChange;

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
            _logger.LogInformation("[ConceptProgressStore] hydrated {Count} completions", _cache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConceptProgressStore] hydration failed — starting empty");
            _hydrated = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Returns whether the given concept is marked completed, hydrating the cache first.</summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="conceptId">Concept identifier.</param>
    /// <returns>True if the concept is completed.</returns>
    public async Task<bool> IsCompletedAsync(string moduleId, string conceptId)
    {
        await EnsureHydratedAsync();
        return _cache.Contains(BuildKey(moduleId, conceptId));
    }

    /// <summary>Returns whether the given concept is completed using only the in-memory cache (no hydration).</summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="conceptId">Concept identifier.</param>
    /// <returns>True if the concept is currently cached as completed.</returns>
    public bool IsCompletedCached(string moduleId, string conceptId)
        => _cache.Contains(BuildKey(moduleId, conceptId));

    /// <summary>Counts how many of the given concepts in a module are completed.</summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="conceptIds">Concept identifiers to check.</param>
    /// <returns>The number of completed concepts among those supplied.</returns>
    public async Task<int> CompletedCountAsync(string moduleId, IEnumerable<string> conceptIds)
    {
        await EnsureHydratedAsync();
        return conceptIds.Count(id => _cache.Contains(BuildKey(moduleId, id)));
    }

    /// <summary>Marks a concept as completed and persists it.</summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="conceptId">Concept identifier.</param>
    public async Task MarkCompletedAsync(string moduleId, string conceptId)
    {
        await EnsureHydratedAsync();
        var key = BuildKey(moduleId, conceptId);
        if (!_cache.Add(key)) return;
        try
        {
            await _db.SetSettingAsync(key, "1");
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _cache.Remove(key);
            _logger.LogError(ex, "[ConceptProgressStore] failed to persist {Key}", key);
        }
    }

    /// <summary>Clears a concept's completion mark and persists the change.</summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="conceptId">Concept identifier.</param>
    public async Task MarkIncompleteAsync(string moduleId, string conceptId)
    {
        await EnsureHydratedAsync();
        var key = BuildKey(moduleId, conceptId);
        if (!_cache.Remove(key)) return;
        try
        {
            await _db.SetSettingAsync(key, "0");
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _cache.Add(key);
            _logger.LogError(ex, "[ConceptProgressStore] failed to un-persist {Key}", key);
        }
    }

    /// <summary>Returns the total number of completed concepts across all modules.</summary>
    /// <returns>The count of completed concepts.</returns>
    public async Task<int> TotalCompletedAsync()
    {
        await EnsureHydratedAsync();
        return _cache.Count;
    }

    /// <summary>Records the most recently opened concept as the last-opened pointer.</summary>
    /// <param name="moduleId">Module identifier.</param>
    /// <param name="conceptId">Concept identifier.</param>
    public async Task TouchLastOpenedAsync(string moduleId, string conceptId)
    {
        if (string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(conceptId)) return;
        try
        {
            await _db.InitDBAsync();
            await _db.SetSettingAsync(LastOpenedKey, $"{moduleId}/{conceptId}");
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConceptProgressStore] failed to persist last-opened");
        }
    }

    /// <summary>Clears all completions and the last-opened pointer, persisting the reset.</summary>
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
                await _db.SetSettingAsync(key, "0");
            }
            await _db.SetSettingAsync(LastOpenedKey, string.Empty);
            OnChange?.Invoke();
            _logger.LogInformation("[ConceptProgressStore] reset {Count} completions", snapshot.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConceptProgressStore] reset failed");
        }
    }

    /// <summary>Returns the last-opened concept as a (module, concept) pair, or null if none is recorded.</summary>
    /// <returns>The last-opened module/concept pair, or null.</returns>
    public async Task<(string ModuleId, string ConceptId)?> GetLastOpenedAsync()
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
            _logger.LogError(ex, "[ConceptProgressStore] failed to read last-opened");
            return null;
        }
    }

    private static string BuildKey(string moduleId, string conceptId)
        => $"{KeyPrefix}{moduleId}.{conceptId}";
}
