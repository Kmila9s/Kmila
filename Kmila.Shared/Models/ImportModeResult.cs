namespace Kmila.Shared.Models;

/// <summary>
/// The user's choice from the import-mode dialog: import a bundle as a raw
/// project, or promote it to a reusable gallery sample (with display metadata).
/// </summary>
public class ImportModeResult
{
    /// <summary>True → save as a gallery sample; false → raw project import.</summary>
    public bool AsSample { get; set; }

    /// <summary>Sample title (only meaningful when <see cref="AsSample"/>).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>BlazorBootstrap icon-name string for the sample card.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Short sample description shown under the title.</summary>
    public string Description { get; set; } = string.Empty;
}
