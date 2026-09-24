using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Reads / writes per-project editor sessions to <c>localStorage</c>.
/// Storage key is <c>kmila-editor-session-{projectId}</c>. Errors are
/// logged but never bubble — a corrupt or inaccessible session blob
/// must never block the editor from opening, just fall back to the
/// "open main file" default.
/// </summary>
public class EditorSessionStore
{
    private readonly IJSRuntime _js;
    private readonly ILogger<EditorSessionStore> _logger;

    private const string KeyPrefix = "kmila-editor-session-";
    private static string Key(int projectId) => $"{KeyPrefix}{projectId}";

    public EditorSessionStore(IJSRuntime js, ILogger<EditorSessionStore>? logger = null)
    {
        _js = js;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EditorSessionStore>.Instance;
    }

    public async Task<EditorSession?> LoadAsync(int projectId)
    {
        if (projectId <= 0) return null;
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key(projectId));
            if (string.IsNullOrWhiteSpace(json)) return null;
            return System.Text.Json.JsonSerializer.Deserialize<EditorSession>(json);
        }
        catch (Exception ex) when (IsTransientJsInteropFailure(ex))
        {
            // Either static prerender (JS interop unavailable) or the
            // circuit was already torn down (e.g. page navigation /
            // refresh). Both are expected — the Editor's first render
            // retries on the prerender side, and a save lost during
            // teardown will be re-issued the next time the project opens.
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[EditorSessionStore] Load failed for project {Id}", projectId);
            return null;
        }
    }

    public async Task SaveAsync(int projectId, EditorSession session)
    {
        if (projectId <= 0 || session == null) return;
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(session);
            await _js.InvokeVoidAsync("localStorage.setItem", Key(projectId), json);
        }
        catch (Exception ex) when (IsTransientJsInteropFailure(ex))
        {
            // Prerender pass or circuit torn down — see LoadAsync comment.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[EditorSessionStore] Save failed for project {Id}", projectId);
        }
    }

    public async Task ClearAsync(int projectId)
    {
        if (projectId <= 0) return;
        try { await _js.InvokeVoidAsync("localStorage.removeItem", Key(projectId)); }
        catch (Exception ex) when (IsTransientJsInteropFailure(ex)) { /* prerender or teardown */ }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[EditorSessionStore] Clear failed for project {Id}", projectId);
        }
    }

    /// <summary>
    /// Recognises the two JS-interop failure modes that aren't real
    /// errors: static prerender (no JS available) and circuit
    /// disconnect (server-side Blazor losing its client). Both can
    /// fire while a save is mid-flight after the user navigates away
    /// and would otherwise spam the log on every session change.
    /// </summary>
    private static bool IsTransientJsInteropFailure(Exception ex)
        => ex is InvalidOperationException
        || ex is JSDisconnectedException
        || ex is TaskCanceledException;
}
