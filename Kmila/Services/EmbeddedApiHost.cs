#if KMILA_MAUI_API
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Kmila.Shared.Services.Api;

namespace Kmila.Services;

/// <summary>
/// Tier 5.2 Phase B — hosts <c>/api/v1</c> inside the MAUI Blazor Hybrid
/// process so SimulatorBench / curl can be pointed at the running mobile /
/// Mac Catalyst / iOS app.
///
/// Transport: <see cref="HttpListener"/> from the BCL on every platform except
/// iOS, where it throws <c>PlatformNotSupportedException</c> at runtime; iOS uses
/// a small <see cref="TcpListener"/>-based HTTP/1.1 server instead (B2,
/// 2026-11-06). Both transports funnel every request through the SAME
/// transport-neutral <see cref="RouteAsync"/> → <see cref="SimulationApiHandlers"/>
/// dispatch — the same code path Kmila.Web's Minimal-API map uses — so all hosts
/// return byte-identical JSON. Kestrel isn't used because
/// <c>Microsoft.AspNetCore.App</c> has no runtime pack for the mobile RIDs.
///
/// iOS has no foreground-service equivalent, so the host runs only while the app
/// is foregrounded: it is stopped on sleep and restarted on resume (see
/// <c>App.xaml.cs</c>). Loopback (127.0.0.1) needs no special entitlement.
/// </summary>
public sealed class EmbeddedApiHost : ILocalApiHost, IAsyncDisposable
{
    private readonly ILogger<EmbeddedApiHost> _log;
    private readonly LocalApiTokenProvider    _tokens;

    private HttpListener?    _listener;
    private TcpListener?     _tcp;          // iOS transport (HttpListener is unsupported there)
    private volatile bool    _running;      // transport-neutral running flag
    private CancellationTokenSource? _loopCts;
    private Task?            _loopTask;
    private SandboxProjectStore? _store;
    private HeadlessSimulator?   _sim;
    private SandboxJanitorTimer? _janitor;
    private readonly SemaphoreSlim _gate = new(1, 1);

#if ANDROID
    // Android only: true when the host was promoted via the foreground service
    // (LAN binding), false when it was started in-process (loopback default).
    // StopAsync uses this to tear down the matching path — stopping a service
    // that was never started would leave an in-process listener running.
    private bool _startedViaService;
#endif

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

    public bool IsRunning => _running;

    // NOTE: OperatingSystem.IsIOS() returns true on Mac Catalyst too (Catalyst
    // is derived from iOS) — the correct "iOS specifically" check excludes
    // Mac Catalyst explicitly. Getting this wrong once already refused
    // to start on Mac Catalyst; watch out.
    // Supported on every MAUI head now: HttpListener elsewhere, a TcpListener
    // HTTP server on iOS (B2, 2026-11-06). Kept as a property so callers/UI can
    // still branch on it, but it no longer excludes iOS.
    public static bool IsPlatformSupported => true;

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
        if (IsRunning) return;

        // Restart-on-enable fix (2026-11-06): the foreground-service path below
        // (StartForegroundService + its POST_NOTIFICATIONS prompt) was tearing
        // the activity down and restarting the whole app on some OEMs
        // (Xiaomi/MIUI), which looked like a crash. The foreground service only
        // exists to keep the socket alive while the app is BACKGROUNDED. The
        // loopback default (127.0.0.1 — local use, adb-forward from a dev
        // machine) is used while the app is foregrounded, so it doesn't need the
        // service at all: start in-process, no service, no notification prompt,
        // no restart. LAN binding still uses the foreground service because it
        // genuinely needs to survive backgrounding.
        if (!bindLan)
        {
            _startedViaService = false;
            await StartInternalAsync(port, bindLan, ct).ConfigureAwait(false);
            return;
        }
        _startedViaService = true;

