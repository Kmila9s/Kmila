namespace Kmila.Shared.Services.Api;

/// <summary>
/// Tier 5.2 Phase B follow-up — cross-host holder for the bearer token
/// gating <c>/api/v1</c>. Volatile string reference so read/write from
/// different threads are atomic; both endpoint filters (Kmila.Web Minimal
/// APIs) and the MAUI HttpListener dispatcher read <see cref="Current"/>
/// per-request.
///
/// Persisted value is <see cref="Kmila.Shared.Models.AppSettings.KEY_LOCAL_API_TOKEN"/>.
/// Hosts hydrate at startup; the Settings page calls <see cref="Set"/> when
/// the user regenerates or clears the token so changes take effect without
/// restarting the host.
///
/// Empty string / null both map to "no token required" (dev-parity default).
/// </summary>
public sealed class LocalApiTokenProvider
{
    private volatile string? _token;

    /// <summary>Currently active token, or null when auth is disabled.</summary>
    public string? Current => _token;

    /// <summary>Update the cached token. Empty / whitespace / null all
    /// disable auth. Safe to call from any thread.</summary>
    public void Set(string? token)
    {
        _token = string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
