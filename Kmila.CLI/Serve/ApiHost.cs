using System.Net;
using System.Text;
using Kmila.Shared.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kmila.CLI.Serve;

/// <summary>
/// A console host for Kmila's local <c>/api/v1</c> headless-simulation API, built
/// on the BCL <see cref="HttpListener"/>. It mirrors the MAUI head's
/// <c>EmbeddedApiHost</c> dispatch — the endpoint bodies live in the shared,
/// transport-neutral <see cref="SimulationApiHandlers"/>, so this host only
/// translates HTTP to those calls and writes the <see cref="ApiResponse"/> back.
/// (The vendored Swagger UI the MAUI head also serves is omitted here.)
/// </summary>
public sealed class ApiHost
{
    private readonly SandboxProjectStore _store;
    private readonly HeadlessSimulator _sim;
    private readonly string? _token;

    /// <summary>Creates the host with its own sandbox store + simulator.</summary>
    /// <param name="token">Optional bearer/X-Api-Key secret; null disables auth.</param>
    public ApiHost(string? token = null)
    {
        _store = new SandboxProjectStore();
        _sim = new HeadlessSimulator(_store);
        _token = token;
    }

    /// <summary>
    /// Starts listening and serves requests until <paramref name="ct"/> is
    /// cancelled. Returns the bound URL via <paramref name="onReady"/>.
    /// </summary>
    public async Task RunAsync(int port, bool bindLan, Action<string> onReady, CancellationToken ct)
    {
        var listener = new HttpListener();
        // On localhost the managed HttpListener binds without elevation; a LAN
        // bind uses the wildcard prefix.
        var host = bindLan ? "+" : "localhost";
        listener.Prefixes.Add($"http://{host}:{port}/");
        listener.Start();
        onReady($"http://{(bindLan ? "0.0.0.0" : "localhost")}:{port}{SimulationApiHandlers.BasePath}");

        using var reg = ct.Register(() => { try { listener.Stop(); } catch { } });
        try
        {
            while (!ct.IsCancellationRequested && listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync().ConfigureAwait(false); }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }

                _ = Task.Run(async () =>
                {
                    try { await HandleAsync(ctx).ConfigureAwait(false); }
                    catch (Exception ex)
                    {
                        try { WriteResponse(ctx, ApiResponse.Json(500, new ApiError { Error = "internal_error", Detail = ex.Message })); }
                        catch { }
                    }
                    finally { try { ctx.Response.Close(); } catch { } }
                }, ct);
            }
        }
        finally
        {
            try { listener.Close(); } catch { }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var method = req.HttpMethod;
        var path = req.Url?.AbsolutePath ?? "/";

        if (!path.StartsWith(SimulationApiHandlers.BasePath, StringComparison.Ordinal))
        {
            WriteResponse(ctx, ApiResponse.Json(404, new ApiError { Error = "not_found" }));
            return;
        }

        var route = path[SimulationApiHandlers.BasePath.Length..];
        if (route.Length == 0) route = "/";

        if (route != "/health")
        {
            var verdict = SimulationApiHandlers.CheckAuth(_token, name => req.Headers[name]);
            if (verdict is not null) { WriteResponse(ctx, verdict); return; }
        }

        ApiResponse response;
        if (method == "GET" && route == "/health")
        {
            response = SimulationApiHandlers.Health();
        }
        else if (method == "POST" && route == "/projects")
        {
            var dto = SimulationApiHandlers.Deserialize<CreateProjectRequest>(await ReadBodyAsync(req));
            response = SimulationApiHandlers.CreateProject(dto, _store);
        }
        else if (method == "DELETE" && TryMatch(route, "/projects/{id}", out var del))
        {
            response = SimulationApiHandlers.DeleteProject(del["id"], _store);
        }
        else if (method == "POST" && TryMatch(route, "/projects/{id}/files", out var wr))
        {
            var dto = SimulationApiHandlers.Deserialize<WriteFileRequest>(await ReadBodyAsync(req)) ?? new WriteFileRequest();
            response = SimulationApiHandlers.WriteFile(wr["id"], dto, _store);
        }
        else if (method == "GET" && TryMatchFilePath(route, out var fp))
        {
            response = SimulationApiHandlers.ReadFile(fp["id"], fp["name"], _store);
        }
        else if (method == "POST" && route == "/simulate")
        {
            var dto = SimulationApiHandlers.Deserialize<SimulateRequest>(await ReadBodyAsync(req)) ?? new SimulateRequest();
            response = await SimulationApiHandlers.SimulateAsync(dto, _sim, NullLogger.Instance).ConfigureAwait(false);
        }
        else
        {
            response = ApiResponse.Json(404, new ApiError { Error = "not_found" });
        }

        WriteResponse(ctx, response);
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest req)
    {
        if (!req.HasEntityBody) return string.Empty;
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static void WriteResponse(HttpListenerContext ctx, ApiResponse r)
    {
        var res = ctx.Response;
        res.StatusCode = r.StatusCode;
        if (r.StatusCode == 204 || r.Body.Length == 0) { res.ContentLength64 = 0; return; }
        res.ContentType = r.ContentType;
        var bytes = Encoding.UTF8.GetBytes(r.Body);
        res.ContentLength64 = bytes.LongLength;
        res.OutputStream.Write(bytes, 0, bytes.Length);
    }

    // {placeholder} matcher — literal segments must equal, {name} captures one.
    private static bool TryMatch(string route, string pattern, out Dictionary<string, string> parameters)
    {
        parameters = new(StringComparer.Ordinal);
        var routeSegs = route.TrimEnd('/').Split('/');
        var patternSegs = pattern.TrimEnd('/').Split('/');
        if (routeSegs.Length != patternSegs.Length) return false;
        for (int i = 0; i < patternSegs.Length; i++)
        {
            var pp = patternSegs[i];
            var rr = routeSegs[i];
            if (pp.Length >= 2 && pp[0] == '{' && pp[^1] == '}') parameters[pp[1..^1]] = Uri.UnescapeDataString(rr);
            else if (!string.Equals(pp, rr, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    // /projects/{id}/files/{*name} — the name may contain slashes.
    private static bool TryMatchFilePath(string route, out Dictionary<string, string> parameters)
    {
        parameters = new(StringComparer.Ordinal);
        const string prefix = "/projects/";
        if (!route.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var rest = route[prefix.Length..];
        var slash = rest.IndexOf('/');
        if (slash < 0) return false;
        var id = rest[..slash];
        var afterId = rest[(slash + 1)..];
        const string filesSeg = "files/";
        if (!afterId.StartsWith(filesSeg, StringComparison.Ordinal)) return false;
        var name = afterId[filesSeg.Length..];
        if (name.Length == 0) return false;
        parameters["id"] = Uri.UnescapeDataString(id);
        parameters["name"] = Uri.UnescapeDataString(name);
        return true;
    }
}
