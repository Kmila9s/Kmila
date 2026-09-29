namespace Kmila.Shared.Services.Api;

/// <summary>
/// Cross-host contract for the Phase B in-process API host. The concrete
/// implementation lives in <c>Kmila/Services/EmbeddedApiHost.cs</c> and is
/// only registered by the MAUI head — <c>Kmila.Web</c> ships the
/// same API surface natively (via <c>MapSimulationApi</c> in Program.cs)
/// and does NOT register this abstraction.
///
/// Exists so <c>Settings.razor</c> (compiled into both hosts) can inject
/// and drive the toggle without taking a compile-time reference to the
/// MAUI-only concrete type.
/// </summary>
public interface ILocalApiHost
{
    /// <summary>Effective URI the host is currently bound to; null when stopped.</summary>
    Uri? BoundUrl { get; }

    /// <summary>True between <see cref="StartAsync"/> and <see cref="StopAsync"/>.</summary>
    bool IsRunning { get; }

    /// <summary>False on runtime-refused platforms (iOS today).</summary>
    bool IsPlatformSupported { get; }

    /// <param name="port">TCP port; 0 means "let the OS pick".</param>
    /// <param name="bindLan">When true, bind IPAddress.Any; else loopback.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StartAsync(int port, bool bindLan, CancellationToken ct = default);

    /// <summary>Stops the API host if running.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task StopAsync(CancellationToken ct = default);
}
