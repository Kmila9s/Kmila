namespace Kmila.Shared.Services;

/// <summary>One captured log entry bound for the in-app Logs viewer.</summary>
public record AppLogEntry(
    DateTime Timestamp,
    Microsoft.Extensions.Logging.LogLevel Level,
    string Category,
    string Message,
    string? Exception
);

/// <summary>
/// In-memory ring-buffer for app-wide log entries. Fed by
/// <see cref="InMemoryLoggerProvider"/> — every time code anywhere in the app
/// logs through <c>ILogger&lt;T&gt;</c>, an entry lands here. The Logs page
/// subscribes to <see cref="OnChange"/> to stream updates live.
///
/// Thread-safe: the logging infrastructure may fire from worker threads
/// (e.g. simulation progress on a background task). <see cref="OnChange"/>
/// consumers marshal back to the UI thread.
///
/// Distinct from <see cref="BuildLog"/>, which is editor/simulation-specific
/// and not wired to ILogger — it's a UI state for the Output dock tab.
/// </summary>
public class AppLogStore
{
    private readonly List<AppLogEntry> _entries = new();
    private readonly object _lock = new();
    private const int MaxEntries = 2000;

    public event Action? OnChange;

    /// <summary>
    /// Returns a snapshot of current entries. Returns an array so consumers
    /// can iterate / sort without holding the internal lock.
    /// </summary>
    public IReadOnlyList<AppLogEntry> Snapshot()
    {
        lock (_lock) { return _entries.ToArray(); }
    }

    public void Add(AppLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            // Drop oldest batch when full — amortised O(1) cost per add since we
            // trim by 100 entries at a time rather than one-by-one per overflow.
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries + 100);
        }
        OnChange?.Invoke();
    }

    public void Clear()
    {
        lock (_lock) { _entries.Clear(); }
        OnChange?.Invoke();
    }
}
