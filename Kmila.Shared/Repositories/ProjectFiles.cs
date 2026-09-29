using Kmila.Shared.Data;
using Kmila.Shared.Interfaces;
using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Repositories;
/// <summary>
/// Repository for managing project files, implementing the <see cref="IRepository{T}"/> interface for <see cref="ProjectFile"/>.
/// Files can live in sub-directories — see <see cref="ProjectFile.DirectoryPath"/>. The repository keeps the DB row
/// and the file on disk in sync; create/move/delete operations propagate to both.
/// </summary>
public class ProjectFiles : IRepository<ProjectFile>
{
    // Logger for recording errors and diagnostic information
    private readonly ILogger<ProjectFiles> _logger;
    // Database context for accessing project file records
    private readonly ApplicationDbContext _applicationDbContext;
    // Cancellation token source for cooperative cancellation of file I/O operations
    private readonly CancellationTokenSource _cts = new();
    // Handle to the one-time DB init the constructor kicks off. Public async
    // methods `await` this before touching the DB so a slow init can't lose a
    // race with the first call. Same pattern as Projects.cs — session 10
    // fixed F-A-05, session 12 closed this latent copy in ProjectFiles.
    private readonly Task _initTask;
    /// <summary>
    /// Gets the last error message that occurred.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectFiles"/> class.
    /// </summary>
    public ProjectFiles(ILogger<ProjectFiles> logger, ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _initTask = _applicationDbContext.InitDBAsync();
    }

