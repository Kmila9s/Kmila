namespace Kmila.Shared.Services;

/// <summary>
/// Severity of a build/run log entry. Named distinctly from
/// <c>Microsoft.Extensions.Logging.LogLevel</c> so the two can coexist in
/// the same namespace scope without ambiguity (the Output panel uses this;
/// the system-wide Logs page uses the Microsoft one).
/// </summary>
public enum BuildLogLevel { Info, Success, Warn, Error }

/// <summary>Single append-only log entry for the Output tab.</summary>
public record LogEntry(DateTime Timestamp, BuildLogLevel Level, string Message);

/// <summary>
/// In-memory log of build + run events. The Output dock tab renders
/// <see cref="Entries"/>; anything with state transitions (Editor.razor,
/// RunBar, maybe Builder adapters) appends via <see cref="Add"/>.
/// </summary>
public class BuildLog
{
    private readonly List<LogEntry> _entries = new();
    private const int MaxEntries = 500;

    public IReadOnlyList<LogEntry> Entries => _entries;

    public event Action? OnChange;

    public void Add(BuildLogLevel level, string message)
    {
        _entries.Add(new LogEntry(DateTime.Now, level, message));
        // Bound the buffer so long-running sessions don't leak — drop the oldest
        // 50 entries at a time to keep shifts rare.
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(0, 50);
        OnChange?.Invoke();
    }

    public void Info(string message)    => Add(BuildLogLevel.Info, message);
    public void Success(string message) => Add(BuildLogLevel.Success, message);
    public void Warn(string message)    => Add(BuildLogLevel.Warn, message);
    public void Error(string message)   => Add(BuildLogLevel.Error, message);

    public void Clear()
    {
        if (_entries.Count == 0) return;
        _entries.Clear();
        OnChange?.Invoke();
    }
}
