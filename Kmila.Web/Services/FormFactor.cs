using Kmila.Shared.Services;

namespace Kmila.Web.Services;

/// <summary>
/// Web implementation of <see cref="IFormFactor"/> that provides device form factor
/// and platform information for the Blazor Server hosted environment.
/// </summary>
public class FormFactor : IFormFactor
{
    /// <summary>
    /// Gets the form factor for the Web platform.
    /// </summary>
    /// <returns>Always returns "Web" since Blazor Server runs in a browser context.</returns>
    public string GetFormFactor()
    {
        return "Web";
    }

    /// <summary>
    /// Gets the platform information of the server hosting the Blazor application.
    /// </summary>
    /// <returns>A string representation of the server's operating system version.</returns>
    public string GetPlatform()
    {
        return Environment.OSVersion.ToString();
    }
}
