using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Models;

/// <summary>
/// Version of the bundled content schema (Practice, Learn and Templates
/// manifests). v2 = one variant per HDL language. Every content loader reads
/// exactly this version; older files are rejected, never half-read.
/// </summary>
public static class ContentSchema
{
    /// <summary>The schema version this build reads.</summary>
    public const int Version = 2;

    /// <summary>
    /// True when <paramref name="version"/> is <see cref="Version"/>; otherwise
    /// logs why <paramref name="document"/> is ignored and returns false.
    /// </summary>
    public static bool Accepts(int version, string document, ILogger logger)
    {
        if (version == Version) return true;
        logger.LogError("[ContentSchema] {Document} is schema v{Found}; this build reads v{Expected} — ignored",
            document, version, Version);
        return false;
    }
}
