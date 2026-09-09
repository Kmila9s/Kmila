#if KMILA_MAUI_API && !IOS
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Kmila.Shared.Services.Api;

namespace Kmila.Services;

/// <summary>
/// Tier 5.2 Phase B — hosts <c>/api/v1</c> inside the MAUI Blazor Hybrid
/// process so SimulatorBench / curl can be pointed at the running mobile /
/// Mac Catalyst app.
///
/// Uses <see cref="HttpListener"/> from the BCL rather than Kestrel because
/// <c>Microsoft.AspNetCore.App</c> has no runtime pack for maccatalyst/ios/
/// android RIDs. Dispatches requests through <see cref="SimulationApiHandlers"/>
/// — the same code path Kmila.Web's Minimal-API map uses — so both hosts
/// return byte-identical JSON.
///
/// iOS gate: <see cref="IsPlatformSupported"/> is false on iOS, and
/// <see cref="StartAsync"/> refuses-and-logs there.
/// </summary>
public sealed class EmbeddedApiHost : ILocalApiHost, IAsyncDisposable
{
    private readonly ILogger<EmbeddedApiHost> _log;
    private readonly LocalApiTokenProvider    _tokens;

    private HttpListener?    _listener;
    private CancellationTokenSource? _loopCts;
    private Task?            _loopTask;
    private SandboxProjectStore? _store;
    private HeadlessSimulator?   _sim;
    private SandboxJanitorTimer? _janitor;
    private readonly SemaphoreSlim _gate = new(1, 1);

    // Tier 5.2 Phase B follow-up — cached bytes for the vendored Swagger UI
    // shim + the OpenAPI snapshot. Loaded once in StartAsync from bundled
    // MauiAssets (see Kmila.csproj MauiAsset globs for api-docs/, openapi/,
    // lib/swagger-ui/). Null when the assets are missing from the bundle,
    // in which case the corresponding routes 404 rather than 500.
    private byte[]? _apiDocsHtml;
    private byte[]? _openapiJson;
    private readonly Dictionary<string, (byte[] Bytes, string Mime)> _swaggerUiAssets =
        new(StringComparer.Ordinal);

    public EmbeddedApiHost(ILogger<EmbeddedApiHost> log, LocalApiTokenProvider tokens)
    {
        _log    = log;
        _tokens = tokens;
    }

    public Uri? BoundUrl { get; private set; }

    public bool IsRunning => _listener?.IsListening == true;

    // NOTE: OperatingSystem.IsIOS() returns true on Mac Catalyst too (Catalyst
    // is derived from iOS) — the correct "iOS specifically" check excludes
    // Mac Catalyst explicitly. Getting this wrong once already refused
    // to start on Mac Catalyst; watch out.
    public static bool IsPlatformSupported =>
        !(OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst());

    bool ILocalApiHost.IsPlatformSupported => IsPlatformSupported;

