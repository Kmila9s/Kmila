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
    // Logger for recording errors and diagnostic information
    private readonly ILogger<Projects> _logger;
    // Database context for accessing project records
    private readonly ApplicationDbContext _applicationDbContext;
    // Cancellation token source for cooperative cancellation of async operations
    private readonly CancellationTokenSource _cts = new();
    // Handle to the one-time DB init the constructor kicks off. Public async
    // methods `await` this before touching the DB so a slow init (Android
    // package-copy + PRAGMA setup) can't lose a race with the very first
    // GetListAsync from OnInitializedAsync — F-A-05.
    private readonly Task _initTask;
    /// <summary>
    /// Gets the last error message that occurred.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Fires after any successful Create / Remove / Update. Any UI surface
    /// that displays the project list (e.g. the NavMenu sidebar) subscribes
    /// to this to stay in sync without polling.
    /// </summary>
    public event Action? OnListChanged;
    /// <summary>
    /// Initializes a new instance of the <see cref="Projects"/> class.
    /// </summary>
    /// <param name="logger">The logger for logging errors and information.</param>
    /// <param name="applicationDbContext">The database context for data access.</param>
    public Projects(ILogger<Projects> logger, ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _initTask = _applicationDbContext.InitDBAsync();
    }
    /// <summary>
    /// Creates a new project asynchronously.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
    public async Task<bool> CreateAsync(Project project)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            // Check for duplicate project (same name and path, case-insensitive)
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p =>
                p.Name.ToLower().Equals(project.Name.ToLower()) && p.BasePath.ToLower().Equals(project.BasePath.ToLower())
            );
            if (oldProject != null)
            {
                LastError = "Project and path already registered";
                return false;
            }
            // Create the project directory on disk and register in the database
            Directory.CreateDirectory(project.BasePath);
            await _applicationDbContext.InsertAsync(project);
            OnListChanged?.Invoke();
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
        await _initTask;
        LastError = string.Empty;
        try
        {
            // Verify the project exists in the database
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (oldProject == null)
            {
                LastError = "Project not registered";
                return false;
            }
            // Ensure no other project is using the same base path (case-insensitive)
            if ((await _applicationDbContext.Projects.ToListAsync()).Any(p => p.BasePath.ToLower() == project.BasePath.ToLower() && p.Id != project.Id))
            {
                LastError = "Path already in use";
                return false;
            }
            // If the base path changed, move the project directory on disk
            if (oldProject.BasePath.ToLower() != project.BasePath.ToLower())
            {
                if (!Directory.Exists(oldProject.BasePath))
                {
                    LastError = "Old directory not found";
                    return false;
                }
                Directory.Move(oldProject.BasePath, project.BasePath);
            }
            // Update the project metadata in the database
            oldProject.Name = project.Name;
            oldProject.Description = project.Description;
            oldProject.BasePath = project.BasePath;
            await _applicationDbContext.UpdateAsync(project);
            OnListChanged?.Invoke();
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
        await _initTask;
        LastError = string.Empty;
        try
        {
            // Verify the project exists in the database
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return false;
            }
            // Delete the project directory and all its contents from disk — but
            // only if no other project record still points at the same folder.
            // Legacy rows (the Learn sample flow used to reuse a single folder
            // per lesson) can share a BasePath; wiping it out here would
            // destroy the other project's files.
            if (Directory.Exists(oldProject.BasePath))
            {
                var otherReferences = await _applicationDbContext.Projects
                    .Where(p => p.Id != oldProject.Id &&
                                p.BasePath.ToLower() == oldProject.BasePath.ToLower())
                    .ToListAsync();
                if (otherReferences.Count == 0)
                {
                    Directory.Delete(oldProject.BasePath, true);
                }
            }
            // Remove the project record from the database
            await _applicationDbContext.DeleteAsync(project);
            OnListChanged?.Invoke();
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
        await _initTask;
        LastError = string.Empty;
        try
        {
            var list = await _applicationDbContext.Projects.ToListAsync();
            foreach (var p in list) await RehomeIfStaleAsync(p);
            return list;
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
        await _initTask;
        LastError = string.Empty;
        try
        {
            Project project = null!;
            // Supports lookup by integer ID or string name (case-insensitive)
            if (id is int intId)
                project = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == intId);
            else if(id is string strId)
                project = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Name.ToLower() == strId.ToLower());
            if (project == null)
            {
                LastError = "Project not found";
            }
            else
            {
                await RehomeIfStaleAsync(project);
            }
            // Return the found project or an empty default if not found
            return project ?? new();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        return null!;
    }

    /// <summary>
    /// Repairs sandbox-container drift. On iOS/Android (and macCatalyst in some cases),
    /// the app's data-container UUID changes after a reinstall, so the absolute
    /// <see cref="Project.BasePath"/> stored in the DB stops resolving even though the
    /// project folder still lives under the new container at the same relative location
    /// (<c>{AppDataDirectory}/Projects/{folder}</c>). When that's the case we rewrite the
    /// row to point at the live path. No-op when the stored path already exists.
    /// </summary>
    private async Task RehomeIfStaleAsync(Project project)
    {
        // Belt-and-braces: this method is called for every row on every list
        // load. Any filesystem hiccup used to bubble up and empty the whole
        // list — worse, on Android WebView it could leave the /projects
        // skeleton spinning forever (audit F-A-05, 2026-07-09). Now we swallow
        // everything and just log.
        try
        {
            if (project == null || string.IsNullOrEmpty(project.BasePath)) return;
            if (Directory.Exists(project.BasePath)) return;

            var folder = Path.GetFileName(project.BasePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(folder)) return;

            string root;
#if WINDOWS || MACCATALYST || ANDROID || IOS
            root = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
#else
            // Blazor WebAssembly persists under /kmila in MEMFS (see UserAssetStore).
            root = OperatingSystem.IsBrowser()
                ? "/kmila"
                : Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "KmilaData");
#endif
            var projectsRoot = Path.Combine(root, "Projects");
            var candidate = Path.Combine(projectsRoot, folder);

            // Fallback 1: case-insensitive lookup — reinstall on Android can
            // change casing when SAF-backed paths are involved.
            if (!Directory.Exists(candidate) && Directory.Exists(projectsRoot))
            {
                var hit = Directory.EnumerateDirectories(projectsRoot)
                    .FirstOrDefault(d => string.Equals(Path.GetFileName(d), folder, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(hit)) candidate = hit;
            }

            // Fallback 2: match by project.Name against subdirs — covers the
            // rare case where the persisted folder name drifts from the row.
            if (!Directory.Exists(candidate) && Directory.Exists(projectsRoot) && !string.IsNullOrEmpty(project.Name))
            {
                var hit = Directory.EnumerateDirectories(projectsRoot)
                    .FirstOrDefault(d => Path.GetFileName(d).Contains(project.Name!, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(hit)) candidate = hit;
            }

            if (!Directory.Exists(candidate))
            {
                _logger.LogWarning($"[Projects] '{project.Name}' (Id={project.Id}) has stale BasePath '{project.BasePath}' and no fallback under '{projectsRoot}' matched.");
                return;
            }

            var stale = project.BasePath;
            project.BasePath = candidate;
            await _applicationDbContext.UpdateAsync(project);
            _logger.LogInformation($"[Projects] Rehomed '{project.Name}': '{stale}' -> '{candidate}'");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[Projects] RehomeIfStaleAsync failed for '{project?.Name}': {ex.Message}");
        }
    }
    /// <summary>
    /// Releases the resources used by the <see cref="Projects"/> class.
    /// Cancels any pending operations and disposes the CancellationTokenSource.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}