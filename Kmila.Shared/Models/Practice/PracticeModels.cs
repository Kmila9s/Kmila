using Kmila.Frontend;

namespace Kmila.Shared.Models.Practice;

/// <summary>How a Practice exercise asks the student to engage with the canonical solution.</summary>
public enum PracticeModality {
    /// <summary>Student writes the solution from scratch given the spec only.</summary>
    FromZero,
    /// <summary>Student is given a buggy solution and asked to fix it.</summary>
    FixBug,
    /// <summary>Student is given a partial solution and asked to fill in a marked region.</summary>
    FillGap
}

/// <summary>Pedagogical difficulty bucket used to sort and badge Practice exercises.</summary>
public enum PracticeDifficulty {
    /// <summary>Beginner-friendly exercise; few signals, short logic.</summary>
    Easy,
    /// <summary>Intermediate exercise; involves processes or moderate-width vectors.</summary>
    Medium,
    /// <summary>Advanced exercise; FSMs, counters, library usage.</summary>
    Hard
}

/// <summary>How candidate output is matched against the expected waveform.</summary>
public enum PracticeMatchKind
{
    /// <summary>Compare every transition tick-by-tick on the listed output ports.</summary>
    Exact,
    /// <summary>Sample the listed output ports at the end of the simulation only.</summary>
    FinalValueOnly,
    /// <summary>Sample the listed output ports at each tick in <see cref="PracticeExercise.SampleTimesNs"/>.</summary>
    ValueAtTimes
}

/// <summary>Top-level outcome of grading a student submission against an exercise's tests.</summary>
public enum PracticeVerdictKind
{
    /// <summary>Every test case passed.</summary>
    Solved,
    /// <summary>Build succeeded but at least one test produced an output mismatch.</summary>
    WrongAnswer,
    /// <summary>Submission failed to parse / synthesise.</summary>
    BuildError,
    /// <summary>Build succeeded but the simulator threw during execution.</summary>
    RuntimeError,
    /// <summary>A test exceeded the per-test wall-clock budget.</summary>
    Timeout
}

/// <summary>Light catalog row — what the Practice grid needs to render a card without loading bodies/tests/code.</summary>
public record PracticeExerciseMeta(
    string Id,
    PracticeModality Modality,
    PracticeDifficulty Difficulty,
    Dictionary<string, string> Titles,
    Dictionary<string, string> Summaries,
    int TestCaseCount,
    // Optional engine-feature gate — when set, the exercise is hidden from
    // grading until the simulation engine supports the named feature.
    // Currently the only flag used is "numeric_std-sim" for exercises
    // that depend on IEEE.NUMERIC_STD arithmetic in the simulator (the
    // interpreter's expression evaluator currently underflows on those).
    // Null when the exercise is fully runnable.
    string? RequiresEngine = null);

/// <summary>
/// One tiered hint for a Practice exercise. Hints escalate from general (level 1)
/// to very specific (level 3); the student reveals them one at a time, and reveals
/// are recorded so we know the student required help. <see cref="Text"/> is a
/// language → Markdown-body map (authored en + es; other languages fall back via
/// <see cref="Services.Practice.PracticeCatalog.Localize"/>).
/// </summary>
/// <param name="Level">Escalation level, 1 (most general) … 3 (most specific).</param>
/// <param name="Text">Localized Markdown bodies keyed by language code.</param>
public record PracticeHint(
    int Level,
    Dictionary<string, string> Text);

/// <summary>
/// One test case for a Practice exercise: identifier, multi-language descriptions,
/// visibility flag (hidden tests still grade), the input port-moment script, total
/// simulated duration, and the system clock frequency in MHz.
/// </summary>
public record PracticeTestCase(
    string Id,
    Dictionary<string, string> Descriptions,
    bool Visible,
    List<PortMomentConfiguration> Inputs,
    TimeSpan Duration,
    int SystemClockMhz);