    /// <summary>
    /// Bring the host up. Called by Settings.razor and by the MauiProgram
    /// auto-start block. On Android this dispatches to the foreground
    /// service (see <c>EmbeddedApiForegroundService</c>) so the accept
    /// loop survives backgrounding; the service then calls
    /// <see cref="StartInternalAsync"/> which does the real work.
    /// </summary>
    public async Task StartAsync(int port, bool bindLan, CancellationToken ct = default)
    {
#if ANDROID
        // Tier 5.2 Phase B follow-up — foreground-service path. Dispatch,
        // then poll IsRunning so callers observe the same "true when I
        // return" semantics as the direct path.
        //
        // API guard (rev 2, 2026-08-01): Context.StartForegroundService was
        // added in API 26 (Android 8.0 Oreo). Our SupportedOSPlatformVersion
        // is 24, so calling it unconditionally crashes on Android 7.0-7.1
        // with MissingMethodException. On pre-Oreo devices, fall back to
        // Context.StartService — the service's own StartForeground(id,
        // notif) call in OnStartCommand still promotes the process; the
        // API-26 entry point is only about *how* you kick off the start.
        if (IsRunning) return;
        var ctx = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(
            ctx, typeof(Kmila.Platforms.Android.EmbeddedApiForegroundService));
        intent.PutExtra(Kmila.Platforms.Android.EmbeddedApiForegroundService.ExtraPort, port);
        intent.PutExtra(Kmila.Platforms.Android.EmbeddedApiForegroundService.ExtraBindLan, bindLan);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            ctx.StartForegroundService(intent);
        else
            ctx.StartService(intent);
        var deadline = Environment.TickCount64 + 3000;
        while (!IsRunning && Environment.TickCount64 < deadline)
        {
            await Task.Delay(50, ct).ConfigureAwait(false);
        }
        return;
#else
        await StartInternalAsync(port, bindLan, ct).ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// The real StartAsync body — direct on desktop / Mac Catalyst, wrapped
    /// by the foreground service on Android. Kept <c>internal</c> so the
    /// Android service (same assembly) can call it without giving external
    /// callers a way to skip the service-promotion path.
    /// </summary>
    internal async Task StartInternalAsync(int port, bool bindLan, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (IsRunning) return;

            if (!IsPlatformSupported)
            {
                _log.LogWarning("[EmbeddedApiHost] Refusing to start on iOS — feature disabled at runtime.");
                return;
            }

            // Sandbox root inside AppDataDirectory so bench-created dirs
            // don't leak into the user profile on mobile.
            var sandboxRoot = Path.Combine(FileSystem.AppDataDirectory, "api-sandbox");
            _store = new SandboxProjectStore(sandboxRoot);
            _sim   = new HeadlessSimulator(_store);

            _janitor = new SandboxJanitorTimer(_store, _log);
            _janitor.Start();

            // Load the vendored Swagger UI + OpenAPI snapshot once. Best-effort:
            // a missing asset makes the corresponding route 404, but the
            // /api/v1 surface still works.
            await LoadDocsAssetsAsync().ConfigureAwait(false);

            var host = bindLan ? "+" : "127.0.0.1";
            // HttpListener requires a trailing slash on the prefix, and the
            // "+" host requires admin/root on some platforms; loopback is
            // the safe default.
            var prefix = $"http://{host}:{port}/";

            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            _listener = listener;

            // Effective URL — advertise 127.0.0.1 (or the machine's LAN IP
            // when bindLan) rather than "+", which is a wildcard binding
            // convention that no HTTP client understands.
            var advertisedHost = bindLan ? GetPrimaryLanIp() : "127.0.0.1";
            var effectivePort  = ExtractListeningPort(listener) ?? port;
            BoundUrl = new Uri($"http://{advertisedHost}:{effectivePort}/");

            _loopCts  = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loopTask = Task.Run(() => AcceptLoopAsync(listener, _loopCts.Token));

            _log.LogInformation(
                "[EmbeddedApiHost] Started on {Url} (lan={Lan}, sandbox={Root}).",
                BoundUrl, bindLan, sandboxRoot);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[EmbeddedApiHost] Start failed.");
            await TeardownAsyncLocked();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Bring the host down. On Android this stops the foreground service,
    /// which in turn calls <see cref="StopInternalAsync"/> from
    /// <c>OnDestroy</c>. Elsewhere it hits the internal path directly.
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
#if ANDROID
        if (!IsRunning) return;
        var ctx = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(
            ctx, typeof(Kmila.Platforms.Android.EmbeddedApiForegroundService));
        ctx.StopService(intent);
        // Poll — service.OnDestroy sync-waits StopInternalAsync so this
        // should flip within a few hundred ms.
        var deadline = Environment.TickCount64 + 3000;
        while (IsRunning && Environment.TickCount64 < deadline)
        {
            await Task.Delay(50, ct).ConfigureAwait(false);
        }
        return;
#else
        await StopInternalAsync(ct).ConfigureAwait(false);
#endif
    }

    /// <summary>The real StopAsync body — direct on desktop, wrapped by
    /// the foreground service on Android.</summary>
    internal async Task StopInternalAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await TeardownAsyncLocked();
            _log.LogInformation("[EmbeddedApiHost] Stopped.");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task TeardownAsyncLocked()
    {
        try { _loopCts?.Cancel(); } catch { }
        try { _listener?.Stop(); }  catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;

        try { _janitor?.Stop(); } catch { }
        _janitor = null;

        if (_loopTask != null)
        {
            try { await _loopTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected */ }
            catch (Exception ex) { _log.LogDebug(ex, "[EmbeddedApiHost] Loop task threw during teardown."); }
        }
        _loopTask = null;

        _loopCts?.Dispose();
        _loopCts = null;

        _store = null;
        _sim   = null;
        BoundUrl = null;
    }

    public async ValueTask DisposeAsync()
    {
        try { await StopAsync(); } catch { /* best-effort */ }
        _gate.Dispose();
    }

    // ─── Dispatch loop ───────────────────────────────────────────

    private async Task AcceptLoopAsync(HttpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.LogDebug(ex, "[EmbeddedApiHost] Accept failed.");
                continue;
            }

            // Handle each request on the thread pool so a slow /simulate
            // doesn't block subsequent /health probes.
            _ = Task.Run(async () =>
            {
                try { await HandleAsync(ctx).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[EmbeddedApiHost] Unhandled request error.");
                    try
                    {
                        WriteResponse(ctx, ApiResponse.Json(500, new ApiError
                        {
                            Error  = "internal_error",
                            Detail = ex.Message,
                        }));
                    }
                    catch { /* connection already gone */ }
                }
                finally
                {
                    try { ctx.Response.Close(); } catch { /* already closed */ }
                }
            });
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var req    = ctx.Request;
        var method = req.HttpMethod;
        var path   = req.Url?.AbsolutePath ?? "/";

        // Static-file dispatch — Swagger UI shim + OpenAPI snapshot + the
        // vendored swagger-ui-dist assets. These are all GET-only and safe
        // to expose even when a bearer token gates /api/v1 (they reveal
        // the API shape only, not project data).
        if (method == "GET" && TryServeDocsAsset(ctx, path))
        {
            return;
        }

        if (!path.StartsWith(SimulationApiHandlers.BasePath, StringComparison.Ordinal))
        {
            WriteResponse(ctx, ApiResponse.Json(404, new ApiError { Error = "not_found" }));
            return;
        }

        var route = path.Substring(SimulationApiHandlers.BasePath.Length);
        if (route.Length == 0) route = "/";

        if (_store is null || _sim is null)
        {
            WriteResponse(ctx, ApiResponse.Json(503, new ApiError { Error = "not_ready" }));
            return;
        }

        // Tier 5.2 Phase B follow-up — bearer auth. Same shape as the Web
        // endpoint filter in SimulationApiEndpoints; /health stays exempt.
        if (route != "/health")
        {
            var verdict = SimulationApiHandlers.CheckAuth(
                _tokens.Current,
                name => req.Headers[name]);
            if (verdict is not null)
            {
                WriteResponse(ctx, verdict);
                return;
            }
        }

        ApiResponse response;

        // Order matters — more specific patterns first.
        if (method == "GET" && route == "/health")
        {
            response = SimulationApiHandlers.Health();
        }
        else if (method == "POST" && route == "/projects")
        {
            var body = await ReadBodyAsync(req).ConfigureAwait(false);
            var dto  = SimulationApiHandlers.Deserialize<CreateProjectRequest>(body);
            response = SimulationApiHandlers.CreateProject(dto, _store);
        }
        else if (method == "DELETE" && TryMatch(route, "/projects/{id}", out var delParams))
        {
            response = SimulationApiHandlers.DeleteProject(delParams["id"], _store);
        }
        else if (method == "POST" && TryMatch(route, "/projects/{id}/files", out var writeParams))
        {
            var body = await ReadBodyAsync(req).ConfigureAwait(false);
            var dto  = SimulationApiHandlers.Deserialize<WriteFileRequest>(body)
                       ?? new WriteFileRequest();
            response = SimulationApiHandlers.WriteFile(writeParams["id"], dto, _store);
        }
        else if (method == "GET" && TryMatchFilePath(route, out var fileParams))
        {
            response = SimulationApiHandlers.ReadFile(fileParams["id"], fileParams["name"], _store);
        }
        else if (method == "POST" && route == "/simulate")
        {
            var body = await ReadBodyAsync(req).ConfigureAwait(false);
            var dto  = SimulationApiHandlers.Deserialize<SimulateRequest>(body)
                       ?? new SimulateRequest();
            // _log is already routed through InMemoryLoggerProvider (registered
            // in MauiProgram.cs), so /simulate errors surface in /logs. The
            // handler only calls LogError on the passed-in ILogger.
            response = await SimulationApiHandlers.SimulateAsync(dto, _sim, _log).ConfigureAwait(false);
        }
        else
        {
            response = ApiResponse.Json(404, new ApiError { Error = "not_found" });
        }

        WriteResponse(ctx, response);
    }

