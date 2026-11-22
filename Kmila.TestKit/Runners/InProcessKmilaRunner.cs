using Kmila.Shared.Services.Api;

namespace Kmila.TestKit.Runners;

/// <summary>
/// Kmila in this process: a <see cref="SandboxProjectStore"/> under
/// <c>%TEMP%/kmila-bench/sandbox</c> and <see cref="HeadlessSimulator.RunAsync"/>,
/// exactly what the HTTP endpoint calls — so both runners produce the same VCD
/// without a server.
/// </summary>
public sealed class InProcessKmilaRunner : IKmilaRunner
{
    private readonly SandboxProjectStore _store = new(Path.Combine(Path.GetTempPath(), "kmila-bench", "sandbox"));
    private readonly bool _keep;

    /// <summary>Creates the runner; with <paramref name="keep"/> the sandbox projects are left on disk.</summary>
    public InProcessKmilaRunner(bool keep = false) { _keep = keep; }

    /// <inheritdoc />
    public async Task<SimulateResponse> SimulateAsync(IReadOnlyList<SourceFile> sources, SimulateRequest request, CancellationToken ct = default)
    {
        var project = _store.Create(null, _keep);
        try
        {
            foreach (var f in sources) project.WriteFile(f.Name, f.Content);
            request.ProjectId = project.Id;
            return await new HeadlessSimulator(_store).RunAsync(request);
        }
        finally
        {
            if (!_keep) _store.Delete(project.Id);
        }
    }
}
