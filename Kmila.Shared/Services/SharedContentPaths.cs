namespace Kmila.Shared.Services;

/// <summary>
/// Finds the bundled content folder (<c>App/Kmila/Kmila.Shared/wwwroot</c>) in a
/// source checkout by walking up from the running assembly and then from the
/// current directory. The one locator for the CLI's resources, the bench, the
/// practice validator and the unit tests (all run from inside the repo tree).
/// </summary>
public static class SharedContentPaths
{
    private const int MaxLevels = 10;

    /// <summary>
    /// Absolute path of <c>wwwroot/<paramref name="relative"/></c> (a folder or a
    /// file; empty for <c>wwwroot</c> itself), or null when no ancestor holds it.
    /// </summary>
    public static string? Find(string relative = "")
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = start;
            for (int i = 0; i < MaxLevels && dir != null; i++)
            {
                foreach (var root in new[] { Path.Combine(dir, "App", "Kmila", "Kmila.Shared", "wwwroot"), Path.Combine(dir, "Kmila.Shared", "wwwroot") })
                {
                    var candidate = relative.Length == 0 ? root : Path.Combine(root, relative);
                    if (Directory.Exists(candidate) || File.Exists(candidate)) return candidate;
                }
                dir = Path.GetDirectoryName(dir);
            }
        }
        return null;
    }
}
