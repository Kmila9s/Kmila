using Kmila.Shared.Models.VersionControl;

namespace Kmila.Shared.Services.VersionControl;

/// <summary>
/// Per-project version control over a GitHub repository. Owns the VC logic
/// (status/diff/commit/history) locally and uses <see cref="GitHubRemote"/> for
/// the remote. v1 is linear: link/create, status, diff, manual commit→push,
/// pull-as-fast-forward, history, plus an opt-in per-project auto-commit.
/// </summary>
public interface IProjectVersionControl
{
    /// <summary>The repo link for a project, or null when not linked.</summary>
    Task<VcProjectLink?> GetLinkAsync(int projectId);

    /// <summary>Links an existing repo and seeds the local baseline from it
    /// (remote content is adopted — the caller should confirm first).</summary>
    Task<VcResult> LinkExistingAsync(int projectId, string owner, string repo, string branch);

    /// <summary>Creates a new repo under the signed-in account and links it.
    /// The project's files then show as additions to push.</summary>
    Task<VcResult> CreateAndLinkAsync(int projectId, string repoName, string? description, bool isPrivate);

    /// <summary>Removes the link + local snapshot (does not touch the remote repo or local files).</summary>
    Task UnlinkAsync(int projectId);

    /// <summary>Working-tree changes vs. the last-synced snapshot (offline, no API calls).</summary>
    Task<VcStatus> GetStatusAsync(int projectId);

    /// <summary>Unified diff of one file (snapshot vs. working).</summary>
    Task<string> GetFileDiffAsync(int projectId, string relativePath);

    /// <summary>Commits all working changes and pushes. Blocks on divergence unless forced.</summary>
    Task<VcResult> CommitAndPushAsync(int projectId, string message, bool force = false);

    /// <summary>Fast-forward pull. Blocks if local has changes and remote advanced, unless forced.</summary>
    Task<VcResult> PullAsync(int projectId, bool force = false);

    /// <summary>Recent commit history from the remote.</summary>
    Task<List<VcCommit>?> GetHistoryAsync(int projectId);

    /// <summary>Sets the per-project auto-commit-on-save flag.</summary>
    Task SetAutoCommitAsync(int projectId, bool enabled);

    /// <summary>Auto-commit hook (called from the editor save flow). No-op unless the
    /// project has auto-commit on, is signed in, and has changes.</summary>
    Task<VcResult> TryAutoCommitAsync(int projectId);

    /// <summary>True when the most recent remote call hit a rate limit.</summary>
    bool LastRateLimited { get; }
}
