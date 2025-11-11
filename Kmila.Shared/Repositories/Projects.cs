using Kmila.Shared.Data;
using Kmila.Shared.Interfaces;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Repositories;
/// <summary>
/// Repository for managing projects, implementing the <see cref="IRepository{T}"/> interface for <see cref="Project"/>.
/// </summary>
public class Projects : IRepository<Project>
{
    private readonly ILogger<Projects> _logger;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly CancellationTokenSource _cts = new();
    /// <summary>
    /// Gets the last error message that occurred.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;
    /// <summary>
    /// Initializes a new instance of the <see cref="Projects"/> class.
    /// </summary>
    /// <param name="logger">The logger for logging errors and information.</param>
    /// <param name="applicationDbContext">The database context for data access.</param>
    public Projects(ILogger<Projects> logger, ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _ = _applicationDbContext.InitDBAsync();
    }
    /// <summary>
    /// Creates a new project asynchronously.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
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
    /// <summary>
    /// Updates an existing project asynchronously.
    /// </summary>
    /// <param name="project">The project to update.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
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
    /// <summary>
    /// Removes a project asynchronously.
    /// </summary>
    /// <param name="project">The project to remove.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
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
            if(Directory.Exists(oldProject.BasePath))
                Directory.Delete(oldProject.BasePath, true);
            await _applicationDbContext.DeleteAsync(project);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return false;
    }
    /// <summary>
    /// Gets a list of all projects asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of projects.</returns>
    public async Task<List<Project>> GetListAsync()
    {
        LastError = string.Empty;
        try
        {
            return await _applicationDbContext.Projects.ToListAsync();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return new();
    }
    /// <summary>
    /// Gets a project by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the project.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the project.</returns>
    public async Task<Project> GetByAsync(object id)
    {
        LastError = string.Empty;
        try
        {
            Project project = null!;
            if (id is int intId)
                project = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == intId);
            else if(id is string strId)
                project = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Name.ToLower() == strId.ToLower());
            if (project == null)
            {
                LastError = "Project not found";
            }
            return project ?? new();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return null!;
    }
    /// <summary>
    /// Releases the resources used by the <see cref="Projects"/> class.
    /// </summary>
    public void Dispose()
    {

    }
}