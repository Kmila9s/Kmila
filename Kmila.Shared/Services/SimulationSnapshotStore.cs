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
    /// <summary>Maximum number of unpinned snapshots retained per project.</summary>
    public const int MaxPerProject = 50;

    private readonly ApplicationDbContext _db;
    private readonly ILogger _logger;

    /// <summary>Creates the store over the given database context.</summary>
    /// <param name="db">Application database context.</param>
    /// <param name="logger">Optional logger.</param>
    public SimulationSnapshotStore(ApplicationDbContext db,
                                   ILogger<SimulationSnapshotStore>? logger = null)
    {
        _db = db;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Fires after any insert / update / delete so UIs re-render.</summary>
    public event Action<int>? OnChange;  // projectId that changed

    /// <summary>Captures a simulation run as a new snapshot and enforces retention.</summary>
    /// <param name="projectId">Project the run belongs to.</param>
    /// <param name="waveform">Waveform data to persist.</param>
    /// <param name="time">Simulated time value.</param>
    /// <param name="scale">Time scale unit.</param>
    /// <param name="fpgaModel">FPGA model used for the run, if any.</param>
    /// <param name="label">Optional user label; a timestamp is used when omitted.</param>
    /// <returns>The captured snapshot, or null on failure.</returns>
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
                                 ? DateTime.Now.ToString("HH:mm:ss · MMM d", System.Globalization.CultureInfo.InvariantCulture)
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

    /// <summary>Returns all snapshots for a project.</summary>
    /// <param name="projectId">Project to list snapshots for.</param>
    /// <returns>The project's snapshots.</returns>
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

    /// <summary>
    /// Metadata-only list — returns the same <see cref="SimulationSnapshot"/>
    /// rows as <see cref="ListForProjectAsync"/>, but the heavy
    /// <see cref="SimulationSnapshot.WaveformJson"/> column is left as
    /// <c>string.Empty</c>. The Runs tab and the Editor's runs-badge call
    /// this so opening a project doesn't pull all 50 captured waveforms
    /// (potentially gigabytes of JSON) into the Blazor heap just to render
    /// labels + counts.
    /// </summary>
    /// <remarks>
    /// Callers that actually need the waveform payload (loading a single
    /// run into the viewer, comparing multiple runs, exporting) must
    /// re-fetch the row via <see cref="GetByIdAsync"/>, which still returns
    /// the full record.
    /// </remarks>
    public async Task<List<SimulationSnapshot>> ListMetadataAsync(int projectId)
    {
        try
        {
            await _db.InitDBAsync();
            // Raw SQL projection — sqlite-net's AsyncTableQuery doesn't
            // support arbitrary .Select, and we explicitly do NOT want the
            // WaveformJson column to make it into the result set.
            var rows = await _db.QueryAsync<SimulationSnapshot>(
                "SELECT Id, ProjectId, CreatedAt, Label, Time, Scale, FpgaModel, " +
                "SignalCount, SampleCount, Pinned FROM SimulationSnapshots " +
                "WHERE ProjectId = ? ORDER BY CreatedAt DESC",
                projectId);
            return rows ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] List metadata failed for project {P}", projectId);
            return new();
        }
    }

    /// <summary>
    /// Cheap row count for the Runs-tab badge — issues a single
    /// <c>COUNT(*)</c> instead of materialising every row (which is what
    /// <see cref="ListForProjectAsync"/> used to do when the Editor just
    /// wanted <c>.Count</c>). Defaults to 0 on DB error so badge rendering
    /// never throws.
    /// </summary>
    public async Task<int> CountForProjectAsync(int projectId)
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.SimulationSnapshots
                .Where(s => s.ProjectId == projectId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Count failed for project {P}", projectId);
            return 0;
        }
    }

    /// <summary>Returns a single snapshot by id, or null if not found.</summary>
    /// <param name="snapshotId">Snapshot id.</param>
    /// <returns>The snapshot, or null.</returns>
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

    /// <summary>Deletes a snapshot by id.</summary>
    /// <param name="snapshotId">Snapshot id.</param>
    /// <returns>True if a snapshot was deleted.</returns>
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

    /// <summary>Renames a snapshot's label.</summary>
    /// <param name="snapshotId">Snapshot id.</param>
    /// <param name="newLabel">New label; ignored when blank.</param>
    /// <returns>True if the snapshot was found and updated.</returns>
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

    /// <summary>Pins or unpins a snapshot, exempting it from retention eviction.</summary>
    /// <param name="snapshotId">Snapshot id.</param>
    /// <param name="pinned">Whether the snapshot should be pinned.</param>
    /// <returns>True if the snapshot was found and updated.</returns>
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
    /// Imports a snapshot from a JSON payload previously produced by the Runs
    /// panel's Export action. Re-targets the row to <paramref name="projectId"/>
    /// regardless of the project the JSON came from — students often share
    /// runs across machines/projects, and binding the import to the current
    /// project is the only sensible option (otherwise it would land in a
    /// possibly non-existent project id and never appear in the UI).
    /// Returns the inserted snapshot or null on parse / DB failure.
    /// </summary>
    public async Task<SimulationSnapshot?> ImportFromJsonAsync(int projectId, string json)
    {
        try
        {
            await _db.InitDBAsync();
            var parsed = System.Text.Json.JsonSerializer.Deserialize<SimulationSnapshot>(json);
            if (parsed == null || string.IsNullOrEmpty(parsed.WaveformJson))
            {
                _logger.LogWarning("[SnapshotStore] Import rejected — payload missing required fields");
                return null;
            }
            // Reset identity / project binding; preserve label, time, scale,
            // FPGA hint and waveform data. Mark as just imported so the user
            // sees it at the top of the list.
            parsed.Id = 0;
            parsed.ProjectId = projectId;
            parsed.CreatedAt = DateTime.UtcNow;
            parsed.Pinned    = false;
            if (string.IsNullOrWhiteSpace(parsed.Label))
                parsed.Label = "Imported · " + DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            await _db.InsertAsync(parsed);
            await EnforceRetentionAsync(projectId);
            OnChange?.Invoke(projectId);
            _logger.LogInformation("[SnapshotStore] Imported run #{Id} into project {P}", parsed.Id, projectId);
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Import failed for project {P}", projectId);
            return null;
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
            // One SQL statement, no payloads: the old version materialised EVERY
            // unpinned row — including its full WaveformJson — just to count and
            // Skip(MaxPerProject), after every capture/import. Same semantics:
            // keep the newest MaxPerProject unpinned rows, never touch pinned ones.
            // (LIMIT -1 OFFSET n = "all rows after the first n" in SQLite.)
            int evicted = await _db.ExecuteAsync(
                "DELETE FROM SimulationSnapshots WHERE Id IN (" +
                "SELECT Id FROM SimulationSnapshots " +
                "WHERE ProjectId = ? AND Pinned = 0 " +
                "ORDER BY CreatedAt DESC LIMIT -1 OFFSET ?)",
                projectId, MaxPerProject);
            if (evicted > 0)
                _logger.LogInformation("[SnapshotStore] Evicted {N} old snapshots from project {P}",
                    evicted, projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SnapshotStore] Retention sweep failed for project {P}", projectId);
        }
    }
}
