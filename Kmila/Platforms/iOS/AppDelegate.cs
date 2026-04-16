using Foundation;

namespace Kmila;

/// <summary>
/// iOS application delegate for the Kmila MAUI app.
/// Registered with the Objective-C runtime as "AppDelegate" and serves as the
/// entry point for the iOS application lifecycle.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>
    /// Creates the MAUI application instance by delegating to <see cref="MauiProgram.CreateMauiApp"/>.
    /// </summary>
    /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
