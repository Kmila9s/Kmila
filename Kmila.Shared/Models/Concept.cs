using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Difficulty bucket for a Concepts module / lesson. Mirrors
/// <see cref="LessonLevel"/> but stays separate so the two tracks can evolve
/// their difficulty taxonomies independently.
/// </summary>
public enum ConceptLevel
{
    Beginner,
    Easy,
    Intermediate,
    Advanced,
    Expert
}

/// <summary>
/// Pedagogical flavor of a concept lesson.
/// <list type="bullet">
///   <item><see cref="Reading"/> — pure markdown, no embedded player.</item>
///   <item><see cref="Player"/> — markdown + an embedded <c>ExecutionPlayer</c>
///         fed by a <c>{lessonId}.timeline.json</c> sidecar.</item>
/// </list>
/// </summary>
public enum ConceptKind
{
    /// <summary>Text + diagrams; no execution timeline.</summary>
    Reading,
    /// <summary>Text + an embedded ExecutionPlayer driven by a timeline JSON.</summary>
    Player
}

/// <summary>
/// One top-level module in the Concepts curriculum (e.g. "Execution Models").
/// Mirrors <see cref="LessonModule"/> shape so the catalog UI can render both
/// tracks with the same components.
/// </summary>
public class ConceptModule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "Diagram3";

    [JsonPropertyName("level")]
    public ConceptLevel Level { get; set; } = ConceptLevel.Beginner;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("titles")]
    public Dictionary<string, string> Titles { get; set; } = new();

    [JsonPropertyName("summaries")]
    public Dictionary<string, string> Summaries { get; set; } = new();

    [JsonPropertyName("lessons")]
    public List<Concept> Lessons { get; set; } = new();

    /// <summary>
    /// Optional Scratch-style exercises tied to this module. Each exercise
    /// has its toolbox + pre-baked composition catalog in a sidecar file
    /// at <c>Concepts/exercises/{id}.exercise.json</c>.
    /// </summary>
    [JsonPropertyName("exercises")]
    public List<ConceptExercise> Exercises { get; set; } = new();
}

/// <summary>
/// One lesson inside a <see cref="ConceptModule"/>. The Markdown body lives at
/// <c>wwwroot/Concepts/{lang}/{moduleId}/{conceptId}.md</c>; the timeline JSON
/// at <c>wwwroot/Concepts/{lang}/{moduleId}/{conceptId}.timeline.json</c>
/// (English fallback). Everything here is metadata.
/// </summary>
public class Concept
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public ConceptKind Kind { get; set; } = ConceptKind.Reading;

    [JsonPropertyName("level")]
    public ConceptLevel Level { get; set; } = ConceptLevel.Beginner;

    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 8;

    [JsonPropertyName("titles")]
    public Dictionary<string, string> Titles { get; set; } = new();

    [JsonPropertyName("summaries")]
    public Dictionary<string, string> Summaries { get; set; } = new();

    /// <summary>
    /// Optional end-of-lesson micro-quiz. Renders after the body / player as
    /// a single radio question — wrong answers reveal an explanation, the
    /// correct answer auto-marks the lesson complete. Lessons without a
    /// quizlet just skip the panel.
    /// </summary>
    [JsonPropertyName("quizlet")]
    public ConceptQuizlet? Quizlet { get; set; }
}

/// <summary>
/// One micro-quiz attached to a concept lesson. Phase 4 / "validation gates"
/// in the roadmap — keeps lessons honest by checking whether the student
/// actually internalized the key idea before letting them mark complete.
/// </summary>
public class ConceptQuizlet
{
    /// <summary>Localized question text (one short paragraph max).</summary>
    [JsonPropertyName("question")]
    public Dictionary<string, string> Question { get; set; } = new();

    /// <summary>
    /// 2-3 options. Exactly one should have <c>Correct = true</c>; we
    /// don't currently enforce that constraint, but the panel only treats
    /// the first <c>Correct</c> entry as the right answer.
    /// </summary>
    [JsonPropertyName("options")]
    public List<ConceptQuizletOption> Options { get; set; } = new();
}

/// <summary>One option in a <see cref="ConceptQuizlet"/>.</summary>
public class ConceptQuizletOption
{
    /// <summary>Stable id used in the URL/persistence (e.g. "a", "b", "c").</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Localized option label.</summary>
    [JsonPropertyName("label")]
    public Dictionary<string, string> Label { get; set; } = new();

    /// <summary>True only for the correct answer.</summary>
    [JsonPropertyName("correct")]
    public bool Correct { get; set; }

    /// <summary>
    /// Localized explanation shown after the student commits an answer.
    /// Required on every option — both right and wrong answers should
    /// teach. The correct option's explanation reinforces the idea; the
    /// wrong options' explain the misconception.
    /// </summary>
    [JsonPropertyName("explanation")]
    public Dictionary<string, string> Explanation { get; set; } = new();
}

/// <summary>Root of the Concepts <c>manifest.json</c> file.</summary>
public class ConceptManifest
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("modules")]
    public List<ConceptModule> Modules { get; set; } = new();
}
