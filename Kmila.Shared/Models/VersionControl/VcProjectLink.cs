using SQLite;

namespace Kmila.Shared.Models.VersionControl;

/// <summary>
/// Links a Kmila project to a GitHub repository for the home-grown version
/// control engine. One row per linked project. The engine keeps its own VC
/// logic (status/diff/commit/history) locally and uses GitHub's Git Data API
/// as the remote — this row holds the remote coordinates + the last-synced
/// head so divergence can be detected without extra calls.
/// </summary>
[Table("VcProjectLink")]
public class VcProjectLink
{
    /// <summary>Primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>The Kmila project this link belongs to (one link per project).</summary>
    [Unique]
    public int ProjectId { get; set; }

    /// <summary>Repository owner (GitHub login or org).</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Repository name.</summary>
    public string Repo { get; set; } = string.Empty;

    /// <summary>Tracked branch. Defaults to the repo's default branch (usually "main").</summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// Commit SHA of the last successful push/pull. Compared against the live
    /// remote head to detect divergence (remote advanced elsewhere). Null until
    /// the first successful sync.
    /// </summary>
    public string? LastSyncedCommitSha { get; set; }

    /// <summary>
    /// When true, saving a file triggers a debounced auto commit+push. Default
    /// OFF — must be explicitly enabled per project to preserve the
    /// offline-by-default stance.
    /// </summary>
    public bool AutoCommit { get; set; }

    /// <summary>Last time this link row was updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
