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
    /// <summary>Step ordinal — purely informational, the player iterates by index.</summary>
    [JsonPropertyName("t")]
    public int T { get; set; }

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
}
