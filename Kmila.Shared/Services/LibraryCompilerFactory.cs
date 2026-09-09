using Microsoft.Extensions.Logging;
using LibraryCompiler.Services;

namespace Kmila.Shared.Services;

/// <summary>
/// Shared construction path for the singleton <see cref="LibraryCompiler.Services.LibraryCompiler"/>.
/// Extracted so Kmila.Web/Program.cs, Kmila/MauiProgram.cs and the Phase B
/// embedded API host in the MAUI app (Kmila/Services/EmbeddedApiHost.cs) all
/// wire the same OpenLibrarySource callback + FileSystemBlobStore rooted at
/// <see cref="UserAssetStore.LibraryCacheDir"/>. Two independent instances
/// still share the on-disk cache, so a /simulate call gets a hot IEEE build
/// after the Editor compiles once.
/// </summary>
public static class LibraryCompilerFactory
{
    public static LibraryCompiler.Services.LibraryCompiler Create(
        UserAssetStore userAssets,
        ILogger<FileSystemBlobStore>? blobLogger = null)
    {
        var blobStore = new FileSystemBlobStore(userAssets.LibraryCacheDir, blobLogger);

        return new LibraryCompiler.Services.LibraryCompiler(
            new LibraryCompilerOptions(
                OpenLibrarySource: (key, ct) =>
                {
                    // SourceKey may carry a #package=X fragment when one physical
                    // .vhd declares several packages. Trim to just that package's
                    // bytes so each compiled artifact hashes deterministically
                    // and survives the addition / removal of unrelated packages
                    // in the same file.
                    var (path, package) = UserAssetStore.ParseLibrarySourceKey(key);
                    if (!string.IsNullOrEmpty(path) && Path.IsPathRooted(path) && File.Exists(path))
                    {
                        var bytes = UserAssetStore.ReadPackageBytes(path, package);
                        if (bytes != null) return Task.FromResult<Stream?>(new MemoryStream(bytes));
                    }
                    // Embedded-resource key for IEEE builtins, or unknown path —
                    // fall through; the LibraryBuilder's embedded resource
                    // fallback handles the IEEE case.
                    return Task.FromResult<Stream?>(null);
                },
                CacheStore:        blobStore,
                AdditionalSources: userAssets.ListUserLibrarySources(),
                Policy:            LibraryBuildPolicy.AlwaysPersistent));
    }
}
