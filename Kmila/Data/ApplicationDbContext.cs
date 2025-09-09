using Kmila.Shared.Models;
using SQLite;

namespace Kmila.Shared.Data;

public class ApplicationDbContext : SQLiteAsyncConnection
{
    public ApplicationDbContext(string path) : base(path)
    {
        Console.WriteLine(path);
    }
    public async Task InitDBAsync()
    {
        try
        {
            var result = await CreateTableAsync<Project>();
            Console.WriteLine($"[DB] Project table create result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    public AsyncTableQuery<Project> Projects => Table<Project>();
}