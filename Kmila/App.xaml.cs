namespace Kmila;

/// <summary>
/// Main application class for the Kmila MAUI app.
/// Inherits from <see cref="Application"/> and configures the app's initial window and page.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class
    /// and loads the XAML-defined resources and styles.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the main application window with the root page.
    /// </summary>
    /// <param name="activationState">The activation state provided by the platform, or <c>null</c> if not available.</param>
    /// <returns>A new <see cref="Window"/> containing the <see cref="MainPage"/> with the title "Kmila".</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "Kmila" };
    }

    /// <summary>
    /// Tier 5.2 Phase B — release the embedded socket on background/quit
    /// so a re-launch doesn't fight the OS for the port.
    ///
    /// On Mac Catalyst / Windows this fires at quit — stop the host.
    ///
    /// On Android this fires on backgrounding. Since the Phase B follow-up
    /// added a foreground service (see <c>EmbeddedApiForegroundService</c>)
    /// whose whole job is to keep the socket alive, we deliberately DO NOT
    /// stop here. The service's <c>OnDestroy</c> / <c>OnTaskRemoved</c> is
    /// the correct teardown hook: quit / swipe-away → OnDestroy → StopInternalAsync.
    ///
    /// On iOS the whole EmbeddedApiHost code path is compile-time excluded
    /// (rev 3, 2026-08-02) so the reference here would fail to resolve —
    /// the guard now excludes iOS explicitly.
    /// </summary>
    protected override void OnSleep()
    {
        base.OnSleep();
#if KMILA_MAUI_API && !ANDROID && !IOS
        try
        {
            var services = IPlatformApplication.Current?.Services;
            var host     = services?.GetService(typeof(Kmila.Services.EmbeddedApiHost))
                           as Kmila.Services.EmbeddedApiHost;
            if (host is { IsRunning: true })
            {
                // Fire-and-forget: OnSleep runs on the UI thread and MAUI
                // does not expose an async override. StopAsync completes
                // in well under a second on loopback teardown.
                _ = Task.Run(async () =>
                {
                    try { await host.StopAsync(); }
                    catch { /* best-effort */ }
                });
            }
        }
        catch { /* best-effort teardown */ }
#endif
    }
}
