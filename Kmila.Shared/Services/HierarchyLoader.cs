using Interpreter.Services;
using Kmila.Frontend;

namespace Kmila.Shared.Services;

/// <summary>
/// Parses a multi-file HDL design into a name→<see cref="Entity"/> map and
/// registers its component-instance (port-map) edges in
/// <see cref="EntityRegistry"/>, so <see cref="Resources.ComputeHierarchical"/>
/// can walk the hierarchy and aggregate a structural top's resource usage.
/// </summary>
/// <remarks>
/// <para>Why this exists: a single-source parse
/// only parses the entities declared in the one file it is given, and it is the
/// Interpreter's synthesis (not <see cref="EntityRegistry.ScanFile"/>) that
/// records port-map edges. To sum a structural parent's sub-components we need
/// every child <em>fully parsed</em> (so each carries its self
/// <see cref="Entity.ResourceUsage"/>) and every edge registered.</para>
/// <para>The registry is cleared first: <see cref="EntityRegistry.RegisterComponentInstance"/>
/// <em>appends</em>, so without a reset a re-parse would double-count edges and
/// a prior design's entities could leak in.</para>
/// </remarks>
public static class HierarchyLoader
{
    /// <summary>
    /// Fully parses every HDL source in <paramref name="files"/> into a
    /// case-insensitive name→entity map, with the registry reset so edges are
    /// fresh. Best-effort per file — a child that fails to parse simply
    /// contributes nothing (the aggregate degrades to self-usage).
    /// </summary>
    /// <param name="files">Design source files (top + any siblings).</param>
    /// <param name="entryName">Preferred top entity name; falls back to the
    /// first entity that has an architecture.</param>
    /// <param name="top">The resolved top entity, or null when none parsed.</param>
    /// <returns>All parsed entities keyed by name.</returns>
    public static Dictionary<string, Entity> ParseDesign(
        IEnumerable<string> files, string? entryName, out Entity? top)
        => ParseDesign(files, entryName, out top, out _);

    /// <summary>
    /// Overload that also yields a generic-specialization resolver for
    /// <see cref="Resources.ComputeHierarchical"/>: given a child entity name and
    /// an instance's generic overrides, it re-synthesizes that child at the
    /// overridden generics and returns its self estimate (or null to fall back to
    /// the default-generic figures).
    /// </summary>
    public static Dictionary<string, Entity> ParseDesign(
        IEnumerable<string> files, string? entryName, out Entity? top,
        out Func<string, IReadOnlyDictionary<string, string>, Resources?> specializeChild)
    {
        EntityRegistry.Instance.Clear();

        var byName = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
        // First file that defines each entity → used to re-read its source for
        // generic specialization.
        var fileByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in files)
        {
            if (string.IsNullOrWhiteSpace(f) || !HdlLanguages.IsHdlSource(f))
                continue;
            var unit = Frontends.ForPath(f).ParseFile(f);
            if (unit.Error != null) continue; // best-effort: skip files that fail to parse
            foreach (var e in unit.Entities)
                if (!byName.ContainsKey(e.Name))
                {
                    byName[e.Name] = e;
                    fileByName[e.Name] = f;
                }
        }

        top = null;
        if (!string.IsNullOrWhiteSpace(entryName) && byName.TryGetValue(entryName!, out var t))
            top = t;
        top ??= byName.Values.FirstOrDefault(e => e.Architecture != null)
             ?? byName.Values.FirstOrDefault();

        var cache = new Dictionary<string, Resources?>(StringComparer.Ordinal);
        specializeChild = (childName, overrides) => SpecializeChild(childName, overrides, fileByName, cache);
        return byName;
    }

    /// <summary>
    /// Re-synthesizes a child at the given generic overrides and returns its self
    /// estimate. Only leaf children (that instantiate nothing) are specialized, so
    /// re-parsing never registers extra port-map edges into the live
    /// <see cref="EntityRegistry"/> that the aggregation walk is reading.
    /// Results are cached per (child, overrides). Returns null on any failure.
    /// </summary>
    private static Resources? SpecializeChild(
        string childName, IReadOnlyDictionary<string, string> overrides,
        Dictionary<string, string> fileByName, Dictionary<string, Resources?> cache)
    {
        // Non-leaf: specializing would require re-parsing its sub-tree and would
        // mutate the registry mid-walk. Fall back to default-generic figures.
        if (EntityRegistry.Instance.GetComponentInstances(childName).Count > 0)
            return null;
        if (!fileByName.TryGetValue(childName, out var path))
            return null;

        var sig = childName + "|" + string.Join(";",
            overrides.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                     .Select(k => $"{k.Key.ToLowerInvariant()}={k.Value}"));
        if (cache.TryGetValue(sig, out var cached))
            return cached;

        Resources? result = null;
        try
        {
            var src = File.ReadAllText(path);
            var specialized = SourceElaborator.SpecializeEntitySource(src, childName, overrides);
            if (!string.IsNullOrEmpty(specialized))
            {
                var frontend = Frontends.ForPath(path);
                var unit = frontend.ParseSource(specialized);
                var e = unit.Error == null ? frontend.PickTop(unit.Entities, childName) : null;
                if (e?.ResourceUsage != null) result = e.ResourceUsage;
            }
        }
        catch { result = null; }

        cache[sig] = result;
        return result;
    }
}
