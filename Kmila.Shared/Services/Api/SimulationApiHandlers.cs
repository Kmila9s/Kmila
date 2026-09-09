using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Api;

/// <summary>
/// Target-agnostic bodies of the <c>/api/v1</c> endpoints. Called from both
/// <c>SimulationApiEndpoints.MapSimulationApi</c> (Kmila.Web, Minimal APIs)
/// and <c>EmbeddedApiHost</c> (MAUI heads, HttpListener). Keeping the
/// endpoint logic here means one authoritative implementation — the two
/// transport shims just translate their transport's request into these
/// calls and marshal <see cref="ApiResponse"/> back onto the wire.
/// </summary>
public static class SimulationApiHandlers
{
    /// <summary>Route prefix shared by every /api/v1 endpoint. Declared here
    /// (target-agnostic file) so the MAUI HttpListener host can reach it
    /// even when the ASP.NET-guarded <c>SimulationApiEndpoints</c> class is
    /// compiled out.</summary>
    public const string BasePath = "/api/v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(object? payload) =>
        JsonSerializer.Serialize(payload, JsonOpts);

    public static T? Deserialize<T>(string body) =>
        string.IsNullOrWhiteSpace(body)
            ? default
            : JsonSerializer.Deserialize<T>(body, JsonOpts);

    public static ApiResponse Health() => ApiResponse.Json(200, new
    {
        status     = "ok",
        version    = "1",
        startedUtc = DateTime.UtcNow,
    });

    /// <summary>
    /// Tier 5.2 Phase B follow-up — verifies the caller's bearer token
    /// against the configured secret. Returns <c>null</c> when auth is
    /// disabled (<paramref name="expectedToken"/> null/empty) or when the
    /// caller passed the right credential; returns a 401 <see cref="ApiResponse"/>
    /// otherwise. Both transports (Minimal-API filter + HttpListener
    /// dispatcher) delegate to this so their behavior stays byte-identical.
    ///
    /// Accepts either <c>Authorization: Bearer &lt;token&gt;</c> (case-insensitive
    /// scheme) or <c>X-Api-Key: &lt;token&gt;</c>. Comparison uses
    /// <see cref="CryptographicOperations.FixedTimeEquals"/> so a valid token
    /// can't be inferred from timing differences.
    /// </summary>
    public static ApiResponse? CheckAuth(string? expectedToken, Func<string, string?> readHeader)
    {
        if (string.IsNullOrEmpty(expectedToken)) return null;

        string? presented = null;
        var authz = readHeader("Authorization");
        if (!string.IsNullOrEmpty(authz))
        {
            const string bearerPrefix = "Bearer ";
            if (authz.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                presented = authz.Substring(bearerPrefix.Length).Trim();
        }
        if (string.IsNullOrEmpty(presented))
        {
            var apiKey = readHeader("X-Api-Key");
            if (!string.IsNullOrEmpty(apiKey)) presented = apiKey.Trim();
        }

        if (string.IsNullOrEmpty(presented))
            return ApiResponse.Json(401, new ApiError { Error = "unauthorized" });

        var expBytes  = Encoding.UTF8.GetBytes(expectedToken);
        var gotBytes  = Encoding.UTF8.GetBytes(presented);
        if (expBytes.Length != gotBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expBytes, gotBytes))
        {
            return ApiResponse.Json(401, new ApiError { Error = "unauthorized" });
        }

        return null;
    }

    public static ApiResponse CreateProject(CreateProjectRequest? req, SandboxProjectStore store)
    {
        req ??= new CreateProjectRequest();
        var p = store.Create(req.Name, req.Retain);
        return ApiResponse.Json(200, new CreateProjectResponse
        {
            ProjectId   = p.Id,
            SandboxPath = p.Path,
        });
    }

    public static ApiResponse WriteFile(string id, WriteFileRequest body, SandboxProjectStore store)
    {
        var project = store.Get(id);
        if (project == null)
            return ApiResponse.Json(404, new ApiError { Error = "project_not_found" });
        try
        {
            string full = project.WriteFile(body.Name, body.Content ?? string.Empty);
            long bytes  = new FileInfo(full).Length;
            return ApiResponse.Json(200, new WriteFileResponse
            {
                Name  = body.Name,
                Path  = full,
                Bytes = bytes,
            });
        }
        catch (ArgumentException ex)
        {
            return ApiResponse.Json(400, new ApiError { Error = "invalid_file_name", Detail = ex.Message });
        }
    }

    public static ApiResponse ReadFile(string id, string name, SandboxProjectStore store)
    {
        var project = store.Get(id);
        if (project == null)
            return ApiResponse.Json(404, new ApiError { Error = "project_not_found" });
        var content = project.ReadFile(name);
        if (content == null)
            return ApiResponse.Json(404, new ApiError { Error = "file_not_found" });
        return ApiResponse.Text(200, content);
    }

    public static ApiResponse DeleteProject(string id, SandboxProjectStore store)
    {
        return store.Delete(id)
            ? ApiResponse.NoContent()
            : ApiResponse.Json(404, new ApiError { Error = "project_not_found" });
    }

    public static async Task<ApiResponse> SimulateAsync(SimulateRequest req, HeadlessSimulator sim, ILogger log)
    {
        try
        {
            var result = await sim.RunAsync(req);
            return ApiResponse.Json(200, result);
        }
        catch (ArgumentException ex)
        {
            return ApiResponse.Json(400, new ApiError { Error = "bad_request", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse.Json(422, new ApiError { Error = "sim_unprocessable", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "[SimulationApi] simulate failed");
            return ApiResponse.Json(500, new
            {
                type   = "sim_failed",
                title  = "sim_failed",
                detail = ex.Message,
                status = 500,
            });
        }
    }
}

/// <summary>
/// Transport-neutral response record. HttpListener writes it verbatim;
/// Minimal APIs wrap it via <see cref="Microsoft.AspNetCore.Http.Results"/>
/// helpers.
/// </summary>
public sealed record ApiResponse(int StatusCode, string ContentType, string Body)
{
    public static ApiResponse Json(int status, object? payload) =>
        new(status, "application/json; charset=utf-8", SimulationApiHandlers.Serialize(payload));

    public static ApiResponse Text(int status, string body) =>
        new(status, "text/plain; charset=utf-8", body);

    public static ApiResponse NoContent() =>
        new(204, string.Empty, string.Empty);
}
