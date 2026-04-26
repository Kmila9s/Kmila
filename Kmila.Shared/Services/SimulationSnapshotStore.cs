using Kmila.Shared.Data;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Captures every simulation run so the student can browse history, compare
/// runs, and pin favorites. Metadata + full waveform payload live in one
/// SQLite row (<see cref="SimulationSnapshot"/>) — simple to query, easy to
/// export, and bounded by per-project retention.
/// </summary>
/// <remarks>
/// Retention: <see cref="MaxPerProject"/> most-recent rows per project.
/// Pinned rows are exempt — the LRU eviction only removes unpinned ones.
/// Fires <see cref="OnChange"/> after every insert / delete so the Runs tab
/// can refresh without polling.
/// </remarks>
public class SimulationSnapshotStore
{
    public const int MaxPerProject = 50;

    private readonly ApplicationDbContext _db;
    private readonly ILogger _logger;

    public SimulationSnapshotStore(ApplicationDbContext db,
                                   ILogger<SimulationSnapshotStore>? logger = null)
    {
        _db = db;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Fires after any insert / update / delete so UIs re-render.</summary>
    public event Action<int>? OnChange;  // projectId that changed

    public async Task<SimulationSnapshot?> CaptureAsync(int projectId,
                                                       WaveformData waveform,
                                                       int time, string scale,
                                                       string? fpgaModel,
                                                       string? label = null)
    {
        try
        {
            await _db.InitDBAsync();

            var snap = new SimulationSnapshot
            {
                ProjectId    = projectId,
                CreatedAt    = DateTime.UtcNow,
                Label        = string.IsNullOrWhiteSpace(label)
                                 ? DateTime.Now.ToString("HH:mm:ss · MMM d")
                                 : label.Trim(),
                Time         = time,
                Scale        = scale,
                FpgaModel    = fpgaModel,
                SignalCount  = waveform.Signals.Count,
                SampleCount  = waveform.Signals.Sum(s => s.Transitions.Count),
                WaveformJson = waveform.ToJson()
            };

            await _db.InsertAsync(snap);
            await EnforceRetentionAsync(projectId);
            OnChange?.Invoke(projectId);
            _logger.LogInformation("[SnapshotStore] Captured run #{Id} for project {P} ({S} signals, {Samples} samples)",
                snap.Id, projectId, snap.SignalCount, snap.SampleCount);
            return snap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Capture failed for project {P}", projectId);
            return null;
        }
    }

    public async Task<List<SimulationSnapshot>> ListForProjectAsync(int projectId)
    {
        try
        {
            await _db.InitDBAsync();
            var rows = await _db.SimulationSnapshots
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] List failed for project {P}", projectId);
            return new();
        }
    }

    public async Task<SimulationSnapshot?> GetByIdAsync(int snapshotId)
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.SimulationSnapshots
                .Where(s => s.Id == snapshotId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] GetById {Id} failed", snapshotId);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(int snapshotId)
    {
        try
        {
            await _db.InitDBAsync();
            var row = await _db.SimulationSnapshots
                .Where(s => s.Id == snapshotId)
                .FirstOrDefaultAsync();
            if (row == null) return false;
            await _db.DeleteAsync(row);
            OnChange?.Invoke(row.ProjectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Delete {Id} failed", snapshotId);
            return false;
        }
    }

    public async Task<bool> RenameAsync(int snapshotId, string newLabel)
    {
        try
        {
            await _db.InitDBAsync();
            var row = await _db.SimulationSnapshots
                .Where(s => s.Id == snapshotId)
                .FirstOrDefaultAsync();
            if (row == null) return false;
            row.Label = string.IsNullOrWhiteSpace(newLabel) ? row.Label : newLabel.Trim();
            await _db.UpdateAsync(row);
            OnChange?.Invoke(row.ProjectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Rename {Id} failed", snapshotId);
            return false;
        }
    }

    public async Task<bool> SetPinnedAsync(int snapshotId, bool pinned)
    {
        try
        {
            await _db.InitDBAsync();
            var row = await _db.SimulationSnapshots
                .Where(s => s.Id == snapshotId)
                .FirstOrDefaultAsync();
            if (row == null) return false;
            row.Pinned = pinned;
            await _db.UpdateAsync(row);
            OnChange?.Invoke(row.ProjectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Pin {Id} failed", snapshotId);
            return false;
        }
    }

    /// <summary>
    /// LRU cap per project: unpinned oldest rows evicted once the count
    /// crosses <see cref="MaxPerProject"/>. Pinned rows are never touched.
    /// </summary>
    private async Task EnforceRetentionAsync(int projectId)
    {
        try
        {
            var all = await _db.SimulationSnapshots
                .Where(s => s.ProjectId == projectId && !s.Pinned)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            if (all.Count <= MaxPerProject) return;

            var toEvict = all.Skip(MaxPerProject).ToList();
            foreach (var s in toEvict)
            {
                await _db.DeleteAsync(s);
            }
            _logger.LogInformation("[SnapshotStore] Evicted {N} old snapshots from project {P}",
                toEvict.Count, projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Retention sweep failed for project {P}", projectId);
        }
    }
}
