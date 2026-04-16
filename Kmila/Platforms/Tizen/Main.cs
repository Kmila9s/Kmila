using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Kmila;

/// <summary>
/// Tizen platform entry point for the Kmila MAUI application.
/// Extends <see cref="MauiApplication"/> to integrate with the Tizen application lifecycle.
/// </summary>
class Program : MauiApplication
{
    /// <summary>
    /// Creates the MAUI application instance by delegating to <see cref="MauiProgram.CreateMauiApp"/>.
    /// </summary>
    /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>
    /// Main entry point of the Tizen application.
    /// Creates a new <see cref="Program"/> instance and starts the Tizen application run loop.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application at launch.</param>
    static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }
}
