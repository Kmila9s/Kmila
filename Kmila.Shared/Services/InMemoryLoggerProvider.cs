using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// ILoggerProvider that pushes every log event into <see cref="AppLogStore"/>
/// so the in-app Logs page can render them. Registered alongside the other
/// logger providers (Console, Debug, …) via:
/// <code>
///     builder.Logging.AddProvider(new InMemoryLoggerProvider(store));
/// </code>
/// The provider doesn't filter — global filters are already applied by the
/// .NET logging pipeline before log events reach us. If the Logs page wants
/// a higher floor (e.g. drop Trace spam), filter at read time.
/// </summary>
public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly AppLogStore _store;

    /// <summary>Creates a provider that writes log events into the given store.</summary>
    /// <param name="store">Shared in-memory log buffer to receive entries.</param>
    public InMemoryLoggerProvider(AppLogStore store) { _store = store; }

    /// <summary>Creates a logger for the given category.</summary>
    /// <param name="categoryName">Category name supplied by the logging framework.</param>
    /// <returns>An <see cref="ILogger"/> that forwards to the shared store.</returns>
    public ILogger CreateLogger(string categoryName) =>
        new InMemoryLogger(categoryName, _store);

    /// <summary>No-op; the backing store outlives this provider.</summary>
    public void Dispose() { /* AppLogStore lives longer than this provider */ }
}

/// <summary>
/// Per-category <see cref="ILogger"/> implementation backing
/// <see cref="InMemoryLoggerProvider"/>. Forwards every log entry to the shared
/// <see cref="AppLogStore"/>; scope/level filtering is delegated upstream.
/// </summary>
internal sealed class InMemoryLogger : ILogger
{
    /// <summary>Logger category name passed by the framework (typically a fully-qualified type name).</summary>
    private readonly string _category;
    /// <summary>Shared in-memory log buffer that receives every emitted entry.</summary>
    private readonly AppLogStore _store;

    public InMemoryLogger(string category, AppLogStore store)
    {
        _category = category;
        _store = store;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>
    /// Cheap floor. Trace is noisy and usually off by default via
    /// appsettings; we still cull it here in case appsettings let it through.
    /// </summary>
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level) =>
        level >= Microsoft.Extensions.Logging.LogLevel.Debug;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null) return;

        _store.Add(new AppLogEntry(
            Timestamp: DateTime.Now,
            Level: logLevel,
            Category: _category,
            Message: message,
            Exception: exception?.ToString()
        ));
    }
}
