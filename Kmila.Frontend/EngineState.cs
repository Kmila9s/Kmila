using Interpreter.Services;

namespace Kmila.Frontend;

/// <summary>
/// The only place that resets the engine's process-global registries.
/// Every design load starts from this clean, deterministic state.
/// </summary>
public static class EngineState
{
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Clears per-design state. Library symbols (function entries tagged with
    /// a package name, and <see cref="Parser.Services.UserTypeRegistry"/>) are
    /// owned by the library step and survive, exactly as before.
    /// </summary>
    public static void Reset()
    {
        EntityRegistry.Instance.Clear();
        PackageRegistry.Instance.Clear();
        FunctionRegistry.Instance.Clear();
        SkipDiagnosticLog.Instance.Clear();
        DesignTimeGrid.Instance.Clear();
        global::Parser.Services.ParseProfile.Reset();
    }

    /// <summary>
    /// Serialises design loads: the registries are process-global, so two
    /// concurrent loads (e.g. two web sessions) would otherwise interleave
    /// their reset/scan/parse steps.
    /// </summary>
    public static IDisposable Enter(CancellationToken cancellationToken = default)
    {
        _gate.Wait(cancellationToken);
        return new Releaser();
    }

    private sealed class Releaser : IDisposable
    {
        private int _released;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0) _gate.Release();
        }
    }
}
