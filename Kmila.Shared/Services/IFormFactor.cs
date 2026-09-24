namespace Kmila.Shared.Services;
/// <summary>
/// Defines an interface for getting information about the device's form factor and platform.
/// </summary>
public interface IFormFactor
{
    /// <summary>
    /// Gets the form factor of the device (e.g., Desktop, Mobile, Tablet).
    /// </summary>
    /// <returns>A string representing the form factor.</returns>
    public string GetFormFactor();
    /// <summary>
    /// Gets the platform of the device (e.g., Windows, Android, iOS).
    /// </summary>
    /// <returns>A string representing the platform.</returns>
    public string GetPlatform();
}
