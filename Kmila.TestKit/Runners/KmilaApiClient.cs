using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kmila.Shared.Services.Api;

namespace Kmila.TestKit.Runners;

/// <summary>
/// HttpClient wrapper over a running host's <c>/api/v1</c>, with the request and
/// response types the host itself serializes (<c>Kmila.Shared.Services.Api</c>).
/// </summary>
public sealed class KmilaApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly string _host;
    private readonly string _base;

    /// <summary>Targets <paramref name="baseUrl"/> (host root); a non-empty <paramref name="token"/> is sent as a bearer token.</summary>
    public KmilaApiClient(string baseUrl, TimeSpan? timeout = null, string? token = null)
    {
        _http = new HttpClient { Timeout = timeout ?? TimeSpan.FromMinutes(5) };
        _host = baseUrl.TrimEnd('/');
        _base = _host + "/api/v1";
        // Hosts started with KMILA_LOCAL_API_TOKEN require it on every endpoint but /health.
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>True when <c>/health</c> answers with success.</summary>
    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try { return (await _http.GetAsync(_base + "/health", ct)).IsSuccessStatusCode; }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    /// <summary>The host's live OpenAPI document (<c>/openapi/v1.json</c>), or null when it does not serve one.</summary>
    public async Task<string?> GetOpenApiJsonAsync(CancellationToken ct = default)
    {
        try
        {
            var r = await _http.GetAsync(_host + "/openapi/v1.json", ct);
            return r.IsSuccessStatusCode ? await r.Content.ReadAsStringAsync(ct) : null;
        }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>Creates a sandbox project and returns its id.</summary>
    public async Task<string> CreateProjectAsync(string? name, bool retain = false, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync(_base + "/projects", new CreateProjectRequest { Name = name, Retain = retain }, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreateProjectResponse>(cancellationToken: ct))!.ProjectId;
    }

    /// <summary>Writes one file into a sandbox project.</summary>
    public async Task WriteFileAsync(string projectId, string name, string content, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync(_base + $"/projects/{projectId}/files", new WriteFileRequest { Name = name, Content = content }, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>Runs <c>/simulate</c>; a non-success status throws with the response body.</summary>
    public async Task<SimulateResponse> SimulateAsync(SimulateRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync(_base + "/simulate", req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"simulate failed {(int)resp.StatusCode}: {raw}");
        return JsonSerializer.Deserialize<SimulateResponse>(raw, JsonOpts)!;
    }

    /// <summary>Deletes a sandbox project (best effort).</summary>
    public async Task DeleteProjectAsync(string projectId, CancellationToken ct = default)
    {
        try { await _http.DeleteAsync(_base + $"/projects/{projectId}", ct); }
        catch (HttpRequestException) { /* best effort: the host sweeps orphans */ }
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();
}
