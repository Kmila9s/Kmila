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
    /// <summary>
    /// Optional engine-feature gate — when set, the exercise is hidden from
    /// grading until the simulation engine supports the named feature.
    /// Currently the only flag used is <c>"numeric_std-sim"</c> for exercises
    /// that depend on IEEE.NUMERIC_STD arithmetic in the simulator (the
    /// interpreter's expression evaluator currently underflows on those).
    /// Null when the exercise is fully runnable.
    /// </summary>
    string? RequiresEngine = null);

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
/// FillGap/FixBug modalities, match policy for grading, and the test case list.
/// </summary>
public record PracticeExercise(
    string Id,
    PracticeModality Modality,
    PracticeDifficulty Difficulty,
    Dictionary<string, string> Titles,
    Dictionary<string, string> Summaries,
    Dictionary<string, string> Statements,
    string SolutionVhdl,
    string StarterVhdl,
    PracticeMatchKind MatchKind,
    List<string> OutputPortsToCompare,
    List<long> SampleTimesNs,
    List<PracticeTestCase> TestCases,
    string? RequiresEngine = null);

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

public record PracticeAttemptVerdict(
    PracticeVerdictKind Kind,
    string ExerciseId,
    DateTime SubmittedAt,
    long BuildDurationMs,
    long TotalDurationMs,
    string? BuildErrorMessage,
    string? BuildErrorDetail,
    List<PracticeTestResult> TestResults);
