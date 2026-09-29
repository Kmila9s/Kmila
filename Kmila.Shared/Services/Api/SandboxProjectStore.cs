using System.Collections.Concurrent;

namespace Kmila.Shared.Services.Api;

// Ephemeral project store for the simulation API. Lives at
// ~/.kmila/api-sandbox/<run-id>/ — outside ProjectsService so bench runs
// don't pollute the user's real project list. Threadsafe enough for the
// API: per-project file writes serialize through the per-instance lock.
//
// Phase B: the parameterless default is what Kmila.Web has always used;
// the MAUI-embedded host (EmbeddedApiHost) instead passes an explicit
// rootOverride under FileSystem.AppDataDirectory so sandboxes land in
// the app's writable data dir instead of the user profile.
/// <summary>Ephemeral, thread-safe store of sandbox projects for the simulation API.</summary>
public sealed class SandboxProjectStore
{
    private readonly ConcurrentDictionary<string, SandboxProject> _projects = new();
    private readonly string _root;

    /// <summary>Creates the store, ensuring the sandbox root directory exists.</summary>
    /// <param name="rootOverride">Optional explicit root directory; defaults to <c>~/.kmila/api-sandbox</c>.</param>
    public SandboxProjectStore(string? rootOverride = null)
    {
        _root = rootOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".kmila", "api-sandbox");
        Directory.CreateDirectory(_root);
    }

    /// <summary>Root directory under which all sandbox project directories live.</summary>
    public string Root => _root;

    /// <summary>Creates a new sandbox project with a fresh id and directory.</summary>
    /// <param name="name">Optional display name; defaults to the generated id.</param>
    /// <param name="retain">When true, the project is kept longer before being swept.</param>
    /// <returns>The newly created sandbox project.</returns>
    public SandboxProject Create(string? name, bool retain)
    {
        string id = Guid.NewGuid().ToString("N").Substring(0, 12);
        string dir = Path.Combine(_root, id);
        Directory.CreateDirectory(dir);
        var proj = new SandboxProject(id, dir, name ?? id, retain);
        _projects[id] = proj;
        return proj;
    }

    /// <summary>Looks up a sandbox project by id.</summary>
    /// <param name="id">The project id.</param>
    /// <returns>The project, or null if not found.</returns>
    public SandboxProject? Get(string id) => _projects.TryGetValue(id, out var p) ? p : null;

    /// <summary>Removes a project from the store and best-effort deletes its directory.</summary>
    /// <param name="id">The project id.</param>
    /// <returns>True if the project was tracked and removed; otherwise false.</returns>
    public bool Delete(string id)
    {
        if (!_projects.TryRemove(id, out var proj)) return false;
        try { if (Directory.Exists(proj.Path)) Directory.Delete(proj.Path, recursive: true); }
        catch { /* best-effort cleanup; the API caller already considers this gone */ }
        return true;
    }

    // Sweep in-memory projects: delete any Retain=false project older than
    // `ttl` and any Retain=true project older than `retainedTtl` (an absolute
    // upper bound so retained-but-forgotten projects don't leak forever).
    // Returns the number of projects reaped.
    /// <summary>Deletes in-memory projects older than their TTL (<paramref name="retainedTtl"/> for retained ones).</summary>
    /// <param name="ttl">Maximum age for non-retained projects.</param>
    /// <param name="retainedTtl">Absolute upper-bound age for retained projects.</param>
    /// <returns>The number of projects reaped.</returns>
    public int SweepInMemory(TimeSpan ttl, TimeSpan retainedTtl)
    {
        var now = DateTime.UtcNow;
        int reaped = 0;
        foreach (var (id, proj) in _projects)
        {
            var age = now - proj.CreatedAt;
            bool expired = proj.Retain ? age > retainedTtl : age > ttl;
            if (expired && Delete(id)) reaped++;
        }
        return reaped;
    }

    // Sweep on-disk directories that are NOT tracked in `_projects` and whose
    // mtime is older than `ttl`. Catches orphans left by past process crashes
    // (in-memory state is lost on restart; the dict does not rehydrate from
    // disk on startup, so pre-restart dirs are unreachable via the API).
    // Returns the number of orphan directories deleted.
    /// <summary>Deletes on-disk sandbox directories not tracked in memory and older than <paramref name="ttl"/>.</summary>
    /// <param name="ttl">Maximum age (by directory mtime) before an untracked directory is deleted.</param>
    /// <returns>The number of orphan directories deleted.</returns>
    public int SweepDiskOrphans(TimeSpan ttl)
    {
        int reaped = 0;
        if (!Directory.Exists(_root)) return 0;
        var cutoff = DateTime.UtcNow - ttl;
        foreach (var dir in Directory.EnumerateDirectories(_root))
        {
            string id = Path.GetFileName(dir);
            if (_projects.ContainsKey(id)) continue; // still live in-memory
            try
            {
                var mtime = Directory.GetLastWriteTimeUtc(dir);
                if (mtime > cutoff) continue;
                Directory.Delete(dir, recursive: true);
                reaped++;
            }
            catch { /* best-effort; skip on any IO issue */ }
        }
        return reaped;
    }
}

/// <summary>A single sandbox project: an isolated directory with serialized file access.</summary>
public sealed class SandboxProject
{
    private readonly object _lock = new();

    /// <summary>Unique project identifier.</summary>
    public string Id { get; }
    /// <summary>Absolute path of the project's directory.</summary>
    public string Path { get; }
    /// <summary>Display name of the project.</summary>
    public string Name { get; }
    /// <summary>When true, the project is retained longer before being swept.</summary>
    public bool Retain { get; }
    /// <summary>UTC timestamp when the project was created.</summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>Creates a sandbox project descriptor.</summary>
    /// <param name="id">Unique project id.</param>
    /// <param name="path">Absolute directory path.</param>
    /// <param name="name">Display name.</param>
    /// <param name="retain">Whether the project should be retained longer.</param>
    public SandboxProject(string id, string path, string name, bool retain)
    {
        Id = id; Path = path; Name = name; Retain = retain;
    }

    /// <summary>Writes text to a file within the project, rejecting rooted or traversing names.</summary>
    /// <param name="name">Relative file name within the project.</param>
    /// <param name="content">Text content to write.</param>
    /// <returns>The absolute path of the written file.</returns>
    public string WriteFile(string name, string content)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains("..", System.StringComparison.Ordinal) ||
            System.IO.Path.IsPathRooted(name))
            throw new ArgumentException("Invalid file name.", nameof(name));

        string full = System.IO.Path.Combine(Path, name);
        string? parent = System.IO.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        lock (_lock) File.WriteAllText(full, content);
        return full;
    }

    /// <summary>Reads a file from the project.</summary>
    /// <param name="name">Relative file name within the project.</param>
    /// <returns>The file's text, or null if it does not exist.</returns>
    public string? ReadFile(string name)
    {
        string full = System.IO.Path.Combine(Path, name);
        if (!File.Exists(full)) return null;
        return File.ReadAllText(full);
    }

    /// <summary>Enumerates all files in the project as project-relative paths.</summary>
    /// <returns>Relative paths of every file under the project directory.</returns>
    public IEnumerable<string> ListFiles()
    {
        if (!Directory.Exists(Path)) yield break;
        foreach (var f in Directory.EnumerateFiles(Path, "*.*", SearchOption.AllDirectories))
            yield return System.IO.Path.GetRelativePath(Path, f);
    }
}
