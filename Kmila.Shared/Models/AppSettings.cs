using SQLite;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents application settings stored in the SQLite database.
/// Uses a key-value pair approach for flexibility.
/// </summary>
[Table("AppSettings")]
public class AppSettings
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the setting key.
    /// </summary>
    [Unique]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    #region Setting Keys Constants

    /// <summary>
    /// Key for storing the selected language.
    /// </summary>
    public const string KEY_LANGUAGE = "KMILA_LANGUAGE";

    /// <summary>
    /// Key for storing the selected theme.
    /// </summary>
    public const string KEY_THEME = "KMILA_THEME";

    #endregion
}
