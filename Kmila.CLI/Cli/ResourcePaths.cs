namespace Kmila.CLI.Cli;

/// <summary>
/// Locates read-only resource folders (FPGA device specs, UI dictionaries)
/// that originate in <c>Kmila.Shared/wwwroot</c>. Two strategies, in order:
/// <list type="number">
///   <item>a copy next to the executable (the csproj links these into bin/ so
///   a published binary is self-contained);</item>
///   <item>a walk up the directory tree to the repo's
///   <c>App/Kmila/Kmila.Shared/wwwroot/&lt;folder&gt;</c> (covers a bare
///   <c>dotnet run</c> from anywhere in the tree).</item>
/// </list>
/// An environment variable overrides both when set.
/// </summary>
public static class ResourcePaths
{
    /// <summary>Resolves the <c>Devices</c> folder holding <c>*.json</c> FPGA specs, or null if not found.</summary>
    public static string? Devices() =>
        Resolve("Devices", Environment.GetEnvironmentVariable("KMILA_DEVICES_DIR"));

    /// <summary>Resolves the <c>Dictionaries</c> folder holding the UI translation JSON files, or null if not found.</summary>
    public static string? Dictionaries() =>
        Resolve("Dictionaries", Environment.GetEnvironmentVariable("KMILA_DICT_DIR"));

    /// <summary>
    /// Resolves the <c>Practice</c> folder holding the auto-graded exercise bundle
    /// (<c>manifest.json</c> + per-exercise starter/solution per HDL language,
    /// <c>tests.json</c>, <c>statement.*.md</c>), or null if not found.
    /// </summary>
    public static string? Practice() =>
        Resolve("Practice", Environment.GetEnvironmentVariable("KMILA_PRACTICE_DIR"));

    private static string? Resolve(string folder, string? envOverride)
    {
        if (!string.IsNullOrWhiteSpace(envOverride) && Directory.Exists(envOverride))
            return envOverride;

        // 1) Copied next to the executable.
        var local = Path.Combine(AppContext.BaseDirectory, folder);
        if (Directory.Exists(local)) return local;

        // 2) Walk up to the shared wwwroot in a source checkout.
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "App", "Kmila", "Kmila.Shared", "wwwroot", folder);
            if (Directory.Exists(candidate)) return candidate;
            // Also handle being run from within App/Kmila/*.
            candidate = Path.Combine(dir, "Kmila.Shared", "wwwroot", folder);
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        // 3) From the current working directory as a last resort.
        dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "App", "Kmila", "Kmila.Shared", "wwwroot", folder);
            if (Directory.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir, "Kmila.Shared", "wwwroot", folder);
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