    // ─── Docs static-file serving ────────────────────────────────

    // Load /api-docs/index.html, /openapi/v1.json, and the vendored
    // swagger-ui-dist bundle from MAUI's app-package assets. Best-effort:
    // any missing file simply doesn't get a route.
    private async Task LoadDocsAssetsAsync()
    {
        _apiDocsHtml = await TryReadPackageBytesAsync("api-docs/index.html").ConfigureAwait(false);
        _openapiJson = await TryReadPackageBytesAsync("openapi/v1.json").ConfigureAwait(false);

        _swaggerUiAssets.Clear();
        foreach (var (name, mime) in new[]
        {
            ("swagger-ui.css",                   "text/css; charset=utf-8"),
            ("swagger-ui-bundle.js",             "application/javascript; charset=utf-8"),
            ("swagger-ui-standalone-preset.js",  "application/javascript; charset=utf-8"),
            ("favicon-16x16.png",                "image/png"),
            ("favicon-32x32.png",                "image/png"),
        })
        {
            var bytes = await TryReadPackageBytesAsync($"lib/swagger-ui/{name}").ConfigureAwait(false);
            if (bytes is not null) _swaggerUiAssets[name] = (bytes, mime);
        }
    }

    private static async Task<byte[]?> TryReadPackageBytesAsync(string logicalPath)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(logicalPath).ConfigureAwait(false);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms).ConfigureAwait(false);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private bool TryServeDocsAsset(HttpListenerContext ctx, string path)
    {
        // /api-docs → serve the HTML shim (both with and without trailing
        // slash — the shim uses relative URLs which resolve identically
        // in either case).
        if (path is "/api-docs" or "/api-docs/" or "/api-docs/index.html")
        {
            if (_apiDocsHtml is null) return false;
            WriteBytes(ctx, 200, "text/html; charset=utf-8", _apiDocsHtml);
            return true;
        }

        // /openapi/v1.json → the committed spec snapshot.
        if (path == "/openapi/v1.json")
        {
            if (_openapiJson is null) return false;
            WriteBytes(ctx, 200, "application/json", _openapiJson);
            return true;
        }

        // /lib/swagger-ui/<file> → vendored swagger-ui-dist asset.
        const string swPrefix = "/lib/swagger-ui/";
        if (path.StartsWith(swPrefix, StringComparison.Ordinal))
        {
            var name = path[swPrefix.Length..];
            if (_swaggerUiAssets.TryGetValue(name, out var asset))
            {
                WriteBytes(ctx, 200, asset.Mime, asset.Bytes);
                return true;
            }
        }

        return false;
    }

    private static void WriteBytes(HttpListenerContext ctx, int status, string contentType, byte[] bytes)
    {
        var res = ctx.Response;
        res.StatusCode      = status;
        res.ContentType     = contentType;
        res.ContentLength64 = bytes.LongLength;
        res.OutputStream.Write(bytes, 0, bytes.Length);
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
        if (r.StatusCode == 204 || r.Body.Length == 0)
        {
            res.ContentLength64 = 0;
            return;
        }
        res.ContentType = r.ContentType;
        var bytes = Encoding.UTF8.GetBytes(r.Body);
        res.ContentLength64 = bytes.LongLength;
        res.OutputStream.Write(bytes, 0, bytes.Length);
    }

    // Simple {placeholder} matcher — literal segments must equal, {name}
    // captures one path segment. Enough for the five /api/v1 routes; a
    // full router would be over-engineering for this surface.
    private static bool TryMatch(string route, string pattern, out Dictionary<string, string> parameters)
    {
        parameters = new(StringComparer.Ordinal);
        var routeSegs   = route.TrimEnd('/').Split('/');
        var patternSegs = pattern.TrimEnd('/').Split('/');
        if (routeSegs.Length != patternSegs.Length) return false;
        for (int i = 0; i < patternSegs.Length; i++)
        {
            var p = patternSegs[i];
            var r = routeSegs[i];
            if (p.Length >= 2 && p[0] == '{' && p[^1] == '}')
            {
                parameters[p[1..^1]] = Uri.UnescapeDataString(r);
            }
            else if (!string.Equals(p, r, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    // Special case: /projects/{id}/files/{*name} — the file name may contain
    // slashes, which the generic matcher would reject.
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
        parameters["id"]   = Uri.UnescapeDataString(id);
        parameters["name"] = Uri.UnescapeDataString(name);
        return true;
    }

    private static string GetPrimaryLanIp()
    {
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    var a = ua.Address;
                    if (a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(a)) continue;
                    return a.ToString();
                }
            }
        }
        catch { /* best-effort */ }
        return "0.0.0.0";
    }

    private static int? ExtractListeningPort(HttpListener listener)
    {
        // HttpListener doesn't surface the effective port when the prefix
        // requested a specific one, but if the caller asked for 0 we can't
        // easily learn what was picked — fall back to the requested value.
        foreach (var prefix in listener.Prefixes)
        {
            if (Uri.TryCreate(prefix, UriKind.Absolute, out var u)) return u.Port;
        }
        return null;
    }
}

