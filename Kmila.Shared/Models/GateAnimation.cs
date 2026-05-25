using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// One node on a gate-animation diagram. A node is either a labelled
/// terminal (<c>kind = "input" | "output"</c>) or a primitive gate
/// (<c>kind = "and" | "or" | "xor" | "not" | "nand" | "nor" | "xnor" |
/// "buffer"</c>). The renderer switches on <see cref="Kind"/> to pick the
/// SVG shape; unknown kinds fall back to a small labelled box so an
/// experimental schema never blows up the lesson.
/// </summary>
public class GateNode
{
    /// <summary>Stable id used by <see cref="GateWire"/> + <see cref="GateStep.Values"/>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Lower-case primitive name; see class docs for the supported set.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "input";

    /// <summary>Caption drawn next to the node (e.g. <c>a</c>, <c>sum</c>, <c>AND</c>).</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Top-left x in the diagram's viewBox coordinate space.</summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>Top-left y in the diagram's viewBox coordinate space.</summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Number of inputs for n-input gate variants (AND/OR/XOR/NAND/NOR/XNOR).
    /// Default 2; values 3–6 expand the gate body vertically and distribute
    /// pins evenly. Ignored for non-gate kinds.
    /// </summary>
    [JsonPropertyName("inputCount")]
    public int InputCount { get; set; } = 2;

    /// <summary>
    /// Number of MUX data inputs (e.g. 2, 4, 8). Combined with
    /// <c>selectWidth</c> via <see cref="SelectWidth"/>. Ignored for non-MUX.
    /// </summary>
    [JsonPropertyName("dataInputs")]
    public int DataInputs { get; set; } = 2;

    /// <summary>MUX select bus width (1 = single sel bit, 2 = sel[1:0], …).</summary>
    [JsonPropertyName("selectWidth")]
    public int SelectWidth { get; set; } = 1;

    /// <summary>
    /// Block / adder / register primitives may carry a custom width and
    /// height (in viewBox units). Falls back to the per-kind default when
    /// zero or unset.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; set; }

    /// <summary>Body height in viewBox units; see <see cref="Width"/>.</summary>
    [JsonPropertyName("height")]
    public double Height { get; set; }

    /// <summary>
    /// Optional pin layout for the <c>block</c>/<c>adder</c>/<c>register</c>
    /// primitives. Each entry declares one pin's name and its edge position.
    /// Side is <c>"left"</c>/<c>"right"</c>/<c>"top"</c>/<c>"bottom"</c>;
    /// offset is a 0..1 fraction along that edge (top-to-bottom for left/right,
    /// left-to-right for top/bottom). Use the pin's name as the port suffix
    /// in <see cref="GateWire.From"/>/<see cref="GateWire.To"/>.
    /// </summary>
    [JsonPropertyName("pins")]
    public List<GatePin> Pins { get; set; } = new();
}

/// <summary>
/// One pin on a block/adder/register primitive. See <see cref="GateNode.Pins"/>.
/// </summary>
public class GatePin
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"left", "right", "top", or "bottom".</summary>
    [JsonPropertyName("side")]
    public string Side { get; set; } = "left";

    /// <summary>0..1 along the chosen edge.</summary>
    [JsonPropertyName("offset")]
    public double Offset { get; set; } = 0.5;
}

/// <summary>
/// One wire connecting two nodes on a gate-animation diagram. The author
/// picks ONE of two routing styles:
/// <list type="bullet">
///   <item><b>Auto-router</b>: set <see cref="From"/> + <see cref="To"/>
///   to <c>"nodeId"</c> or <c>"nodeId.port"</c> and leave <see cref="Path"/>
///   empty. The renderer computes an L-shape that stops 20px before the
///   destination and rises/falls into it — covers the typical
///   left-to-right gate-fanout layout in two lines of JSON.</item>
///   <item><b>Manual path</b>: provide <see cref="Path"/> as a raw SVG
///   <c>d</c> attribute; <see cref="From"/>/<see cref="To"/> are ignored.
///   Use when the auto-router would backtrack or when you need a custom
///   trunk-plus-branch routing.</item>
/// </list>
/// The wire's stroke colour pulses in time with the value at <see cref="Source"/>
/// so the student sees the logic "1" travelling through the circuit.
/// </summary>
public class GateWire
{
    /// <summary>Optional id, useful for per-wire test hooks.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Node id whose value drives this wire's colour. Usually the upstream
    /// gate or input — when the step's value for this id is <c>1</c>, the
    /// wire renders as "hot"; otherwise "cold".
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// SVG path data. When set, this wins over <see cref="From"/>/<see cref="To"/>.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Auto-router source endpoint, in the form <c>"nodeId"</c> or
    /// <c>"nodeId.port"</c>. Common ports: <c>out</c> (default for gates),
    /// <c>in1</c>, <c>in2</c>, <c>in</c>, <c>d</c>, <c>clk</c>, <c>reset</c>,
    /// <c>en</c>, <c>q</c>, <c>qbar</c>. Input/output terminal nodes ignore
    /// the port suffix (their position is the dot itself).
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>Auto-router destination endpoint; see <see cref="From"/>.</summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Optional fan-out gate. When non-empty, this wire renders "hot" only when
    /// <see cref="Source"/> carries logic 1 AND at least one of these node ids
    /// appears in the current step's <see cref="GateStep.Active"/> list.
    /// Lets an author show a signal propagating down ONE branch of a fan-out
    /// instead of pulsing every branch in lockstep. When empty (the default)
    /// the wire's hotness is source-driven, matching pre-2026-05-22 behaviour.
    /// </summary>
    [JsonPropertyName("activeFor")]
    public List<string> ActiveFor { get; set; } = new();
}

