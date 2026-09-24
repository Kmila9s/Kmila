using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Kmila;

/// <summary>
/// Android main activity for the Kmila MAUI application.
/// Configured as the launcher activity with the MAUI splash theme and
/// handles multiple configuration changes without recreating the activity.
/// </summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <summary>
    /// NAV-1 (2026-07-24, Session 31): intercept the hardware back key
    /// and route it through JS so open modals/drawers/popovers close
    /// before the OS default (finish activity) fires. Prior to this
    /// override, tapping Back inside the Editor with a modal open
    /// exited the app instead of closing the modal.
    ///
    /// Flow:
    ///  1. Locate the Android WebView hosted by MAUI's BlazorWebView
    ///     (walking the DecorView tree — there's exactly one).
    ///  2. Call <c>window.kmilaHandleAndroidBack()</c> in the page. That
    ///     handler (in <c>kmila-shared-head.js</c>) closes the topmost
    ///     open surface (modal → drawer → popover → &lt;details&gt;) OR
    ///     calls <c>window.history.back()</c> if there's Blazor router
    ///     history to consume. Returns <c>true</c> when it handled the
    ///     press, <c>false</c> when nothing was open and no history
    ///     was available.
    ///  3. When JS returns <c>false</c>, finish the activity — the
    ///     user really is at the root and wanted to exit.
    ///
    /// <c>OnBackPressed</c> is marked deprecated on Android 13+ but is
    /// still the reliable path for MAUI single-Activity apps. Using
    /// <c>OnBackPressedDispatcher</c> would require carefully juggling
    /// the callback's Enabled flag around the JS round-trip (async
    /// callback re-fires the dispatcher recursively otherwise) — no
    /// UX benefit, more state.
    /// </summary>
#pragma warning disable CS0672 // OnBackPressed deprecation on target API 33+
    public override void OnBackPressed()
    {
        var webview = FindWebView(Window?.DecorView?.RootView);
        if (webview is null)
        {
            base.OnBackPressed();
            return;
        }
        webview.EvaluateJavascript(
            "(function(){try{return window.kmilaHandleAndroidBack ? kmilaHandleAndroidBack() : false;}catch(e){return false;}})()",
            new BackJsCallback(this));
        // Do NOT call base.OnBackPressed() here — the JS callback runs
        // async on the UI thread and decides whether to finish() or not.
    }
#pragma warning restore CS0672

    private static Android.Webkit.WebView? FindWebView(Android.Views.View? root)
    {
        if (root is Android.Webkit.WebView webview) return webview;
        if (root is Android.Views.ViewGroup group)
        {
            for (int i = 0; i < group.ChildCount; i++)
            {
                var found = FindWebView(group.GetChildAt(i));
                if (found is not null) return found;
            }
        }
        return null;
    }

    private sealed class BackJsCallback : Java.Lang.Object, Android.Webkit.IValueCallback
    {
        private readonly MainActivity _owner;
        public BackJsCallback(MainActivity owner) { _owner = owner; }
        public void OnReceiveValue(Java.Lang.Object? value)
        {
            // WebView.EvaluateJavascript wraps the JS return value as a
            // JSON-encoded string: `"true"`, `"false"`, `"null"`, etc.
            var s = value?.ToString();
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                return; // JS handled — nothing to do.

            // Nothing was open in the UI and no history to pop → exit the
            // activity the same way default OnBackPressed would.
            _owner.Finish();
        }
    }
}
