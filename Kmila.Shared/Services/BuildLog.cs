namespace Kmila.Shared.Services;

/// <summary>
/// Severity of a build/run log entry. Named distinctly from
/// <c>Microsoft.Extensions.Logging.LogLevel</c> so the two can coexist in
/// the same namespace scope without ambiguity (the Output panel uses this;
/// the system-wide Logs page uses the Microsoft one).
/// </summary>
public enum BuildLogLevel
{
    /// <summary>Informational message.</summary>
    Info,
    /// <summary>Successful outcome.</summary>
    Success,
    /// <summary>Warning.</summary>
    Warn,
    /// <summary>Error.</summary>
    Error
}

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

    /// <summary>Read-only view of the captured entries, oldest first.</summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <summary>Fires after every <see cref="Add"/> or <see cref="Clear"/> so the Output dock can re-render.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Appends a new entry timestamped at <see cref="DateTime.Now"/>. The buffer is
    /// bounded — once it exceeds 500 entries the oldest 50 are dropped in one
    /// shift so reallocation is rare.
    /// </summary>
    /// <param name="level">Severity bucket used by the Output tab to colour the row.</param>
    /// <param name="message">Human-readable message body.</param>
    public void Add(BuildLogLevel level, string message)
    {
        _entries.Add(new LogEntry(DateTime.Now, level, message));
        // Bound the buffer so long-running sessions don't leak — drop the oldest
        // 50 entries at a time to keep shifts rare.
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(0, 50);
        OnChange?.Invoke();
    }

    /// <summary>Convenience wrapper that calls <see cref="Add"/> with <see cref="BuildLogLevel.Info"/>.</summary>
    public void Info(string message)    => Add(BuildLogLevel.Info, message);
    /// <summary>Convenience wrapper that calls <see cref="Add"/> with <see cref="BuildLogLevel.Success"/>.</summary>
    public void Success(string message) => Add(BuildLogLevel.Success, message);
    /// <summary>Convenience wrapper that calls <see cref="Add"/> with <see cref="BuildLogLevel.Warn"/>.</summary>
    public void Warn(string message)    => Add(BuildLogLevel.Warn, message);
    /// <summary>Convenience wrapper that calls <see cref="Add"/> with <see cref="BuildLogLevel.Error"/>.</summary>
    public void Error(string message)   => Add(BuildLogLevel.Error, message);

    /// <summary>
    /// Empties the buffer and fires <see cref="OnChange"/>. No-op (and no event)
    /// when the buffer is already empty so callers can invoke it idempotently.
    /// </summary>
    public void Clear()
    {
        if (_entries.Count == 0) return;
        _entries.Clear();
        OnChange?.Invoke();
    }
}
