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

    public InMemoryLoggerProvider(AppLogStore store) { _store = store; }

    public ILogger CreateLogger(string categoryName) =>
        new InMemoryLogger(categoryName, _store);

    public void Dispose() { /* AppLogStore lives longer than this provider */ }
}

internal sealed class InMemoryLogger : ILogger
{
    private readonly string _category;
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
