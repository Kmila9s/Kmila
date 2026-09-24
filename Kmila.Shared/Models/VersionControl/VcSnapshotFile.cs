using SQLite;

namespace Kmila.Shared.Models.VersionControl;

/// <summary>
/// One file of a project's last-synced snapshot (the state at the last
/// successful push/pull). This is the hidden local mirror that lets the engine
/// compute status and diffs entirely offline (working files vs. these rows,
/// zero API calls) and lets a push reuse <see cref="BlobSha"/> for unchanged
/// files so only changed blobs are uploaded. Replaced wholesale after each
/// successful sync.
/// </summary>
[Table("VcSnapshotFile")]
public class VcSnapshotFile
{
    /// <summary>Primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>The Kmila project this snapshot file belongs to.</summary>
    [Indexed]
    public int ProjectId { get; set; }

    /// <summary>Project-relative path (forward slashes), matching <see cref="ProjectFile.RelativePath"/>.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>The git blob SHA GitHub assigned to this file at last sync (reused to skip re-upload).</summary>
    public string BlobSha { get; set; } = string.Empty;

    /// <summary>The file content at last sync — the baseline for status and diffs.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>When this snapshot row was written (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
