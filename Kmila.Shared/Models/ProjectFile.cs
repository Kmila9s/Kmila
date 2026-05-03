using System.ComponentModel.DataAnnotations;
using SQLite;

namespace Kmila.Shared.Models;
/// <summary>
/// Represents a file within a project, including its content and metadata.
/// </summary>
public class ProjectFile
{
    /// <summary>
    /// Gets or sets the unique identifier for the project file.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the file (leaf only — no path separators).
    /// </summary>
    [Required(ErrorMessage = "File name required")]
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Sub-directory path inside the project, relative to <see cref="Project.BasePath"/>.
    /// Empty string means the file lives at the project root. Uses forward slashes
    /// (e.g. "src" or "src/cores") regardless of OS — combined with Path.Combine at
    /// I/O time. Added so users can organize files into folders.
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content of the file.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the identifier of the project this file belongs to.
    /// </summary>
    public int ProjectId { get; set; } = -1;
    /// <summary>
    /// Gets or sets the project this file belongs to.
    /// </summary>
    [Ignore]
    public Project Project { get; set; } = default!;

    /// <summary>
    /// Project-relative path joining <see cref="DirectoryPath"/> and <see cref="FileName"/>
    /// using forward slashes. Convenient for entry-point comparisons and display.
    /// </summary>
    [Ignore]
    public string RelativePath =>
        string.IsNullOrEmpty(DirectoryPath) ? FileName : $"{DirectoryPath}/{FileName}";
}
