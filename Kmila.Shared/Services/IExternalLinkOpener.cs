using Microsoft.JSInterop;

namespace Kmila.Shared.Services;

/// <summary>
/// Platform-abstracted opener for EXTERNAL links (the satisfaction survey, the
/// GitHub device-flow verification page, docs, etc.).
///
/// Needed because <c>window.open(url, "_blank")</c> — the pattern previously
/// used at every call site — silently does nothing inside the MAUI
/// BlazorWebView (WKWebView on macCatalyst, WebView2 on Windows): there is no
/// browser chrome to host a new tab, so the call no-ops and the user sees
/// nothing happen (issue #8).
///
/// Implementations:
/// <list type="bullet">
///   <item>Web / WASM → <see cref="JsExternalLinkOpener"/> in this assembly:
///         real browsers honour <c>window.open</c>, so a new tab opens.</item>
///   <item>MAUI → <c>KmilaMauiExternalLinkOpener</c> in the Kmila host
///         project: routes through <c>Microsoft.Maui.ApplicationModel.Launcher</c>
///         so the link opens in the user's system browser.</item>
/// </list>
/// </summary>
public interface IExternalLinkOpener
{
    /// <summary>
    /// Opens <paramref name="url"/> outside the app (new tab on web, system
    /// browser on desktop/mobile). Best-effort and fire-and-forget safe: a
    /// blank/invalid URL or a missing platform capability is swallowed rather
    /// than thrown, so callers never need a try/catch.
    /// </summary>
    Task OpenAsync(string url);
}

/// <summary>
/// Web/WASM implementation: a real browser honours <c>window.open</c>. Includes
/// <c>noopener</c> so the opened tab can't reach back into this one.
/// </summary>
public class JsExternalLinkOpener : IExternalLinkOpener
{
    private readonly IJSRuntime _js;

    public JsExternalLinkOpener(IJSRuntime js) => _js = js;

    /// <inheritdoc />
    public async Task OpenAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try { await _js.InvokeVoidAsync("open", url, "_blank", "noopener"); }
        catch { /* prerender / no JS available — nothing we can do from here */ }
    }
}
