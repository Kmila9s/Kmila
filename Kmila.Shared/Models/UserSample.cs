using SQLite;

namespace Kmila.Shared.Models;

/// <summary>
/// A user-authored sample: an exported project bundle promoted to a reusable
/// gallery entry. Lives alongside the built-in examples in the Examples gallery,
/// but is stored in the database (writable) rather than as a bundled asset.
/// The bundle payload is the exact JSON produced by
/// <see cref="Services.ProjectBundleService.ExportAsync"/>, so "use this sample"
/// just re-imports it as a fresh project.
/// </summary>
[Table("UserSamples")]
public class UserSample
{
    /// <summary>Primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Display title shown on the gallery card.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// BlazorBootstrap <c>IconName</c> enum member name (e.g. "Cpu", "123").
    /// Resolved defensively at render time; an unknown value falls back to a
    /// default icon, matching the built-in examples' <c>ResolveIcon</c> path.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Short description shown under the title.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The full project bundle JSON (schema per
    /// <see cref="Services.ProjectBundleService.SchemaVersion"/>). Re-imported
    /// verbatim when the user picks this sample.
    /// </summary>
    public string BundleJson { get; set; } = string.Empty;

    /// <summary>When this sample was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
