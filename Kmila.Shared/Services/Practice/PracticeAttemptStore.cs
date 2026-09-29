using Kmila.Shared.Data;
using Kmila.Shared.Models.Practice;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Practice;

/// <summary>
/// Persists every Practice submission and the matching aggregate progress
/// row. Designed so the eventual export feature is just a SQLite JOIN —
/// every datapoint the verdict UI shows is captured here verbatim.
/// </summary>
/// <remarks>
/// <para>
/// Engine-version tag travels with the row so an engine bump can later
/// flag old attempts as "graded under v1.15" or trigger a re-grade. The
/// constant lives here (not in <see cref="PracticeEvaluator"/>) so all
/// persistence-related metadata sits in one file.
/// </para>
/// <para>
/// Scoped service: each Blazor circuit gets its own instance using the
/// circuit-scoped <see cref="ApplicationDbContext"/> — same lifetime
/// pattern as <see cref="LessonProgressStore"/> and
/// <see cref="SimulationSnapshotStore"/>.
/// </para>
/// </remarks>
public class PracticeAttemptStore
{
    /// <summary>Bumped any time grader behavior changes in a way that could re-grade old code differently.</summary>
    public const string CurrentEngineVersion = "practice-1.0";

    /// <summary>Cap on per-exercise attempt history kept in the DB. Older rows are evicted on insert.</summary>
    public const int MaxAttemptsPerExercise = 200;

    private readonly ApplicationDbContext _db;
    private readonly ILogger _logger;

