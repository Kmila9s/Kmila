using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Pedagogical flavor of a Concepts exercise.
/// <list type="bullet">
///   <item><see cref="Recipe"/> — vertical stack of named steps (E1, E2).</item>
///   <item><see cref="Logic"/> — value-block tree feeding output assignments (E3).</item>
///   <item><see cref="Clocked"/> — wrapper "on rising_edge" with a body (E4).</item>
/// </list>
/// </summary>
public enum ConceptExerciseKind
{
    /// <summary>Vertical stack of named steps (E1, E2).</summary>
    Recipe,
    /// <summary>Value-block tree feeding output assignments (E3).</summary>
    Logic,
    /// <summary>Wrapper "on rising_edge" with a body (E4).</summary>
    Clocked
}

/// <summary>
/// One Concepts exercise declared in the manifest. The companion JSON
/// payload (toolbox + pre-baked compositions) lives at
/// <c>wwwroot/Concepts/exercises/{id}.exercise.json</c> and is loaded on
/// demand by <see cref="Services.ConceptCatalog.GetExerciseAsync"/>.
/// </summary>
public class ConceptExercise
{
    /// <summary>Unique exercise identifier, matching its payload file name.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Pedagogical flavor of the exercise.</summary>
    [JsonPropertyName("kind")]
    public ConceptExerciseKind Kind { get; set; } = ConceptExerciseKind.Recipe;

    /// <summary>Estimated time to complete, in minutes.</summary>
    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 10;

    /// <summary>Localized titles keyed by culture code.</summary>
    [JsonPropertyName("titles")]
    public Dictionary<string, string> Titles { get; set; } = new();

    /// <summary>Localized summaries keyed by culture code.</summary>
    [JsonPropertyName("summaries")]
    public Dictionary<string, string> Summaries { get; set; } = new();
}

/// <summary>
/// One pre-baked composition stored inside an exercise payload. The
/// <see cref="Fingerprint"/> is matched against the canonical fingerprint
/// computed from the user's Blockly workspace; on match, the player runs
/// <see cref="Timeline"/>.
/// </summary>
public class ExerciseComposition
{
    /// <summary>
    /// Canonical fingerprint string in the format described in
    /// <see cref="Services.BlockProgram.Fingerprint"/>. Insertion-order
    /// sensitive — two programs with the same blocks in different stack
    /// order produce different fingerprints.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>Human-readable name shown when the match succeeds.</summary>
    [JsonPropertyName("label")]
    public Dictionary<string, string> Label { get; set; } = new();

    /// <summary>Inlined timeline document the ExecutionPlayer will play.</summary>
    [JsonPropertyName("timeline")]
    public ExecutionTimelineDoc Timeline { get; set; } = new();
}

/// <summary>
/// On-disk payload for one exercise: the Blockly toolbox + the catalog of
/// pre-baked compositions. Loaded as one file per exercise so the catalog
/// stays browsable and a future authoring tool can write atomically.
/// </summary>
public class ExercisePayload
{
    /// <summary>Schema version of this exercise payload.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Blockly toolbox definition — passed verbatim to <c>Blockly.inject</c>.
    /// Kept as raw JSON so we don't need to mirror Blockly's whole schema
    /// in C# models. The BlockEditor component serializes it back out for
    /// JS interop.
    /// </summary>
    [JsonPropertyName("toolbox")]
    public JsonElement Toolbox { get; set; }

    /// <summary>
    /// Optional seed workspace — placed into the editor when the student
    /// first opens the exercise. Use it to give them a partial start
    /// (e.g. inputs already on the canvas, only gates missing).
    /// </summary>
    [JsonPropertyName("starter")]
    public JsonElement Starter { get; set; }

    /// <summary>The pre-baked timeline catalog — see <see cref="ExerciseComposition"/>.</summary>
    [JsonPropertyName("compositions")]
    public List<ExerciseComposition> Compositions { get; set; } = new();
}
