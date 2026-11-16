namespace Kmila.Frontend;

/// <summary>
/// Describes one hardware description language Kmila can load. Adding a
/// language means registering a descriptor here and an <see cref="IHdlFrontend"/>
/// for it — never sprinkling <c>if (lang == ...)</c> through the codebase.
/// </summary>
/// <param name="Id">Stable lowercase id used in manifests, APIs and file maps (e.g. "vhdl").</param>
/// <param name="DisplayName">Human-readable name.</param>
/// <param name="Extensions">Source file extensions, lowercase, with the leading dot.</param>
/// <param name="LineComment">Line-comment prefix (used e.g. for exercise edit markers).</param>
/// <param name="MonacoId">Monaco editor language id.</param>
/// <param name="CaseSensitiveIdentifiers">Whether identifiers are case-sensitive in this language.</param>
public sealed record HdlLanguage(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Extensions,
    string LineComment,
    string MonacoId,
    bool CaseSensitiveIdentifiers)
{
    /// <summary>The primary (preferred) extension for new files.</summary>
    public string PrimaryExtension => Extensions[0];

    /// <summary>True when <paramref name="path"/> has one of this language's extensions.</summary>
    public bool Owns(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public override string ToString() => Id;
}

/// <summary>
/// Registry of the HDL languages Kmila supports. The single source of truth
/// for "is this file an HDL source?" and "which language is it?".
/// </summary>
public static class HdlLanguages
{
    /// <summary>VHDL (IEEE 1076).</summary>
    public static readonly HdlLanguage Vhdl = new(
        Id: "vhdl",
        DisplayName: "VHDL",
        Extensions: [".vhd", ".vhdl"],
        LineComment: "--",
        MonacoId: "vhdl",
        CaseSensitiveIdentifiers: false);

    private static readonly List<HdlLanguage> _supported = [Vhdl];

    /// <summary>Languages that are fully supported (front-end + reference simulator).</summary>
    public static IReadOnlyList<HdlLanguage> Supported => _supported;

    /// <summary>
    /// File-name prefixes of artefacts older Kmila versions wrote next to user
    /// sources (pre-v3.0.2 elaborator output, still found in existing project
    /// folders). They are never user sources.
    /// </summary>
    private static readonly string[] GeneratedPrefixes = ["_elaborated_"];

    /// <summary>Finds a supported language by id (case-insensitive), or null.</summary>
    public static HdlLanguage? FromId(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null
        : _supported.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The language that owns <paramref name="path"/>'s extension, or null.</summary>
    public static HdlLanguage? FromPath(string? path) =>
        string.IsNullOrEmpty(path) ? null : _supported.FirstOrDefault(l => l.Owns(path));

    /// <summary>True for a user HDL source file (supported extension, not a Kmila artefact).</summary>
    public static bool IsHdlSource(string? path) =>
        FromPath(path) != null && !IsGeneratedArtifact(path!);

    /// <summary>True for files Kmila generated itself (never treated as user sources).</summary>
    public static bool IsGeneratedArtifact(string path)
    {
        var name = Path.GetFileName(path);
        return GeneratedPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Comma-separated extension list for an HTML <c>accept</c> attribute.</summary>
    public static string AcceptList() =>
        string.Join(",", _supported.SelectMany(l => l.Extensions));

    /// <summary>
    /// Registers an additional language. Test-only hook used to prove that
    /// multi-variant suites fail when a variant is missing; production code
    /// registers languages statically above.
    /// </summary>
    internal static IDisposable RegisterForTesting(HdlLanguage language)
    {
        _supported.Add(language);
        return new Unregister(language);
    }

    private sealed class Unregister(HdlLanguage language) : IDisposable
    {
        public void Dispose() => _supported.Remove(language);
    }
}
