using ObjCRuntime;
using UIKit;

namespace Kmila;

/// <summary>
/// Contains the main entry point for the Kmila Mac Catalyst application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point of the Mac Catalyst application.
    /// Bootstraps the UIKit application with the <see cref="AppDelegate"/> class.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application at launch.</param>
    static void Main(string[] args)
    {
        // To use a different Application Delegate class from "AppDelegate",
        // replace typeof(AppDelegate) with the desired delegate type.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}