using Microsoft.Extensions.Logging;
using Kmila.Shared.Models;
using Kmila.Shared.Models.Practice;
using SQLite;

namespace Kmila.Shared.Data;
/// <summary>
/// Represents the application's database context for interacting with the SQLite database.
/// It provides access to the tables for projects, project files, and application settings.
/// </summary>
public class ApplicationDbContext : SQLiteAsyncConnection
{
    /// <summary>
    /// Static event raised when a setting value changes.
    /// Components can subscribe to react to configuration changes.
    /// </summary>
    public static event Action<string>? StaticOnValueChanged;

    /// <summary>
    /// Logger — flows to the in-app Logs page via InMemoryLoggerProvider.
    /// Optional at construction time so the MAUI/Web DI factories that don't
    /// inject one fall back to a no-op.
    /// </summary>
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="path">The database file path.</param>
    /// <param name="logger">Optional logger (NullLogger fallback when omitted).</param>
    public ApplicationDbContext(string path, Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>? logger = null)
        : base(path)
    {
        _logger = (Microsoft.Extensions.Logging.ILogger?)logger
               ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ApplicationDbContext>.Instance;
    }
    /// <summary>
    /// Initializes the database asynchronously by creating the necessary tables if they don't exist.
    /// </summary>
    public async Task InitDBAsync()
    {
        // Create each table independently so a failure in one does not prevent others from being created
        try
        {
            // Create the Projects table for storing project metadata
            var result = await CreateTableAsync<Project>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
        try
        {
            // Create the ProjectFiles table for storing file records per project
            var result = await CreateTableAsync<ProjectFile>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
        try
        {
            // Create the AppSettings table for key-value application settings
            var result = await CreateTableAsync<AppSettings>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
        try
        {
            // Simulation snapshots — one row per captured run so the Runs
            // dock tab can list + replay past executions.
            var result = await CreateTableAsync<SimulationSnapshot>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
        try
        {
            // Practice attempts — one row per Submit click. Holds the code
            // snapshot, verdict, and timing telemetry so the export feature
            // can build a per-user activity log.
            var result = await CreateTableAsync<PracticeAttempt>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
        try
        {
            // Per-test result rows linked to a PracticeAttempt by AttemptId.
            // Keeps first-mismatch detail so the verdict UI can re-render
            // without re-running the simulation.
            var result = await CreateTableAsync<PracticeAttemptTestResult>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
        try
        {
            // Aggregate progress — one row per exercise. Powers the catalog
            // status chips and the nav-bar "n / N solved" badge.
            var result = await CreateTableAsync<PracticeProgress>();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
    }
    /// <summary>
    /// Gets the queryable collection of projects.
    /// </summary>
    public AsyncTableQuery<Project> Projects => Table<Project>();
    /// <summary>
    /// Gets the queryable collection of captured simulation runs.
    /// </summary>
    public AsyncTableQuery<SimulationSnapshot> SimulationSnapshots => Table<SimulationSnapshot>();
    /// <summary>
    /// Gets the queryable collection of project files.
    /// </summary>
    public AsyncTableQuery<ProjectFile> ProjectFiles => Table<ProjectFile>();
    /// <summary>
    /// Gets the queryable collection of application settings.
    /// </summary>
    public AsyncTableQuery<AppSettings> Settings => Table<AppSettings>();
    /// <summary>Gets the queryable collection of Practice submissions (one row per Submit click).</summary>
    public AsyncTableQuery<PracticeAttempt> PracticeAttempts => Table<PracticeAttempt>();
    /// <summary>Gets the queryable collection of per-test results inside Practice attempts.</summary>
    public AsyncTableQuery<PracticeAttemptTestResult> PracticeAttemptTestResults => Table<PracticeAttemptTestResult>();
    /// <summary>Gets the aggregate progress per exercise (status + counters).</summary>
    public AsyncTableQuery<PracticeProgress> PracticeProgress => Table<PracticeProgress>();

    #region Settings Helper Methods

    /// <summary>
    /// Gets a setting value by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <returns>The setting value or null if not found.</returns>
    public async Task<string?> GetSettingAsync(string key)
    {
        try
        {
            var setting = await Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
            return setting?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DB] Error getting setting {key}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets a setting value by key.
    /// Fires StaticOnValueChanged event after successful save.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    public async Task SetSettingAsync(string key, string value)
    {
        try
        {
            // Upsert: update existing setting or insert a new one
            var existing = await Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
            if (existing != null)
            {
                // Update the existing record with new value and timestamp
                existing.Value = value;
                existing.LastModified = DateTime.UtcNow;
                await UpdateAsync(existing);
            }
            else
            {
                // Insert a new setting record
                await InsertAsync(new AppSettings
                {
                    Key = key,
                    Value = value,
                    LastModified = DateTime.UtcNow
                });
            }
            // Notify subscribers that a setting has changed (triggers reactive updates like language switching)
            StaticOnValueChanged?.Invoke(key);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DB] Error setting {key}: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a setting row entirely. Used when the user clears a project's
    /// entry-point marker — leaving an empty-string row would still satisfy
    /// "key exists" lookups elsewhere, so we delete instead of overwriting.
    /// Fires <see cref="StaticOnValueChanged"/> so reactive UI re-evaluates.
    /// </summary>
    public async Task DeleteSettingAsync(string key)
    {
        try
        {
            var existing = await Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
            if (existing == null) return;
            await DeleteAsync(existing);
            StaticOnValueChanged?.Invoke(key);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[DB] Error deleting setting {key}: {ex.Message}");
        }
    }

    #endregion

    #region Project Entry Point

    /// <summary>
    /// Settings key prefix for the per-project entry-point marker. The full
    /// key is <c>KMILA_PROJECT_MAIN_FILE_{projectId}</c>; the value is the
    /// chosen file's <c>FileName</c> (project-scoped unique). Stored as a
    /// settings row instead of a column on <c>Project</c> to avoid a schema
    /// migration — the trade-off is one extra query per build / tab switch
    /// in the editor, which is negligible.
    /// </summary>
    private const string MAIN_FILE_KEY_PREFIX = "KMILA_PROJECT_MAIN_FILE_";

    private static string MainFileKey(int projectId) => $"{MAIN_FILE_KEY_PREFIX}{projectId}";

    /// <summary>
    /// Reads the file marked as the project's entry point. Returns null when
    /// no explicit marker has been set — callers fall back to the active
    /// editor tab in that case.
    /// </summary>
    public async Task<string?> GetProjectMainFileAsync(int projectId)
    {
        if (projectId <= 0) return null;
        var name = await GetSettingAsync(MainFileKey(projectId));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    /// <summary>Persists <paramref name="fileName"/> as the project's entry
    /// point. Overwrites any prior selection.</summary>
    public Task SetProjectMainFileAsync(int projectId, string fileName)
        => SetSettingAsync(MainFileKey(projectId), fileName);

    /// <summary>Clears the project's entry-point marker. Build then falls
    /// back to the active editor tab.</summary>
    public Task ClearProjectMainFileAsync(int projectId)
        => DeleteSettingAsync(MainFileKey(projectId));

    #endregion
}