using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kmila.Shared.Services.Export;
using Microsoft.Extensions.Logging;

#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
#endif

namespace Kmila.Services;

public sealed class MauiFileExportService : IFileExportService
{
    private readonly ILogger<MauiFileExportService>? _logger;

    public MauiFileExportService(ILogger<MauiFileExportService>? logger = null)
        => _logger = logger;

    public Task<FileExportResult> SaveTextAsync(
        ExportCategory category,
        string fileName,
        string mime,
        string content,
        CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        return SaveBytesAsync(category, fileName, mime, bytes, ct);
    }

    public Task<FileExportResult> SaveBytesAsync(
        ExportCategory category,
        string fileName,
        string mime,
        byte[] bytes,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(FileExportResult.Failure("File name is required."));

        var safeName = SanitizeFileName(fileName);

        try
        {
#if ANDROID
            return Task.FromResult(WriteAndroid(category, safeName, mime, bytes));
#else
            return Task.FromResult(WriteSandboxDocuments(category, safeName, bytes));
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Export failed for {Category}/{File}", category, safeName);
            return Task.FromResult(FileExportResult.Failure(ex.Message));
        }
    }

    private FileExportResult WriteSandboxDocuments(ExportCategory category, string fileName, byte[] bytes)
    {
        var docs = ResolveDocumentsRoot();
        if (string.IsNullOrEmpty(docs))
            return FileExportResult.Failure("Documents folder is unavailable on this device.");

        var dir = Path.Combine(docs, ExportCategoryFolders.RootFolder, ExportCategoryFolders.FolderFor(category));
        Directory.CreateDirectory(dir);

        var resolved = ExportCategoryFolders.ResolveUniqueName(dir, fileName);
        var full = Path.Combine(dir, resolved);
        File.WriteAllBytes(full, bytes);
        return FileExportResult.Success(full);
    }

    /// <summary>
    /// Resolves the Documents root the user can actually reach with Finder /
    /// Files.app on each target.
    /// <list type="bullet">
    ///   <item><b>iOS</b>: the app sandbox Documents folder. Combined with
    ///   <c>UIFileSharingEnabled</c> + <c>LSSupportsOpeningDocumentsInPlace</c>
    ///   in Info.plist, this surfaces as "On My iPhone → Kmila" in Files.app.</item>
    ///   <item><b>macCatalyst</b>: the user's real <c>~/Documents</c>. The app
    ///   ships with the sandbox disabled (MacCatalyst/Entitlements.plist, wired
    ///   into the csproj via <c>CodesignEntitlements</c>). Even so, <c>HOME</c>
    ///   and <c>SpecialFolder.MyDocuments</c> can still resolve to the per-app
    ///   container; deriving the home from the login user name
    ///   (<c>/Users/&lt;user&gt;</c>) is immune to that redirection, so we try
    ///   that first and only accept a path that isn't inside a sandbox
    ///   container.</item>
    ///   <item><b>Windows</b>: the user profile's Documents folder via
    ///   <c>SpecialFolder.MyDocuments</c>.</item>
    /// </list>
    /// </summary>
    private static string? ResolveDocumentsRoot()
    {
#if MACCATALYST
        // Ordered candidates for the *real* (Finder-visible) home. The login
        // name maps to /Users/<user> on macOS regardless of sandbox/container
        // redirection; HOME is the fallback when it doesn't.
        foreach (var home in RealHomeCandidates())
        {
            if (string.IsNullOrEmpty(home)) continue;
            // Skip a per-app container path — writing there "succeeds" but the
            // user never sees the file in Finder (the original bug).
            if (home.Contains("/Library/Containers/", StringComparison.Ordinal)) continue;
            return Path.Combine(home, "Documents");
        }
#endif
        return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
    }

#if MACCATALYST
    private static System.Collections.Generic.IEnumerable<string?> RealHomeCandidates()
    {
        var user = Environment.UserName;
        if (!string.IsNullOrEmpty(user)) yield return "/Users/" + user;
        yield return Environment.GetEnvironmentVariable("HOME");
    }
#endif

#if ANDROID
    private FileExportResult WriteAndroid(ExportCategory category, string fileName, string mime, byte[] bytes)
    {
        var folder = ExportCategoryFolders.FolderFor(category);

        // Android 10+ (API 29+) uses scoped storage via MediaStore. No
        // runtime permission needed; the user sees the file in the system
        // Files app under Documents/Kmila/<Folder>/.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            var context = Android.App.Application.Context;
            var resolver = context.ContentResolver
                           ?? throw new InvalidOperationException("ContentResolver unavailable.");

            var relPath = $"{Android.OS.Environment.DirectoryDocuments}/{ExportCategoryFolders.RootFolder}/{folder}/";

            // Resolve filename collisions by listing existing entries.
            var unique = ResolveUniqueMediaStoreName(resolver, relPath, fileName);

            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, unique);
            values.Put(MediaStore.IMediaColumns.MimeType, string.IsNullOrEmpty(mime) ? "application/octet-stream" : mime);
            values.Put(MediaStore.IMediaColumns.RelativePath, relPath);

