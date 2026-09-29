namespace Kmila.Shared.Models.VersionControl;

/// <summary>Kind of change of a file relative to the last-synced snapshot.</summary>
public enum VcChangeKind
{
    /// <summary>File is new relative to the snapshot.</summary>
    Added,
    /// <summary>File's contents changed relative to the snapshot.</summary>
    Modified,
    /// <summary>File was removed relative to the snapshot.</summary>
    Deleted
}

/// <summary>One changed file in a project's working tree vs. its snapshot.</summary>
public record VcChange(string RelativePath, VcChangeKind Kind);

/// <summary>Working-tree status: the set of files that differ from the snapshot.</summary>
public class VcStatus
{
    /// <summary>All files that differ from the snapshot.</summary>
    public List<VcChange> Changes { get; init; } = new();
    /// <summary>Whether there is at least one change.</summary>
    public bool HasChanges => Changes.Count > 0;
    /// <summary>Number of added files.</summary>
    public int Added    => Changes.Count(c => c.Kind == VcChangeKind.Added);
    /// <summary>Number of modified files.</summary>
    public int Modified => Changes.Count(c => c.Kind == VcChangeKind.Modified);
    /// <summary>Number of deleted files.</summary>
    public int Deleted  => Changes.Count(c => c.Kind == VcChangeKind.Deleted);
}

/// <summary>Outcome of a commit/push or pull operation.</summary>
public enum VcOutcome
{
    /// <summary>Operation completed successfully.</summary>
    Success,
    /// <summary>Local and remote were already in sync.</summary>
    UpToDate,
    /// <summary>There was nothing to commit or push.</summary>
    NoChanges,
    /// <summary>Remote advanced since last sync — pull before pushing.</summary>
    NeedsPull,      // remote advanced since last sync — pull before pushing
    /// <summary>Remote advanced AND local has changes — manual reconcile.</summary>
    Conflict,       // remote advanced AND local has changes — manual reconcile
    /// <summary>No authenticated user is signed in.</summary>
    NotSignedIn,
    /// <summary>The remote rejected the request due to rate limiting.</summary>
    RateLimited,
    /// <summary>The operation failed with an error.</summary>
    Error
}

/// <summary>Result of a VC operation, with a user-facing message.</summary>
public record VcResult(VcOutcome Outcome, string? Message = null, string? CommitSha = null)
{
    /// <summary>Whether the outcome represents a non-error state.</summary>
    public bool Ok => Outcome is VcOutcome.Success or VcOutcome.UpToDate or VcOutcome.NoChanges;
    /// <summary>Creates a successful result.</summary>
    /// <param name="sha">Commit SHA, when applicable.</param>
    /// <param name="msg">Optional user-facing message.</param>
    /// <returns>A successful <see cref="VcResult"/>.</returns>
    public static VcResult Success(string? sha = null, string? msg = null) => new(VcOutcome.Success, msg, sha);
}

/// <summary>A commit entry from the repository history.</summary>
public record VcCommit(string Sha, string Message, string AuthorName, DateTime Date, string? HtmlUrl)
{
    /// <summary>The first seven characters of the commit SHA.</summary>
    public string ShortSha => Sha.Length >= 7 ? Sha[..7] : Sha;
}

/// <summary>Minimal repository descriptor for the link picker / create result.</summary>
public record VcRepoInfo(string Owner, string Name, string DefaultBranch, bool Private)
{
    /// <summary>The full repository name in <c>Owner/Name</c> form.</summary>
    public string FullName => $"{Owner}/{Name}";
}

/// <summary>One entry of a git tree (path → blob sha).</summary>
public record VcTreeEntry(string Path, string Sha, string Type);
