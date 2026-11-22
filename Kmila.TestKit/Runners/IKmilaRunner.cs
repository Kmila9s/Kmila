using Kmila.Shared.Services.Api;

namespace Kmila.TestKit.Runners;

/// <summary>A source file handed to Kmila verbatim: its name in the project and its text.</summary>
public sealed record SourceFile(string Name, string Content);

/// <summary>
/// Runs a design through Kmila's headless pipeline (the same one behind
/// <c>/api/v1/simulate</c>): the sources are written to a fresh sandbox project,
/// simulated with the request's stimulus and timing, and the
/// project is discarded unless the runner was told to keep it.
/// </summary>
public interface IKmilaRunner
{
    /// <summary>Simulates <paramref name="sources"/>; <see cref="SimulateRequest.ProjectId"/> is filled in by the runner.</summary>
    Task<SimulateResponse> SimulateAsync(IReadOnlyList<SourceFile> sources, SimulateRequest request, CancellationToken ct = default);
}
