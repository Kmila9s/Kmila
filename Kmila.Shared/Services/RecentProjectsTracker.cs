using Kmila.Shared.Data;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// MRU list of recently-opened projects, persisted in AppSettings as a tiny
/// CSV. Keeps the sidebar short even for students with hundreds of projects
/// — only the last <see cref="Capacity"/> IDs survive.
/// </summary>
/// <remarks>
/// Stored as a CSV string under <c>recent.projects</c>: "12,7,33,4". Front of
/// the list is most-recent. Adding an ID that's already present moves it to
/// the front without duplicating. A dedicated table would be overkill for a
/// bounded sequence that's always read whole.
/// </remarks>
public class RecentProjectsTracker
{
    public const int Capacity = 20;    // stored
    public const int SidebarLimit = 10; // rendered in the sidebar

    private const string SettingKey = "recent.projects";

    private readonly ApplicationDbContext _db;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<int> _cache = new();
    private bool _hydrated;

    public RecentProjectsTracker(ApplicationDbContext db,
                                 ILogger<RecentProjectsTracker>? logger = null)
    {
        _db = db;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Fires on every change so the sidebar can re-render.</summary>
    public event Action? OnChange;

    private async Task EnsureHydratedAsync()
    {
        if (_hydrated) return;
        await _gate.WaitAsync();
        try
        {
            if (_hydrated) return;
            await _db.InitDBAsync();
            var raw = await _db.GetSettingAsync(SettingKey);
            _cache = ParseCsv(raw);
            _hydrated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecentProjectsTracker] hydration failed");
            _hydrated = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Full list, front-to-back = most-recent-to-oldest.</summary>
    public async Task<IReadOnlyList<int>> GetOrderedAsync()
    {
        await EnsureHydratedAsync();
        return _cache.ToList();
    }

    /// <summary>Records an opening of the given project id.</summary>
    public async Task TouchAsync(int projectId)
    {
        if (projectId <= 0) return;
        await EnsureHydratedAsync();

        _cache.RemoveAll(id => id == projectId);
        _cache.Insert(0, projectId);
        if (_cache.Count > Capacity) _cache = _cache.Take(Capacity).ToList();

        await PersistAsync();
        OnChange?.Invoke();
    }

    /// <summary>Removes an id from history (project deleted, etc.).</summary>
    public async Task ForgetAsync(int projectId)
    {
        await EnsureHydratedAsync();
        if (_cache.RemoveAll(id => id == projectId) == 0) return;
        await PersistAsync();
        OnChange?.Invoke();
    }

    private async Task PersistAsync()
    {
        try
        {
            await _db.SetSettingAsync(SettingKey, string.Join(",", _cache));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecentProjectsTracker] failed to persist MRU list");
        }
    }

    private static List<int> ParseCsv(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        var list = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out var id) && id > 0 && !list.Contains(id))
                list.Add(id);
        }
        return list;
    }
}
