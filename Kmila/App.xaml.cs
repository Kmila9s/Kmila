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
    /// On iOS (B2, 2026-11-06) there is no foreground-service equivalent, so the
    /// TcpListener host runs only while foregrounded: stop it here on sleep and
    /// restart it in <see cref="OnResume"/>.
    /// </summary>
    protected override void OnSleep()
    {
        base.OnSleep();
#if KMILA_MAUI_API && !ANDROID
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

#if KMILA_MAUI_API && IOS
    /// <summary>
    /// iOS only (B2): the embedded API host has no background keep-alive, so it
    /// is stopped on sleep. Restart it here when the app returns to the
    /// foreground, honouring the persisted enable / port / LAN settings.
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();
        try
        {
            var services = IPlatformApplication.Current?.Services;
            var host     = services?.GetService(typeof(Kmila.Services.EmbeddedApiHost))
                           as Kmila.Services.EmbeddedApiHost;
            if (host is null || host.IsRunning) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    var db = services?.GetService(typeof(Kmila.Shared.Data.ApplicationDbContext))
                             as Kmila.Shared.Data.ApplicationDbContext;
                    if (db is null) return;
                    var enabled = (await db.GetSettingAsync(Kmila.Shared.Models.AppSettings.KEY_LOCAL_API_ENABLED)) == "true";
                    if (!enabled) return;

                    var portStr = await db.GetSettingAsync(Kmila.Shared.Models.AppSettings.KEY_LOCAL_API_PORT);
                    int port    = int.TryParse(portStr, out var p) ? p : 5298;
                    var lan     = (await db.GetSettingAsync(Kmila.Shared.Models.AppSettings.KEY_LOCAL_API_BIND_LAN)) == "true";

                    await host.StartAsync(port, lan, CancellationToken.None);
                }
                catch { /* best-effort restart */ }
            });
        }
        catch { /* best-effort */ }
    }
#endif
}
