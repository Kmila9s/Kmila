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
    /// Gets or sets the name of the file.
    /// </summary>
    [Required(ErrorMessage = "File name required")]
    public string FileName { get; set; } = string.Empty;
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
}
