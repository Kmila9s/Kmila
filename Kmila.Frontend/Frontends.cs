namespace Kmila.Frontend;

/// <summary>Resolves the <see cref="IHdlFrontend"/> for a language or a set of files.</summary>
public static class Frontends
{
    private static readonly Dictionary<string, IHdlFrontend> _byId = new(StringComparer.OrdinalIgnoreCase)
    {
        [HdlLanguages.Vhdl.Id] = new VhdlFrontend(),
    };

    /// <summary>The front-end for <paramref name="language"/>.</summary>
    /// <exception cref="NotSupportedException">No front-end is registered for the language.</exception>
    public static IHdlFrontend For(HdlLanguage language) =>
        _byId.TryGetValue(language.Id, out var fe) ? fe
        : throw new NotSupportedException($"No front-end registered for {language.DisplayName}.");

    /// <summary>The front-end for the language that owns <paramref name="path"/>.</summary>
    /// <exception cref="NotSupportedException">The file is not a supported HDL source.</exception>
    public static IHdlFrontend ForPath(string path) =>
        For(HdlLanguages.FromPath(path) ?? throw new NotSupportedException($"'{Path.GetFileName(path)}' is not a supported HDL source."));

    /// <summary>
    /// The front-end for a set of sources. All HDL files must share one
    /// language (mixed-language designs are not supported yet).
    /// </summary>
    /// <exception cref="NotSupportedException">No HDL source, or more than one language.</exception>
    public static IHdlFrontend ForSources(IEnumerable<string> paths)
    {
        var languages = paths.Select(HdlLanguages.FromPath).OfType<HdlLanguage>().Distinct().ToList();
        return languages.Count switch
        {
            0 => throw new NotSupportedException("No supported HDL source found."),
            1 => For(languages[0]),
            _ => throw new NotSupportedException(
                     $"Mixed-language designs are not supported ({string.Join(", ", languages.Select(l => l.DisplayName))})."),
        };
    }
}
