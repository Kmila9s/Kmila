using Kmila.CLI.Cli;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Supplies the file-reader delegate the shared <c>PracticeCatalog</c> expects
/// (<c>Func&lt;string, Task&lt;Stream&gt;&gt;</c>). The catalog asks for paths like
/// <c>Practice/manifest.json</c> or <c>Practice/m2-fix-bug/.../tests.json</c>;
/// we resolve them under the bundled <c>Practice/</c> folder located by
/// <see cref="ResourcePaths.Practice"/>.
/// </summary>
public static class PracticeContent
{
    /// <summary>The resolved Practice folder, or null when the bundle can't be found.</summary>
    public static string? Root => ResourcePaths.Practice();

    /// <summary>
    /// Builds the open-file delegate rooted at the Practice bundle.
    /// Throws when the bundle is missing so the caller can report it cleanly.
    /// </summary>
    public static Func<string, Task<Stream>> OpenDelegate()
    {
        var root = Root
            ?? throw new DirectoryNotFoundException(
                "Practice content not found. Build the CLI (it bundles Practice/) or set KMILA_PRACTICE_DIR.");

        return path =>
        {
            // Strip the leading "Practice/" the catalog prepends; the resolved
            // root already IS that folder.
            var rel = path.Replace('\\', '/');
            const string prefix = "Practice/";
            if (rel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                rel = rel[prefix.Length..];

            var full = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult<Stream>(File.OpenRead(full));
        };
    }
}
