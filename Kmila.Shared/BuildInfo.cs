using System.Globalization;
using System.Reflection;

namespace Kmila.Shared;

/// <summary>
/// Build provenance for the running app, surfaced in the sidebar version-chip
/// tooltip so you can confirm which build is actually running — e.g. that a
/// clean rebuild took effect rather than a stale binary being served.
///
/// The git commit is baked at compile time as assembly metadata (see the
/// <c>KmilaBuildStamp</c> target in Kmila.Shared.csproj); the build time is
/// read at runtime from this assembly's file stamp. Neither forces a recompile
/// on every build, so the dev inner loop stays incremental.
/// </summary>
public static class BuildInfo
{
    private static readonly Assembly Self = typeof(BuildInfo).Assembly;

    /// <summary>
    /// Short git commit the build was compiled from, or "" when git wasn't
    /// available at build time. Suffixed "+local" when the working tree had
    /// uncommitted edits, so a dev build is distinguishable from a clean commit.
    /// </summary>
    public static string Commit { get; } = ReadMeta("KmilaGitCommit");

    /// <summary>
    /// Local time this assembly was last built, or null when the on-disk path
    /// isn't available (single-file publish, trimmed WASM, sandbox).
    /// </summary>
    public static DateTime? BuildTimeLocal { get; } = ReadBuildTime();

    /// <summary>"2026-10-08 14:32"-style build time, or "" when unknown.</summary>
    public static string BuildTimeDisplay =>
        BuildTimeLocal is { } t ? t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "";

    /// <summary>True when at least one provenance detail is worth showing.</summary>
    public static bool HasStamp => BuildTimeLocal is not null || Commit.Length > 0;

    private static string ReadMeta(string key)
    {
        try
        {
            foreach (var a in Self.GetCustomAttributes<AssemblyMetadataAttribute>())
                if (string.Equals(a.Key, key, StringComparison.Ordinal))
                    return a.Value?.Trim() ?? "";
        }
        catch { /* reflection blocked — fall through */ }
        return "";
    }

    private static DateTime? ReadBuildTime()
    {
        try
        {
            var loc = Self.Location;
            if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
                return File.GetLastWriteTime(loc);
        }
        catch { /* single-file / WASM / sandboxed — no path */ }
        return null;
    }
}
