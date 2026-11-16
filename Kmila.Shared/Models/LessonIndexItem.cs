using System.Globalization;
using System.Text;
using Kmila.Shared.Services;

namespace Kmila.Shared.Models;

/// <summary>One lesson row in the searchable <c>LessonIndex</c> drawer.</summary>
/// <param name="Id">Lesson identifier (unique within its group).</param>
/// <param name="Number">1-based position inside its module.</param>
/// <param name="Title">Localized lesson title.</param>
/// <param name="Summary">Localized lesson summary (searched, shown on match).</param>
/// <param name="Href">Route to open the lesson.</param>
/// <param name="Completed">Whether the student marked it complete.</param>
/// <param name="IsCurrent">Whether this is the lesson currently open.</param>
public sealed record LessonIndexItem(
    string Id, int Number, string Title, string Summary, string Href, bool Completed, bool IsCurrent);

/// <summary>A module and its lessons, as one collapsible section of the index.</summary>
/// <param name="Id">Module identifier.</param>
/// <param name="Title">Localized module title.</param>
/// <param name="Items">Lessons in display order.</param>
public sealed record LessonIndexGroup(string Id, string Title, IReadOnlyList<LessonIndexItem> Items);

/// <summary>
/// Builds <see cref="LessonIndexGroup"/> lists from the Learn and Concepts
/// manifests, plus the accent/case-insensitive matching the index uses.
/// </summary>
public static class LessonIndexBuilder
{
    /// <summary>Index groups for the Concepts track.</summary>
    public static List<LessonIndexGroup> ForConcepts(
        ConceptManifest manifest, string lang, Func<string, string, bool> isCompleted,
        string? currentModuleId = null, string? currentLessonId = null) =>
        manifest.Modules
            .OrderBy(m => (int)m.Level).ThenBy(m => m.Order)
            .Select(m => new LessonIndexGroup(
                m.Id,
                ConceptCatalog.Localize(m.Titles, lang, m.Id),
                m.Lessons.Select((l, i) => new LessonIndexItem(
                    l.Id, i + 1,
                    ConceptCatalog.Localize(l.Titles, lang, l.Id),
                    Plain(ConceptCatalog.Localize(l.Summaries, lang, string.Empty)),
                    $"/concepts/{m.Id}/{l.Id}",
                    isCompleted(m.Id, l.Id),
                    IsSame(m.Id, currentModuleId) && IsSame(l.Id, currentLessonId))).ToList()))
            .ToList();

    /// <summary>Index groups for the Learn track.</summary>
    public static List<LessonIndexGroup> ForLearn(
        LessonManifest manifest, string lang, Func<string, string, bool> isCompleted,
        string? currentModuleId = null, string? currentLessonId = null) =>
        manifest.Modules
            .OrderBy(m => (int)m.Level).ThenBy(m => m.Order)
            .Select(m => new LessonIndexGroup(
                m.Id,
                LessonCatalog.Localize(m.Titles, lang, m.Id),
                m.Lessons.Select((l, i) => new LessonIndexItem(
                    l.Id, i + 1,
                    LessonCatalog.Localize(l.Titles, lang, l.Id),
                    Plain(LessonCatalog.Localize(l.Summaries, lang, string.Empty)),
                    $"/learn/{m.Id}/{l.Id}",
                    isCompleted(m.Id, l.Id),
                    IsSame(m.Id, currentModuleId) && IsSame(l.Id, currentLessonId))).ToList()))
            .ToList();

    /// <summary>"n / total" of the current lesson within its module, or null.</summary>
    public static (int Index, int Total)? PositionOf(IEnumerable<LessonIndexGroup> groups)
    {
        foreach (var g in groups)
        {
            var cur = g.Items.FirstOrDefault(i => i.IsCurrent);
            if (cur != null) return (cur.Number, g.Items.Count);
        }
        return null;
    }

    /// <summary>
    /// Every lesson whose title or summary matches <paramref name="query"/>,
    /// paired with its module title. Empty query → empty list.
    /// </summary>
    public static List<(LessonIndexGroup Group, LessonIndexItem Item)> Search(
        IEnumerable<LessonIndexGroup> groups, string? query)
    {
        var needle = Fold(query?.Trim());
        if (needle.Length == 0) return new();
        return groups
            .SelectMany(g => g.Items.Select(i => (g, i)))
            .Where(p => Fold(p.i.Title).Contains(needle, StringComparison.Ordinal) || Fold(p.i.Summary).Contains(needle, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Lower-cases and strips diacritics character by character, so the
    /// result has the same length as the input ("Comparación" → "comparacion").
    /// Keeping lengths equal lets callers map a match index back onto the
    /// original string for highlighting.
    /// </summary>
    public static string Fold(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            var d = c.ToString().Normalize(NormalizationForm.FormD);
            var b = d.Length > 0 && CharUnicodeInfo.GetUnicodeCategory(d[0]) != UnicodeCategory.NonSpacingMark ? d[0] : c;
            sb.Append(char.ToLowerInvariant(b));
        }
        return sb.ToString();
    }

    /// <summary>Drops inline Markdown emphasis/code markers so summaries read as plain text.</summary>
    private static string Plain(string s) => s.Replace("*", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal);

    private static bool IsSame(string a, string? b) =>
        b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
