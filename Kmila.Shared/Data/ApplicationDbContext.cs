using Kmila.Shared.Models;
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
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="path">The database file path.</param>
    public ApplicationDbContext(string path) : base(path)
    {
    }
    /// <summary>
    /// Initializes the database asynchronously by creating the necessary tables if they don't exist.
    /// </summary>
    public async Task InitDBAsync()
    {
        try
        {
            var result = await CreateTableAsync<Project>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        try
        {
            var result = await CreateTableAsync<ProjectFile>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        try
        {
            var result = await CreateTableAsync<AppSettings>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    /// <summary>
    /// Gets the queryable collection of projects.
    /// </summary>
    public AsyncTableQuery<Project> Projects => Table<Project>();
    /// <summary>
    /// Gets the queryable collection of project files.
    /// </summary>
    public AsyncTableQuery<ProjectFile> ProjectFiles => Table<ProjectFile>();
    /// <summary>
    /// Gets the queryable collection of application settings.
    /// </summary>
    public AsyncTableQuery<AppSettings> Settings => Table<AppSettings>();

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
            Console.WriteLine($"[DB] Error getting setting {key}: {ex.Message}");
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
            var existing = await Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.Value = value;
                existing.LastModified = DateTime.UtcNow;
                await UpdateAsync(existing);
            }
            else
            {
                await InsertAsync(new AppSettings
                {
                    Key = key,
                    Value = value,
                    LastModified = DateTime.UtcNow
                });
            }
            // Notify subscribers that a setting has changed
            StaticOnValueChanged?.Invoke(key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Error setting {key}: {ex.Message}");
        }
    }

    #endregion
}