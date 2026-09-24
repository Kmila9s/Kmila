namespace Kmila.Shared.Services;

/// <summary>
/// Optional capability gate for hosts that expose a restricted build. Only the
/// Blazor WebAssembly public demo (Kmila.Wasm) registers an implementation;
/// the MAUI and Blazor Server heads leave it unregistered, so shared components
/// that inject it as a NULLABLE dependency see <c>null</c> and behave exactly as
/// before (all features on). Shared UI should treat <c>null</c> as "allow".
/// </summary>
public interface IDemoCapabilities
{
    /// <summary>True when this is the anonymous, sandboxed public demo build.</summary>
    bool IsDemo { get; }

    /// <summary>
    /// GitHub OAuth device-flow login. Unavailable in the browser sandbox —
    /// GitHub's token endpoints send no CORS headers — so the demo hides it.
    /// </summary>
    bool AllowGitHubLogin { get; }

    /// <summary>User-supplied VHDL library import (Settings → Libraries), hidden in the demo.</summary>
    bool AllowLibraryImport { get; }
}
