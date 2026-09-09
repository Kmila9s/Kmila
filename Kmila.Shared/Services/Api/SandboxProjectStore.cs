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
public sealed class SandboxProjectStore
{
    private readonly ConcurrentDictionary<string, SandboxProject> _projects = new();
    private readonly string _root;

    public SandboxProjectStore(string? rootOverride = null)
    {
        _root = rootOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".kmila", "api-sandbox");
        Directory.CreateDirectory(_root);
    }

    public string Root => _root;

    public SandboxProject Create(string? name, bool retain)
    {
        string id = Guid.NewGuid().ToString("N").Substring(0, 12);
        string dir = Path.Combine(_root, id);
        Directory.CreateDirectory(dir);
        var proj = new SandboxProject(id, dir, name ?? id, retain);
        _projects[id] = proj;
        return proj;
    }

    public SandboxProject? Get(string id) => _projects.TryGetValue(id, out var p) ? p : null;

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

public sealed class SandboxProject
{
    private readonly object _lock = new();

    public string Id { get; }
    public string Path { get; }
    public string Name { get; }
    public bool Retain { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public SandboxProject(string id, string path, string name, bool retain)
    {
        Id = id; Path = path; Name = name; Retain = retain;
    }

    public string WriteFile(string name, string content)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains("..") ||
            System.IO.Path.IsPathRooted(name))
            throw new ArgumentException("Invalid file name.", nameof(name));

        string full = System.IO.Path.Combine(Path, name);
        string? parent = System.IO.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        lock (_lock) File.WriteAllText(full, content);
        return full;
    }

    public string? ReadFile(string name)
    {
        string full = System.IO.Path.Combine(Path, name);
        if (!File.Exists(full)) return null;
        return File.ReadAllText(full);
    }

    public IEnumerable<string> ListFiles()
    {
        if (!Directory.Exists(Path)) yield break;
        foreach (var f in Directory.EnumerateFiles(Path, "*.*", SearchOption.AllDirectories))
            yield return System.IO.Path.GetRelativePath(Path, f);
    }
}
