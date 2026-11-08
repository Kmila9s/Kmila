using Foundation;

namespace Kmila;

/// <summary>
/// UIScene delegate for the Kmila MAUI app. Apps built with the iOS 27 SDK MUST adopt the
/// UIScene life cycle (UIKit refuses to launch AppDelegate-only apps: "UIScene life cycle
/// is required for apps built with this SDK"). MAUI's <see cref="MauiUISceneDelegate"/>
/// creates and drives the window; Info.plist's UIApplicationSceneManifest points at the
/// Objective-C name registered here.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
