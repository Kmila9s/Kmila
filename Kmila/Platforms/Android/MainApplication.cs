using Android.App;
using Android.Content;
using Android.Runtime;
using Kmila.Shared.Services;

namespace Kmila;

/// <summary>
/// Android application entry point for the Kmila MAUI app.
/// Registered as the main <see cref="Android.App.Application"/> class and delegates
/// MAUI app creation to <see cref="MauiProgram"/>.
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainApplication"/> class.
    /// Required by Android for application instantiation via the Java runtime.
    /// </summary>
    /// <param name="handle">A pointer to the Java object that this managed object wraps.</param>
    /// <param name="ownership">Indicates how the handle ownership is managed between the Java and managed runtimes.</param>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <summary>
    /// Creates the MAUI application instance by delegating to <see cref="MauiProgram.CreateMauiApp"/>.
    /// </summary>
    /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// Android memory hardening: forwards the OS's memory signals to the run
    /// watchdog. Every level first releases garbage; once the system says it is
    /// about to start killing processes (RUNNING_CRITICAL while in the foreground,
    /// COMPLETE in the background) a running simulation is stopped gracefully, so
    /// the user keeps the partial waveform instead of the app being killed.
    /// </summary>
    public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
    {
        base.OnTrimMemory(level);
        if (level == TrimMemory.RunningCritical || level == TrimMemory.Complete)
            RunWatchdog.ReportCriticalMemoryPressure();
        if (level >= TrimMemory.RunningLow)
            GC.Collect();
    }

    /// <summary>Last-resort low-memory callback: stop a running simulation and collect.</summary>
    public override void OnLowMemory()
    {
        base.OnLowMemory();
        RunWatchdog.ReportCriticalMemoryPressure();
        GC.Collect();
    }
}