    /// <summary>
    /// Normalizes a directory path: trims, replaces backslashes, strips leading/trailing
    /// slashes, and rejects path traversal segments. Empty result means project root.
    /// </summary>
    public static string NormalizeDirectoryPath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw.Replace('\\', '/').Trim().Trim('/');
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // Reject ".." traversal — easier than resolving a normalized form here.
        var segments = s.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(seg => seg == "." || seg == ".."))
            return string.Empty;
        return string.Join('/', segments);
    }

    /// <summary>Combines BasePath with the file's DirectoryPath and FileName for disk I/O.</summary>
    private static string GetDiskPath(string basePath, ProjectFile file)
        => GetDiskPath(basePath, file.DirectoryPath, file.FileName);

    /// <summary>Combines BasePath + (optional) directoryPath + fileName.</summary>
    private static string GetDiskPath(string basePath, string directoryPath, string fileName)
    {
        if (string.IsNullOrEmpty(directoryPath))
            return Path.Combine(basePath, fileName);
        // Translate forward slashes to OS separators via Combine on each segment.
        var parts = new List<string> { basePath };
        parts.AddRange(directoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        parts.Add(fileName);
        return Path.Combine(parts.ToArray());
    }

    private static string GetDiskDirectory(string basePath, string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath)) return basePath;
        var parts = new List<string> { basePath };
        parts.AddRange(directoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(parts.ToArray());
    }

    /// <summary>
    /// Creates a new project file asynchronously. The parent directory is created on disk
    /// when missing so callers can write into a fresh sub-folder without a separate step.
    /// </summary>
    public async Task<bool> CreateAsync(ProjectFile file)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            var oldProject = await _applicationDbContext.Projects.FirstOrDefaultAsync(p => p.Id == file.Project.Id);
            if (oldProject == null)
            {
                LastError = "Project not found";
                return false;
            }

            file.DirectoryPath = NormalizeDirectoryPath(file.DirectoryPath);

            // Duplicate scoped to (project, directory, filename) — case-insensitive
            // so "Foo.vhd" and "foo.vhd" don't collide on case-insensitive filesystems.
            var oldFile = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p =>
                p.ProjectId == oldProject.Id &&
                p.DirectoryPath.ToLower().Equals(file.DirectoryPath.ToLower()) &&
                p.FileName.ToLower().Equals(file.FileName.ToLower()));
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

            var dirOnDisk = GetDiskDirectory(oldProject.BasePath, file.DirectoryPath);
            if (!Directory.Exists(dirOnDisk))
                Directory.CreateDirectory(dirOnDisk);

            string filePath = GetDiskPath(oldProject.BasePath, file);
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

    /// <summary>
    /// Reads file content from disk; falls back to the DB-stored backup on failure.
    /// </summary>
    public async Task<ProjectFile> ReadFileContentAsync(ProjectFile projectFile, Project project)
    {
        string fileContent = string.Empty;
        try
        {
            fileContent = await File.ReadAllTextAsync(GetDiskPath(project.BasePath, projectFile), _cts.Token);
        }
        catch (Exception)
        {
            // Disk read failed (file missing / permissions / etc). Fall back to
            // the DB-stored backup content cleanly — don't inject the error
            // message into the editor body. A caller-side banner / toast is the
            // right surface for that. Saving from the editor will re-create the
            // disk file from this content.
            fileContent = projectFile.Content ?? string.Empty;
        }
        return new()
        {
            Id = projectFile.Id,
            FileName = projectFile.FileName,
            DirectoryPath = projectFile.DirectoryPath,
            Content = fileContent,
            ProjectId = project.Id,
            Project = project
        };
    }

    /// <summary>Not supported for project files; always throws.</summary>
    /// <param name="id">Ignored.</param>
    /// <returns>Never returns; throws <see cref="NotImplementedException"/>.</returns>
    public async Task<ProjectFile> GetByAsync(object id) { await Task.CompletedTask; throw new NotImplementedException(); }

    /// <summary>
    /// Updates a project file's content on disk and refreshes the DB backup.
    /// </summary>
    public async Task<bool> UpdateAsync(ProjectFile item)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            var oldFile = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.Id == item.Id);
            if (oldFile == null)
            {
                LastError = "File not found, consider save it first";
                return false;
            }
            string path = GetDiskPath(item.Project.BasePath, oldFile);
            if (!File.Exists(path))
            {
                LastError = "File not found in disk";
                return false;
            }
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
    /// Removes a project file from the database and the filesystem. Missing-on-disk is
    /// logged but does not fail the DB removal — a stale file is less bad than a zombie row.
    /// </summary>
    public async Task<bool> RemoveAsync(ProjectFile item)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            var existing = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.Id == item.Id);
            if (existing == null)
            {
                LastError = "File not found in database";
                return false;
            }

            if (item.Project != null && !string.IsNullOrEmpty(item.Project.BasePath))
            {
                try
                {
                    var diskPath = GetDiskPath(item.Project.BasePath, existing);
                    if (File.Exists(diskPath))
                        File.Delete(diskPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[ProjectFiles] Failed to delete file from disk (continuing with DB removal): {ex.Message}");
                }
            }

            await _applicationDbContext.DeleteAsync(existing);
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
    /// Renames a project file on disk and updates the DB row. The file stays in the same
    /// directory — use <see cref="MoveAsync"/> to change its parent folder.
    /// </summary>
    public async Task<bool> RenameAsync(ProjectFile item, string newFileName)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                LastError = "File name is empty";
                return false;
            }
            newFileName = newFileName.Trim();
            // Forbid path separators in the new name — that's a Move, not a Rename.
            if (newFileName.Contains('/', System.StringComparison.Ordinal) || newFileName.Contains('\\', System.StringComparison.Ordinal))
            {
                LastError = "File name cannot contain path separators";
                return false;
            }

            var existing = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.Id == item.Id);
            if (existing == null)
            {
                LastError = "File not found in database";
                return false;
            }

            if (string.Equals(existing.FileName, newFileName, StringComparison.OrdinalIgnoreCase))
                return true;

            var conflict = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p =>
                p.ProjectId == existing.ProjectId &&
                p.Id != existing.Id &&
                p.DirectoryPath.ToLower().Equals(existing.DirectoryPath.ToLower()) &&
                p.FileName.ToLower().Equals(newFileName.ToLower()));
            if (conflict != null)
            {
                LastError = "A file with that name already exists";
                return false;
            }

            if (item.Project != null && !string.IsNullOrEmpty(item.Project.BasePath))
            {
                var oldPath = GetDiskPath(item.Project.BasePath, existing);
                var newPath = GetDiskPath(item.Project.BasePath, existing.DirectoryPath, newFileName);
                if (File.Exists(oldPath))
                {
                    if (File.Exists(newPath))
                    {
                        LastError = "Target file already exists on disk";
                        return false;
                    }
                    File.Move(oldPath, newPath);
                }
            }

            existing.FileName = newFileName;
            await _applicationDbContext.UpdateAsync(existing);
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
    /// Moves a file to a different sub-directory inside the same project. Creates the
    /// target folder on disk when missing. Empty path = project root.
    /// </summary>
    public async Task<bool> MoveAsync(ProjectFile item, string newDirectoryPath)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            var normalized = NormalizeDirectoryPath(newDirectoryPath);

            var existing = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p => p.Id == item.Id);
            if (existing == null)
            {
                LastError = "File not found in database";
                return false;
            }

            if (string.Equals(existing.DirectoryPath, normalized, StringComparison.OrdinalIgnoreCase))
                return true;

            var conflict = await _applicationDbContext.ProjectFiles.FirstOrDefaultAsync(p =>
                p.ProjectId == existing.ProjectId &&
                p.Id != existing.Id &&
                p.DirectoryPath.ToLower().Equals(normalized.ToLower()) &&
                p.FileName.ToLower().Equals(existing.FileName.ToLower()));
            if (conflict != null)
            {
                LastError = "A file with that name already exists in target folder";
                return false;
            }

            if (item.Project != null && !string.IsNullOrEmpty(item.Project.BasePath))
            {
                var basePath = item.Project.BasePath;
                var targetDir = GetDiskDirectory(basePath, normalized);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                var oldPath = GetDiskPath(basePath, existing);
                var newPath = GetDiskPath(basePath, normalized, existing.FileName);
                if (File.Exists(oldPath))
                {
                    if (File.Exists(newPath))
                    {
                        LastError = "Target file already exists on disk";
                        return false;
                    }
                    File.Move(oldPath, newPath);
                }
            }

            existing.DirectoryPath = normalized;
            await _applicationDbContext.UpdateAsync(existing);
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
    /// Creates an empty sub-directory under the project's BasePath. The DB has no concept of
    /// directories — this is purely a disk operation so the folder shows up in the tree even
    /// when it has no files yet.
    /// </summary>
    public async Task<bool> CreateDirectoryAsync(Project project, string directoryPath)
    {
        LastError = string.Empty;
        try
        {
            var normalized = NormalizeDirectoryPath(directoryPath);
            if (string.IsNullOrEmpty(normalized))
            {
                LastError = "Directory path is empty";
                return false;
            }
            if (string.IsNullOrEmpty(project.BasePath) || !Directory.Exists(project.BasePath))
            {
                LastError = "Base path not found";
                return false;
            }
            var fullPath = GetDiskDirectory(project.BasePath, normalized);
            if (Directory.Exists(fullPath))
            {
                LastError = "Directory already exists";
                return false;
            }
            Directory.CreateDirectory(fullPath);
            await Task.CompletedTask;
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
    /// Recursively removes a sub-directory from disk and deletes every DB row whose
    /// DirectoryPath matches or is nested below it. Equivalent to "rm -rf".
    /// </summary>
    public async Task<bool> RemoveDirectoryAsync(Project project, string directoryPath)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            var normalized = NormalizeDirectoryPath(directoryPath);
            if (string.IsNullOrEmpty(normalized))
            {
                LastError = "Cannot remove project root";
                return false;
            }

            var prefix = normalized + "/";
            var rows = await _applicationDbContext.ProjectFiles
                .Where(p => p.ProjectId == project.Id)
                .ToListAsync();
            var toDelete = rows.Where(r =>
                string.Equals(r.DirectoryPath, normalized, StringComparison.OrdinalIgnoreCase) ||
                r.DirectoryPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var row in toDelete)
                await _applicationDbContext.DeleteAsync(row);

            if (!string.IsNullOrEmpty(project.BasePath))
            {
                var fullPath = GetDiskDirectory(project.BasePath, normalized);
                if (Directory.Exists(fullPath))
                {
                    try { Directory.Delete(fullPath, recursive: true); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"[ProjectFiles] Failed to delete directory from disk: {ex.Message}");
                    }
                }
            }
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
    /// Renames a sub-directory and rewrites every nested file's DirectoryPath. Disk move first,
    /// then a single pass over matching DB rows.
    /// </summary>
    public async Task<bool> RenameDirectoryAsync(Project project, string oldPath, string newPath)
    {
        await _initTask;
        LastError = string.Empty;
        try
        {
            var oldNorm = NormalizeDirectoryPath(oldPath);
            var newNorm = NormalizeDirectoryPath(newPath);
            if (string.IsNullOrEmpty(oldNorm) || string.IsNullOrEmpty(newNorm))
            {
                LastError = "Invalid path";
                return false;
            }
            if (string.Equals(oldNorm, newNorm, StringComparison.OrdinalIgnoreCase)) return true;

            if (!string.IsNullOrEmpty(project.BasePath))
            {
                var oldDisk = GetDiskDirectory(project.BasePath, oldNorm);
                var newDisk = GetDiskDirectory(project.BasePath, newNorm);
                if (Directory.Exists(newDisk))
                {
                    LastError = "Target directory already exists";
                    return false;
                }
                if (Directory.Exists(oldDisk))
                {
                    var parent = Path.GetDirectoryName(newDisk);
                    if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                        Directory.CreateDirectory(parent);
                    Directory.Move(oldDisk, newDisk);
                }
            }

            var prefix = oldNorm + "/";
            var rows = await _applicationDbContext.ProjectFiles
                .Where(p => p.ProjectId == project.Id)
                .ToListAsync();
            foreach (var row in rows)
            {
                if (string.Equals(row.DirectoryPath, oldNorm, StringComparison.OrdinalIgnoreCase))
                {
                    row.DirectoryPath = newNorm;
                    await _applicationDbContext.UpdateAsync(row);
                }
                else if (row.DirectoryPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    row.DirectoryPath = newNorm + "/" + row.DirectoryPath.Substring(prefix.Length);
                    await _applicationDbContext.UpdateAsync(row);
                }
            }
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
    /// Returns disk metadata (size, dates, line count) for a file. Throws if the file
    /// is missing on disk — callers should catch and surface a friendly message.
    /// </summary>
    public static ProjectFileMetadata GetMetadata(ProjectFile file, Project project)
    {
        var path = GetDiskPath(project.BasePath, file);
        var info = new System.IO.FileInfo(path);
        long size = info.Exists ? info.Length : 0;
        DateTime created = info.Exists ? info.CreationTime : DateTime.MinValue;
        DateTime modified = info.Exists ? info.LastWriteTime : DateTime.MinValue;
        int lines = 0;
        if (info.Exists)
        {
            try { lines = File.ReadAllLines(path).Length; }
            catch { lines = -1; }
        }
        return new ProjectFileMetadata
        {
            FullPath = path,
            RelativePath = file.RelativePath,
            Exists = info.Exists,
            SizeBytes = size,
            CreatedUtc = created,
            ModifiedUtc = modified,
            LineCount = lines
        };
    }

    /// <summary>
    /// Lists every distinct directory path observed across the project's files. Useful for
    /// the tree UI when a folder was created via <see cref="CreateDirectoryAsync"/> but has
    /// no files yet — combine with on-disk enumeration.
    /// </summary>
    public async Task<List<string>> GetDirectoriesAsync(Project project)
    {
        await _initTask;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var rows = await _applicationDbContext.ProjectFiles
                .Where(p => p.ProjectId == project.Id)
                .ToListAsync();
            foreach (var r in rows)
            {
                if (string.IsNullOrEmpty(r.DirectoryPath)) continue;
                // Add every prefix so "src/cores/uart" implies "src" and "src/cores" too.
                var parts = r.DirectoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i <= parts.Length; i++)
                    set.Add(string.Join('/', parts.Take(i)));
            }

            // Pick up empty directories that were created on disk but have no files yet.
            if (!string.IsNullOrEmpty(project.BasePath) && Directory.Exists(project.BasePath))
            {
                foreach (var dir in Directory.EnumerateDirectories(project.BasePath, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(project.BasePath, dir).Replace('\\', '/');
                    if (!string.IsNullOrEmpty(rel)) set.Add(rel);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[ProjectFiles] GetDirectoriesAsync: {ex.Message}");
        }
        return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Gets a list of all project files for a given project asynchronously.
    /// </summary>
    public async Task<List<ProjectFile>> GetProjectFilesAsync(Project project)
    {
        await _initTask;
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

    /// <summary>
    /// Gets a list of all project files asynchronously.
    /// </summary>
    public async Task<List<ProjectFile>> GetListAsync()
    {
        await _initTask;
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

    /// <summary>Cancels and disposes the background cancellation source.</summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Disk metadata snapshot for a project file. Returned by
/// <see cref="ProjectFiles.GetMetadata"/>.
/// </summary>
public class ProjectFileMetadata
{
    /// <summary>Absolute path to the file on disk.</summary>
    public string FullPath { get; set; } = string.Empty;
    /// <summary>Project-relative path to the file.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Whether the file currently exists on disk.</summary>
    public bool Exists { get; set; }
    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; set; }
    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedUtc { get; set; }
    /// <summary>UTC last-modified timestamp.</summary>
    public DateTime ModifiedUtc { get; set; }
    /// <summary>Number of lines in the file.</summary>
    public int LineCount { get; set; }

    /// <summary>Human-readable size: "1.4 KB", "23.0 MB", etc.</summary>
    public string SizeHuman
    {
        get
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double s = SizeBytes;
            int u = 0;
            while (s >= 1024 && u < units.Length - 1) { s /= 1024; u++; }
            return $"{s:0.##} {units[u]}";
        }
    }
}
