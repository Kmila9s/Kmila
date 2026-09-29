using System.Threading;
using System.Threading.Tasks;

namespace Kmila.Shared.Services.Export;

/// <summary>Category of exported file, used to route the file into a sub-folder.</summary>
public enum ExportCategory
{
    /// <summary>Exported practice exercises.</summary>
    Practices,
    /// <summary>Exported projects.</summary>
    Projects,
    /// <summary>Exported log files.</summary>
    Logs,
    /// <summary>Exported simulation runs.</summary>
    Runs,
    /// <summary>Exported SVG images.</summary>
    Svg,
    /// <summary>Exported PNG images.</summary>
    Png,
    /// <summary>Exported UML diagrams.</summary>
    Uml,
    /// <summary>Exported VCD (Value Change Dump) waveform files.</summary>
    Vcd,
    /// <summary>Exported language definition files.</summary>
    Languages,
    /// <summary>Exported settings.</summary>
    Settings,
}

/// <summary>Abstraction for saving exported text or binary files to platform storage.</summary>
public interface IFileExportService
{
    /// <summary>Saves text content as a file under the given export category.</summary>
    /// <param name="category">Category that determines the destination sub-folder.</param>
    /// <param name="fileName">Suggested file name.</param>
    /// <param name="mime">MIME type of the content.</param>
    /// <param name="content">Text content to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The outcome of the save operation.</returns>
    Task<FileExportResult> SaveTextAsync(ExportCategory category, string fileName, string mime, string content, CancellationToken ct = default);
    /// <summary>Saves binary content as a file under the given export category.</summary>
    /// <param name="category">Category that determines the destination sub-folder.</param>
    /// <param name="fileName">Suggested file name.</param>
    /// <param name="mime">MIME type of the content.</param>
    /// <param name="bytes">Binary content to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The outcome of the save operation.</returns>
    Task<FileExportResult> SaveBytesAsync(ExportCategory category, string fileName, string mime, byte[] bytes, CancellationToken ct = default);
}

/// <summary>Result of a file export attempt.</summary>
/// <param name="Saved">Whether the file was saved successfully.</param>
/// <param name="Path">Path of the saved file, when available.</param>
/// <param name="Error">Error message when the save failed.</param>
public sealed record FileExportResult(bool Saved, string? Path, string? Error)
{
    /// <summary>Creates a successful result for the given saved path.</summary>
    /// <param name="path">Path of the saved file, if known.</param>
    /// <returns>A successful <see cref="FileExportResult"/>.</returns>
    public static FileExportResult Success(string? path) => new(true, path, null);
    /// <summary>Creates a failed result with the given error message.</summary>
    /// <param name="error">Description of the failure.</param>
    /// <returns>A failed <see cref="FileExportResult"/>.</returns>
    public static FileExportResult Failure(string error) => new(false, null, error);
}

/// <summary>Maps export categories to folder names and resolves export file paths.</summary>
public static class ExportCategoryFolders
{
    /// <summary>Returns the folder name used for the given category.</summary>
    /// <param name="category">Export category.</param>
    /// <returns>The folder name for the category.</returns>
    public static string FolderFor(ExportCategory category) => category switch
    {
        ExportCategory.Practices => "Practices",
        ExportCategory.Projects  => "Projects",
        ExportCategory.Logs      => "Logs",
        ExportCategory.Runs      => "Runs",
        ExportCategory.Svg       => "SVG",
        ExportCategory.Png       => "PNG",
        ExportCategory.Uml       => "UML",
        ExportCategory.Vcd       => "VCD",
        ExportCategory.Languages => "Languages",
        ExportCategory.Settings  => "Settings",
        _ => "Other",
    };

    /// <summary>Root folder name under which all exports are grouped.</summary>
    public const string RootFolder = "Kmila";

    /// <summary>Builds the export-relative path for a file of the given category.</summary>
    /// <param name="category">Export category that selects the sub-folder.</param>
    /// <param name="fileName">File name to append.</param>
    /// <returns>The path relative to the export root.</returns>
    public static string RelativePath(ExportCategory category, string fileName)
        => System.IO.Path.Combine(RootFolder, FolderFor(category), fileName);

    /// <summary>Returns a file name that does not collide with an existing file in the directory, appending a numeric or GUID suffix as needed.</summary>
    /// <param name="directory">Target directory to check for collisions.</param>
    /// <param name="fileName">Desired file name.</param>
    /// <returns>A unique file name within the directory.</returns>
    public static string ResolveUniqueName(string directory, string fileName)
    {
        if (!System.IO.Directory.Exists(directory))
            return fileName;

        var full = System.IO.Path.Combine(directory, fileName);
        if (!System.IO.File.Exists(full))
            return fileName;

        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var ext  = System.IO.Path.GetExtension(fileName);
        for (int i = 2; i < 1000; i++)
        {
            var candidate = $"{stem}-{i}{ext}";
            if (!System.IO.File.Exists(System.IO.Path.Combine(directory, candidate)))
                return candidate;
        }
        return $"{stem}-{System.Guid.NewGuid():N}{ext}";
    }
}
