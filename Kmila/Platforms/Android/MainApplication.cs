using Android.App;
using Android.Runtime;

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
}
