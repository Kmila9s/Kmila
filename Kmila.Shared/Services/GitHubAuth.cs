using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Kmila.Shared.Data;

namespace Kmila.Shared.Services;

/// <summary>
/// GitHub Device Flow OAuth client + token store. Why Device Flow: it
/// works without a server-side secret (we ship a public Client ID to
/// the browser), and the user never has to paste a personal access
/// token. The trade-off is the two-step UX — show a code, ask the
/// user to type it on github.com — which we surface in
/// <see cref="GitHubLoginModal"/>.
///
/// Login is OPTIONAL. The app is fully usable signed-out; the only
/// features that depend on auth are the in-app issue submission flow
/// (<see cref="SubmitIssueAsync"/>) and the "auto-send error reports"
/// toggle. Tier 1 (copy diagnostics) and Tier 2 (open prefilled issue
/// URL) in <see cref="FeedbackHelper"/> remain available either way.
///
/// Token storage uses <see cref="ApplicationDbContext"/>'s settings
/// table — same pattern as language / theme / tick-resolution — so it
/// survives across sessions on a per-install basis. The token is
/// stored as-is (no encryption); for a learning tool with optional
/// login this matches the threat model. Users can sign out from the
/// Settings page at any time, which deletes the row.
/// </summary>
public class GitHubAuth
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GitHubAuth> _logger;

    private static readonly HttpClient _http = new()
    {
        // Github API + device-flow endpoints both live behind these hosts;
        // a single client is fine. Default User-Agent because the API
        // refuses requests without one.
        DefaultRequestHeaders = { { "User-Agent", "Kmila/0.1 (+https://github.com/Kmila9s/Kmila)" } }
    };

    private const string TokenSettingKey    = "KMILA_GITHUB_TOKEN";
    private const string ClientIdSettingKey = "KMILA_GITHUB_CLIENT_ID";

    /// <summary>
    /// Effective OAuth App Client ID. Resolves from the SQLite settings
    /// table first (so deployers can paste one in via the Settings page
    /// without recompiling), then falls back to the build-time
    /// <see cref="GitHubAuthConfig.DefaultClientId"/>.
    /// </summary>
    private string _clientId = GitHubAuthConfig.DefaultClientId;

    /// <summary>Cached current account (null when logged out). Updated
    /// after sign-in / sign-out and on first use.</summary>
    public GitHubAccount? Account { get; private set; }

    public bool IsLoggedIn => Account != null;

    /// <summary>True when an effective Client ID is set (build-time
    /// constant or runtime override). Drives the "configure first"
    /// banner in the login modal.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId);

    /// <summary>The Client ID as currently in effect — for the
    /// Settings UI to display whatever override is active. Never
    /// returns null.</summary>
    public string CurrentClientId => _clientId ?? string.Empty;

    /// <summary>Fires whenever <see cref="Account"/> changes — sign in,
    /// sign out, or refresh. Subscribers (TopBar pill, Settings card)
    /// re-render on the event.</summary>
    public event Action? OnChange;

    public GitHubAuth(ApplicationDbContext db, ILogger<GitHubAuth>? logger = null)
    {
        _db = db;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubAuth>.Instance;
    }

    /// <summary>
    /// Bootstraps the cached Client ID + account from settings storage.
    /// Called from the login modal / TopBar / Settings on first use so
    /// the UI lights up with the user's avatar + username on cold
    /// start without forcing a fresh sign-in. Idempotent.
    /// </summary>
    public async Task RestoreAsync()
    {
        // Pick up any runtime-configured Client ID (set via Settings)
        // before deciding whether to skip sign-in attempts.
        try
        {
            var storedClientId = await _db.GetSettingAsync(ClientIdSettingKey);
            if (!string.IsNullOrWhiteSpace(storedClientId))
                _clientId = storedClientId;
        }
        catch { /* DB read hiccup is non-fatal — fall back to const */ }

        if (Account != null) return;
        var token = await _db.GetSettingAsync(TokenSettingKey);
        if (string.IsNullOrWhiteSpace(token)) return;
        try
        {
            var account = await FetchAccountAsync(token);
            if (account != null)
            {
                Account = account;
                OnChange?.Invoke();
            }
            else
            {
                // Token rejected — clear it so we don't keep retrying.
                await _db.DeleteSettingAsync(TokenSettingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHubAuth] Restore failed — keeping token, will retry next time");
        }
    }

    /// <summary>
    /// Step 1 of device flow — exchange the configured Client ID for a
    /// device + user code pair. The caller (login modal) shows the
    /// user_code and verification_uri to the user, then calls
    /// <see cref="PollForTokenAsync"/> with the device_code.
    /// </summary>
    public async Task<DeviceCodeResponse> StartDeviceFlowAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("GitHub Client ID not configured. See GitHubAuthConfig.");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/device/code");
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["scope"]     = GitHubAuthConfig.Scope
        });
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<DeviceCodeResponse>(cancellationToken: ct);
        if (data == null)
            throw new InvalidOperationException("Empty response from GitHub device-code endpoint.");
        return data;
    }

    /// <summary>
    /// Step 2 of device flow — poll the token endpoint at the
    /// interval GitHub gave us until the user authorises (or refuses,
    /// or the code expires). Saves the access token on success and
    /// fetches the account profile for the UI.
    /// </summary>
    public async Task<GitHubAccount?> PollForTokenAsync(
        DeviceCodeResponse code, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("GitHub Client ID not configured.");

        var interval = TimeSpan.FromSeconds(Math.Max(5, code.IntervalSeconds));
        var deadline = DateTime.UtcNow.AddSeconds(code.ExpiresIn > 0 ? code.ExpiresIn : 600);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(interval, ct);

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"]    = _clientId,
                ["device_code"]  = code.DeviceCode,
                ["grant_type"]   = "urn:ietf:params:oauth:grant-type:device_code"
            });
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GitHubAuth] Token poll returned {Status}", res.StatusCode);
                continue;
            }

            var token = await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (token == null) continue;

            // GitHub returns 200 OK for both pending-authorization and
            // success; we distinguish on the body shape. The `error`
            // field tells us to keep waiting (or stop).
            if (!string.IsNullOrEmpty(token.Error))
            {
                switch (token.Error)
                {
                    case "authorization_pending":
                        // User hasn't entered the code yet — keep going.
                        continue;
                    case "slow_down":
                        // GitHub asked us to back off. Bump interval.
                        interval = interval.Add(TimeSpan.FromSeconds(5));
                        continue;
                    case "expired_token":
                    case "access_denied":
                    default:
                        return null; // give up — caller surfaces the error
                }
            }

            if (!string.IsNullOrWhiteSpace(token.AccessToken))
            {
                await _db.SetSettingAsync(TokenSettingKey, token.AccessToken);
                var account = await FetchAccountAsync(token.AccessToken);
                if (account != null)
                {
                    Account = account;
                    OnChange?.Invoke();
                }
                return account;
            }
        }
        return null; // deadline elapsed
    }

    /// <summary>
    /// Updates the runtime Client ID override. Pass null/empty to
    /// clear the override (falls back to <see cref="GitHubAuthConfig.DefaultClientId"/>).
    /// Fires <see cref="OnChange"/> so the login modal / Settings
    /// card / TopBar pill all re-evaluate <see cref="IsConfigured"/>.
    /// </summary>
    public async Task SetClientIdAsync(string? clientId)
    {
        var trimmed = clientId?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            await _db.DeleteSettingAsync(ClientIdSettingKey);
            _clientId = GitHubAuthConfig.DefaultClientId;
        }
        else
        {
            await _db.SetSettingAsync(ClientIdSettingKey, trimmed);
            _clientId = trimmed;
        }
        OnChange?.Invoke();
    }

    /// <summary>Wipes the saved token + cached account. Fires
    /// <see cref="OnChange"/> so listeners (TopBar pill, Settings
    /// card) flip back to the logged-out state.</summary>
    public async Task SignOutAsync()
    {
        await _db.DeleteSettingAsync(TokenSettingKey);
        if (Account != null)
        {
            Account = null;
            OnChange?.Invoke();
        }
    }

    /// <summary>
    /// Submits a GitHub issue to the configured target repo using the
    /// stored access token. Returns the issue's URL on success, null
    /// on failure (logged). Caller is responsible for adding any
    /// diagnostics / labels they want in the body.
    /// </summary>
    public async Task<string?> SubmitIssueAsync(
        string title, string body, IEnumerable<string>? labels = null, CancellationToken ct = default)
    {
        var token = await _db.GetSettingAsync(TokenSettingKey);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var payload = new Dictionary<string, object>
        {
            ["title"] = title,
            ["body"]  = body
        };
        if (labels != null) payload["labels"] = labels.ToArray();

        var url = $"https://api.github.com/repos/{GitHubAuthConfig.IssueOwner}/{GitHubAuthConfig.IssueRepo}/issues";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Content = JsonContent.Create(payload);

        try
        {
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("[GitHubAuth] Issue submission returned {Status}", res.StatusCode);
                return null;
            }
            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.TryGetProperty("html_url", out var h) ? h.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHubAuth] Issue submission failed");
            return null;
        }
    }

    private async Task<GitHubAccount?> FetchAccountAsync(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        try
        {
            using var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<GitHubAccount>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHubAuth] /user fetch failed");
            return null;
        }
    }

    // ─── Wire types ──────────────────────────────────────────────

    /// <summary>
    /// Response payload from GitHub's device-flow <c>/login/device/code</c> endpoint —
    /// holds the device + user codes the client polls and the verification URL the
    /// user types into a browser.
    /// </summary>
    public class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]      public string DeviceCode      { get; set; } = "";
        [JsonPropertyName("user_code")]        public string UserCode        { get; set; } = "";
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = "";
        [JsonPropertyName("expires_in")]       public int    ExpiresIn       { get; set; }
        [JsonPropertyName("interval")]         public int    IntervalSeconds { get; set; }
    }

    /// <summary>
    /// Response payload from GitHub's <c>/login/oauth/access_token</c> endpoint while
    /// polling the device-flow handshake. Either <see cref="AccessToken"/> is filled
    /// or <see cref="Error"/> carries the still-pending / failed reason.
    /// </summary>
    public class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("token_type")]   public string? TokenType   { get; set; }
        [JsonPropertyName("scope")]        public string? Scope       { get; set; }
        [JsonPropertyName("error")]        public string? Error       { get; set; }
    }

    /// <summary>
    /// Subset of the GitHub <c>/user</c> response that the app surfaces in the
    /// account UI (login handle, display name, avatar URL, profile page URL).
    /// </summary>
    public class GitHubAccount
    {
        [JsonPropertyName("login")]      public string  Login     { get; set; } = "";
        [JsonPropertyName("name")]       public string? Name      { get; set; }
        [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
        [JsonPropertyName("html_url")]   public string? HtmlUrl   { get; set; }
    }
}

