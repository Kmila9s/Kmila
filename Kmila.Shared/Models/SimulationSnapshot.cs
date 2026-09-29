using SQLite;

namespace Kmila.Shared.Models;

/// <summary>
/// One captured simulation run — what was simulated, when, and the full
/// waveform payload. Stored in SQLite alongside Projects / ProjectFiles so
/// the Runs tab can list them instantly and the Compare view can replay
/// multiple into the waveform viewer.
/// </summary>
/// <remarks>
/// Storage shape decision: metadata + inline JSON payload in a single table.
/// A typical run serializes to ~5–50 KB depending on transition count, and
/// we hard-cap retention at <c>MaxPerProject</c> — well within SQLite's
/// comfort zone and saves the on-disk-file coordination nobody wants.
/// Export still works because <c>WaveformJson</c> is portable text.
/// </remarks>
[Table("SimulationSnapshots")]
public class SimulationSnapshot
{
    /// <summary>Auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Id of the project this run belongs to.</summary>
    [Indexed]
    public int ProjectId { get; set; }

    /// <summary>UTC timestamp when the run was captured.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User-supplied label, defaults to the ISO timestamp.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Simulated duration in <see cref="Scale"/> units.</summary>
    public int Time { get; set; }
    /// <summary>Time scale/unit of <see cref="Time"/> (e.g. "US").</summary>
    public string Scale { get; set; } = "US";
    /// <summary>FPGA model the run targeted, if any.</summary>
    public string? FpgaModel { get; set; }
    /// <summary>Number of signals captured in the run.</summary>
    public int SignalCount { get; set; }
    /// <summary>Number of samples captured in the run.</summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Full <see cref="WaveformData"/> serialized as JSON — not the raw
    /// SignalHistory (which references live Variable objects from the
    /// Interpreter and wouldn't round-trip).
    /// </summary>
    public string WaveformJson { get; set; } = string.Empty;

    /// <summary>Optional "pinned" flag — pinned runs are exempt from LRU eviction.</summary>
    public bool Pinned { get; set; }
}
