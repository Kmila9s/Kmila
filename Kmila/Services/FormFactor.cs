using Kmila.Shared.Services;

namespace Kmila.Services;

/// <summary>
/// MAUI implementation of <see cref="IFormFactor"/> that provides device form factor
/// and platform information using MAUI's <see cref="DeviceInfo"/> API.
/// </summary>
public class FormFactor : IFormFactor
{
    /// <summary>
    /// Gets the form factor of the current device using MAUI's DeviceInfo.
    /// </summary>
    /// <returns>A string representing the device idiom (e.g., "Phone", "Tablet", "Desktop", "TV", "Watch").</returns>
    public string GetFormFactor()
    {
        return DeviceInfo.Idiom.ToString();
    }

    /// <summary>
    /// Gets the platform and version information of the current device.
    /// </summary>
    /// <returns>A string combining the platform name and version (e.g., "Android - 14.0").</returns>
    public string GetPlatform()
    {
        return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
    }
}
