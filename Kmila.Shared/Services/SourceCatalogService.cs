using System.Text.Json;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Loads the app-wide bibliography from <c>wwwroot/Data/sources.json</c>.
/// Follows the same stream-delegate pattern as <see cref="LessonCatalog"/> so a
/// single implementation works on the Web host, MAUI Blazor Hybrid, and WASM.
/// </summary>
/// <remarks>
/// The catalog is loaded once and cached (service registered as a singleton).
/// A key→entry index is built lazily on first lookup so per-lesson source
/// resolution is O(1). Missing/malformed files degrade to an empty catalog so
/// the Sources page and per-lesson blocks render empty instead of crashing.
/// </remarks>
public class SourceCatalogService
{
    private readonly Func<string, Task<Stream>> _open;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private SourceCatalog? _catalog;
    private Dictionary<string, SourceEntry>? _index;

    /// <summary>Creates the service with a delegate for opening bundled files.</summary>
    /// <param name="openFileAsync">Opens a bundled asset by relative path.</param>
    /// <param name="logger">Optional logger.</param>
    public SourceCatalogService(Func<string, Task<Stream>> openFileAsync,
                                ILogger<SourceCatalogService>? logger = null)
    {
        _open = openFileAsync;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Returns the loaded catalog. First call loads + caches; subsequent calls
    /// are free. Returns an empty catalog if the file can't be read.
    /// </summary>
    public async Task<SourceCatalog> GetCatalogAsync()
    {
        if (_catalog != null) return _catalog;
        await _loadGate.WaitAsync();
        try
        {
            if (_catalog != null) return _catalog;
            _catalog = await LoadCatalogAsync();
            _index = _catalog.Sources
                .Where(s => !string.IsNullOrEmpty(s.Key))
                .GroupBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            return _catalog;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task<SourceCatalog> LoadCatalogAsync()
    {
        try
        {
            using var stream = await _open("Data/sources.json");
            var catalog = await JsonSerializer.DeserializeAsync<SourceCatalog>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new JsonStringEnumConverter() }
                });
            if (catalog == null)
            {
                _logger.LogWarning("[SourceCatalog] sources.json deserialized to null — empty bibliography");
                return new SourceCatalog();
            }
            _logger.LogInformation("[SourceCatalog] Loaded {Count} sources", catalog.Sources.Count);
            return catalog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SourceCatalog] Failed to load sources.json");
            return new SourceCatalog();
        }
    }

    /// <summary>
    /// Resolves a list of citation keys to their entries, preserving the order
    /// the keys were given in and silently dropping keys with no match (so a
    /// stale key in the manifest never throws on a lesson page). Safe to call
    /// before <see cref="GetCatalogAsync"/> — it loads on demand.
    /// </summary>
    public async Task<List<SourceEntry>> ResolveAsync(IEnumerable<string> keys)
    {
        if (keys == null) return new();
        await GetCatalogAsync();
        var index = _index ?? new();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SourceEntry>();
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key) || !seen.Add(key)) continue;
            if (index.TryGetValue(key, out var entry)) result.Add(entry);
            else _logger.LogDebug("[SourceCatalog] Unknown citation key: {Key}", key);
        }
        return result;
    }
}
