using System.Collections.Concurrent;

namespace Kmila.Shared.Services.Api;

// Ephemeral project store for the simulation API. Lives at
// ~/.kmila/api-sandbox/<run-id>/ — outside ProjectsService so bench runs
// don't pollute the user's real project list. Threadsafe enough for the
// API: per-project file writes serialize through the per-instance lock.
public sealed class SandboxProjectStore
{
    private readonly ConcurrentDictionary<string, SandboxProject> _projects = new();
    private readonly string _root;

    public SandboxProjectStore()
    {
        _root = Path.Combine(
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
