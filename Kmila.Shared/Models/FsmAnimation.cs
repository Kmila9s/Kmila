using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// One state node in an FSM animation. Renders as a labelled circle (or
/// rounded rectangle when the label is long). States are positioned by hand
/// in viewBox coordinates — the renderer doesn't auto-layout.
/// </summary>
public class FsmState
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Centre x in viewBox units.</summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>Centre y in viewBox units.</summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Optional Moore-machine outputs that hold while the FSM sits in this
    /// state. Rendered as a small block under the state label for emphasis.
    /// </summary>
    [JsonPropertyName("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = new();
}

/// <summary>
/// One transition edge between two states. The renderer auto-routes:
/// <list type="bullet">
///   <item>Same-state self-loops become an arc above the state.</item>
///   <item>Adjacent states get a straight line.</item>
///   <item>Forward + reverse pairs are curved apart so the labels don't
///   collide.</item>
/// </list>
/// </summary>
public class FsmTransition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Source state id.</summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>Destination state id.</summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Trigger expression rendered next to the arrow (e.g. <c>"in=1"</c>,
    /// <c>"in=0 / out=1"</c>). Mealy machines may include both the condition
    /// and the output after a slash.
    /// </summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;
}

/// <summary>
/// One frame in an FSM walk: the current state, the inputs and outputs at
/// this moment, optionally the id of the transition that was just taken,
/// and a teaching note.
/// </summary>
public class FsmStep
{
    [JsonPropertyName("t")]
    public int T { get; set; }

    /// <summary>State id the FSM is sitting in at this step.</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>Input signal values at this step.</summary>
    [JsonPropertyName("inputs")]
    public Dictionary<string, string> Inputs { get; set; } = new();

    /// <summary>Output signal values at this step.</summary>
    [JsonPropertyName("outputs")]
    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>
    /// Optional id of the transition the FSM took to reach this state
    /// (compared to the previous step). Drives the "this arrow just fired"
    /// pulse animation.
    /// </summary>
    [JsonPropertyName("transition")]
    public string Transition { get; set; } = string.Empty;

    /// <summary>Localized teaching note for this step.</summary>
    [JsonPropertyName("note")]
    public Dictionary<string, string> Note { get; set; } = new();
}

/// <summary>
/// Sidecar document loaded from <c>Learn/{lang}/{moduleId}/{lessonId}.fsm.json</c>.
/// Drives the <c>FsmAnimator</c> component. Sibling to
/// <see cref="GateAnimationDoc"/> — same tape transport, same per-step note
/// model; different visual primitives (state circles + transition arrows).
/// </summary>
public class FsmAnimationDoc
{
    [JsonPropertyName("viewBox")]
    public string ViewBox { get; set; } = "0 0 720 360";

    [JsonPropertyName("states")]
    public List<FsmState> States { get; set; } = new();

    [JsonPropertyName("transitions")]
    public List<FsmTransition> Transitions { get; set; } = new();

    /// <summary>
    /// Ordered list of input + output signal ids for the side table. Falls
    /// back to first step's inputs followed by outputs when not set.
    /// </summary>
    [JsonPropertyName("signalOrder")]
    public List<string> SignalOrder { get; set; } = new();

    [JsonPropertyName("steps")]
    public List<FsmStep> Steps { get; set; } = new();
}
