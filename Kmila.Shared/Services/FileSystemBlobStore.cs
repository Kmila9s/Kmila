using System.Runtime.CompilerServices;
using LibraryCompiler.Services;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// File-system backed <see cref="IBlobStore"/>. The <c>LibraryCompiler</c>
/// module owns the interface and ships only the in-memory and null
/// reference implementations; this is the host-side concrete that the
/// Web and MAUI builds inject when they want compiled libraries to
/// survive across sessions.
/// </summary>
/// <remarks>
/// Storage layout under <c>rootPath</c> mirrors the keys the orchestrator
/// produces (e.g. <c>libcache/numeric_std.json</c>): the key is treated
/// as a relative file path. Slashes inside keys map to directory
/// separators, so a key like <c>libcache/foo.json</c> writes
/// <c>{rootPath}/libcache/foo.json</c>. The store creates intermediate
/// directories on first write — the host doesn't need to pre-flight the
/// folder layout.
///
/// This class is platform-agnostic: hand it
/// <see cref="Microsoft.Maui.Storage.FileSystem.AppDataDirectory"/> on
/// MAUI or an <c>ApplicationData/KmilaData</c>-derived path on the Web
/// build and it'll behave the same.
/// </remarks>
public sealed class FileSystemBlobStore : IBlobStore
{
    private readonly string _rootPath;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructs the store rooted at <paramref name="rootPath"/>. The
    /// directory is created on first use; constructor failures here
    /// would be invisible since DI registration runs before any logger
    /// is hooked up, so we defer the IO until the first write.
    /// </summary>
    public FileSystemBlobStore(string rootPath, ILogger<FileSystemBlobStore>? logger = null)
    {
        _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
        _logger   = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { return Task.FromResult(File.Exists(ResolvePath(key))); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FileSystemBlobStore] Exists check failed for key {Key}", key);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> ReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var path = ResolvePath(key);
            if (!File.Exists(path)) return null;
            return await File.ReadAllBytesAsync(path, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FileSystemBlobStore] Read failed for key {Key}", key);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(string key, byte[] payload, CancellationToken ct = default)
    {
        try
        {
            var path = ResolvePath(key);
            var dir  = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(path, payload, ct);
        }
        catch (Exception ex)
        {
            // Persistence is best-effort: the orchestrator already wraps
            // this call in a swallowed try/catch, but we log here so
            // failures surface in /logs rather than disappearing.
            _logger.LogWarning(ex, "[FileSystemBlobStore] Write failed for key {Key}", key);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var path = ResolvePath(key);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FileSystemBlobStore] Delete failed for key {Key}", key);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Wipes every blob under the store root. Used by Settings →
    /// Library compilation → "Clear cache" so the user can force a cold
    /// rebuild after corrupting the cache or just to free disk space.
    /// Returns the number of files removed.
    /// </summary>
    public int ClearAll()
    {
        if (!Directory.Exists(_rootPath)) return 0;
        int removed = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(f); removed++; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[FileSystemBlobStore] ClearAll: failed to delete {File}", f);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FileSystemBlobStore] ClearAll enumeration failed under {Root}", _rootPath);
        }
        return removed;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> EnumerateKeysAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Directory.Exists(_rootPath)) yield break;

        string[] files;
        try
        {
            files = Directory.GetFiles(_rootPath, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FileSystemBlobStore] Enumerate failed under {Root}", _rootPath);
            yield break;
        }

        foreach (var f in files)
        {
            ct.ThrowIfCancellationRequested();
            // Re-derive the key the orchestrator wrote (forward slashes,
            // root-relative). Keeps Windows / Unix output identical.
            var rel = Path.GetRelativePath(_rootPath, f).Replace(Path.DirectorySeparatorChar, '/');
            yield return rel;
            await Task.Yield();
        }
    }

    /// <summary>
    /// Maps an orchestrator-supplied key (always forward-slashed, e.g.
    /// <c>libcache/numeric_std.json</c>) onto a path under
    /// <see cref="_rootPath"/>. Rejects absolute keys and any path that
    /// would escape the root via <c>..</c> traversal.
    /// </summary>
    private string ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Blob key must be non-empty", nameof(key));
        if (Path.IsPathRooted(key))
            throw new ArgumentException("Blob key must be relative", nameof(key));

        var combined = Path.GetFullPath(Path.Combine(_rootPath, key));
        var rootFull = Path.GetFullPath(_rootPath);
        // Path.GetFullPath normalises trailing separators, so add one
        // before comparison to make sure /tmp/foo doesn't match /tmp/foobar.
        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull : rootFull + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSep, StringComparison.Ordinal) && combined != rootFull)
            throw new ArgumentException($"Blob key escapes the store root: {key}", nameof(key));
        return combined;
    }
}