/// <summary>
/// One frame in a gate animation: which inputs/outputs hold which values,
/// which gates are "active" (just toggled their output relative to the
/// previous step), and the teaching note to show in the caption pane.
/// </summary>
public class GateStep
{
    /// <summary>Step ordinal — informational; the runner iterates by index.</summary>
    [JsonPropertyName("t")]
    public int T { get; set; }

    /// <summary>
    /// Map of node id → display value (typically <c>0</c> / <c>1</c>, but can
    /// be any short string such as <c>U</c> or <c>"0101"</c>). Drives the wire
    /// colour, the per-node value chip, and the side signal table.
    /// </summary>
    [JsonPropertyName("values")]
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>
    /// Node ids that should pulse this step — typically gates whose output
    /// changed since the previous step. Used to draw the student's eye to
    /// "what just flipped".
    /// </summary>
    [JsonPropertyName("active")]
    public List<string> Active { get; set; } = new();

    /// <summary>Localized teaching note (one line). Falls back to <c>en</c>.</summary>
    [JsonPropertyName("note")]
    public Dictionary<string, string> Note { get; set; } = new();
}

/// <summary>
/// Sidecar document loaded from <c>Learn/{lang}/{moduleId}/{lessonId}.gates.json</c>.
/// Drives the <c>GateAnimator</c> component. Mirrors
/// <see cref="ExecutionTimelineDoc"/>'s lifecycle: hand-authored in v1, with the
/// option to emit from the simulator later (Editor Replay's frame-log path
/// would feed both surfaces).
/// </summary>
public class GateAnimationDoc
{
    /// <summary>
    /// SVG viewBox for the diagram (<c>"minX minY width height"</c>). The
    /// renderer applies this verbatim so authors get pixel-accurate control
    /// over the canvas aspect ratio.
    /// </summary>
    [JsonPropertyName("viewBox")]
    public string ViewBox { get; set; } = "0 0 720 480";

    /// <summary>Nodes on the diagram (inputs, gates, outputs).</summary>
    [JsonPropertyName("nodes")]
    public List<GateNode> Nodes { get; set; } = new();

    /// <summary>Wires connecting the nodes.</summary>
    [JsonPropertyName("wires")]
    public List<GateWire> Wires { get; set; } = new();

    /// <summary>
    /// Optional ordered list of node ids for the side signal table. Falls
    /// back to "every input/output node, in declaration order".
    /// </summary>
    [JsonPropertyName("signalOrder")]
    public List<string> SignalOrder { get; set; } = new();

    /// <summary>
    /// Optional list of internal-net value chips to render at fixed positions
    /// in the diagram. Each chip pulls its value from
    /// <c>step.Values[chip.Net]</c> on every frame — useful for showing
    /// intermediate signals (carry, partial sum, mux output) that aren't
    /// terminated by a labelled output node.
    /// </summary>
    [JsonPropertyName("chips")]
    public List<GateChip> Chips { get; set; } = new();

    /// <summary>
    /// Optional list of explicit junction dots (where two wires sharing a
    /// source visually branch). Renderer also auto-detects junctions for
    /// any source-id that originates two or more <see cref="GateWire.From"/>
    /// entries — manual entries here cover trunk-and-stub manual-path layouts
    /// that the heuristic can't see.
    /// </summary>
    [JsonPropertyName("junctions")]
    public List<GateJunction> Junctions { get; set; } = new();

    /// <summary>
    /// When set, the renderer draws a clock-strip timing diagram above the
    /// main canvas, showing the named signal's value across every step with
    /// a vertical cursor at the current step. The signal id must appear in
    /// every <see cref="GateStep.Values"/> map.
    /// </summary>
    [JsonPropertyName("clockSignal")]
    public string ClockSignal { get; set; } = string.Empty;

    /// <summary>The steps. Must contain at least one entry to be playable.</summary>
    [JsonPropertyName("steps")]
    public List<GateStep> Steps { get; set; } = new();

    /// <summary>
    /// Opt-in channel routing for the auto-router. When true, wires that
    /// share a destination column are spread across distinct vertical lanes
    /// so their drop legs no longer paint on top of each other — essential
    /// for high-fan-in clusters like the decoder where 8 wires land at the
    /// same AND-gate column. Has no effect on wires authored with a manual
    /// <c>path</c>. Default false so existing demos keep their geometry.
    /// </summary>
    [JsonPropertyName("staggerLanes")]
    public bool StaggerLanes { get; set; } = false;

    /// <summary>
    /// Opt-in "spotlight" rendering. When true, gates whose output is cold
    /// AND which have no hot wire feeding them are visually dimmed each step
    /// so the live propagation path stands out. Input/output terminals stay
    /// at full opacity to preserve layout anchors. Cuts visual noise on
    /// dense diagrams where many overlapping cold wires + idle gates make
    /// the firing path hard to read. Default false.
    /// </summary>
    [JsonPropertyName("spotlight")]
    public bool Spotlight { get; set; } = false;
}

/// <summary>
/// Floating value chip rendered at an absolute (x, y) in the diagram. Used
/// to make internal nets (carry, partial-sum, mux output) visible — they
/// don't have a labelled output terminal but the student needs to see them.
/// </summary>
public class GateChip
{
    /// <summary>Source id; chip pulls its value from <c>step.Values[net]</c>.</summary>
    [JsonPropertyName("net")]
    public string Net { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>Optional short caption rendered above the chip (e.g. "s1").</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Manually-placed junction dot. Renders as a small filled circle in the
/// wire's source colour. Most fan-outs are detected automatically — only
/// add entries here for trunk-and-stub manual paths the heuristic misses.
/// </summary>
public class GateJunction
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