            var collection = MediaStore.Files.GetContentUri("external")
                             ?? throw new InvalidOperationException("MediaStore Files URI unavailable.");
            var uri = resolver.Insert(collection, values)
                      ?? throw new InvalidOperationException("Failed to create MediaStore entry.");

            using (var stream = resolver.OpenOutputStream(uri, "w")
                                ?? throw new InvalidOperationException("Failed to open output stream."))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }

            return FileExportResult.Success($"Documents/{ExportCategoryFolders.RootFolder}/{folder}/{unique}");
        }

        // Legacy path (API ≤ 28): direct write to the public Documents folder.
        // Requires WRITE_EXTERNAL_STORAGE which we declare with
        // maxSdkVersion=28 in AndroidManifest.xml.
        var legacyDocs = Android.OS.Environment.GetExternalStoragePublicDirectory(
                             Android.OS.Environment.DirectoryDocuments)?.AbsolutePath
                         ?? throw new InvalidOperationException("External Documents directory unavailable.");
        var legacyDir = Path.Combine(legacyDocs, ExportCategoryFolders.RootFolder, folder);
        Directory.CreateDirectory(legacyDir);

        var legacyResolved = ExportCategoryFolders.ResolveUniqueName(legacyDir, fileName);
        var legacyFull = Path.Combine(legacyDir, legacyResolved);
        File.WriteAllBytes(legacyFull, bytes);
        return FileExportResult.Success(legacyFull);
    }

    private static string ResolveUniqueMediaStoreName(ContentResolver resolver, string relativePath, string fileName)
    {
        // Query MediaStore for entries already at relativePath with matching
        // display names; if any, append -2, -3, ... to dodge silent overwrite
        // (MediaStore.Insert with a duplicate name auto-suffixes anyway, but
        // the auto-suffix is implementation-defined, so we control it).
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext  = Path.GetExtension(fileName);
        var candidate = fileName;
        for (int i = 2; i < 1000; i++)
        {
            if (!MediaStoreEntryExists(resolver, relativePath, candidate))
                return candidate;
            candidate = $"{stem}-{i}{ext}";
        }
        return $"{stem}-{Guid.NewGuid():N}{ext}";
    }

    private static bool MediaStoreEntryExists(ContentResolver resolver, string relativePath, string displayName)
    {
        var uri = MediaStore.Files.GetContentUri("external");
        if (uri == null) return false;
        var projection = new[] { MediaStore.IMediaColumns.DisplayName! };
        var selection = $"{MediaStore.IMediaColumns.RelativePath}=? AND {MediaStore.IMediaColumns.DisplayName}=?";
        var args = new[] { relativePath, displayName };
        using var cursor = resolver.Query(uri, projection, selection, args, sortOrder: null);
        return cursor != null && cursor.Count > 0;
    }
#endif

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        var clean = sb.ToString().Trim();
        return string.IsNullOrEmpty(clean) ? "kmila-export" : clean;
    }
}
