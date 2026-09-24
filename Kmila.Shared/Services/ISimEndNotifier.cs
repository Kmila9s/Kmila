namespace Kmila.Shared.Services;

/// <summary>
/// Platform-abstracted push-style notifier fired when a simulation
/// finishes. Implementations:
/// <list type="bullet">
///   <item><see cref="NoopSimEndNotifier"/> — used as the fallback when no
///         platform-specific notifier is registered. Useful for tests and
///         for the maccatalyst target where push notifications aren't a
///         priority.</item>
///   <item>Web — uses the browser Notification API via JS interop. Lives
///         in <see cref="WebSimEndNotifier"/> in this assembly.</item>
///   <item>MAUI — uses the native iOS UNUserNotificationCenter and Android
///         NotificationManager APIs. Lives in the Kmila MAUI project.</item>
/// </list>
///
/// The notifier is responsible for permission acquisition: callers can
/// fire-and-forget and don't need to know whether the user has granted
/// permission. Implementations should be idempotent w.r.t. permission
/// state — never block the caller waiting for a UI prompt.
/// </summary>
public interface ISimEndNotifier
{
    /// <summary>
    /// Fires a "sim finished" notification. Returns immediately; any
    /// platform UI (permission dialog, OS banner) happens out of band.
    /// Both strings come in already localized by the caller, so the
    /// platform layer doesn't need to know about <c>Traductor</c>.
    /// </summary>
    /// <param name="title">Notification title (typically the project name).</param>
    /// <param name="body">Body line (typically "Simulación completa · N señales").</param>
    Task NotifySimEndAsync(string title, string body);
}

/// <summary>No-op default — registered when no platform notifier is available.</summary>
public class NoopSimEndNotifier : ISimEndNotifier
{
    public Task NotifySimEndAsync(string title, string body) => Task.CompletedTask;
}
