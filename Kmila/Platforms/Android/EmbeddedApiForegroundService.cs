#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Extensions.Logging;

namespace Kmila.Platforms.Android;

/// <summary>
/// Tier 5.2 Phase B follow-up — Android foreground service that keeps the
/// process (and thus <see cref="Kmila.Services.EmbeddedApiHost"/>'s
/// <c>HttpListener</c> accept loop) alive when the user backgrounds the
/// app. Without this, Android's OOM-killer reclaims the socket within a
/// few seconds and SimulatorBench / curl calls start timing out.
///
/// Design:
/// <list type="bullet">
///   <item>Started by <c>EmbeddedApiHost.StartAsync</c> on Android via
///     <see cref="Context.StartForegroundService"/>; stopped by
///     <c>StopAsync</c> via <see cref="Context.StopService"/>.</item>
///   <item>Delegates the actual HTTP work to the singleton
///     <c>EmbeddedApiHost.StartInternalAsync</c> (resolved from the MAUI
///     DI container) — the host stays lifecycle-agnostic.</item>
///   <item>Shows a low-importance ongoing notification with the bound URL
///     so Android's foreground-service policy is satisfied and the user
///     can see the server is running.</item>
///   <item>API 34+ (Android 14) requires <c>foregroundServiceType</c> in
///     the manifest AND at the <c>StartForeground</c> call site; we pass
///     <see cref="ForegroundService.TypeDataSync"/> at both. See
///     AndroidManifest.xml for the permission triad.</item>
/// </list>
/// </summary>
[Service(
    Exported             = false,
    ForegroundServiceType = ForegroundService.TypeDataSync,
    Name                  = "kmila.kmila.EmbeddedApiForegroundService")]
public sealed class EmbeddedApiForegroundService : Service
{
    private const int    NotificationId      = 42_98; // shares digits with default port 5298
    private const string NotificationChannel = "kmila-local-api";

    public const string ExtraPort    = "kmila.port";
    public const string ExtraBindLan = "kmila.bindLan";

    public override IBinder? OnBind(Intent? intent) => null; // started service, no binder

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var port    = intent?.GetIntExtra(ExtraPort, 5298)     ?? 5298;
        var bindLan = intent?.GetBooleanExtra(ExtraBindLan, false) ?? false;

        // Post the ongoing notification BEFORE any await — Android 8+
        // requires StartForeground() within ~5 s of OnStartCommand.
        var notification = BuildNotification(port);
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        // Bring up the HttpListener on a thread-pool thread so
        // OnStartCommand returns promptly.
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var services = IPlatformApplication.Current?.Services;
                var host = services?.GetService<Kmila.Services.EmbeddedApiHost>();
                if (host is null) return;
                await host.StartInternalAsync(port, bindLan, System.Threading.CancellationToken.None);
                UpdateNotificationForBoundUrl(host);
            }
            catch (System.Exception ex)
            {
                var log = IPlatformApplication.Current?.Services
                    .GetService<ILoggerFactory>()?.CreateLogger("EmbeddedApiForegroundService");
                log?.LogError(ex, "[EmbeddedApiForegroundService] Start failed.");
                StopSelf();
            }
        });

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        // User-initiated stop path (StopService from Settings toggle) OR
        // OS teardown. Either way, drop the HttpListener before we die
        // so a re-start doesn't fight the OS for port 5298.
        try
        {
            var services = IPlatformApplication.Current?.Services;
            var host = services?.GetService<Kmila.Services.EmbeddedApiHost>();
            if (host is { IsRunning: true })
            {
                // Sync-wait — OnDestroy has no async override and the
                // process is going down anyway, so blocking is fine.
                host.StopInternalAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
            }
        }
        catch { /* best-effort */ }

        if (OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            StopForeground(StopForegroundFlags.Remove);
        }
        else
        {
#pragma warning disable CA1422 // pre-API 24 fallback
            StopForeground(true);
#pragma warning restore CA1422
        }

        base.OnDestroy();
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        // User swiped Kmila off the recents list. Treat that as a stop —
        // the app is truly gone; keep-alive would be surprising.
        StopSelf();
        base.OnTaskRemoved(rootIntent);
    }

    private Notification BuildNotification(int port, string? boundUrl = null)
    {
        EnsureChannel();
        var text = boundUrl ?? $"Kmila local API — starting on port {port}…";
        var builder = new NotificationCompat.Builder(this, NotificationChannel)
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload) // stock icon; avoids a res dependency
            .SetContentTitle("Kmila local API")
            .SetContentText(text)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .SetCategory(NotificationCompat.CategoryService);
        return builder.Build();
    }

    private void UpdateNotificationForBoundUrl(Kmila.Services.EmbeddedApiHost host)
    {
        var url = host.BoundUrl?.ToString();
        if (string.IsNullOrEmpty(url)) return;
        var notif   = BuildNotification(0, boundUrl: url);
        var manager = GetSystemService(NotificationService) as NotificationManager;
        manager?.Notify(NotificationId, notif);
    }

    private void EnsureChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var manager = GetSystemService(NotificationService) as NotificationManager;
        if (manager is null) return;
        var existing = manager.GetNotificationChannel(NotificationChannel);
        if (existing is not null) return;
        var channel = new NotificationChannel(
            NotificationChannel,
            "Local API",
            NotificationImportance.Low)
        {
            Description = "Ongoing notification while the /api/v1 host is serving.",
        };
        manager.CreateNotificationChannel(channel);
    }
}
#endif
