using System.Text;

namespace Kmila.Shared.Services;

/// <summary>
/// Shared builder for "Report an issue" diagnostics + GitHub issue URLs.
/// Used by the Settings feedback card (generic report) and the Logs page
/// (per-entry Error / Critical report). Keeping it centralized so the two
/// entry points can't drift in format.
/// </summary>
public static class FeedbackHelper
{
    /// <summary>GitHub repository slug for the Kmila project.</summary>
    public const string GithubRepo = "Kmila9s/Kmila";

    /// <summary>
    /// Builds a Markdown diagnostics blob. When <paramref name="logContext"/>
    /// is non-null it's embedded verbatim in a <c>## Log entry</c> section
    /// — used by the Logs page's per-entry report path so the maintainer
    /// sees exactly which error the student was reporting.
    /// </summary>
    public static string BuildDiagnostics(string uiLang, string? logContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Kmila diagnostics");
        sb.AppendLine($"- UTC timestamp: {DateTime.UtcNow:O}");
        sb.AppendLine($"- OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine($"- Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"- .NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"- UI language: {uiLang}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(logContext))
        {
            sb.AppendLine("## Log entry");
            sb.AppendLine("```");
            // Trim each line to keep the payload tidy and within GitHub's
            // URL length budget. Entries longer than ~6 KB get clipped —
            // the student can paste the full thing if really needed.
            var clipped = logContext.Length > 6000
                ? logContext[..6000] + "\n… (truncated)"
                : logContext;
            sb.AppendLine(clipped);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## What happened");
        sb.AppendLine("<!-- describe the issue here -->");
        sb.AppendLine();
        sb.AppendLine("## Steps to reproduce");
        sb.AppendLine("1.");
        sb.AppendLine("2.");
        sb.AppendLine();
        sb.AppendLine("## Expected vs actual");
        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Builds the GitHub "new issue" URL with title + body pre-filled. Title
    /// is truncated so the URL stays under the 8 KB limit most browsers
    /// enforce — the full detail lives in the body anyway.
    /// </summary>
    public static string BuildIssueUrl(string title, string body)
    {
        var safeTitle = title.Length > 160 ? title[..160] + "…" : title;
        var qTitle = Uri.EscapeDataString(safeTitle);
        var qBody  = Uri.EscapeDataString(body);
        return $"https://github.com/{GithubRepo}/issues/new?title={qTitle}&body={qBody}";
    }
}
