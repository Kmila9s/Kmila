using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Kmila;

/// <summary>
/// Android main activity for the Kmila MAUI application.
/// Configured as the launcher activity with the MAUI splash theme and
/// handles multiple configuration changes without recreating the activity.
/// </summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
