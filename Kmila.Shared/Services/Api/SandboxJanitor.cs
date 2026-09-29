#if KMILA_HAS_ASPNETCORE
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Api;

// Background sweeper for /api/v1 sandbox projects.
//
// Two-part GC:
//   1. In-memory sweep — Retain=false projects older than 30 min, Retain=true
//      projects older than 24 h. The bench harness sets retain=false by
//      default (KmilaApiClient.CreateProjectAsync), so all bench-created
//      sandboxes get reaped 30 min after creation regardless of whether
//      the client ever called DELETE.
//   2. On-disk orphan sweep — directories under ~/.kmila/api-sandbox/ whose
//      names are not in the live `_projects` dict AND whose mtime is older
//      than 30 min. Catches the "process crashed mid-bench-run" case: on-disk
//      state is not rehydrated on startup, so pre-restart dirs are orphaned
//      forever without this pass.
//
// Cadence: 5-minute tick. First run fires 30 s after startup so the initial
// disk sweep runs early enough to matter, but late enough that healthy new
// projects aren't racy with it.
/// <summary>Background service that periodically sweeps expired and orphaned /api/v1 sandbox projects.</summary>
public sealed class SandboxJanitor : BackgroundService
{
    private static readonly TimeSpan Interval           = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan InitialDelay       = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Ttl                = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RetainedTtl        = TimeSpan.FromHours(24);
    private static readonly TimeSpan DiskOrphanTtl      = TimeSpan.FromMinutes(30);

    private readonly SandboxProjectStore _store;
    private readonly ILogger<SandboxJanitor> _log;

    /// <summary>Creates the janitor with its target store and logger.</summary>
    /// <param name="store">The sandbox project store to sweep.</param>
    /// <param name="log">Logger for sweep activity and errors.</param>
    public SandboxJanitor(SandboxProjectStore store, ILogger<SandboxJanitor> log)
    {
        _store = store; _log = log;
    }

    /// <summary>Runs the periodic sweep loop until cancellation.</summary>
    /// <param name="stoppingToken">Token signaled when the host is shutting down.</param>
    /// <returns>A task that completes when the loop stops.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(InitialDelay, stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int inMem = _store.SweepInMemory(Ttl, RetainedTtl);
                int disk  = _store.SweepDiskOrphans(DiskOrphanTtl);
                if (inMem > 0 || disk > 0)
                    _log.LogInformation(
                        "[sandbox-janitor] reaped {InMemory} in-memory + {Disk} orphan dirs",
                        inMem, disk);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "[sandbox-janitor] sweep failed; will retry next tick");
            }
            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { return; }
        }
    }
}
#endif
