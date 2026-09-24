namespace Kmila.Shared.Models.Practice;

/// <summary>Output container the modal hands to <c>PracticeSessionExportService</c>.</summary>
public enum PracticeSessionExportFormat
{
    /// <summary>Single human-readable Markdown file with everything inline.</summary>
    Markdown,
    /// <summary>ZIP bundle: report.md + session.json + per-attempt .vhd + per-pair .diff.</summary>
    Zip
}

/// <summary>
/// User choices captured by <c>ExportPracticeSessionModal</c>. Scope is fixed to
/// "current exercise — full attempt history" so it isn't a field; the modal
/// passes the exercise id separately.
/// </summary>
public sealed record PracticeSessionExportRequest(
    PracticeSessionExportFormat Format,
    bool IncludeDiffs,
    bool IncludeHiddenTests,
    bool IncludeFullCode);

/// <summary>What the export service hands back to the page for download dispatch.</summary>
public sealed record PracticeSessionExportPayload(
    string FileName,
    string MimeType,
    string? TextContent,
    byte[]? BinaryContent);
