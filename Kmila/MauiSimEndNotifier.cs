using Kmila.Shared.Services;
using Microsoft.Extensions.Logging;

#if IOS || MACCATALYST
using UserNotifications;
#endif

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace Kmila;

/// <summary>
/// Native iOS / Android / MacCatalyst implementation of
/// <see cref="ISimEndNotifier"/>. Uses the platform notification APIs
/// directly so we don't take a third-party NuGet dependency — keeping
/// the MAUI build surface small.
///
/// Permission is requested lazily on first fire: the prompt arrives
/// when the user has just finished a simulation, which gives the dialog
/// clear context ("Want to be notified when sims finish?") instead of
/// landing as an out-of-context boot prompt.
///
/// Behavior parity across platforms:
/// <list type="bullet">
///   <item>iOS / MacCatalyst — UNUserNotificationCenter. Foregrounded apps
///         don't display the banner by default, which is the desired
///         behavior (in-app toast covers foreground).</item>
///   <item>Android — NotificationManagerCompat. Channel <c>kmila-sim-end</c>
///         created on first use; API 33+ requires runtime POST_NOTIFICATIONS
///         permission which we request inline.</item>
/// </list>
/// </summary>
public class MauiSimEndNotifier : ISimEndNotifier
{
    private readonly ILogger<MauiSimEndNotifier> _log;
#if ANDROID
    private bool _channelEnsured;
#endif

    public MauiSimEndNotifier(ILogger<MauiSimEndNotifier> log)
    {
        _log = log;
    }

    public async Task NotifySimEndAsync(string title, string body)
    {
        try
        {
#if IOS || MACCATALYST
            await NotifyAppleAsync(title, body);
#elif ANDROID
            await NotifyAndroidAsync(title, body);
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[MauiSimEndNotifier] notify failed");
        }
    }

#if IOS || MACCATALYST
    private async Task NotifyAppleAsync(string title, string body)
    {
        var center = UNUserNotificationCenter.Current;

        // Lazily request authorization. If the user denies, subsequent
        // calls return immediately — the OS caches the decision.
        var (granted, err) = await center.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        if (!granted)
        {
            if (err != null)
                _log.LogDebug("[MauiSimEndNotifier] notification permission denied: {Err}", err.LocalizedDescription);
            return;
        }

        var content = new UNMutableNotificationContent
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Kmila" : title,
            Body  = body ?? string.Empty,
            Sound = UNNotificationSound.Default
        };

        // 0.1s trigger — anything shorter and iOS may swallow it; null
        // trigger ("immediate") isn't supported via this API path.
        var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.1, repeats: false);
        var request = UNNotificationRequest.FromIdentifier(
            $"kmila-sim-end-{Guid.NewGuid()}", content, trigger);

        try { await center.AddNotificationRequestAsync(request); }
        catch (Exception ex) { _log.LogWarning(ex, "[MauiSimEndNotifier] add request failed"); }
    }
#endif

#if ANDROID
    private const string AndroidChannelId   = "kmila-sim-end";
    private const string AndroidChannelName = "Kmila simulations";

    private Task NotifyAndroidAsync(string title, string body)
    {
        var ctx = Android.App.Application.Context;
        if (ctx == null) return Task.CompletedTask;

        EnsureChannel(ctx);

        // API 33+ requires runtime POST_NOTIFICATIONS permission. Without
        // it Notify is silently dropped. Use the MAUI permission API.
        // Fire-and-forget — by the time the user sees the dialog the sim
        // is already done; their answer applies to future runs.
        _ = RequestAndroidPermissionIfNeededAsync();

        var notifManager = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
        if (notifManager == null) return Task.CompletedTask;

        var builder = new Notification.Builder(ctx, AndroidChannelId)
            .SetContentTitle(string.IsNullOrWhiteSpace(title) ? "Kmila" : title)
            .SetContentText(body ?? string.Empty)
            .SetSmallIcon(Android.Resource.Drawable.SymActionEmail)
            .SetAutoCancel(true);

        try { notifManager.Notify(notificationId: Math.Abs(Guid.NewGuid().GetHashCode()), builder.Build()); }
        catch (Exception ex) { _log.LogWarning(ex, "[MauiSimEndNotifier] notify failed"); }
        return Task.CompletedTask;
    }

    private void EnsureChannel(Context ctx)
    {
        if (_channelEnsured) return;
        var notifManager = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
        if (notifManager == null) return;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(AndroidChannelId, AndroidChannelName, NotificationImportance.Default)
            {
                Description = "Notifications when a simulation finishes."
            };
            notifManager.CreateNotificationChannel(channel);
        }
        _channelEnsured = true;
    }

    private async Task RequestAndroidPermissionIfNeededAsync()
    {
        try
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) return; // API < 33: no runtime grant
            var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
            if (status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted) return;
            await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "[MauiSimEndNotifier] android permission request failed");
        }
    }
#endif

}
