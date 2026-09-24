using Kmila.Shared.Services.Export;
using Microsoft.JSInterop;

namespace Kmila.Wasm.Services;

/// <summary>
/// Blazor WebAssembly implementation of <see cref="IFileExportService"/>.
/// Identical in behavior to Kmila.Web's WebFileExportService — it delegates to
/// the shared JS anchor-download helpers (kmilaDownloadText / kmilaDownloadBytes
/// in app.js). Duplicated here rather than referenced because that class lives
/// in the Kmila.Web project, which this head does not depend on; the JS contract
/// is the shared seam.
/// </summary>
public sealed class WasmFileExportService : IFileExportService
{
    private readonly IJSRuntime _js;

    public WasmFileExportService(IJSRuntime js) => _js = js;

    public async Task<FileExportResult> SaveTextAsync(
        ExportCategory category,
        string fileName,
        string mime,
        string content,
        CancellationToken ct = default)
    {
        try
        {
            var ok = await _js.InvokeAsync<bool>("kmilaDownloadText", ct, fileName, mime, content);
            return ok
                ? FileExportResult.Success(path: null)
                : FileExportResult.Failure("Browser blocked or canceled the download.");
        }
        catch (Exception ex)
        {
            return FileExportResult.Failure(ex.Message);
        }
    }

    public async Task<FileExportResult> SaveBytesAsync(
        ExportCategory category,
        string fileName,
        string mime,
        byte[] bytes,
        CancellationToken ct = default)
    {
        try
        {
            var base64 = Convert.ToBase64String(bytes);
            var ok = await _js.InvokeAsync<bool>("kmilaDownloadBytes", ct, fileName, mime, base64);
            return ok
                ? FileExportResult.Success(path: null)
                : FileExportResult.Failure("Browser blocked or canceled the download.");
        }
        catch (Exception ex)
        {
            return FileExportResult.Failure(ex.Message);
        }
    }
}
