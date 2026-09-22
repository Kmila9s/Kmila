using Kmila.Shared.Services;

namespace Kmila.Wasm.Services;

/// <summary>
/// WebAssembly-demo implementation of <see cref="IDemoCapabilities"/>. Registered
/// only by Kmila.Wasm; shared UI injects <see cref="IDemoCapabilities"/> as a
/// nullable dependency, so the MAUI/Server heads (which never register it) keep
/// every feature. Hides features that can't work — or shouldn't be exposed — in
/// an anonymous, sandboxed browser session.
/// </summary>
public sealed class DemoMode : IDemoCapabilities
{
    /// <summary>Always the WASM demo build.</summary>
    public bool IsDemo => true;

    /// <summary>GitHub OAuth is unavailable in the browser sandbox (CORS).</summary>
    public bool AllowGitHubLogin => false;

    /// <summary>
    /// User VHDL-library import works client-side (it writes to MEMFS and
    /// persists via IndexedDB), so the demo KEEPS it — no reason to hide a
    /// feature that functions. Left in the interface for future restriction.
    /// </summary>
    public bool AllowLibraryImport => true;
}
