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
/// <c>GitHubLoginModal</c>.
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

    private const string TokenSettingKey        = "KMILA_GITHUB_TOKEN";
    private const string ClientIdSettingKey     = "KMILA_GITHUB_CLIENT_ID";
    // Opt-in: request the wider `repo` scope (private repos) instead of the
    // default `public_repo`. Read at device-flow start; changing it only takes
    // effect on the next sign-in (OAuth scope is fixed at authorization time).
    private const string PrivateScopeSettingKey = "KMILA_GITHUB_PRIVATE_SCOPE";

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

    /// <summary>True when a GitHub account is currently signed in.</summary>
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

    /// <summary>Creates a new <see cref="GitHubAuth"/>.</summary>
    /// <param name="db">Application database context used for token/settings storage.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
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
            ["scope"]     = await ResolveScopeAsync()
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

    /// <summary>True when the user has opted in to request the wider
    /// <c>repo</c> scope (needed for private repositories).</summary>
    public async Task<bool> GetPrivateScopeAsync()
    {
        try
        {
            var raw = await _db.GetSettingAsync(PrivateScopeSettingKey);
            return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>Sets whether the next sign-in requests the <c>repo</c> scope
    /// (private repos) instead of <c>public_repo</c>. Fires <see cref="OnChange"/>
    /// so the Settings UI can prompt for re-consent.</summary>
    public async Task SetPrivateScopeAsync(bool enabled)
    {
        await _db.SetSettingAsync(PrivateScopeSettingKey, enabled ? "true" : "false");
        OnChange?.Invoke();
    }

    /// <summary>Resolves the OAuth scope to request at sign-in: <c>repo</c> when
    /// the private-repo opt-in is on, else the default <c>public_repo</c>.</summary>
    private async Task<string> ResolveScopeAsync()
        => await GetPrivateScopeAsync() ? "repo" : GitHubAuthConfig.Scope;

    /// <summary>
    /// Sends an authenticated GitHub REST call using the stored token, mirroring
    /// <see cref="SubmitIssueAsync"/>'s request shape (Bearer + vnd.github+json).
    /// This is the single transport seam the version-control engine's
    /// <c>GitHubRemote</c> uses — the token never leaves this class. On 401 the
    /// token is cleared and <see cref="OnChange"/> fires (signed-out). Returns a
    /// disposable result carrying the parsed JSON (may be null for 204/empty).
    /// </summary>
    internal async Task<GitHubApiResult> SendApiAsync(
        HttpMethod method, string url, object? jsonBody = null, CancellationToken ct = default)
    {
        var token = await _db.GetSettingAsync(TokenSettingKey);
        if (string.IsNullOrWhiteSpace(token))
            return new GitHubApiResult { Ok = false, Error = "not-signed-in" };

        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (jsonBody != null) req.Content = JsonContent.Create(jsonBody);

        try
        {
            using var res = await _http.SendAsync(req, ct);
            var status = res.StatusCode;

            if (status == System.Net.HttpStatusCode.Unauthorized)
            {
                await _db.DeleteSettingAsync(TokenSettingKey);
                if (Account != null) { Account = null; OnChange?.Invoke(); }
                return new GitHubApiResult { Ok = false, Status = status, Error = "unauthorized" };
            }

            bool rateLimited = status == System.Net.HttpStatusCode.Forbidden
                && ((res.Headers.TryGetValues("X-RateLimit-Remaining", out var rem)
                        && rem.FirstOrDefault() == "0")
                    || res.Headers.Contains("Retry-After"));

            JsonDocument? doc = null;
            try
            {
                var stream = await res.Content.ReadAsStreamAsync(ct);
                doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            }
            catch { /* empty (204) or non-JSON body — leave doc null */ }

            if (!res.IsSuccessStatusCode)
            {
                string? err = null;
                if (doc != null && doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("message", out var m))
                    err = m.GetString();
                _logger.LogWarning("[GitHubAuth] API {Method} {Url} -> {Status} {Err}",
                    method, url, status, err);
                return new GitHubApiResult { Ok = false, Status = status, Doc = doc, Error = err, RateLimited = rateLimited };
            }

            return new GitHubApiResult { Ok = true, Status = status, Doc = doc };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHubAuth] API {Method} {Url} failed", method, url);
            return new GitHubApiResult { Ok = false, Error = ex.Message };
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
        /// <summary>Device verification code the client uses when polling for the token.</summary>
        [JsonPropertyName("device_code")]      public string DeviceCode      { get; set; } = "";
        /// <summary>Short code the user types into the GitHub verification page.</summary>
        [JsonPropertyName("user_code")]        public string UserCode        { get; set; } = "";
        /// <summary>URL the user opens to enter the user code.</summary>
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = "";
        /// <summary>
        /// URL with the user code pre-filled (<c>verification_uri</c> +
        /// <c>?user_code=…</c>). Opening this skips the manual code-entry step,
        /// which is the fast path the login modal uses (issue #9b).
        /// </summary>
        [JsonPropertyName("verification_uri_complete")] public string VerificationUriComplete { get; set; } = "";
        /// <summary>Seconds until the codes expire.</summary>
        [JsonPropertyName("expires_in")]       public int    ExpiresIn       { get; set; }
        /// <summary>Minimum polling interval, in seconds.</summary>
        [JsonPropertyName("interval")]         public int    IntervalSeconds { get; set; }
    }

    /// <summary>
    /// Response payload from GitHub's <c>/login/oauth/access_token</c> endpoint while
    /// polling the device-flow handshake. Either <see cref="AccessToken"/> is filled
    /// or <see cref="Error"/> carries the still-pending / failed reason.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>OAuth access token, present once the handshake succeeds.</summary>
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        /// <summary>Token type (typically <c>bearer</c>).</summary>
        [JsonPropertyName("token_type")]   public string? TokenType   { get; set; }
        /// <summary>Granted OAuth scopes.</summary>
        [JsonPropertyName("scope")]        public string? Scope       { get; set; }
        /// <summary>Error code while the handshake is still pending or has failed.</summary>
        [JsonPropertyName("error")]        public string? Error       { get; set; }
    }

    /// <summary>
    /// Subset of the GitHub <c>/user</c> response that the app surfaces in the
    /// account UI (login handle, display name, avatar URL, profile page URL).
    /// </summary>
    public class GitHubAccount
    {
        /// <summary>GitHub login handle.</summary>
        [JsonPropertyName("login")]      public string  Login     { get; set; } = "";
        /// <summary>Display name, if set on the account.</summary>
        [JsonPropertyName("name")]       public string? Name      { get; set; }
        /// <summary>URL of the account's avatar image.</summary>
        [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
        /// <summary>URL of the account's profile page.</summary>
        [JsonPropertyName("html_url")]   public string? HtmlUrl   { get; set; }
    }
}

/// <summary>
/// Result of an authenticated GitHub REST call made via
/// <see cref="GitHubAuth.SendApiAsync"/>. Owns the parsed response
/// <see cref="Doc"/> — callers should <c>using</c> it (or call
/// <see cref="Dispose"/>) to release the <see cref="JsonDocument"/>.
/// </summary>
internal sealed class GitHubApiResult : IDisposable
{
    /// <summary>True on a 2xx response.</summary>
    public bool Ok { get; init; }
    /// <summary>HTTP status code (default 0 when the request never left).</summary>
    public System.Net.HttpStatusCode Status { get; init; }
    /// <summary>Parsed JSON body, or null for empty/204/non-JSON responses.</summary>
    public JsonDocument? Doc { get; init; }
    /// <summary>Error message (GitHub's <c>message</c> field, or a local reason like "not-signed-in"/"unauthorized").</summary>
    public string? Error { get; init; }
    /// <summary>True when a 403 looks like a rate/secondary limit.</summary>
    public bool RateLimited { get; init; }

    public void Dispose() => Doc?.Dispose();
}

/// <summary>
/// Build-time configuration for the GitHub OAuth integration. The
/// values here are PUBLIC by design — Device Flow doesn't use a
/// client secret, so embedding the Client ID in the shipped binary
/// is the documented pattern. To deploy your own fork:
///
///   1. Register a new OAuth App at https://github.com/settings/developers
///   2. Tick "Enable Device Flow" in the app's settings.
///   3. Paste the Client ID into <see cref="DefaultClientId"/> below.
///   4. Update <see cref="IssueOwner"/> / <see cref="IssueRepo"/> if
///      issues should land in a different repo than the upstream one.
///
/// Until <see cref="DefaultClientId"/> is set, the in-app login flow stays
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

    /// <summary>Default token scope requested at sign-in. <c>public_repo</c>
    /// covers issue submission and version control against PUBLIC repos. The
    /// user can opt in to the wider <c>repo</c> scope (private repos) via the
    /// Settings toggle — see <see cref="GitHubAuth.SetPrivateScopeAsync"/> — which
    /// overrides this at the next sign-in.</summary>
    public const string Scope = "public_repo";

    /// <summary>Owner of the issue-target repo.</summary>
    public const string IssueOwner = "Kmila9s";

    /// <summary>Issue-target repo name.</summary>
    public const string IssueRepo = "Kmila";
}
