using Kmila.Shared.Data;
using Kmila.Shared.Interfaces;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Repositories;

public class ProjectFiles : IRepository<ProjectFile>
{
    private readonly ILogger<ProjectFiles> _logger;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly CancellationTokenSource _cts = new();
    public string LastError { get; private set; } = string.Empty;
    public ProjectFiles(ILogger<ProjectFiles> logger, ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _ = _applicationDbContext.InitDBAsync();
    }
    public async Task<bool> CreateAsync(ProjectFile file)
    {
        LastError = string.Empty;
        try
        {
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == file.Project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return false;
            }
            var oldFile = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.FileName.ToLower().Equals(file.FileName.ToLower()));
            if (oldFile != null)
            {
                LastError = "File exists in db";
                return false;
            }
            if (!Directory.Exists(oldProject.BasePath))
            {
                LastError = "Base path not found";
                return false;
            }
            string filePath = Path.Combine(oldProject.BasePath, file.FileName);
            if (Directory.Exists(filePath))
            {
                LastError = "File already exists";
                return false;
            }
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
    private async Task<ProjectFile> ReadFileContentAsync(ProjectFile projectFile, Project project)
    {
        string fileContent = string.Empty;
        try
        {
            fileContent = await File.ReadAllTextAsync(Path.Combine(project.BasePath, projectFile.FileName), _cts.Token);
        }
        catch(Exception ex)
        {
            fileContent = $"Something fail while openning the file: {projectFile.FileName}, the next file content is a backup made by the app, consider save it. Error: {ex.Message}\n" + projectFile.Content;
        }
        return new()
        {
            Id = projectFile.Id,
            FileName = projectFile.FileName,
            Content = fileContent,
            ProjectId = project.Id,
            Project = project
        };
    }
    public async Task<ProjectFile> GetByAsync(object id) => throw new NotImplementedException();
    public async Task<bool> UpdateAsync(ProjectFile item) => throw new NotImplementedException();
    public async Task<bool> RemoveAsync(ProjectFile item) => throw new NotImplementedException();
    public async Task<List<ProjectFile>> GetProjectFilesAsync(Project project)
    {
        LastError = string.Empty;
        try
        {
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return new();
            }
            var projectFiles = await _applicationDbContext.ProjectFiles
                .Where(p => p.ProjectId == project.Id)
                .ToListAsync();
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
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}