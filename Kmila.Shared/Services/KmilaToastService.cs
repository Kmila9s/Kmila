using BlazorBootstrap;

namespace Kmila.Shared.Services;

/// <summary>Severity level of a toast notification, controlling its styling.</summary>
public enum KmilaToastLevel
{
    /// <summary>Informational toast.</summary>
    Info,
    /// <summary>Success toast.</summary>
    Success,
    /// <summary>Warning toast.</summary>
    Warning,
    /// <summary>Error/danger toast.</summary>
    Danger
}

/// <summary>An immutable toast notification queued for display.</summary>
public sealed class KmilaToastItem
{
    /// <summary>Unique identifier for this toast instance.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>Severity level controlling the toast's styling.</summary>
    public KmilaToastLevel Level { get; init; }
    /// <summary>Optional title shown above the body.</summary>
    public string? Title { get; init; }
    /// <summary>Main message text of the toast.</summary>
    public string Body { get; init; } = string.Empty;
    /// <summary>Auto-dismiss delay in milliseconds; non-positive keeps the toast until dismissed.</summary>
    public int DurationMs { get; init; } = 5000;
}

/// Drop-in replacement for BlazorBootstrap's ToastService. The `Notify(ToastMessage)`
/// overload preserves the BB call-site signature so the ~113 existing callers
/// continue to compile after a single `_Imports.razor` swap. The KmilaToastHost
/// component subscribes to OnChange and re-renders the toast stack with the
/// project's glass design.
public sealed class KmilaToastService
{
    /// <summary>Raised whenever the toast collection changes, so hosts can re-render.</summary>
    public event Action? OnChange;

    private readonly List<KmilaToastItem> _items = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = new();
    private readonly object _gate = new();

    /// <summary>Returns a point-in-time copy of the current toasts.</summary>
    /// <returns>A snapshot of the currently queued toasts.</returns>
    public IReadOnlyList<KmilaToastItem> Snapshot()
    {
        lock (_gate) return _items.ToArray();
    }

    /// BB compatibility — the existing helper `ToastMessages.Message(...)` and
    /// inline `new ToastMessage(...)` callers funnel through here unchanged.
    public void Notify(ToastMessage msg)
        => Show(MapLevel(msg.Type), msg.Message ?? string.Empty, msg.Title);

    /// <summary>Adds a toast with the given level and schedules its auto-dismiss.</summary>
    /// <param name="level">Severity level controlling styling.</param>
    /// <param name="body">Main message text.</param>
    /// <param name="title">Optional title shown above the body.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds; non-positive keeps it until dismissed.</param>
    public void Show(KmilaToastLevel level, string body, string? title = null, int durationMs = 5000)
    {
        var item = new KmilaToastItem
        {
            Level = level,
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Body = body ?? string.Empty,
            DurationMs = durationMs,
        };

        CancellationTokenSource? cts = null;
        lock (_gate)
        {
            _items.Add(item);
            if (durationMs > 0)
            {
                cts = new CancellationTokenSource();
                _timers[item.Id] = cts;
            }
        }
        OnChange?.Invoke();

        if (cts != null)
        {
            var token = cts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(durationMs, token);
                    Dismiss(item.Id);
                }
                catch (OperationCanceledException) { /* dismissed early */ }
            });
        }
    }

    /// <summary>Shows a success-level toast.</summary>
    /// <param name="body">Main message text.</param>
    /// <param name="title">Optional title shown above the body.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds; non-positive keeps it until dismissed.</param>
    public void Success(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Success, body, title, durationMs);

    /// <summary>Shows a danger-level toast.</summary>
    /// <param name="body">Main message text.</param>
    /// <param name="title">Optional title shown above the body.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds; non-positive keeps it until dismissed.</param>
    public void Danger(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Danger, body, title, durationMs);

    /// <summary>Shows a warning-level toast.</summary>
    /// <param name="body">Main message text.</param>
    /// <param name="title">Optional title shown above the body.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds; non-positive keeps it until dismissed.</param>
    public void Warning(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Warning, body, title, durationMs);

    /// <summary>Shows an info-level toast.</summary>
    /// <param name="body">Main message text.</param>
    /// <param name="title">Optional title shown above the body.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds; non-positive keeps it until dismissed.</param>
    public void Info(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Info, body, title, durationMs);

    /// <summary>Removes the toast with the given id and cancels its timer.</summary>
    /// <param name="id">Identifier of the toast to dismiss.</param>
    public void Dismiss(Guid id)
    {
        bool removed = false;
        lock (_gate)
        {
            var idx = _items.FindIndex(i => i.Id == id);
            if (idx >= 0) { _items.RemoveAt(idx); removed = true; }
            if (_timers.Remove(id, out var cts)) cts.Cancel();
        }
        if (removed) OnChange?.Invoke();
    }

    /// <summary>Removes all toasts and cancels their timers.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _items.Clear();
            foreach (var cts in _timers.Values) cts.Cancel();
            _timers.Clear();
        }
        OnChange?.Invoke();
    }

    private static KmilaToastLevel MapLevel(ToastType t) => t switch
    {
        ToastType.Success => KmilaToastLevel.Success,
        ToastType.Danger => KmilaToastLevel.Danger,
        ToastType.Warning => KmilaToastLevel.Warning,
        _ => KmilaToastLevel.Info,
    };
}
