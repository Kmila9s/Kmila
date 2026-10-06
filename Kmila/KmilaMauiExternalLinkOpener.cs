using Kmila.Shared.Services;
using Microsoft.Maui.ApplicationModel;

namespace Kmila;

/// <summary>
/// MAUI implementation of <see cref="IExternalLinkOpener"/>. Opens external
/// URLs in the user's system browser via the platform <see cref="Launcher"/>,
/// because <c>window.open(url, "_blank")</c> is a no-op inside the
/// BlazorWebView (WKWebView / WebView2) — see the interface docs and issue #8.
/// </summary>
public class KmilaMauiExternalLinkOpener : IExternalLinkOpener
{
    /// <inheritdoc />
    public async Task OpenAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        try { await Launcher.Default.OpenAsync(uri); }
        catch { /* launcher unavailable on this platform — best-effort, swallow */ }
    }
}
