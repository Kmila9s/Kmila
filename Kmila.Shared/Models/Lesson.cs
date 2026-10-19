using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Ordered difficulty bucket used to sort the catalog and color the chip on
/// each lesson card. Designed to cover the whole span from "never seen a
/// logic gate" up to graduate-level SoC / pipelined CPU work.
/// </summary>
public enum LessonLevel
{
    /// <summary>Pre-requisite: binary, boolean algebra, what an FPGA is.</summary>
    Beginner,
    /// <summary>VHDL syntax, combinational circuits, first simulations.</summary>
    Easy,
    /// <summary>Sequential logic, FSMs, parameterized designs, simple ALUs.</summary>
    Intermediate,
    /// <summary>Multi-module systems: memories, datapath/control split, small CPUs.</summary>
    Advanced,
    /// <summary>Graduate/SoC-level: pipelining, hazards, AXI-style peripherals, verification.</summary>
    Expert
}

/// <summary>
/// The pedagogical flavor of a lesson. Drives how the UI renders it:
/// reading-only, pre-loaded sample, fix-the-bug exercise, or an open-ended
/// project with acceptance criteria.
/// </summary>
public enum LessonKind
{
    /// <summary>Text + diagrams; no VHDL file is loaded.</summary>
    Reading,
    /// <summary>Text + a "Try this sample" button that opens a working design in the editor.</summary>
    Sample,
    /// <summary>LeetCode-style: we give a broken or partial design; the student fixes it.</summary>
    Exercise,
    /// <summary>From-zero project brief with acceptance criteria (ALU, ROM, CPU…).</summary>
    Project
}

/// <summary>
/// One top-level module in the curriculum (e.g. "Digital Fundamentals",
/// "Sequential Design"). A module groups lessons and shows up as one card on
/// the Learn catalog page.
/// </summary>
public class LessonModule
{
    /// <summary>
    /// Stable URL-safe identifier used in routes (e.g. <c>digital-fundamentals</c>).
    /// Must be unique across the manifest.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// BlazorBootstrap <c>IconName</c> string shown on the module card
    /// (e.g. <c>Lightning</c>, <c>Diagram3</c>). Looked up by name — invalid
    /// values render as the default icon.
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "BookFill";

    /// <summary>
    /// Lowest difficulty level inside the module — used to bucket modules on
    /// the catalog page ("Start here", "Build up", "Go deep"). Individual
    /// lessons carry their own <see cref="Lesson.Level"/> for finer control.
    /// </summary>
    [JsonPropertyName("level")]
    public LessonLevel Level { get; set; } = LessonLevel.Beginner;

    /// <summary>
    /// 1-based display order on the catalog page. Lower numbers render first.
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// ESCOM ISC-2020 asignatura this module belongs to, as a URL-safe id
    /// (e.g. <c>fundamentos-diseno-digital</c>). Used by the Learn catalog's
    /// "By asignatura" grouping mode. Empty when unmapped — such modules fall
    /// into a catch-all track in that mode. Independent of <see cref="Level"/>,
    /// which still drives the "By difficulty" grouping.
    /// </summary>
    [JsonPropertyName("asignatura")]
    public string Asignatura { get; set; } = string.Empty;

    /// <summary>
    /// Period within the asignatura (bimestre), 1-based. Sub-groups modules
    /// under their asignatura track in "By asignatura" mode. 0 = unassigned.
    /// </summary>
    [JsonPropertyName("bimestre")]
    public int Bimestre { get; set; }

    /// <summary>Localized titles keyed by language code (en, es, fr, …).</summary>
    [JsonPropertyName("titles")]
    public Dictionary<string, string> Titles { get; set; } = new();

    /// <summary>Localized one-line summaries keyed by language code.</summary>
    [JsonPropertyName("summaries")]
    public Dictionary<string, string> Summaries { get; set; } = new();

    /// <summary>Lessons in reading order. Empty is valid (module placeholder).</summary>
    [JsonPropertyName("lessons")]
    public List<Lesson> Lessons { get; set; } = new();
}

/// <summary>
/// One lesson inside a <see cref="LessonModule"/>. The Markdown body lives on
/// disk at <c>wwwroot/Learn/{lang}/{moduleId}/{lessonId}.md</c>; everything
/// here is metadata.
/// </summary>
public class Lesson
{
    /// <summary>URL-safe lesson identifier, unique within its module.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Pedagogical kind — see <see cref="LessonKind"/>.</summary>
    [JsonPropertyName("kind")]
    public LessonKind Kind { get; set; } = LessonKind.Reading;

    /// <summary>Difficulty — drives the level chip color in the catalog.</summary>
    [JsonPropertyName("level")]
    public LessonLevel Level { get; set; } = LessonLevel.Beginner;

    /// <summary>Rough reading+doing time for the progress chip.</summary>
    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 10;

    /// <summary>Localized titles keyed by language code.</summary>
    [JsonPropertyName("titles")]
    public Dictionary<string, string> Titles { get; set; } = new();

    /// <summary>Localized one-line summaries keyed by language code.</summary>
    [JsonPropertyName("summaries")]
    public Dictionary<string, string> Summaries { get; set; } = new();

