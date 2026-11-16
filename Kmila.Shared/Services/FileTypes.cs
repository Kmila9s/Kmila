using BlazorBootstrap;
using Kmila.Frontend;

namespace Kmila.Shared.Services;

/// <summary>
/// The single extension → (icon, style variant, Monaco language) table used by
/// the file list, editor tabs and editor header. HDL sources are resolved from
/// <see cref="HdlLanguages"/>, so registering a new language updates every view.
/// </summary>
public static class FileTypes
{
    /// <summary>How a file is presented in the UI.</summary>
    /// <param name="Icon">Bootstrap icon.</param>
    /// <param name="Variant">CSS variant key (the HDL language id for sources).</param>
    /// <param name="Label">Upper-case extension without the dot ("" when none).</param>
    /// <param name="MonacoLanguage">Monaco editor language id.</param>
    public sealed record Kind(IconName Icon, string Variant, string Label, string MonacoLanguage);

    private static readonly Dictionary<string, (IconName Icon, string Variant, string Monaco)> Others =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".md"]   = (IconName.FileTypeMd,  "markdown", "markdown"),
            [".txt"]  = (IconName.FileTypeTxt, "text",     "plaintext"),
            [".json"] = (IconName.Braces,      "json",     "json"),
            [".xml"]  = (IconName.Code,        "xml",      "xml"),
            // Not a supported HDL yet; Monaco still colours it.
            [".v"]    = (IconName.FileCode,    "other",    "systemverilog"),
            [".sv"]   = (IconName.FileCode,    "other",    "systemverilog"),
        };

    /// <summary>Describes <paramref name="fileName"/> for display.</summary>
    public static Kind Of(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty);
        var label = ext.TrimStart('.').ToUpperInvariant();
        if (HdlLanguages.FromPath(fileName) is { } language)
            return new Kind(IconName.FileCode, language.Id, label, language.MonacoId);
        return Others.TryGetValue(ext, out var o)
            ? new Kind(o.Icon, o.Variant, label, o.Monaco)
            : new Kind(IconName.FileEarmark, "other", label, "plaintext");
    }
}
