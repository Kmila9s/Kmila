using Kmila.Shared.Services;

namespace Kmila.Wasm.Services;

/// <summary>
/// Blazor WebAssembly implementation of <see cref="IFormFactor"/>. Reports
/// "Web" so the shared UI takes the same browser-oriented branches it takes
/// under Kmila.Web (Blazor Server); the demo-vs-full distinction is carried
/// separately by <see cref="DemoMode"/>, not by the form factor string.
/// </summary>
public sealed class WasmFormFactor : IFormFactor
{
    public string GetFormFactor() => "Web";

    public string GetPlatform() => "Browser (WebAssembly)";
}
