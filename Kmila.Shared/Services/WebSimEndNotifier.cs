using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Kmila.Shared.Services;

/// <summary>
/// Web (browser) implementation of <see cref="ISimEndNotifier"/>. Uses
/// the browser <c>Notification</c> API via JS interop. Permission is
/// requested lazily on first fire so the prompt has clear context
/// (the user just finished a sim).
///
/// Most browsers only display the notification when the tab is
/// backgrounded; foreground tabs typically suppress it. That matches
/// the desired behavior: when the user is looking at the app, the
/// in-app toast is sufficient; when they're elsewhere, the OS-level
/// banner pings them.
/// </summary>
public class WebSimEndNotifier : ISimEndNotifier
{
    private readonly IJSRuntime _js;
    private readonly ILogger<WebSimEndNotifier> _log;

    /// <summary>Creates the notifier with the JS runtime and logger.</summary>
    /// <param name="js">JS runtime used to invoke the browser Notification API.</param>
    /// <param name="log">Logger for notification failures.</param>
    public WebSimEndNotifier(IJSRuntime js, ILogger<WebSimEndNotifier> log)
    {
        _js = js;
        _log = log;
    }

    /// <summary>Shows an OS-level notification that a simulation has ended.</summary>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body text.</param>
    /// <returns>A task that completes once the JS interop call returns.</returns>
    public async Task NotifySimEndAsync(string title, string body)
    {
        try
        {
            await _js.InvokeVoidAsync("kmilaNotifySimEnd", title, body);
        }
        catch (Microsoft.JSInterop.JSException ex)
        {
            _log.LogDebug(ex, "[WebSimEndNotifier] JS notify call failed (Notification API unsupported or denied)");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[WebSimEndNotifier] unexpected notify failure");
        }
    }
}
