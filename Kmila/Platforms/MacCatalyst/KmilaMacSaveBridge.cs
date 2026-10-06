using Foundation;
using UIKit;
using WebKit;

namespace Kmila;

/// <summary>
/// Bridges native macOS menu commands to the Blazor editor. On Mac Catalyst a
/// ⌘-key equivalent is handled by the menu / responder chain BEFORE the
/// WKWebView gets the key event, so the JS keydown handler in
/// kmila-shared-head.js never fires for ⌘-combos (issue #2 on macOS). The
/// native menu commands (see <c>AppDelegate.BuildMenu</c>) call into here, which
/// drives the WebView via JavaScript.
///
/// Clipboard (⌘X/⌘C/⌘V/⌘A) gets special treatment: Mac Catalyst's WKWebView
/// does not deliver the system edit actions into the web content (a documented
/// platform limitation — Monaco #2205, Uno #9877), so we move text between the
/// system <see cref="UIPasteboard"/> and the focused Monaco editor ourselves.
///
/// The WKWebView is captured once when the BlazorWebView handler is created
/// (see the handler mapper in <c>MauiProgram</c>).
/// </summary>
internal static class KmilaMacSaveBridge
{
    /// <summary>The app's single BlazorWebView platform view, set by the handler mapper.</summary>
    internal static WKWebView? WebView;

    /// <summary>
    /// Runs a named editor command (save / new / build / run / split) by
    /// evaluating the shared <c>window.kmilaRunEditorCommand</c> entry point.
    /// Best-effort: a no-op when the WebView isn't ready or we're off the editor
    /// route (the JS side guards the route itself).
    /// </summary>
    internal static void Run(string command)
    {
        var wv = WebView;
        if (wv == null || string.IsNullOrEmpty(command)) return;
        // command is a fixed internal identifier, but strip quotes defensively
        // so the inline JS literal can never be broken.
        var safe = command.Replace("'", string.Empty).Replace("\\", string.Empty);
        try
        {
            wv.EvaluateJavaScript(
                $"window.kmilaRunEditorCommand && window.kmilaRunEditorCommand('{safe}');",
                (_, __) => { });
        }
        catch { /* WebView mid-teardown — nothing to do */ }
    }

    /// <summary>Back-compat shim.</summary>
    internal static void TriggerSave() => Run("save");

    /// <summary>⌘C — copy Monaco's selection to the system pasteboard.</summary>
    internal static void Copy() => CopySelectionToPasteboard("window.kmilaNativeCopy ? window.kmilaNativeCopy() : ''");

    /// <summary>⌘X — cut Monaco's selection to the system pasteboard.</summary>
    internal static void Cut() => CopySelectionToPasteboard("window.kmilaNativeCut ? window.kmilaNativeCut() : ''");

    private static void CopySelectionToPasteboard(string js)
    {
        var wv = WebView;
        if (wv == null) return;
        try
        {
            wv.EvaluateJavaScript(js, (result, _) =>
            {
                var text = (result as NSString)?.ToString();
                if (!string.IsNullOrEmpty(text)) UIPasteboard.General.String = text;
            });
        }
        catch { /* WebView mid-teardown */ }
    }

    /// <summary>⌘V — insert the system pasteboard text into the focused Monaco editor.</summary>
    internal static void Paste()
    {
        var wv = WebView;
        if (wv == null) return;
        var text = UIPasteboard.General.String;
        if (string.IsNullOrEmpty(text)) return;
        // JSON-encode so newlines/quotes/backslashes in the clipboard survive the
        // inline literal intact.
        var json = System.Text.Json.JsonSerializer.Serialize(text);
        try
        {
            wv.EvaluateJavaScript(
                $"window.kmilaNativePaste && window.kmilaNativePaste({json});",
                (_, __) => { });
        }
        catch { /* WebView mid-teardown */ }
    }

    /// <summary>⌘A — select all text in the focused Monaco editor.</summary>
    internal static void SelectAll()
    {
        var wv = WebView;
        if (wv == null) return;
        try
        {
            wv.EvaluateJavaScript(
                "window.kmilaNativeSelectAll && window.kmilaNativeSelectAll();",
                (_, __) => { });
        }
        catch { /* WebView mid-teardown */ }
    }
}
