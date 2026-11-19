using Kmila.Shared.Services.Api;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Stages a command's sources into a sandbox project: the top file first, then
/// the others in command-line order — the analysis order GHDL would use (G22).
/// Two sources with the same file name are an error instead of one silently
/// overwriting the other.
/// </summary>
internal static class SourceStaging
{
    /// <summary>The staged sources (names in analysis order) and the top unit to load.</summary>
    public sealed record Staged(List<string> Files, string Entry, bool EntryIsExplicit);

    /// <summary>
    /// Writes the sources; the top unit is <paramref name="entry"/> when given, else
    /// inferred from the top file's name (then a missing unit is only a warning).
    /// </summary>
    /// <exception cref="FileNotFoundException">A source does not exist.</exception>
    /// <exception cref="ArgumentException">Two sources share a file name.</exception>
    public static Staged Stage(SandboxProject project, string topFile, IEnumerable<string> extraFiles, string? entry)
    {
        var staged = new List<string>();
        foreach (var src in extraFiles.Prepend(topFile))
        {
            if (!File.Exists(src)) throw new FileNotFoundException($"source file not found: {src}");
            string name = Path.GetFileName(src);
            if (staged.Contains(name, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"two sources are named '{name}'; rename one of them");
            project.WriteFile(name, File.ReadAllText(src));
            staged.Add(name);
        }
        bool explicitEntry = !string.IsNullOrWhiteSpace(entry);
        return new Staged(staged, explicitEntry ? entry! : Path.GetFileNameWithoutExtension(staged[0]), explicitEntry);
    }
}
