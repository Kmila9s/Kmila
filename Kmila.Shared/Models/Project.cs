using System.ComponentModel.DataAnnotations;
using SQLite;

namespace Kmila.Shared.Models;
/// <summary>
/// Represents a project, containing its metadata and properties.
/// </summary>
public class Project
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    [Required(ErrorMessage = "Project name required")]
    public string Name { get; set; } = default!;
    /// <summary>
    /// Gets or sets the description of the project.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the base path of the project.
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the creation time of the project.
    /// </summary>
    public DateTime CreationTime { get; set; } = DateTime.Now;
}