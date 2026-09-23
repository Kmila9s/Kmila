namespace Kmila.Shared.Models.VersionControl;

/// <summary>Kind of change of a file relative to the last-synced snapshot.</summary>
public enum VcChangeKind { Added, Modified, Deleted }

/// <summary>One changed file in a project's working tree vs. its snapshot.</summary>
public record VcChange(string RelativePath, VcChangeKind Kind);

/// <summary>Working-tree status: the set of files that differ from the snapshot.</summary>
public class VcStatus
{
    public List<VcChange> Changes { get; init; } = new();
    public bool HasChanges => Changes.Count > 0;
    public int Added    => Changes.Count(c => c.Kind == VcChangeKind.Added);
    public int Modified => Changes.Count(c => c.Kind == VcChangeKind.Modified);
    public int Deleted  => Changes.Count(c => c.Kind == VcChangeKind.Deleted);
}

/// <summary>Outcome of a commit/push or pull operation.</summary>
public enum VcOutcome
{
    Success,
    UpToDate,
    NoChanges,
    NeedsPull,      // remote advanced since last sync — pull before pushing
    Conflict,       // remote advanced AND local has changes — manual reconcile
    NotSignedIn,
    RateLimited,
    Error
}

/// <summary>Result of a VC operation, with a user-facing message.</summary>
public record VcResult(VcOutcome Outcome, string? Message = null, string? CommitSha = null)
{
    public bool Ok => Outcome is VcOutcome.Success or VcOutcome.UpToDate or VcOutcome.NoChanges;
    public static VcResult Success(string? sha = null, string? msg = null) => new(VcOutcome.Success, msg, sha);
}

/// <summary>A commit entry from the repository history.</summary>
public record VcCommit(string Sha, string Message, string AuthorName, DateTime Date, string? HtmlUrl)
{
    public string ShortSha => Sha.Length >= 7 ? Sha[..7] : Sha;
}

/// <summary>Minimal repository descriptor for the link picker / create result.</summary>
public record VcRepoInfo(string Owner, string Name, string DefaultBranch, bool Private)
{
    public string FullName => $"{Owner}/{Name}";
}

/// <summary>One entry of a git tree (path → blob sha).</summary>
public record VcTreeEntry(string Path, string Sha, string Type);
