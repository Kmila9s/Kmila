using SQLite;

namespace Kmila.Shared.Models.Practice;

/// <summary>Aggregate progress for a given exercise — what the catalog renders as a status chip.</summary>
public enum PracticeStatus
{
    /// <summary>No attempt has been made yet.</summary>
    NotStarted,
    /// <summary>Attempted but not yet solved.</summary>
    InProgress,
    /// <summary>At least one attempt has passed all tests.</summary>
    Solved
}

/// <summary>
/// One row per submission. Stores the full code snapshot so the user can
/// load past attempts back into the editor and compare with their current
/// code. Linked by id to N rows in <see cref="PracticeAttemptTestResult"/>.
/// </summary>
public class PracticeAttempt
{
    /// <summary>Auto-incrementing primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Stable exercise id from the manifest (e.g. <c>m1-half-adder</c>).</summary>
    [Indexed]
    public string ExerciseId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the attempt was submitted.</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Full text of what the user submitted — kept verbatim for diffing.</summary>
    public string CodeSnapshot { get; set; } = string.Empty;

    /// <summary>Overall verdict for this attempt.</summary>
    public PracticeVerdictKind Verdict { get; set; }

    /// <summary>End-to-end grade duration: build + N test simulations + comparison.</summary>
    public long TotalDurationMs { get; set; }

    /// <summary>Time spent in the parser/build phase only (the candidate's, not the solution's).</summary>
    public long BuildDurationMs { get; set; }

    /// <summary>Build-error class and detail, populated only when <see cref="Verdict"/> is BuildError.</summary>
    public string? BuildErrorMessage { get; set; }
    /// <summary>Detailed build-error text, populated only on a build failure.</summary>
    public string? BuildErrorDetail { get; set; }

    /// <summary>
    /// Engine version the run was graded under. Captured so a future engine
    /// bump can re-grade old attempts (or at least flag them as "graded under
    /// vN, may differ on current engine") in the export feature.
    /// </summary>
    public string? EngineVersion { get; set; }
}

/// <summary>
/// One row per test case result inside an attempt. Carries enough
/// information to reconstruct the verdict UI without re-running the sim
/// and to feed the eventual data-export feature.
/// </summary>
public class PracticeAttemptTestResult
{
    /// <summary>Auto-incrementing primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Foreign key into <see cref="PracticeAttempt"/>.Id.</summary>
    [Indexed]
    public int AttemptId { get; set; }

    /// <summary>Id of the test case this result corresponds to.</summary>
    public string TestCaseId { get; set; } = string.Empty;

    /// <summary>Whether this test case passed.</summary>
    public bool Passed { get; set; }

    /// <summary>Free-form classifier: <c>value-mismatch</c> / <c>build</c> / <c>runtime</c> / <c>expected-empty</c>.</summary>
    public string? ErrorClass { get; set; }

    /// <summary>Name of the first signal that diverged from the expected output.</summary>
    public string? FirstMismatchSignal { get; set; }
    /// <summary>Simulation time (ns) of the first mismatch.</summary>
    public long? FirstMismatchTimeNs { get; set; }
    /// <summary>Expected value at the first mismatch.</summary>
    public string? ExpectedAtMismatch { get; set; }
    /// <summary>Actual value at the first mismatch.</summary>
    public string? ActualAtMismatch { get; set; }

    /// <summary>How many sample points across all output ports diverged.</summary>
    public int MismatchCount { get; set; }

    /// <summary>Wall time of just this test's simulation phase.</summary>
    public long SimulationDurationMs { get; set; }
}

/// <summary>
/// One row per exercise (latest aggregate state). Updated after every
/// attempt: status flips to Solved on first success and never regresses;
/// AttemptCount keeps growing; BestSolveTimeMs tracks the fastest passing
/// total duration. Catalog cards + nav badge read from this table.
/// </summary>
public class PracticeProgress
{
    /// <summary>Stable exercise id (primary key).</summary>
    [PrimaryKey]
    public string ExerciseId { get; set; } = string.Empty;

    /// <summary>Current aggregate status for the exercise.</summary>
    public PracticeStatus Status { get; set; }

    /// <summary>Timestamp of the first attempt that reached <see cref="PracticeVerdictKind.Solved"/>; null until then.</summary>
    public DateTime? FirstSolvedAt { get; set; }

    /// <summary>Total number of attempts made for this exercise.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Fastest Solved attempt's total duration. Zero until first solve.</summary>
    public long BestSolveTimeMs { get; set; }

    /// <summary>UTC timestamp of the most recent attempt.</summary>
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;
}
