using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Kmila.Wasm.Services;

/// <summary>
/// Durability for the demo's writable tree. The .NET wasm runtime keeps
/// everything Kmila persists — the SQLite DB and every project's VHDL files —
/// under <c>/kmila</c> in an in-memory (MEMFS) filesystem that is lost on
/// reload. This service hydrates that tree from IndexedDB at startup and
/// flushes it back whenever it changes, so a returning visitor keeps their
/// projects. The heavy lifting on the JS side lives in kmila-wasm-fs.js.
/// </summary>
public sealed class BrowserFsSync : IAsyncDisposable
{
    /// <summary>MEMFS root that mirrors the platform data directory in the browser.</summary>
    public const string Root = "/kmila";

    private readonly IJSRuntime _js;
    private readonly ILogger<BrowserFsSync> _log;
    private DotNetObjectReference<BrowserFsSync>? _selfRef;
    private long _lastSignature = long.MinValue;
    private bool _autoStarted;

    public BrowserFsSync(IJSRuntime js, ILogger<BrowserFsSync> log)
    {
        _js = js;
        _log = log;
    }

    /// <summary>Reads the persisted tree back into MEMFS. Call once before the DB opens.</summary>
    public async Task HydrateAsync()
    {
        try
        {
            Directory.CreateDirectory(Root);
            var entries = await _js.InvokeAsync<FsEntry[]>("kmilaFs.hydrate");
            if (entries is null || entries.Length == 0)
            {
                _lastSignature = ComputeSignature();
                return;
            }

            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Path)) continue;
                try
                {
                    var dir = Path.GetDirectoryName(e.Path);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllBytes(e.Path, Convert.FromBase64String(e.Data ?? string.Empty));
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "[fs] hydrate skipped {Path}", e.Path);
                }
            }

            _lastSignature = ComputeSignature();
            _log.LogInformation("[fs] hydrated {Count} files from IndexedDB", entries.Length);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[fs] hydrate failed — starting with an empty workspace");
        }
    }

    /// <summary>Writes the current MEMFS tree back to IndexedDB (skips if unchanged unless forced).</summary>
    public async Task FlushAsync(bool force = false)
    {
        try
        {
            if (!Directory.Exists(Root)) return;

            var sig = ComputeSignature();
            if (!force && sig == _lastSignature) return;

            var list = new List<FsEntry>();
            foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    list.Add(new FsEntry { Path = path, Data = Convert.ToBase64String(File.ReadAllBytes(path)) });
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "[fs] flush read skipped {Path}", path);
                }
            }

            await _js.InvokeVoidAsync("kmilaFs.persist", (object)list.ToArray());
            _lastSignature = sig;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[fs] flush failed");
        }
    }

    /// <summary>
    /// Starts change-detected periodic flushing and a best-effort flush on
    /// tab-hide / page-hide. No write hooks are needed: a lightweight signature
    /// over file paths/sizes/timestamps detects DB writes and file saves.
    /// </summary>
    public void StartAutoFlush()
    {
        if (_autoStarted) return;
        _autoStarted = true;
        _selfRef = DotNetObjectReference.Create(this);
        _ = _js.InvokeVoidAsync("kmilaFs.registerFlush", _selfRef);
        _ = AutoLoopAsync();
    }

    private async Task AutoLoopAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(4000);
                await FlushAsync();
            }
            catch
            {
                // Keep the loop alive across transient flush failures.
            }
        }
    }

    /// <summary>Invoked from JS on pagehide / tab-hide.</summary>
    [JSInvokable]
    public Task FlushNowAsync() => FlushAsync();

    private static long ComputeSignature()
    {
        long sig = 17;
        try
        {
            foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            {
                var fi = new FileInfo(path);
                unchecked
                {
                    sig = sig * 31 + path.GetHashCode();
                    sig = sig * 31 + fi.Length;
                    sig = sig * 31 + fi.LastWriteTimeUtc.Ticks;
                }
            }
        }
        catch
        {
            // A racing write during enumeration just means we flush next tick.
        }
        return sig;
    }

    public async ValueTask DisposeAsync()
    {
        try { await FlushAsync(force: true); } catch { /* ignore */ }
        _selfRef?.Dispose();
    }

    /// <summary>Wire shape shared with kmilaFs.hydrate/persist (case-insensitive JSInterop).</summary>
    private sealed class FsEntry
    {
        public string Path { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}
