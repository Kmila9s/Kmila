using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Trace;

/// <summary>
/// Long-run Phase 2: where per-run trace files live, plus the guards that keep them
/// from filling the disk. One instance per host (DI singleton):
/// <list type="bullet">
///   <item>MAUI (Android / iOS / Mac Catalyst): <c>FileSystem.CacheDirectory/traces</c>.</item>
///   <item>Blazor Server: <c>&lt;temp&gt;/kmila-traces</c> — a per-run temp file, never the
///   shared app database (that file is shared by every user).</item>
///   <item>WASM: not registered → no trace store; the Phase-1 rolling windows apply
///   (no real disk, single-threaded runtime).</item>
/// </list>
/// "Infinite" runs become disk-bounded, not RAM-bounded: a session stops spilling
/// (and is then not used as an archive) once its file exceeds
/// <see cref="MaxTraceBytes"/> or free space drops below <see cref="MinFreeBytes"/>.
/// </summary>
public sealed class TraceStorage
{
    /// <summary>File extension of trace databases (SQLite).</summary>
    public const string Extension = ".ktrace";

    private readonly ILogger _log;

    /// <summary>Directory holding trace files.</summary>
    public string Directory { get; }

    /// <summary>Per-run trace file size limit. Default 2 GiB.</summary>
    public long MaxTraceBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    /// <summary>Stop spilling when the volume's free space falls below this. Default 512 MiB.</summary>
    public long MinFreeBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>
    /// Trace files untouched for this long are considered orphaned (a crashed or
    /// restarted host) and deleted on startup.
    /// </summary>
    public TimeSpan StaleAfter { get; init; } = TimeSpan.FromHours(6);

    /// <summary>Creates the store rooted at <paramref name="directory"/> and sweeps stale traces.</summary>
    public TraceStorage(string directory, ILogger<TraceStorage>? logger = null)
    {
        _log = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        Directory = directory;
        try
        {
            System.IO.Directory.CreateDirectory(directory);
            SweepStale();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[TraceStorage] could not prepare {Dir}", directory);
        }
    }

    /// <summary>Starts a trace session for one run.</summary>
    /// <param name="tickResNs">Nanoseconds per simulator tick (waveform timescale).</param>
    public TraceSession StartSession(int tickResNs)
    {
        var path = Path.Combine(Directory, $"run-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{Extension}");
        return new TraceSession(path, tickResNs, this, _log);
    }

    /// <summary>
    /// Fraction of <see cref="MaxTraceBytes"/> at which the run watchdog stops the run
    /// (soft limit). Stopping BEFORE the hard guard leaves room to spill the in-memory
    /// tail, so the archive stays complete instead of being disabled. Default 0.9.
    /// </summary>
    public double SoftLimitFraction { get; init; } = 0.9;

    /// <summary>
    /// Soft limit for the run watchdog: true when a trace of <paramref name="bytesWritten"/>
    /// bytes has reached <see cref="SoftLimitFraction"/> of <see cref="MaxTraceBytes"/>, or
    /// the volume is down to twice <see cref="MinFreeBytes"/>.
    /// </summary>
    public bool IsNearLimit(long bytesWritten)
        => bytesWritten >= (long)(MaxTraceBytes * SoftLimitFraction)
           || !HasFreeSpace(MinFreeBytes * 2);

    /// <summary>True while the volume still has at least <paramref name="minFreeBytes"/> free.</summary>
    internal bool HasFreeSpace(long minFreeBytes)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(Directory));
            if (string.IsNullOrEmpty(root)) return true;
            var drive = new DriveInfo(root);
            return drive.AvailableFreeSpace >= minFreeBytes;
        }
        catch
        {
            return true; // platform without DriveInfo support (mobile sandboxes) — size cap still applies
        }
    }

    /// <summary>Deletes trace files (and their SQLite sidecars) older than <see cref="StaleAfter"/>.</summary>
    internal void SweepStale()
    {
        var cutoff = DateTime.UtcNow - StaleAfter;
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*" + Extension + "*"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
            }
            catch { /* in use or already gone */ }
        }
    }
}