/// <summary>
/// Build-time configuration for the GitHub OAuth integration. The
/// values here are PUBLIC by design — Device Flow doesn't use a
/// client secret, so embedding the Client ID in the shipped binary
/// is the documented pattern. To deploy your own fork:
///
///   1. Register a new OAuth App at https://github.com/settings/developers
///   2. Tick "Enable Device Flow" in the app's settings.
///   3. Paste the Client ID into <see cref="ClientId"/> below.
///   4. Update <see cref="IssueOwner"/> / <see cref="IssueRepo"/> if
///      issues should land in a different repo than the upstream one.
///
/// Until <see cref="ClientId"/> is set, the in-app login flow stays
/// disabled and the existing Tier 1 (copy diagnostics) + Tier 2
/// (open prefilled issue URL) feedback paths remain the only
/// channel — neither requires authentication.
/// </summary>
public static class GitHubAuthConfig
{
    /// <summary>
    /// Build-time fallback Client ID. Empty by default — most deployers
    /// paste theirs at runtime via Settings → Account → Configure GitHub
    /// OAuth instead of editing source. Setting this constant only
    /// matters if you want the value baked into the binary; the
    /// runtime override (settings-table key
    /// <c>KMILA_GITHUB_CLIENT_ID</c>) takes priority either way.
    /// </summary>
    public const string DefaultClientId = "";

    /// <summary>Token scopes requested. <c>public_repo</c> is enough
    /// for issue submission against the public Kmila repo; bump to
    /// <c>repo</c> only if you fork to a private one.</summary>
    public const string Scope = "public_repo";

    /// <summary>Owner of the issue-target repo.</summary>
    public const string IssueOwner = "Kmila9s";

    /// <summary>Issue-target repo name.</summary>
    public const string IssueRepo = "Kmila";
}
