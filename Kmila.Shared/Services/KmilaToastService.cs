using BlazorBootstrap;

namespace Kmila.Shared.Services;

public enum KmilaToastLevel { Info, Success, Warning, Danger }

public sealed class KmilaToastItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public KmilaToastLevel Level { get; init; }
    public string? Title { get; init; }
    public string Body { get; init; } = string.Empty;
    public int DurationMs { get; init; } = 5000;
}

/// Drop-in replacement for BlazorBootstrap's ToastService. The `Notify(ToastMessage)`
/// overload preserves the BB call-site signature so the ~113 existing callers
/// continue to compile after a single `_Imports.razor` swap. The KmilaToastHost
/// component subscribes to OnChange and re-renders the toast stack with the
/// project's glass design.
public sealed class KmilaToastService
{
    public event Action? OnChange;

    private readonly List<KmilaToastItem> _items = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = new();
    private readonly object _gate = new();

    public IReadOnlyList<KmilaToastItem> Snapshot()
    {
        lock (_gate) return _items.ToArray();
    }

    /// BB compatibility — the existing helper `ToastMessages.Message(...)` and
    /// inline `new ToastMessage(...)` callers funnel through here unchanged.
    public void Notify(ToastMessage msg)
        => Show(MapLevel(msg.Type), msg.Message ?? string.Empty, msg.Title);

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

    public void Success(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Success, body, title, durationMs);

    public void Danger(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Danger, body, title, durationMs);

    public void Warning(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Warning, body, title, durationMs);

    public void Info(string body, string? title = null, int durationMs = 5000)
        => Show(KmilaToastLevel.Info, body, title, durationMs);

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