/// <summary>
/// Full Practice exercise definition loaded from <c>manifest.json</c> + <c>tests.json</c>:
/// metadata, multi-language statements, canonical solution, optional starter for
/// FillGap/FixBug modalities (one <see cref="PracticeVariant"/> per HDL language), match policy for grading, and the test case list.
/// </summary>
public record PracticeExercise(
    string Id,
    PracticeModality Modality,
    PracticeDifficulty Difficulty,
    Dictionary<string, string> Titles,
    Dictionary<string, string> Summaries,
    Dictionary<string, string> Statements,
    HdlVariants<PracticeVariant> Variants,
    PracticeMatchKind MatchKind,
    List<string> OutputPortsToCompare,
    List<long> SampleTimesNs,
    List<PracticeTestCase> TestCases,
    string? RequiresEngine = null,
    // Tiered hints (level 1 → 3), loaded from the exercise's hints.json.
    // Null/empty when the exercise has no authored hints yet — the UI hides
    // the hint section in that case.
    List<PracticeHint>? Hints = null)
{
    /// <summary>
    /// Language the exercise opens in: the first supported language with a
    /// variant. The catalog never yields an exercise without variants.
    /// </summary>
    public HdlLanguage DefaultLanguage => Variants.Preferred!.Value.Language;

    /// <summary>The variant of <see cref="DefaultLanguage"/>.</summary>
    public PracticeVariant DefaultVariant => Variants.Preferred!.Value.Payload;

    /// <summary>The variant for <paramref name="language"/>; throws when the exercise has none.</summary>
    public PracticeVariant Variant(HdlLanguage language) =>
        Variants.For(language)
        ?? throw new InvalidOperationException($"Exercise '{Id}' has no {language.DisplayName} variant.");
}

/// <summary>One language variant of a Practice exercise: the code the student starts from and the canonical solution.</summary>
/// <param name="Starter">Source shown in the editor when the exercise opens (empty for FromZero).</param>
/// <param name="Solution">Canonical solution; its simulation is the expected waveform.</param>
public sealed record PracticeVariant(string Starter, string Solution);

/// <summary>Outcome of grading one <see cref="PracticeTestCase"/> against the student's simulation.</summary>
/// <param name="TestCaseId">Identifier of the test case this result belongs to.</param>
/// <param name="Passed">True when the student's output matched the expected values for every compared sample.</param>
/// <param name="ErrorClass">Category of failure (e.g. build/runtime/mismatch/timeout), or null when passed.</param>
/// <param name="FirstMismatchSignal">Name of the first output signal that diverged, or null when passed.</param>
/// <param name="FirstMismatchTimeNs">Simulated time in nanoseconds of the first mismatch, or null when passed.</param>
/// <param name="ExpectedAtMismatch">Expected value at the first mismatch, or null when passed.</param>
/// <param name="ActualAtMismatch">Actual student value at the first mismatch, or null when passed.</param>
/// <param name="MismatchCount">Total number of mismatched samples across the compared signals.</param>
/// <param name="SimulationDurationMs">Wall-clock time in milliseconds spent simulating this test case.</param>
public record PracticeTestResult(
    string TestCaseId,
    bool Passed,
    string? ErrorClass,
    string? FirstMismatchSignal,
    long? FirstMismatchTimeNs,
    string? ExpectedAtMismatch,
    string? ActualAtMismatch,
    int MismatchCount,
    long SimulationDurationMs);

/// <summary>Overall verdict for one Practice submission: the aggregate outcome plus per-test results.</summary>
/// <param name="Kind">Aggregate outcome of the attempt (e.g. accepted, wrong answer, build error, timeout).</param>
/// <param name="ExerciseId">Identifier of the exercise that was attempted.</param>
/// <param name="SubmittedAt">Timestamp when the attempt was submitted.</param>
/// <param name="BuildDurationMs">Wall-clock time in milliseconds spent building the student's design.</param>
/// <param name="TotalDurationMs">Total wall-clock time in milliseconds for the whole attempt (build + all tests).</param>
/// <param name="BuildErrorMessage">Short build error message, or null when the build succeeded.</param>
/// <param name="BuildErrorDetail">Detailed build error output, or null when the build succeeded.</param>
/// <param name="TestResults">Per-test-case results produced while grading the attempt.</param>
public record PracticeAttemptVerdict(
    PracticeVerdictKind Kind,
    string ExerciseId,
    DateTime SubmittedAt,
    long BuildDurationMs,
    long TotalDurationMs,
    string? BuildErrorMessage,
    string? BuildErrorDetail,
    List<PracticeTestResult> TestResults);
