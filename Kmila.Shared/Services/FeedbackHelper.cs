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
    /// Kmila website base URL. Host of the bug-report / satisfaction / download
    /// pages the app hands off to (in the browser — the app never POSTs).
    /// </summary>
    public const string KmilaSite = "https://kmila.com.mx";

    /// <summary>
    /// Language prefix for a Kmila-site path: Spanish lives at the root, every
    /// other UI language uses the English mirror under <c>/en</c> (the site only
    /// ships ES + EN UIs). Returns "" for Spanish, "/en" otherwise.
    /// </summary>
    private static string LangPrefix(string? uiLang)
        => string.Equals(uiLang, "es", StringComparison.OrdinalIgnoreCase) ? "" : "/en";

    /// <summary>Satisfaction-survey page URL (the page itself offers the
    /// anonymous / signed-in choice). Language-aware.</summary>
    public static string SatisfactionUrl(string? uiLang) => $"{KmilaSite}{LangPrefix(uiLang)}/satisfaction";

    /// <summary>Desktop-app download page URL (used as the "use the native app"
    /// call-to-action in the WASM demo). Language-aware.</summary>
    public static string DownloadUrl(string? uiLang) => $"{KmilaSite}{LangPrefix(uiLang)}/download";

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
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- UTC timestamp: {DateTime.UtcNow:O}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- .NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- UI language: {uiLang}");
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

    /// <summary>
    /// Builds the Kmila website bug-report URL with the form fields pre-filled as
    /// query parameters (the page prefills itself on GET; the human then reviews
    /// and submits). The app never POSTs — the URL is handed to the browser
    /// (window.open), preserving the offline / no-telemetry stance.
    ///
    /// Matches the site contract (KmilaWeb <c>/bug-report</c>): params
    /// <c>title</c>(200) · <c>message</c>(4000) · <c>category</c>(80) ·
    /// <c>version</c>(40) · <c>platform</c>(60) · <c>timestamp</c>(64). Values are
    /// URL-encoded (newlines become %0A) and clipped to the field limits; the
    /// site truncates over-length values and ignores unknown params anyway.
    /// </summary>
    /// <param name="uiLang">UI language — routes to the ES (root) or EN (/en) form.</param>
    /// <param name="title">Short summary (optional — user can fill it on the form).</param>
    /// <param name="message">What happened / steps / expected vs actual (optional).</param>
    /// <param name="category">Crash / UI / Performance / Data / Other (optional).</param>
    /// <param name="appVersion">App version string (a leading "v" is stripped).</param>
    public static string BuildKmilaReportUrl(
        string uiLang, string? title = null, string? message = null,
        string? category = null, string? appVersion = null)
    {
        var parts = new List<string>();
        void Add(string key, string? val, int max)
        {
            if (string.IsNullOrEmpty(val)) return;
            var v = val.Length > max ? val[..max] : val;
            parts.Add($"{key}={Uri.EscapeDataString(v)}");
        }
        Add("title",    title,    200);
        Add("message",  message,  4000);
        Add("category", category, 80);
        Add("version",  NormalizeVersion(appVersion), 40);
        Add("platform", System.Runtime.InteropServices.RuntimeInformation.OSDescription, 60);
        Add("timestamp", DateTime.UtcNow.ToString("o"), 64);

        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return $"{KmilaSite}{LangPrefix(uiLang)}/bug-report{qs}";
    }

    /// <summary>Strips a leading "v"/"V" (and spaces) from a display version like "v1.25.0".</summary>
    private static string? NormalizeVersion(string? v)
        => string.IsNullOrWhiteSpace(v) ? null : v.Trim().TrimStart('v', 'V').Trim();
}