    /// <summary>
    /// VHDL payload for <see cref="LessonKind.Sample"/> / <see cref="LessonKind.Exercise"/>
    /// / <see cref="LessonKind.Project"/>. Null for pure reading lessons.
    /// </summary>
    [JsonPropertyName("sample")]
    public LessonSample? Sample { get; set; }

    /// <summary>
    /// When <c>true</c>, the lesson view tries to load a sidecar
    /// <c>{lessonId}.timeline.json</c> alongside the Markdown body and
    /// renders the shared <c>ExecutionPlayer</c> inline between the body
    /// and the sample CTA. Backport of the Concepts-track player to
    /// regular Learn lessons; opt-in so existing lessons keep their
    /// current shape until an author explicitly authors a timeline.
    /// </summary>
    [JsonPropertyName("inlinePlayer")]
    public bool InlinePlayer { get; set; } = false;

    /// <summary>
    /// When <c>true</c>, the lesson view tries to load a sidecar
    /// <c>{lessonId}.gates.json</c> and renders an inline
    /// <c>GateAnimator</c> — an SVG circuit walked by the same tape
    /// transport as the ExecutionPlayer, with wires pulsing in time
    /// with each step's signal values. Independent of
    /// <see cref="InlinePlayer"/>: a lesson may have either, both, or
    /// neither.
    /// </summary>
    [JsonPropertyName("gateAnimation")]
    public bool GateAnimation { get; set; } = false;

    /// <summary>
    /// When <c>true</c>, the lesson view tries to load a sidecar
    /// <c>{lessonId}.fsm.json</c> and renders an inline
    /// <c>FsmAnimator</c> — an SVG state machine with state circles and
    /// transition arrows, walked by the same tape transport. Independent
    /// of <see cref="InlinePlayer"/> and <see cref="GateAnimation"/>:
    /// a lesson may carry any combination.
    /// </summary>
    [JsonPropertyName("fsmAnimation")]
    public bool FsmAnimation { get; set; } = false;

    /// <summary>
    /// Citation keys for the sources this lesson's content draws on — the
    /// in-app analogue of <c>\cite{key}</c>. Keys resolve against
    /// <c>wwwroot/Data/sources.json</c> via <c>SourceCatalogService</c> and
    /// render as a "Fuentes" block at the bottom of the lesson. Empty = no
    /// block shown.
    /// </summary>
    [JsonPropertyName("sources")]
    public List<string> Sources { get; set; } = new();
}

/// <summary>
/// Self-contained VHDL starter that a lesson's "Try this sample" button ships
/// into a temporary project. The content is kept inline in the manifest so
/// one JSON round-trips the whole catalog — lesson authors don't need to sync
/// Markdown + manifest + a third .vhd file for each lesson.
/// </summary>
public class LessonSample
{
    /// <summary>File name to create in the throw-away project (e.g. <c>main.vhd</c>).</summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "main.vhd";

    /// <summary>Raw VHDL source to seed the file.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional companion simulation configuration. If set, the sample flow
    /// saves this config and marks it as pending so the Editor auto-applies
    /// it after the project loads (same mechanism the 4-bit counter sample
    /// uses).
    /// </summary>
    [JsonPropertyName("config")]
    public LessonSampleConfig? Config { get; set; }
}

/// <summary>Minimal simulation configuration shape carried by a sample.</summary>
public class LessonSampleConfig
{
    /// <summary>Total simulation time in units of <see cref="Scale"/>.</summary>
    [JsonPropertyName("time")]
    public int Time { get; set; } = 100;

    /// <summary>Time-scale unit for <see cref="Time"/> (e.g. "US").</summary>
    [JsonPropertyName("scale")]
    public string Scale { get; set; } = "US";

    /// <summary>
    /// Port-moment list. Each item carries the port name, optional clock
    /// flag, and the value moments. Matches the <c>PortMomentConfiguration</c>
    /// shape used everywhere else.
    /// </summary>
    [JsonPropertyName("ports")]
    public List<LessonPortStimulus> Ports { get; set; } = new();
}

/// <summary>Per-port stimulus entry inside a lesson sample config.</summary>
public class LessonPortStimulus
{
    /// <summary>Name of the design port this stimulus drives.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>True → treat as a clock (50 MHz default). Ignores <see cref="Values"/>.</summary>
    [JsonPropertyName("clock")]
    public bool Clock { get; set; }

    /// <summary>Ordered stimulus moments applied to the port.</summary>
    [JsonPropertyName("values")]
    public List<LessonPortValue> Values { get; set; } = new();
}

/// <summary>One (time, scale, value) stimulus moment.</summary>
public class LessonPortValue
{
    /// <summary>Moment time in units of <see cref="Scale"/>.</summary>
    [JsonPropertyName("time")]
    public int Time { get; set; }

    /// <summary>Time-scale unit for <see cref="Time"/> (e.g. "US").</summary>
    [JsonPropertyName("scale")]
    public string Scale { get; set; } = "US";

    /// <summary>Value applied to the port at this moment.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "0";
}

/// <summary>Root of the manifest.json file.</summary>
public class LessonManifest
{
    /// <summary>Curriculum version — bumped whenever structure changes.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>Curriculum modules declared in the manifest.</summary>
    [JsonPropertyName("modules")]
    public List<LessonModule> Modules { get; set; } = new();
}
