using System.Threading;
using System.Threading.Tasks;

namespace Kmila.Shared.Services.Export;

public enum ExportCategory
{
    Practices,
    Projects,
    Logs,
    Runs,
    Svg,
    Png,
    Uml,
    Vcd,
    Languages,
    Settings,
}

public interface IFileExportService
{
    Task<FileExportResult> SaveTextAsync(ExportCategory category, string fileName, string mime, string content, CancellationToken ct = default);
    Task<FileExportResult> SaveBytesAsync(ExportCategory category, string fileName, string mime, byte[] bytes, CancellationToken ct = default);
}

public sealed record FileExportResult(bool Saved, string? Path, string? Error)
{
    public static FileExportResult Success(string? path) => new(true, path, null);
    public static FileExportResult Failure(string error) => new(false, null, error);
}

public static class ExportCategoryFolders
{
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

    public const string RootFolder = "Kmila";

    public static string RelativePath(ExportCategory category, string fileName)
        => System.IO.Path.Combine(RootFolder, FolderFor(category), fileName);

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
