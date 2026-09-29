using Kmila.Shared.Data;
using Kmila.Shared.Models;
using Kmila.Shared.Models.VersionControl;
using Kmila.Shared.Repositories;
using Kmila.Shared.Services.Practice;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.VersionControl;

/// <summary>
/// The home-grown version-control engine. Keeps a local snapshot mirror
/// (<see cref="VcSnapshotFile"/>) so status and diffs are computed offline;
/// pushes build real git commits through <see cref="GitHubRemote"/> (blobs →
/// tree → commit → ref), uploading only changed blobs. Linear v1 — no merge.
/// </summary>
public class ProjectVersionControl : IProjectVersionControl
{
    private readonly ApplicationDbContext _db;
    private readonly ProjectFiles _files;
    private readonly Projects _projects;
    private readonly GitHubRemote _remote;
    private readonly GitHubAuth _auth;
    private readonly ILogger<ProjectVersionControl> _logger;
    private readonly Task _initTask;

    /// <summary>True when the most recent remote call hit the GitHub rate limit.</summary>
    public bool LastRateLimited => _remote.LastRateLimited;

    /// <summary>Creates the version-control engine and kicks off database initialization.</summary>
    /// <param name="db">Application database used for links and the local snapshot mirror.</param>
    /// <param name="files">Project file repository for the working tree.</param>
    /// <param name="projects">Project repository.</param>
    /// <param name="remote">GitHub remote used to build commits and read refs/trees/blobs.</param>
    /// <param name="auth">GitHub authentication state.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public ProjectVersionControl(
        ApplicationDbContext db, ProjectFiles files, Projects projects,
        GitHubRemote remote, GitHubAuth auth, ILogger<ProjectVersionControl>? logger = null)
    {
        _db = db;
        _files = files;
        _projects = projects;
        _remote = remote;
        _auth = auth;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectVersionControl>.Instance;
        _initTask = _db.InitDBAsync();
    }

    // ─── Link management ─────────────────────────────────────────

    /// <summary>Returns the repository link for a project, or null when the project is not linked.</summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The link record, or null.</returns>
    public async Task<VcProjectLink?> GetLinkAsync(int projectId)
    {
        await _initTask;
        return await _db.VcProjectLinks.Where(l => l.ProjectId == projectId).FirstOrDefaultAsync();
    }

    private async Task UpsertLinkAsync(VcProjectLink link)
    {
        link.UpdatedAt = DateTime.UtcNow;
        var existing = await _db.VcProjectLinks.Where(l => l.ProjectId == link.ProjectId).FirstOrDefaultAsync();
        if (existing == null) await _db.InsertAsync(link);
        else { link.Id = existing.Id; await _db.UpdateAsync(link); }
    }

    /// <summary>Removes a project's repository link and its local snapshot mirror.</summary>
    /// <param name="projectId">The project id.</param>
    public async Task UnlinkAsync(int projectId)
    {
        await _initTask;
        await _db.ExecuteAsync("DELETE FROM VcProjectLink WHERE ProjectId = ?", projectId);
        await _db.ExecuteAsync("DELETE FROM VcSnapshotFile WHERE ProjectId = ?", projectId);
    }

    /// <summary>Links a project to an existing repository and seeds the baseline by pulling (remote wins).</summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="branch">Branch to track; defaults to "main" when blank.</param>
    /// <returns>The outcome of the operation.</returns>
    public async Task<VcResult> LinkExistingAsync(int projectId, string owner, string repo, string branch)
    {
        await _initTask;
        if (!_auth.IsLoggedIn) return new VcResult(VcOutcome.NotSignedIn);
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return new VcResult(VcOutcome.Error, "Owner and repository are required.");

        await UpsertLinkAsync(new VcProjectLink
        {
            ProjectId = projectId, Owner = owner.Trim(), Repo = repo.Trim(),
            Branch = string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim(),
            LastSyncedCommitSha = null
        });
        // Seed the baseline from the remote (remote wins). force:true because
        // linking is an explicit "adopt this repo" action; the UI confirms.
        return await PullAsync(projectId, force: true);
    }

    /// <summary>Creates a new GitHub repository and links the project to it.</summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="repoName">Name for the new repository.</param>
    /// <param name="description">Optional repository description.</param>
    /// <param name="isPrivate">True to create a private repository (requires the 'repo' scope).</param>
    /// <returns>The outcome of the operation.</returns>
    public async Task<VcResult> CreateAndLinkAsync(int projectId, string repoName, string? description, bool isPrivate)
    {
        await _initTask;
        if (!_auth.IsLoggedIn) return new VcResult(VcOutcome.NotSignedIn);
        if (isPrivate && !await _auth.GetPrivateScopeAsync())
            return new VcResult(VcOutcome.Error, "Private repositories need the 'repo' scope — enable it in Settings and sign in again.");

        var info = await _remote.CreateRepoAsync(repoName.Trim(), description, isPrivate);
        if (info == null)
            return new VcResult(_remote.LastRateLimited ? VcOutcome.RateLimited : VcOutcome.Error,
                "Could not create the repository (name may be taken).");

        // auto_init gave the repo one commit + README. Track that commit as the
        // baseline but DON'T pull the README into the project — the user's files
        // show as additions and the first push replaces the README with them.
        var head = await _remote.GetRefShaAsync(info.Owner, info.Name, info.DefaultBranch);
        await UpsertLinkAsync(new VcProjectLink
        {
            ProjectId = projectId, Owner = info.Owner, Repo = info.Name,
            Branch = info.DefaultBranch, LastSyncedCommitSha = head
        });
        await ReplaceSnapshotAsync(projectId, new Dictionary<string, (string, string)>());
        return VcResult.Success(head, $"Created {info.FullName}.");
    }

    // ─── Status / diff ───────────────────────────────────────────

    /// <summary>Computes the working-tree status by comparing the project files against the local snapshot.</summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The set of added, modified, and deleted files.</returns>
    public async Task<VcStatus> GetStatusAsync(int projectId)
    {
        await _initTask;
        var status = new VcStatus();
        var project = await _projects.GetByAsync(projectId) as Project;
        if (project == null) return status;

        var working = await LoadWorkingAsync(project);
        var snapshot = await LoadSnapshotContentAsync(projectId);

        foreach (var (path, content) in working)
        {
            if (!snapshot.TryGetValue(path, out var snap))
                status.Changes.Add(new VcChange(path, VcChangeKind.Added));
            else if (!string.Equals(content, snap, StringComparison.Ordinal))
                status.Changes.Add(new VcChange(path, VcChangeKind.Modified));
        }
        foreach (var path in snapshot.Keys)
            if (!working.ContainsKey(path))
                status.Changes.Add(new VcChange(path, VcChangeKind.Deleted));

        status.Changes.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return status;
    }

    /// <summary>Produces a unified diff for one file between the snapshot and the working tree.</summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="relativePath">Project-relative path of the file to diff.</param>
    /// <returns>The unified diff text, or an empty string when there is nothing to show.</returns>
    public async Task<string> GetFileDiffAsync(int projectId, string relativePath)
    {
        await _initTask;
        var project = await _projects.GetByAsync(projectId) as Project;
        if (project == null) return string.Empty;

        var working = await LoadWorkingAsync(project);
        var snapshot = await LoadSnapshotContentAsync(projectId);
        working.TryGetValue(relativePath, out var newText);
        snapshot.TryGetValue(relativePath, out var oldText);
        return LineDiff.UnifiedDiff(oldText ?? string.Empty, newText ?? string.Empty,
            $"a/{relativePath}", $"b/{relativePath}");
    }

    // ─── Commit / push ───────────────────────────────────────────

    /// <summary>Commits the working tree and pushes it to the linked repository, uploading only changed blobs.</summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="message">Commit message; a timestamped default is used when blank.</param>
    /// <param name="force">True to overwrite the remote branch even when it has advanced since the last sync.</param>
    /// <returns>The outcome of the operation.</returns>
    public async Task<VcResult> CommitAndPushAsync(int projectId, string message, bool force = false)
    {
        await _initTask;
        if (!_auth.IsLoggedIn) return new VcResult(VcOutcome.NotSignedIn);
        var link = await GetLinkAsync(projectId);
        if (link == null) return new VcResult(VcOutcome.Error, "This project is not linked to a repository.");
        var project = await _projects.GetByAsync(projectId) as Project;
        if (project == null) return new VcResult(VcOutcome.Error, "Project not found.");
        if (string.IsNullOrWhiteSpace(message)) message = $"Update {DateTime.Now:yyyy-MM-dd HH:mm}";

        var working = await LoadWorkingAsync(project);
        var snapshot = await LoadSnapshotAsync(projectId); // path -> (content, blobSha)

        // Anything to do?
        var status = ComputeChanges(working, snapshot);
        if (!status.HasChanges) return new VcResult(VcOutcome.NoChanges, "Nothing to commit.");

        // Divergence: remote advanced since our last sync?
        var remoteHead = await _remote.GetRefShaAsync(link.Owner, link.Repo, link.Branch);
        if (_remote.LastRateLimited) return new VcResult(VcOutcome.RateLimited, "GitHub rate limit — try again shortly.");
        if (!force && !string.Equals(remoteHead, link.LastSyncedCommitSha, StringComparison.Ordinal))
            return new VcResult(VcOutcome.NeedsPull, "The repository has new commits. Pull first, then commit.");

        // Build the tree from the FULL working set (omitted files are deleted
        // implicitly — no base_tree needed). Reuse snapshot blob shas for
        // unchanged files so only changed/new content uploads.
        var entries = new List<(string Path, string? Sha)>();
        var newShaByPath = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (path, content) in working)
        {
            string? sha;
            if (snapshot.TryGetValue(path, out var snap) && string.Equals(snap.Content, content, StringComparison.Ordinal))
                sha = snap.BlobSha;
            else
                sha = await _remote.CreateBlobAsync(link.Owner, link.Repo, content);
            if (string.IsNullOrEmpty(sha))
                return new VcResult(_remote.LastRateLimited ? VcOutcome.RateLimited : VcOutcome.Error, $"Failed to upload {path}.");
            entries.Add((path, sha));
            newShaByPath[path] = sha!;
        }

        var treeSha = await _remote.CreateTreeAsync(link.Owner, link.Repo, baseTreeSha: null, entries);
        if (string.IsNullOrEmpty(treeSha)) return new VcResult(VcOutcome.Error, "Failed to create tree.");

        var commitSha = await _remote.CreateCommitAsync(link.Owner, link.Repo, message, treeSha!, remoteHead);
        if (string.IsNullOrEmpty(commitSha)) return new VcResult(VcOutcome.Error, "Failed to create commit.");

        var ok = await _remote.UpdateRefAsync(link.Owner, link.Repo, link.Branch, commitSha!, force);
        if (!ok) return new VcResult(VcOutcome.NeedsPull, "Push rejected — the branch moved. Pull first.");

        // Snapshot = the working set we just pushed.
        var snapWrite = working.ToDictionary(kv => kv.Key, kv => (kv.Value, newShaByPath[kv.Key]), StringComparer.Ordinal);
        await ReplaceSnapshotAsync(projectId, snapWrite);
        link.LastSyncedCommitSha = commitSha;
        await UpsertLinkAsync(link);
        return VcResult.Success(commitSha, "Committed and pushed.");
    }

    // ─── Pull ────────────────────────────────────────────────────

    /// <summary>Pulls the latest remote commit into the project, mirroring its tree into the working files.</summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="force">True to overwrite local changes (remote wins); otherwise a dirty tree yields a conflict.</param>
    /// <returns>The outcome of the operation.</returns>
    public async Task<VcResult> PullAsync(int projectId, bool force = false)
    {
        await _initTask;
        if (!_auth.IsLoggedIn) return new VcResult(VcOutcome.NotSignedIn);
        var link = await GetLinkAsync(projectId);
        if (link == null) return new VcResult(VcOutcome.Error, "This project is not linked to a repository.");
        var project = await _projects.GetByAsync(projectId) as Project;
        if (project == null) return new VcResult(VcOutcome.Error, "Project not found.");

        var remoteHead = await _remote.GetRefShaAsync(link.Owner, link.Repo, link.Branch);
        if (_remote.LastRateLimited) return new VcResult(VcOutcome.RateLimited, "GitHub rate limit — try again shortly.");
        if (string.IsNullOrEmpty(remoteHead))
            return new VcResult(VcOutcome.UpToDate, "The repository is empty — nothing to pull.");
        if (string.Equals(remoteHead, link.LastSyncedCommitSha, StringComparison.Ordinal))
            return new VcResult(VcOutcome.UpToDate, "Already up to date.");

        // Remote advanced. Refuse if the working tree has uncommitted changes
        // (v1 has no merge) unless the caller forces (remote wins).
        var working = await LoadWorkingAsync(project);
        var snapshot = await LoadSnapshotAsync(projectId);
        if (!force && ComputeChanges(working, snapshot).HasChanges)
            return new VcResult(VcOutcome.Conflict,
                "You have local changes and the repository has new commits. Commit or discard your changes first.");

        // Fetch the remote tree + blobs and mirror them into the project.
        var treeSha = await _remote.GetCommitTreeShaAsync(link.Owner, link.Repo, remoteHead);
        if (string.IsNullOrEmpty(treeSha)) return new VcResult(VcOutcome.Error, "Could not read the remote commit.");
        var tree = await _remote.GetTreeAsync(link.Owner, link.Repo, treeSha!);
        if (tree == null) return new VcResult(VcOutcome.Error, "Could not read the remote tree.");

        var localFiles = await _files.GetProjectFilesAsync(project);
        var localByPath = new Dictionary<string, ProjectFile>(StringComparer.Ordinal);
        foreach (var f in localFiles) localByPath[f.RelativePath] = f;

        var newSnapshot = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        var remotePaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in tree)
        {
            var content = await _remote.GetBlobAsync(link.Owner, link.Repo, entry.Sha) ?? string.Empty;
            remotePaths.Add(entry.Path);
            newSnapshot[entry.Path] = (content, entry.Sha);

            if (localByPath.TryGetValue(entry.Path, out var lf))
            {
                lf.Project = project;
                lf.Content = content;
                await _files.UpdateAsync(lf);
            }
            else
            {
                var (dir, name) = SplitPath(entry.Path);
                await _files.CreateAsync(new ProjectFile
                {
                    Project = project, FileName = name, DirectoryPath = dir, Content = content
                });
            }
        }

        // Delete local files that no longer exist remotely.
        foreach (var f in localFiles)
        {
            if (remotePaths.Contains(f.RelativePath)) continue;
            f.Project = project;
            await _files.RemoveAsync(f);
        }

        await ReplaceSnapshotAsync(projectId, newSnapshot);
        link.LastSyncedCommitSha = remoteHead;
        await UpsertLinkAsync(link);
        return VcResult.Success(remoteHead, "Pulled the latest changes.");
    }

    // ─── History / auto-commit ───────────────────────────────────

    /// <summary>Returns the commit history for the linked branch, or null when not linked or not signed in.</summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The list of commits, or null.</returns>
    public async Task<List<VcCommit>?> GetHistoryAsync(int projectId)
    {
        await _initTask;
        var link = await GetLinkAsync(projectId);
        if (link == null || !_auth.IsLoggedIn) return null;
        return await _remote.GetCommitsAsync(link.Owner, link.Repo, link.Branch);
    }

    /// <summary>Enables or disables auto-commit for a linked project.</summary>
    /// <param name="projectId">The project id.</param>
    /// <param name="enabled">True to enable auto-commit.</param>
    public async Task SetAutoCommitAsync(int projectId, bool enabled)
    {
        await _initTask;
        var link = await GetLinkAsync(projectId);
        if (link == null) return;
        link.AutoCommit = enabled;
        await UpsertLinkAsync(link);
    }

    /// <summary>Commits and pushes automatically when the project is linked, signed in, and auto-commit is on; otherwise a no-op.</summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The outcome of the operation.</returns>
    public async Task<VcResult> TryAutoCommitAsync(int projectId)
    {
        var link = await GetLinkAsync(projectId);
        if (link == null || !link.AutoCommit || !_auth.IsLoggedIn)
            return new VcResult(VcOutcome.NoChanges);
        return await CommitAndPushAsync(projectId, $"Auto-commit {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private async Task<Dictionary<string, string>> LoadWorkingAsync(Project project)
    {
        var files = await _files.GetProjectFilesAsync(project);
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var f in files) dict[f.RelativePath] = f.Content ?? string.Empty;
        return dict;
    }

    private async Task<List<VcSnapshotFile>> LoadSnapshotRowsAsync(int projectId)
        => await _db.VcSnapshotFiles.Where(s => s.ProjectId == projectId).ToListAsync();

    private async Task<Dictionary<string, string>> LoadSnapshotContentAsync(int projectId)
    {
        var rows = await LoadSnapshotRowsAsync(projectId);
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in rows) d[r.RelativePath] = r.Content;
        return d;
    }

    private async Task<Dictionary<string, (string Content, string BlobSha)>> LoadSnapshotAsync(int projectId)
    {
        var rows = await LoadSnapshotRowsAsync(projectId);
        var d = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (var r in rows) d[r.RelativePath] = (r.Content, r.BlobSha);
        return d;
    }

    private async Task ReplaceSnapshotAsync(int projectId, Dictionary<string, (string Content, string BlobSha)> files)
    {
        await _db.ExecuteAsync("DELETE FROM VcSnapshotFile WHERE ProjectId = ?", projectId);
        foreach (var (path, val) in files)
        {
            await _db.InsertAsync(new VcSnapshotFile
            {
                ProjectId = projectId, RelativePath = path, Content = val.Content, BlobSha = val.BlobSha,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static VcStatus ComputeChanges(Dictionary<string, string> working, Dictionary<string, (string Content, string BlobSha)> snapshot)
    {
        var status = new VcStatus();
        foreach (var (path, content) in working)
        {
            if (!snapshot.TryGetValue(path, out var snap))
                status.Changes.Add(new VcChange(path, VcChangeKind.Added));
            else if (!string.Equals(content, snap.Content, StringComparison.Ordinal))
                status.Changes.Add(new VcChange(path, VcChangeKind.Modified));
        }
        foreach (var path in snapshot.Keys)
            if (!working.ContainsKey(path))
                status.Changes.Add(new VcChange(path, VcChangeKind.Deleted));
        return status;
    }

    private static (string Dir, string Name) SplitPath(string relativePath)
    {
        var idx = relativePath.LastIndexOf('/');
        return idx < 0 ? (string.Empty, relativePath) : (relativePath[..idx], relativePath[(idx + 1)..]);
    }
}
