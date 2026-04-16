using Kmila.Shared.Data;
using Kmila.Shared.Interfaces;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Repositories;
/// <summary>
/// Repository for managing project files, implementing the <see cref="IRepository{T}"/> interface for <see cref="ProjectFile"/>.
/// </summary>
public class ProjectFiles : IRepository<ProjectFile>
{
    // Logger for recording errors and diagnostic information
    private readonly ILogger<ProjectFiles> _logger;
    // Database context for accessing project file records
    private readonly ApplicationDbContext _applicationDbContext;
    // Cancellation token source for cooperative cancellation of file I/O operations
    private readonly CancellationTokenSource _cts = new();
    /// <summary>
    /// Gets the last error message that occurred.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectFiles"/> class.
    /// </summary>
    /// <param name="logger">The logger for logging errors and information.</param>
    /// <param name="applicationDbContext">The database context for data access.</param>
    public ProjectFiles(ILogger<ProjectFiles> logger, ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _ = _applicationDbContext.InitDBAsync();
    }
    /// <summary>
    /// Creates a new project file asynchronously.
    /// </summary>
    /// <param name="file">The project file to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
    public async Task<bool> CreateAsync(ProjectFile file)
    {
        LastError = string.Empty;
        try
        {
            // Verify the parent project exists in the database
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == file.Project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return false;
            }
            // Ensure no duplicate file name exists (case-insensitive)
            var oldFile = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.FileName.ToLower().Equals(file.FileName.ToLower()));
            if (oldFile != null)
            {
                LastError = "File exists in db";
                return false;
            }
            // Verify the project's base directory exists on disk
            if (!Directory.Exists(oldProject.BasePath))
            {
                LastError = "Base path not found";
                return false;
            }
            // Ensure the file does not already exist on disk
            string filePath = Path.Combine(oldProject.BasePath, file.FileName);
            if (Directory.Exists(filePath))
            {
                LastError = "File already exists";
                return false;
            }
            // Write the file to disk and register it in the database
            file.ProjectId = oldProject.Id;
            await File.WriteAllTextAsync(filePath, String.IsNullOrEmpty(file.Content) ? "Hello world!" : file.Content);
            await _applicationDbContext.InsertAsync(file);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(LastError);
        }
        return false;
    }
    /// <summary>
    /// Reads the content of a project file from disk asynchronously.
    /// If the file cannot be read from disk, falls back to the database-stored backup content.
    /// </summary>
    /// <param name="projectFile">The project file metadata (contains filename and backup content).</param>
    /// <param name="project">The parent project (provides the base path for file resolution).</param>
    /// <returns>A new <see cref="ProjectFile"/> instance with the file content populated from disk or backup.</returns>
    public async Task<ProjectFile> ReadFileContentAsync(ProjectFile projectFile, Project project)
    {
        string fileContent = string.Empty;
        try
        {
            // Attempt to read the file content from the filesystem
            fileContent = await File.ReadAllTextAsync(Path.Combine(project.BasePath, projectFile.FileName), _cts.Token);
        }
        catch(Exception ex)
        {
            // On failure, prepend an error message and fall back to the DB-stored backup content
            fileContent = $"Something fail while openning the file: {projectFile.FileName}, the next file content is a backup made by the app, consider save it. Error: {ex.Message}\n" + projectFile.Content;
        }
        // Return a new ProjectFile instance with the resolved content and parent project reference
        return new()
        {
            Id = projectFile.Id,
            FileName = projectFile.FileName,
            Content = fileContent,
            ProjectId = project.Id,
            Project = project
        };
    }
    /// <summary>
    /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
    /// </summary>
    public async Task<ProjectFile> GetByAsync(object id) => throw new NotImplementedException();
    /// <summary>
    /// Updates an existing project file asynchronously.
    /// </summary>
    /// <param name="item">The project file to update.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
    public async Task<bool> UpdateAsync(ProjectFile item)
    {
        LastError = string.Empty;
        try
        {
            // Verify the file record exists in the database
            var oldFile = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.Id == item.Id);
            if (oldFile == null)
            {
                LastError = "File not found, consider save it first";
                return false;
            }
            // Verify the physical file exists on disk
            string path = Path.Join(item.Project.BasePath, item.FileName);
            if (!File.Exists(path))
            {
                LastError = "File not found in disk";
                return false;
            }
            // Update both the file on disk and the backup content in the database
            oldFile.Content = item.Content;
            await File.WriteAllTextAsync(path, String.IsNullOrEmpty(item.Content) ? "Hello world!" : item.Content);
            await _applicationDbContext.UpdateAsync(oldFile);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex.Message);
        }
        return false;
    }
    /// <summary>
    /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
    /// </summary>
    public async Task<bool> RemoveAsync(ProjectFile item) => throw new NotImplementedException();
    /// <summary>
    /// Gets a list of all project files for a given project asynchronously.
    /// </summary>
    /// <param name="project">The project to get the files for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of project files.</returns>
    public async Task<List<ProjectFile>> GetProjectFilesAsync(Project project)
    {
        LastError = string.Empty;
        try
        {
            // Verify the project exists in the database
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return new();
            }
            // Fetch all file records associated with this project
            var projectFiles = await _applicationDbContext.ProjectFiles
                .Where(p => p.ProjectId == project.Id)
                .ToListAsync();
            // Read file contents from disk in parallel for all project files
            var tasks = projectFiles.Select(p => ReadFileContentAsync(p, oldProject));
            return (await Task.WhenAll(tasks)).ToList();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(LastError);
        }
        return new();
    }
    /// <summary>
    /// Gets a list of all project files asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of project files.</returns>
    public async Task<List<ProjectFile>> GetListAsync()
    {
        LastError = string.Empty;
        try
        {
            return await _applicationDbContext.ProjectFiles.ToListAsync();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(LastError);
        }
        return new();
    }
    /// <summary>
    /// Releases the resources used by the <see cref="ProjectFiles"/> class.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}