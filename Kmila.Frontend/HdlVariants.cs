namespace Kmila.Frontend;

/// <summary>
/// Per-language variants of one piece of content (a practice exercise, a
/// project template, a lesson sample, a test case). The content schemas (v2)
/// key their variants by <see cref="HdlLanguage.Id"/>; this type resolves
/// those ids against <see cref="HdlLanguages"/> once, so consumers never
/// compare language strings themselves.
/// </summary>
/// <typeparam name="T">Payload of one variant.</typeparam>
public sealed class HdlVariants<T> where T : class
{
    private readonly Dictionary<HdlLanguage, T> _byLanguage;

    private HdlVariants(Dictionary<HdlLanguage, T> byLanguage)
    {
        _byLanguage = byLanguage;
        if (HdlLanguages.Supported.FirstOrDefault(byLanguage.ContainsKey) is { } preferred)
            Preferred = (preferred, byLanguage[preferred]);
    }

    /// <summary>
    /// Builds the set from an id-keyed map (as read from JSON). Entries whose id
    /// is not a supported language are ignored: content may ship a variant
    /// before its language is enabled.
    /// </summary>
    public static HdlVariants<T> FromIds(IReadOnlyDictionary<string, T>? raw)
    {
        var map = new Dictionary<HdlLanguage, T>();
        if (raw != null)
        {
            foreach (var (id, payload) in raw)
            {
                if (payload != null && HdlLanguages.FromId(id) is { } lang) map[lang] = payload;
            }
        }
        return new HdlVariants<T>(map);
    }

    /// <summary>Languages that have a variant.</summary>
    public IReadOnlyCollection<HdlLanguage> Languages => _byLanguage.Keys;

    /// <summary>True when no supported language has a variant.</summary>
    public bool IsEmpty => Preferred is null;

    /// <summary>The variant for <paramref name="language"/>, or null.</summary>
    public T? For(HdlLanguage language) => _byLanguage.GetValueOrDefault(language);

    /// <summary>
    /// The variant shown by default: the first language, in registry order,
    /// that has one. Null when <see cref="IsEmpty"/>.
    /// </summary>
    public (HdlLanguage Language, T Payload)? Preferred { get; }

    /// <summary>Languages of <paramref name="required"/> that have no variant.</summary>
    public IEnumerable<HdlLanguage> MissingFrom(IEnumerable<HdlLanguage> required) =>
        required.Where(l => !_byLanguage.ContainsKey(l));
}
