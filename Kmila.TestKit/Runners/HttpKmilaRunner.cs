using Kmila.Shared.Services.Api;

namespace Kmila.TestKit.Runners;

/// <summary>Kmila behind a running host's <c>/api/v1</c> (Kmila.Web, MAUI's embedded API).</summary>
public sealed class HttpKmilaRunner : IKmilaRunner
{
    private readonly KmilaApiClient _client;
    private readonly bool _keep;

    /// <summary>Uses <paramref name="client"/>; with <paramref name="keep"/> the sandbox projects are retained.</summary>
    public HttpKmilaRunner(KmilaApiClient client, bool keep = false) { _client = client; _keep = keep; }

    /// <inheritdoc />
    public async Task<SimulateResponse> SimulateAsync(IReadOnlyList<SourceFile> sources, SimulateRequest request, CancellationToken ct = default)
    {
        string id = await _client.CreateProjectAsync(null, _keep, ct);
        try
        {
            foreach (var f in sources) await _client.WriteFileAsync(id, f.Name, f.Content, ct);
            request.ProjectId = id;
            return await _client.SimulateAsync(request, ct);
        }
        finally
        {
            if (!_keep) await _client.DeleteProjectAsync(id, ct);
        }
    }
}