        // Tier 5.2 Phase B follow-up — foreground-service path (LAN only now).
        // Dispatch, then poll IsRunning so callers observe the same "true when I
        // return" semantics as the direct path.
        //
        // API guard (rev 2, 2026-08-01): Context.StartForegroundService was
        // added in API 26 (Android 8.0 Oreo). Our SupportedOSPlatformVersion
        // is 24, so calling it unconditionally crashes on Android 7.0-7.1
        // with MissingMethodException. On pre-Oreo devices, fall back to
        // Context.StartService — the service's own StartForeground(id,
        // notif) call in OnStartCommand still promotes the process; the
        // API-26 entry point is only about *how* you kick off the start.
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

            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            int effectivePort;
#if IOS
            // iOS: HttpListener throws PlatformNotSupportedException at runtime,
            // so serve via a TcpListener-based HTTP/1.1 server instead (B2).
            var endpoint = new IPEndPoint(bindLan ? IPAddress.Any : IPAddress.Loopback, port);
            var tcp = new TcpListener(endpoint);
            tcp.Start();
            _tcp = tcp;
            effectivePort = ((IPEndPoint)tcp.LocalEndpoint).Port;
            _loopTask = Task.Run(() => TcpAcceptLoopAsync(tcp, _loopCts.Token));
#else
            var host = bindLan ? "+" : "127.0.0.1";
            // HttpListener requires a trailing slash on the prefix, and the
            // "+" host requires admin/root on some platforms; loopback is
            // the safe default.
            var prefix = $"http://{host}:{port}/";

            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            _listener = listener;
            effectivePort = ExtractListeningPort(listener) ?? port;
            _loopTask = Task.Run(() => AcceptLoopAsync(listener, _loopCts.Token));
#endif

            // Effective URL — advertise 127.0.0.1 (or the machine's LAN IP
            // when bindLan) rather than "+", which is a wildcard binding
            // convention that no HTTP client understands.
            var advertisedHost = bindLan ? GetPrimaryLanIp() : "127.0.0.1";
            BoundUrl = new Uri($"http://{advertisedHost}:{effectivePort}/");
            _running = true;

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
        // In-process (loopback) start has no service to stop — tear it down
        // directly. Only the LAN/foreground-service start goes through StopService.
        if (!_startedViaService)
        {
            await StopInternalAsync(ct).ConfigureAwait(false);
            return;
        }
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
        try { _tcp?.Stop(); } catch { }
        try { _tcp?.Dispose(); } catch { }
        _tcp = null;
        _running = false;

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

    // ─── HttpListener transport (all platforms except iOS) ───────

#if !IOS
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
                        WriteResultToCtx(ctx, ToResult(ApiResponse.Json(500, new ApiError
                        {
                            Error  = "internal_error",
                            Detail = ex.Message,
                        })));
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

