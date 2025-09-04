using Kmila.Shared.Models;
using SQLite;
using Microsoft.EntityFrameworkCore;

namespace Kmila.Shared.Data;

public class ApplicationDbContext : SQLiteAsyncConnection
{
    public ApplicationDbContext(string path) : base(path)
    {
        try
        {
            CreateTableAsync<Project>().Wait();
        }
        catch { }
    }
    public AsyncTableQuery<Project> Projects => Table<Project>();
}