    /// <summary>Creates the practice attempt store.</summary>
    /// <param name="db">Application database used to persist practice attempts.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public PracticeAttemptStore(ApplicationDbContext db,
                                ILogger<PracticeAttemptStore>? logger = null)
    {
        _db = db;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Fires after every attempt save / progress update so reactive UIs (catalog cards, nav badge) refresh.</summary>
    public event Action<string>? OnChange;  // exerciseId

    /// <summary>
    /// Persists one attempt row + N test-result rows + upserts the aggregate
    /// progress row. Returns the inserted attempt id (or -1 on failure).
    /// </summary>
    public async Task<int> SaveAttemptAsync(PracticeAttemptVerdict verdict, string codeSnapshot)
    {
        try
        {
            await _db.InitDBAsync();

            var attempt = new PracticeAttempt
            {
                ExerciseId        = verdict.ExerciseId,
                SubmittedAt       = verdict.SubmittedAt,
                CodeSnapshot      = codeSnapshot ?? string.Empty,
                Verdict           = verdict.Kind,
                TotalDurationMs   = verdict.TotalDurationMs,
                BuildDurationMs   = verdict.BuildDurationMs,
                BuildErrorMessage = verdict.BuildErrorMessage,
                BuildErrorDetail  = verdict.BuildErrorDetail,
                EngineVersion     = CurrentEngineVersion
            };

            await _db.InsertAsync(attempt);

            foreach (var r in verdict.TestResults)
            {
                await _db.InsertAsync(new PracticeAttemptTestResult
                {
                    AttemptId            = attempt.Id,
                    TestCaseId           = r.TestCaseId,
                    Passed               = r.Passed,
                    ErrorClass           = r.ErrorClass,
                    FirstMismatchSignal  = r.FirstMismatchSignal,
                    FirstMismatchTimeNs  = r.FirstMismatchTimeNs,
                    ExpectedAtMismatch   = r.ExpectedAtMismatch,
                    ActualAtMismatch     = r.ActualAtMismatch,
                    MismatchCount        = r.MismatchCount,
                    SimulationDurationMs = r.SimulationDurationMs
                });
            }

            await UpsertProgressAsync(verdict);
            await EnforceRetentionAsync(verdict.ExerciseId);

            OnChange?.Invoke(verdict.ExerciseId);
            _logger.LogInformation(
                "[PracticeAttemptStore] Saved attempt #{Id} for {Ex} verdict={V} ({TestCount} tests, {Ms} ms)",
                attempt.Id, verdict.ExerciseId, verdict.Kind, verdict.TestResults.Count, verdict.TotalDurationMs);
            return attempt.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeAttemptStore] Save failed for {Ex}", verdict.ExerciseId);
            return -1;
        }
    }

    /// <summary>Returns past attempts for the exercise, newest first. Capped to <paramref name="limit"/>.</summary>
    public async Task<List<PracticeAttempt>> GetAttemptsAsync(string exerciseId, int limit = 50)
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.PracticeAttempts
                .Where(a => a.ExerciseId == exerciseId)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeAttemptStore] List attempts failed for {Ex}", exerciseId);
            return new();
        }
    }

    /// <summary>Returns per-test results for an attempt id, in stored order.</summary>
    public async Task<List<PracticeAttemptTestResult>> GetTestResultsAsync(int attemptId)
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.PracticeAttemptTestResults
                .Where(r => r.AttemptId == attemptId)
                .OrderBy(r => r.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeAttemptStore] List test results failed for attempt {A}", attemptId);
            return new();
        }
    }

    /// <summary>Aggregate row for the catalog status chip; null when the user has never attempted the exercise.</summary>
    public async Task<PracticeProgress?> GetProgressAsync(string exerciseId)
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.PracticeProgress
                .Where(p => p.ExerciseId == exerciseId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeAttemptStore] GetProgress failed for {Ex}", exerciseId);
            return null;
        }
    }

    /// <summary>All progress rows — for the nav badge and Practice catalog overlay.</summary>
    public async Task<List<PracticeProgress>> GetAllProgressAsync()
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.PracticeProgress.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeAttemptStore] GetAllProgress failed");
            return new();
        }
    }

    /// <summary>Total exercises the user has marked Solved.</summary>
    public async Task<int> TotalSolvedAsync()
    {
        try
        {
            await _db.InitDBAsync();
            return await _db.PracticeProgress
                .Where(p => p.Status == PracticeStatus.Solved)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PracticeAttemptStore] TotalSolved failed");
            return 0;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // internals
    // ────────────────────────────────────────────────────────────────────

    private async Task UpsertProgressAsync(PracticeAttemptVerdict v)
    {
        var existing = await _db.PracticeProgress
            .Where(p => p.ExerciseId == v.ExerciseId)
            .FirstOrDefaultAsync();

        bool isSolved = v.Kind == PracticeVerdictKind.Solved;

        if (existing == null)
        {
            await _db.InsertAsync(new PracticeProgress
            {
                ExerciseId      = v.ExerciseId,
                Status          = isSolved ? PracticeStatus.Solved : PracticeStatus.InProgress,
                FirstSolvedAt   = isSolved ? v.SubmittedAt : (DateTime?)null,
                AttemptCount    = 1,
                BestSolveTimeMs = isSolved ? v.TotalDurationMs : 0,
                LastAttemptAt   = v.SubmittedAt
            });
            return;
        }

        existing.AttemptCount += 1;
        existing.LastAttemptAt = v.SubmittedAt;

        // Status only ever progresses NotStarted → InProgress → Solved.
        // Once Solved we keep it Solved even if a later attempt fails.
        if (isSolved)
        {
            if (existing.Status != PracticeStatus.Solved)
            {
                existing.Status = PracticeStatus.Solved;
                existing.FirstSolvedAt = v.SubmittedAt;
                existing.BestSolveTimeMs = v.TotalDurationMs;
            }
            else if (v.TotalDurationMs > 0 &&
                     (existing.BestSolveTimeMs == 0 || v.TotalDurationMs < existing.BestSolveTimeMs))
            {
                existing.BestSolveTimeMs = v.TotalDurationMs;
            }
        }
        else if (existing.Status == PracticeStatus.NotStarted)
        {
            existing.Status = PracticeStatus.InProgress;
        }

        await _db.UpdateAsync(existing);
    }

    private async Task EnforceRetentionAsync(string exerciseId)
    {
        try
        {
            var all = await _db.PracticeAttempts
                .Where(a => a.ExerciseId == exerciseId)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();
            if (all.Count <= MaxAttemptsPerExercise) return;

            var toEvict = all.Skip(MaxAttemptsPerExercise).ToList();
            foreach (var a in toEvict)
            {
                // Delete cascading test results first; SQLite-net has no FK enforcement.
                var children = await _db.PracticeAttemptTestResults
                    .Where(r => r.AttemptId == a.Id)
                    .ToListAsync();
                foreach (var c in children) await _db.DeleteAsync(c);
                await _db.DeleteAsync(a);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PracticeAttemptStore] Retention sweep failed for {Ex}", exerciseId);
        }
    }
}