/// <summary>
/// Timer-driven sandbox reaper for the MAUI-embedded host. Mirrors the
/// TTL policy of <see cref="SandboxJanitor"/> without depending on the
/// ASP.NET Core <c>IHostedService</c> plumbing that isn't in this host.
/// </summary>
internal sealed class SandboxJanitorTimer
{
    private static readonly TimeSpan Interval          = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan InitialDelay      = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan NonRetainedTtl    = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RetainedTtl       = TimeSpan.FromHours(24);
    private static readonly TimeSpan DiskOrphanTtl     = TimeSpan.FromMinutes(30);

    private readonly SandboxProjectStore _store;
    private readonly ILogger _log;
    private CancellationTokenSource? _cts;
    private Task? _task;

    public SandboxJanitorTimer(SandboxProjectStore store, ILogger log)
    {
        _store = store;
        _log   = log;
    }

    public void Start()
    {
        _cts  = new CancellationTokenSource();
        _task = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(InitialDelay, ct).ConfigureAwait(false);
            var initial = _store.SweepDiskOrphans(DiskOrphanTtl);
            if (initial > 0)
                _log.LogInformation("[SandboxJanitorTimer] Initial disk-orphan sweep reaped {N}.", initial);
            while (!ct.IsCancellationRequested)
            {
                var mem  = _store.SweepInMemory(NonRetainedTtl, RetainedTtl);
                var disk = _store.SweepDiskOrphans(DiskOrphanTtl);
                if (mem + disk > 0)
                    _log.LogDebug("[SandboxJanitorTimer] Reaped mem={Mem} disk={Disk}.", mem, disk);
                await Task.Delay(Interval, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            _log.LogError(ex, "[SandboxJanitorTimer] Loop crashed.");
        }
    }
}
#endif