    // Thin adapter: translate an HttpListener request into the neutral router
    // and write the resolved result back to the context.
    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var result = await RouteAsync(
            req.HttpMethod,
            req.Url?.AbsolutePath ?? "/",
            name => req.Headers[name],
            () => ReadBodyAsync(req)).ConfigureAwait(false);
        WriteResultToCtx(ctx, result);
    }

    private static void WriteResultToCtx(HttpListenerContext ctx, ApiHttpResult result)
    {
        var res = ctx.Response;
        res.StatusCode = result.Status;
        if (result.Body.Length == 0) { res.ContentLength64 = 0; return; }
        res.ContentType     = result.ContentType;
        res.ContentLength64 = result.Body.LongLength;
        res.OutputStream.Write(result.Body, 0, result.Body.Length);
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest req)
    {
        if (!req.HasEntityBody) return string.Empty;
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
#endif

    // ─── Transport-neutral routing (shared by every transport) ───

    /// <summary>A fully-resolved HTTP response: status, content-type, body
    /// bytes. An empty <see cref="Body"/> means no entity (e.g. 204).</summary>
    internal readonly record struct ApiHttpResult(int Status, string ContentType, byte[] Body);

    private async Task<ApiHttpResult> RouteAsync(
        string method, string path,
        Func<string, string?> getHeader,
        Func<Task<string>> readBodyAsync)
    {
        // Static-file dispatch — Swagger UI shim + OpenAPI snapshot + the
        // vendored swagger-ui-dist assets. GET-only; safe even when a bearer
        // token gates /api/v1 (they reveal the API shape only, not data).
        if (method == "GET" && TryResolveDocsAsset(path, out var asset))
            return asset;

        if (!path.StartsWith(SimulationApiHandlers.BasePath, StringComparison.Ordinal))
            return ToResult(ApiResponse.Json(404, new ApiError { Error = "not_found" }));

        var route = path.Substring(SimulationApiHandlers.BasePath.Length);
        if (route.Length == 0) route = "/";

        if (_store is null || _sim is null)
            return ToResult(ApiResponse.Json(503, new ApiError { Error = "not_ready" }));

        // Bearer auth — same shape as the Web endpoint filter; /health exempt.
        if (route != "/health")
        {
            var verdict = SimulationApiHandlers.CheckAuth(_tokens.Current, getHeader);
            if (verdict is not null) return ToResult(verdict);
        }

        ApiResponse response;

        // Order matters — more specific patterns first.
        if (method == "GET" && route == "/health")
        {
            response = SimulationApiHandlers.Health();
        }
        else if (method == "POST" && route == "/projects")
        {
            var body = await readBodyAsync().ConfigureAwait(false);
            var dto  = SimulationApiHandlers.Deserialize<CreateProjectRequest>(body);
            response = SimulationApiHandlers.CreateProject(dto, _store);
        }
        else if (method == "DELETE" && TryMatch(route, "/projects/{id}", out var delParams))
        {
            response = SimulationApiHandlers.DeleteProject(delParams["id"], _store);
        }
        else if (method == "POST" && TryMatch(route, "/projects/{id}/files", out var writeParams))
        {
            var body = await readBodyAsync().ConfigureAwait(false);
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
            var body = await readBodyAsync().ConfigureAwait(false);
            var dto  = SimulationApiHandlers.Deserialize<SimulateRequest>(body)
                       ?? new SimulateRequest();
            // _log is routed through InMemoryLoggerProvider so /simulate errors
            // surface in /logs. The handler only logs on the passed-in ILogger.
            response = await SimulationApiHandlers.SimulateAsync(dto, _sim, _log).ConfigureAwait(false);
        }
        else
        {
            response = ApiResponse.Json(404, new ApiError { Error = "not_found" });
        }

        return ToResult(response);
    }

    private static ApiHttpResult ToResult(ApiResponse r)
    {
        if (r.StatusCode == 204 || r.Body.Length == 0)
            return new ApiHttpResult(r.StatusCode, string.Empty, Array.Empty<byte>());
        return new ApiHttpResult(r.StatusCode, r.ContentType, Encoding.UTF8.GetBytes(r.Body));
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

    // Resolve a docs/static asset to bytes (transport-neutral). Returns false
    // when the path isn't a docs asset or the asset wasn't bundled.
    private bool TryResolveDocsAsset(string path, out ApiHttpResult result)
    {
        // /api-docs → the HTML shim (with or without trailing slash).
        if (path is "/api-docs" or "/api-docs/" or "/api-docs/index.html")
        {
            if (_apiDocsHtml is not null)
            {
                result = new ApiHttpResult(200, "text/html; charset=utf-8", _apiDocsHtml);
                return true;
            }
        }
        // /openapi/v1.json → the committed spec snapshot.
        else if (path == "/openapi/v1.json")
        {
            if (_openapiJson is not null)
            {
                result = new ApiHttpResult(200, "application/json", _openapiJson);
                return true;
            }
        }
        // /lib/swagger-ui/<file> → vendored swagger-ui-dist asset.
        else
        {
            const string swPrefix = "/lib/swagger-ui/";
            if (path.StartsWith(swPrefix, StringComparison.Ordinal))
            {
                var name = path[swPrefix.Length..];
                if (_swaggerUiAssets.TryGetValue(name, out var asset))
                {
                    result = new ApiHttpResult(200, asset.Mime, asset.Bytes);
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

#if IOS
    // ─── iOS transport: minimal TcpListener HTTP/1.1 server ──────
    // HttpListener is unsupported on iOS, so serve the same RouteAsync dispatch
    // over raw sockets. One request per connection, Connection: close (no
    // keep-alive). Request bodies: Content-Length OR Transfer-Encoding: chunked
    // (.NET HttpClient's JsonContent — used by SimulatorBench — is chunked), plus
    // Expect: 100-continue. Responses are Content-Length framed.

    private async Task TcpAcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.LogDebug(ex, "[EmbeddedApiHost] (iOS) Accept failed.");
                continue;
            }
            _ = Task.Run(() => HandleTcpClientAsync(client, ct));
        }
    }

    private async Task HandleTcpClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                client.NoDelay = true;
                using var stream = client.GetStream();

                var parsed = await ReadHttpRequestAsync(stream, ct).ConfigureAwait(false);
                if (parsed is null) return; // malformed / empty

                var (method, target, headers, body) = parsed.Value;
                var qIdx = target.IndexOf('?');          // routes match the path only
                var path = qIdx >= 0 ? target[..qIdx] : target;

                ApiHttpResult result;
                try
                {
                    result = await RouteAsync(
                        method, path,
                        name => headers.TryGetValue(name.ToLowerInvariant(), out var v) ? v : null,
                        () => Task.FromResult(body)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[EmbeddedApiHost] (iOS) Unhandled request error.");
                    result = ToResult(ApiResponse.Json(500, new ApiError
                    {
                        Error = "internal_error", Detail = ex.Message,
                    }));
                }

                await WriteHttpResponseAsync(stream, result, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "[EmbeddedApiHost] (iOS) Connection error.");
            }
        }
    }

    // Read the request line + headers (until CRLFCRLF), then the Content-Length
    // body. Returns null on a malformed/empty request.
    private static async Task<(string Method, string Target, Dictionary<string, string> Headers, string Body)?>
        ReadHttpRequestAsync(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var acc = new List<byte>(8192);
        int headerEnd = -1;
        while (headerEnd < 0)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
            if (n == 0) break;
            for (int i = 0; i < n; i++) acc.Add(buffer[i]);
            headerEnd = IndexOfDoubleCrlf(acc);
            if (acc.Count > 1_048_576) return null; // 1 MB header guard
        }
        if (headerEnd < 0) return null;

        var headerText = Encoding.ASCII.GetString(acc.ToArray(), 0, headerEnd);
        var lines = headerText.Split("\r\n");
        if (lines.Length == 0 || lines[0].Length == 0) return null;
        var reqParts = lines[0].Split(' ');
        if (reqParts.Length < 2) return null;
        var method = reqParts[0];
        var target = reqParts[1];

        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0) continue;
            var c = line.IndexOf(':');
            if (c <= 0) continue;
            headers[line[..c].Trim().ToLowerInvariant()] = line[(c + 1)..].Trim();
        }

        // Bytes already read past the header block belong to the body.
        int bodyStart = headerEnd + 4; // past CRLFCRLF
        var reader = new BodyReader(stream,
            acc.Count > bodyStart ? acc.GetRange(bodyStart, acc.Count - bodyStart).ToArray() : Array.Empty<byte>());

        // "Expect: 100-continue" (curl sends it for larger bodies): tell the client
        // to go ahead instead of making it wait for its timeout.
        if (headers.TryGetValue("expect", out var expect) &&
            expect.Equals("100-continue", StringComparison.OrdinalIgnoreCase))
        {
            var cont = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
            await stream.WriteAsync(cont, ct).ConfigureAwait(false);
        }

        string body = string.Empty;
        bool chunked = headers.TryGetValue("transfer-encoding", out var te) &&
                       te.Contains("chunked", StringComparison.OrdinalIgnoreCase);
        if (chunked)
        {
            // .NET HttpClient sends JsonContent (PostAsJsonAsync — SimulatorBench) with
            // Transfer-Encoding: chunked and NO Content-Length, so chunked bodies must be
            // decoded (RFC 9112 §7.1): hex size [;ext] CRLF, data CRLF, ..., 0 CRLF,
            // optional trailers, CRLF.
            var bytes = await ReadChunkedBodyAsync(reader, ct).ConfigureAwait(false);
            if (bytes is null) return null; // malformed / oversized
            body = Encoding.UTF8.GetString(bytes);
        }
        else
        {
            int contentLength = 0;
            if (headers.TryGetValue("content-length", out var clStr))
                int.TryParse(clStr, out contentLength);
            if (contentLength > MaxBodyBytes) return null;
            if (contentLength > 0)
            {
                var bodyBytes = new byte[contentLength];
                int read = await reader.ReadAsync(bodyBytes, 0, contentLength, ct).ConfigureAwait(false);
                body = Encoding.UTF8.GetString(bodyBytes, 0, read);
            }
        }

        return (method, target, headers, body);
    }

    // Request body cap (largest real payloads are VHDL sources + stimulus JSON).
    private const int MaxBodyBytes = 64 * 1024 * 1024;

    private static async Task<byte[]?> ReadChunkedBodyAsync(BodyReader reader, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        while (true)
        {
            var sizeLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (sizeLine is null) return null;
            var semi = sizeLine.IndexOf(';');              // drop chunk extensions
            var hex = (semi >= 0 ? sizeLine[..semi] : sizeLine).Trim();
            if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var size) || size < 0)
                return null;
            if (size == 0)
            {
                // Trailer section: header lines until an empty line (or EOF).
                while (true)
                {
                    var trailer = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(trailer)) break;
                }
                return ms.ToArray();
            }
            if (ms.Length + size > MaxBodyBytes) return null;
            var chunk = new byte[size];
            int got = await reader.ReadAsync(chunk, 0, size, ct).ConfigureAwait(false);
            if (got < size) return null;                   // truncated
            ms.Write(chunk, 0, size);
            var crlf = await reader.ReadLineAsync(ct).ConfigureAwait(false); // CRLF after data
            if (crlf is null || crlf.Length != 0) return null;
        }
    }

    // Reads the request body: first from bytes already buffered with the headers,
    // then from the network stream.
    private sealed class BodyReader
    {
        private readonly NetworkStream _stream;
        private byte[] _buf;
        private int _pos;
        private int _len;

        public BodyReader(NetworkStream stream, byte[] pending)
        {
            _stream = stream; _buf = pending; _pos = 0; _len = pending.Length;
        }

        private async Task<bool> FillAsync(CancellationToken ct)
        {
            if (_buf.Length < 8192) _buf = new byte[8192];
            _len = await _stream.ReadAsync(_buf.AsMemory(0, _buf.Length), ct).ConfigureAwait(false);
            _pos = 0;
            return _len > 0;
        }

        /// <summary>Reads up to <paramref name="count"/> bytes; returns fewer only at EOF.</summary>
        public async Task<int> ReadAsync(byte[] dest, int offset, int count, CancellationToken ct)
        {
            int read = 0;
            while (read < count)
            {
                if (_pos >= _len && !await FillAsync(ct).ConfigureAwait(false)) break;
                int n = Math.Min(count - read, _len - _pos);
                Buffer.BlockCopy(_buf, _pos, dest, offset + read, n);
                _pos += n; read += n;
            }
            return read;
        }

        /// <summary>Reads one CRLF-terminated ASCII line (without the CRLF); null at EOF.</summary>
        public async Task<string?> ReadLineAsync(CancellationToken ct)
        {
            var sb = new StringBuilder();
            while (true)
            {
                if (_pos >= _len && !await FillAsync(ct).ConfigureAwait(false))
                    return sb.Length > 0 ? sb.ToString() : null;
                byte b = _buf[_pos++];
                if (b == (byte)'\n')
                {
                    if (sb.Length > 0 && sb[^1] == '\r') sb.Length--;
                    return sb.ToString();
                }
                sb.Append((char)b);
                if (sb.Length > 8192) return null;          // runaway line guard
            }
        }
    }

    private static int IndexOfDoubleCrlf(List<byte> b)
    {
        for (int i = 0; i + 3 < b.Count; i++)
            if (b[i] == 13 && b[i + 1] == 10 && b[i + 2] == 13 && b[i + 3] == 10) return i;
        return -1;
    }

    private static async Task WriteHttpResponseAsync(NetworkStream stream, ApiHttpResult result, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.Append("HTTP/1.1 ").Append(result.Status).Append(' ').Append(ReasonPhrase(result.Status)).Append("\r\n");
        if (result.Body.Length > 0)
        {
            sb.Append("Content-Type: ")
              .Append(string.IsNullOrEmpty(result.ContentType) ? "application/octet-stream" : result.ContentType)
              .Append("\r\n");
            sb.Append("Content-Length: ").Append(result.Body.Length).Append("\r\n");
        }
        else
        {
            sb.Append("Content-Length: 0\r\n");
        }
        sb.Append("Connection: close\r\n\r\n");

        var head = Encoding.ASCII.GetBytes(sb.ToString());
        await stream.WriteAsync(head.AsMemory(0, head.Length), ct).ConfigureAwait(false);
        if (result.Body.Length > 0)
            await stream.WriteAsync(result.Body.AsMemory(0, result.Body.Length), ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static string ReasonPhrase(int status) => status switch
    {
        200 => "OK", 201 => "Created", 204 => "No Content",
        400 => "Bad Request", 401 => "Unauthorized", 403 => "Forbidden",
        404 => "Not Found", 500 => "Internal Server Error", 503 => "Service Unavailable",
        _   => "Status",
    };
#endif

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
