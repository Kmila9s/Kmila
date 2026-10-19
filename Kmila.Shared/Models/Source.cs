using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Bibliographic kind of a <see cref="SourceEntry"/>. Mirrors the BibLaTeX
/// entry types used in the TT thesis (<c>Documentacion/TT*/references.bib</c>)
/// so the in-app bibliography and the thesis stay conceptually aligned.
/// Drives the small type label on each rendered source row.
/// </summary>
public enum SourceType
{
    /// <summary>Catch-all / unspecified.</summary>
    Other,
    /// <summary>A printed or e-book (BibLaTeX <c>@book</c>).</summary>
    Book,
    /// <summary>A web resource — docs, tool pages, vendor pages (BibLaTeX <c>@online</c>).</summary>
    Online,
    /// <summary>A standard, e.g. an IEEE Std (BibLaTeX <c>@standard</c>).</summary>
    Standard,
    /// <summary>A journal/magazine article (BibLaTeX <c>@article</c>).</summary>
    Article,
    /// <summary>A conference/workshop paper (BibLaTeX <c>@inproceedings</c>).</summary>
    Paper,
    /// <summary>A technical report / whitepaper (BibLaTeX <c>@techreport</c>).</summary>
    Report
}

/// <summary>
/// One bibliographic source. The in-app analogue of a single BibLaTeX entry:
/// a lesson references entries by their <see cref="Key"/> (just like
/// <c>\cite{key}</c>), and the global <c>/sources</c> page renders the whole
/// catalog as an IEEE-style list — the app's "Referencias".
/// </summary>
/// <remarks>
/// Keys are kept identical to the thesis <c>references.bib</c> so the two
/// bibliographies stay in sync and a claim can be traced from the app to the
/// same source the document cites.
/// </remarks>
public class SourceEntry
{
    /// <summary>Citation key, unique in the catalog (e.g. <c>ashenden2008</c>).</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Bibliographic kind — drives the type label chip.</summary>
    [JsonPropertyName("type")]
    public SourceType Type { get; set; } = SourceType.Other;

    /// <summary>Work title (plain text; no LaTeX brace-protection).</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Author or issuing organization, display-ready.</summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>Canonical URL, when the source is online. Null for print-only works.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Access date for an online source (e.g. <c>2026-02</c>).</summary>
    [JsonPropertyName("urlDate")]
    public string? UrlDate { get; set; }

    /// <summary>Publisher (books) or issuing body (standards). Optional.</summary>
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    /// <summary>Publication year, as a string so "2008" / "n.d." both fit.</summary>
    [JsonPropertyName("year")]
    public string? Year { get; set; }

    /// <summary>Edition (books) or standard number (standards). Optional.</summary>
    [JsonPropertyName("edition")]
    public string? Edition { get; set; }

    /// <summary>
    /// <c>true</c> when the source comes from an ESCOM syllabus's recommended
    /// bibliography (the "preferred" reading for that asignatura). Surfaced as
    /// a small badge so students can tell course-endorsed sources apart from
    /// the complementary ones we added during research.
    /// </summary>
    [JsonPropertyName("preferred")]
    public bool Preferred { get; set; }

    /// <summary>Optional localized annotation, keyed by language code.</summary>
    [JsonPropertyName("note")]
    public Dictionary<string, string> Note { get; set; } = new();
}

/// <summary>Root of <c>wwwroot/Data/sources.json</c> — the app-wide bibliography.</summary>
public class SourceCatalog
{
    /// <summary>Catalog version — bumped whenever entries change materially.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>All bibliographic entries, in no particular order.</summary>
    [JsonPropertyName("sources")]
    public List<SourceEntry> Sources { get; set; } = new();
}
