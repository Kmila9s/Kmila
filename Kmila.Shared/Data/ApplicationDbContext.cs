using Kmila.Shared.Models;
using SQLite;

namespace Kmila.Shared.Data;
/// <summary>
/// Represents the application's database context for interacting with the SQLite database.
/// It provides access to the tables for projects and project files.
/// </summary>
public class ApplicationDbContext : SQLiteAsyncConnection
{
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
        }catch (Exception ex)
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
}