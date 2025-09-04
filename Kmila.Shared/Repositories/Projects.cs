using Kmila.Shared.Data;
using Kmila.Shared.Interfaces;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Repositories;

public class Projects : IRepository<Project>
{
    private readonly ILogger<Projects> _logger;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly CancellationTokenSource _cts = new();
    public string LastError { get; private set; } = string.Empty;
    public Projects(ILogger<Projects> logger, ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
    }
    public async Task<bool> CreateAsync(Project project)
    {
        LastError = string.Empty;
        try
        {
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p =>
                p.Name.ToLower().Equals(project.Name.ToLower()) && p.BasePath.ToLower().Equals(project.BasePath.ToLower())
            );
            if (oldProject != null)
            {
                LastError = "Project and path already registered";
                return false;
            }
            Directory.CreateDirectory(project.BasePath);
            await _applicationDbContext.InsertAsync(project);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return false;
    }
    public async Task<bool> UpdateAsync(Project project)
    {
        LastError = string.Empty;
        try
        {
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (oldProject == null)
            {
                LastError = "Project not registered";
                return false;
            }
            if ((await _applicationDbContext.Projects.ToListAsync()).Any(p => p.BasePath.ToLower() == project.BasePath.ToLower() && p.Id != project.Id))
            {
                LastError = "Path already in use";
                return false;
            }
            if (oldProject.BasePath.ToLower() != project.BasePath.ToLower())
            {
                if (!Directory.Exists(oldProject.BasePath))
                {
                    LastError = "Old directory not found";
                    return false;
                }
                Directory.Move(oldProject.BasePath, project.BasePath);
            }
            oldProject.Name = project.Name;
            oldProject.Description = project.Description;
            oldProject.BasePath = project.BasePath;
            await _applicationDbContext.UpdateAsync(project);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return false;
    }
    public async Task<bool> RemoveAsync(Project project)
    {
        LastError = string.Empty;
        try
        {
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return false;
            }
            Directory.Delete(oldProject.BasePath, true);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return false;
    }
    public async Task<List<Project>> GetListAsync()
    {
        LastError = string.Empty;
        try
        {

        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return null!;
    }
    public async Task<Project> GetByAsync(object id)
    {
        LastError = string.Empty;
        try
        {

        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return null!;
    }
    public void Dispose()
    {

    }
}