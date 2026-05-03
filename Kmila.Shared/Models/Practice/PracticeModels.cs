namespace Kmila.Shared.Models.Practice;

public enum PracticeModality { FromZero, FixBug, FillGap }

public enum PracticeDifficulty { Easy, Medium, Hard }

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

public enum PracticeVerdictKind
{
    Solved,
    WrongAnswer,
    BuildError,
    RuntimeError,
    Timeout
}

/// <summary>Light catalog row — what the Practice grid needs to render a card without loading bodies/tests/code.</summary>
public record PracticeExerciseMeta(
    string Id,
    PracticeModality Modality,
    PracticeDifficulty Difficulty,
    Dictionary<string, string> Titles,
    Dictionary<string, string> Summaries,
    int TestCaseCount);

public record PracticeTestCase(
    string Id,
    Dictionary<string, string> Descriptions,
    bool Visible,
    List<PortMomentConfiguration> Inputs,
    TimeSpan Duration,
    int SystemClockMhz);

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
    List<PracticeTestCase> TestCases);

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
