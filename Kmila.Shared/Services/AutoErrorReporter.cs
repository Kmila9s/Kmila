using Microsoft.Extensions.Logging;
using Kmila.Shared.Data;
using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Bridges <see cref="AppLogStore"/> to <see cref="GitHubAuth"/>: when an
/// error-level log event lands AND the user has both signed in AND
/// enabled the "auto-send error reports" preference, opens a GitHub
/// issue with the diagnostics body.
///
/// Three guard rails keep this from being annoying:
///
///   1. <b>Loop avoidance</b> — log entries from this class or
///      <see cref="GitHubAuth"/> are skipped, so a failed report doesn't
///      trigger another report. The Editor's prerender JS-interop
///      InvalidOperationException family is also filtered out (those
///      are noisy but not user-actionable).
///
///   2. <b>Dedup / throttle</b> — in-memory hash of (Category +
///      first 200 chars of message). Same hash inside the
///      <see cref="ThrottleWindow"/> is ignored. 24 h window keeps the
///      reporter from spamming the issue tracker on a tight loop.
///
///   3. <b>Opt-in default-off</b> — both the GitHub login and the
///      "Auto-send error reports" toggle in Settings → Account must be
///      explicitly turned on. Either off → no reports.
/// </summary>
public sealed class AutoErrorReporter : IDisposable
{
    private const string AutoReportSettingKey = "KMILA_AUTO_REPORT_ENABLED";
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromHours(24);
    private static readonly string[] AutoReportLabels = new[] { "auto-report" };

    private readonly AppLogStore _store;
    private readonly GitHubAuth _auth;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AutoErrorReporter> _logger;

    /// <summary>Runs together with the lifetime of the host (per
    /// circuit on Web, per app on MAUI). Sized small because dedup
    /// keys are short and we trim opportunistically.</summary>
    private readonly Dictionary<string, DateTime> _seen = new();
    private readonly object _seenLock = new();

    /// <summary>Cached toggle value; refreshed when the setting key
    /// fires <see cref="ApplicationDbContext.StaticOnValueChanged"/>.
    /// Reading SQLite on every error event would be wasteful and could
    /// race the very error we're trying to report.</summary>
    private bool _enabled;

    /// <summary>Creates the reporter and subscribes to log and setting-change events.</summary>
    /// <param name="store">Application log store whose new entries are watched.</param>
    /// <param name="auth">GitHub auth used to submit reports.</param>
    /// <param name="db">Database context providing the enabled-toggle setting.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public AutoErrorReporter(
        AppLogStore store,
        GitHubAuth auth,
        ApplicationDbContext db,
        ILogger<AutoErrorReporter>? logger = null)
    {
        _store = store;
        _auth = auth;
        _db = db;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AutoErrorReporter>.Instance;

        _store.OnEntryAdded += OnEntryAdded;
        ApplicationDbContext.StaticOnValueChanged += OnSettingChanged;

        // Best-effort initial read. Failures (prerender) leave _enabled
        // at its default false; the OnSettingChanged handler picks up
        // the real value once the circuit hydrates.
        _ = RefreshEnabledAsync();
    }

    private async Task RefreshEnabledAsync()
    {
        try
        {
            var raw = await _db.GetSettingAsync(AutoReportSettingKey);
            _enabled = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Prerender / DB hiccup — fall back to disabled.
            _enabled = false;
        }
    }

    private void OnSettingChanged(string key)
    {
        if (key == AutoReportSettingKey)
            _ = RefreshEnabledAsync();
    }

    private void OnEntryAdded(AppLogEntry entry)
    {
        if (entry == null) return;
        if (entry.Level < LogLevel.Error) return;
        if (!_enabled) return;
        if (!_auth.IsLoggedIn) return;

        // Loop guard — never report errors that originated from the
        // reporting / auth path itself, and skip the noisy prerender
        // JS-interop family that already self-suppresses elsewhere.
        var cat = entry.Category ?? string.Empty;
        if (cat.Contains(nameof(AutoErrorReporter), StringComparison.OrdinalIgnoreCase) ||
            cat.Contains(nameof(GitHubAuth),         StringComparison.OrdinalIgnoreCase) ||
            cat.Contains(nameof(EditorSessionStore), StringComparison.OrdinalIgnoreCase))
            return;

        var msg = entry.Message ?? string.Empty;
        if (msg.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.Ordinal))
            return;

        // Dedup. Take the first 200 chars of the message so very long
        // stack-trace-laden messages still hash to the same bucket on
        // repeat occurrences.
        var key = string.Concat(cat, "::", msg.Length > 200 ? msg.Substring(0, 200) : msg);
        lock (_seenLock)
        {
            // Trim entries older than the throttle window — keeps the
            // dictionary bounded over a long-lived MAUI session.
            if (_seen.Count > 200)
            {
                var cutoff = DateTime.UtcNow - ThrottleWindow;
                foreach (var k in _seen.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList())
                    _seen.Remove(k);
            }
            if (_seen.TryGetValue(key, out var lastSeen) &&
                DateTime.UtcNow - lastSeen < ThrottleWindow)
            {
                return; // already reported recently
            }
            _seen[key] = DateTime.UtcNow;
        }

        // Fire-and-forget — the report's own success/failure logging is
        // category-filtered above so it can't loop.
        _ = SubmitAsync(entry);
    }

    private async Task SubmitAsync(AppLogEntry entry)
    {
        try
        {
            // Title: short single-line summary so the issue tracker is
            // browsable. Body: full diagnostics + the actual exception
            // and message, in a Markdown-friendly layout.
            var firstLine = (entry.Message ?? string.Empty)
                .Split('\n', 2, StringSplitOptions.None)[0];
            var truncated = firstLine.Length > 80 ? firstLine.Substring(0, 80) + "…" : firstLine;
            var title = $"[Auto] {entry.Category}: {truncated}";

            string lang;
            try { lang = await _db.GetSettingAsync(AppSettings.KEY_LANGUAGE) ?? "en"; }
            catch { lang = "en"; }

            var diag = FeedbackHelper.BuildDiagnostics(lang);
            var body = $"## Automatic error report\n\n" +
                       $"**Category:** `{entry.Category}`\n" +
                       $"**Level:** `{entry.Level}`\n" +
                       $"**Time (UTC):** `{entry.Timestamp.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}`\n\n" +
                       $"### Message\n```\n{entry.Message}\n```\n\n" +
                       (string.IsNullOrEmpty(entry.Exception)
                            ? string.Empty
                            : $"### Exception\n```\n{entry.Exception}\n```\n\n") +
                       $"---\n\n{diag}";

            var url = await _auth.SubmitIssueAsync(title, body, AutoReportLabels);
            if (url == null)
            {
                _logger.LogWarning("[AutoErrorReporter] SubmitIssueAsync returned null — issue not opened");
            }
        }
        catch (Exception ex)
        {
            // Don't propagate — the report path failing must never
            // crash the user's session. Log at warning so it shows up
            // in /logs but doesn't trigger another auto-report cycle.
            _logger.LogWarning(ex, "[AutoErrorReporter] Submit failed");
        }
    }

    /// <summary>Unsubscribes from log and setting-change events.</summary>
    public void Dispose()
    {
        _store.OnEntryAdded -= OnEntryAdded;
        ApplicationDbContext.StaticOnValueChanged -= OnSettingChanged;
    }
}
