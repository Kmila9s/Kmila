using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// One step in a pre-recorded execution timeline. Drives the
/// <c>ExecutionPlayer</c>: at step <c>T</c>, Monaco highlights
/// <see cref="HighlightLines"/>, the signal table shows <see cref="Signals"/>,
/// and the caption pane renders the localized <see cref="Note"/>.
/// </summary>
public class TimelineStep
{
    /// <summary>
    /// Step time / ordinal — informational, the player iterates by index. 64-bit:
    /// simulator-built docs store simulated time here (ns, or tick*1000+delta for the
    /// live tape), which overflows <c>int</c> after ~2.1 s (ns) / ~2.1 ms (live).
    /// </summary>
    [JsonPropertyName("t")]
    public long T { get; set; }

    /// <summary>
    /// Live-tape storage mode (never serialized). When true, <see cref="Signals"/>
    /// holds ONLY the signals that changed at this step; the full table is
    /// reconstructed by <see cref="ExecutionTimelineDoc.ResolveSignals"/> from the
    /// nearest earlier keyframe (a step with <c>IsDelta == false</c>). Hand-authored
    /// timelines and <c>ReplayBuilder</c> output are always full (false).
    /// </summary>
    [JsonIgnore]
    public bool IsDelta { get; set; }

    /// <summary>
    /// 1-based Monaco line numbers to highlight at this step. Empty array
    /// means "no highlight" (useful for end-of-delta-cycle steps that only
    /// reveal new signal values).
    /// </summary>
    [JsonPropertyName("highlightLines")]
    public List<int> HighlightLines { get; set; } = new();

    /// <summary>
    /// Snapshot of named signal values at this step. Keys are signal names
    /// (e.g. <c>a</c>, <c>sum</c>); values are display strings
    /// (e.g. <c>0</c>, <c>U</c>, <c>"0101"</c>). Insertion order in the JSON
    /// drives table row order in the UI.
    /// </summary>
    [JsonPropertyName("signals")]
    public Dictionary<string, string> Signals { get; set; } = new();

    /// <summary>
    /// Localized teaching note for this step (one line, plain text). Keys are
    /// language codes (<c>en</c>, <c>es</c>, …); falls back to <c>en</c> per
    /// <see cref="Services.LessonCatalog.Localize"/>.
    /// </summary>
    [JsonPropertyName("note")]
    public Dictionary<string, string> Note { get; set; } = new();
}

/// <summary>
/// A full pre-recorded execution timeline for one Concepts lesson sample.
/// Authored by hand in Phase 1; in Phase 2 the live simulator may emit these
/// for arbitrary code. Loaded by <c>ConceptCatalog.GetTimelineAsync</c>.
/// </summary>
public class ExecutionTimelineDoc
{
    /// <summary>
    /// Read-only code rendered in the Monaco editor. Multi-line, with explicit
    /// <c>\n</c> separators preserved by the JSON parser. Line numbers
    /// referenced by <see cref="TimelineStep.HighlightLines"/> are 1-based
    /// against this string.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Monaco language id for syntax highlighting. Defaults to <c>vhdl</c>;
    /// override to <c>plaintext</c> for prose-style timelines (e.g. the
    /// pizza-recipe intro lesson).
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "vhdl";

    /// <summary>
    /// Ordered list of named signals to render in the side table. When non-
    /// empty, the player uses this order; otherwise it falls back to the
    /// insertion order of the first step's <see cref="TimelineStep.Signals"/>.
    /// </summary>
    [JsonPropertyName("signalOrder")]
    public List<string> SignalOrder { get; set; } = new();

    /// <summary>
    /// Phase D (2026-05-19): optional per-signal metadata used by the player's
    /// filter / group UI on designs with many signals. Keys are signal names;
    /// values are port-type strings (<c>IN</c>, <c>OUT</c>, <c>INOUT</c>,
    /// <c>BUFFER</c>, <c>INTERNAL</c>). Hand-authored Concepts lessons leave
    /// this empty — the filter UI hides itself when both the dict is empty
    /// AND the signal count is small.
    /// </summary>
    [JsonPropertyName("signalMeta")]
    public Dictionary<string, string> SignalMeta { get; set; } = new();

    /// <summary>The steps. Must contain at least one entry to be playable.</summary>
    [JsonPropertyName("steps")]
    public List<TimelineStep> Steps { get; set; } = new();

    /// <summary>
    /// True for the live tape that <c>ReplayState</c> appends to on the simulator
    /// thread while the UI reads it. Writers mutate <see cref="Steps"/> /
    /// <see cref="SignalOrder"/> / <see cref="SignalMeta"/> only under
    /// <see cref="SyncRoot"/>; readers must go through <see cref="ReadView"/>.
    /// </summary>
    [JsonIgnore]
    public bool IsLive { get; set; }

    /// <summary>Lock guarding a live doc's collections (see <see cref="IsLive"/>).</summary>
    [JsonIgnore]
    public object SyncRoot { get; } = new();

    /// <summary>Step count, read under the lock for live docs.</summary>
    [JsonIgnore]
    public int StepCount
    {
        get
        {
            if (!IsLive) return Steps.Count;
            lock (SyncRoot) return Steps.Count;
        }
    }

    /// <summary>
    /// A doc that is safe to read on any thread. Non-live docs return themselves
    /// (zero cost). A live doc returns a point-in-time copy of its collections taken
    /// under <see cref="SyncRoot"/>; the steps themselves are shared, which is safe
    /// because the writer never mutates a step after appending it (eviction replaces
    /// step objects instead of editing them).
    /// </summary>
    public ExecutionTimelineDoc ReadView()
    {
        if (!IsLive) return this;
        lock (SyncRoot)
        {
            return new ExecutionTimelineDoc
            {
                Code = Code,
                Language = Language,
                SignalOrder = new List<string>(SignalOrder),
                SignalMeta = new Dictionary<string, string>(SignalMeta, StringComparer.OrdinalIgnoreCase),
                Steps = new List<TimelineStep>(Steps),
            };
        }
    }

    /// <summary>
    /// Full signal table at step <paramref name="index"/>. For a full step this is
    /// its own <see cref="TimelineStep.Signals"/> (no copy). For a delta step it walks
    /// back to the nearest keyframe and applies the deltas forward. Call on a
    /// <see cref="ReadView"/> (or a non-live doc).
    /// </summary>
    public Dictionary<string, string> ResolveSignals(int index)
    {
        if (Steps.Count == 0) return new Dictionary<string, string>();
        index = Math.Clamp(index, 0, Steps.Count - 1);
        var step = Steps[index];
        if (!step.IsDelta) return step.Signals;

        int k = index;
        while (k > 0 && Steps[k].IsDelta) k--;
        var full = new Dictionary<string, string>(Steps[k].Signals, StringComparer.OrdinalIgnoreCase);
        for (int i = k + 1; i <= index; i++)
            foreach (var kv in Steps[i].Signals)
                full[kv.Key] = kv.Value;
        return full;
    }
}
