using System.Collections.Concurrent;

namespace Kmila.Shared.Services.VisualRun;

/// <summary>
/// App-wide lookup for live <see cref="VisualRunCoordinator"/> instances, keyed
/// by project id. Signal-only runs already have such a surface via
/// <see cref="SimulationParameters.RunningProjectId"/> + the singleton
/// <c>ProgramBuilder</c>; visual runs are owned as a private field on the
/// (component-scoped) <c>VisualPanel</c>, so external surfaces like NavMenu /
/// ProjectsView have no way to pause/stop them without this registry.
/// </summary>
/// <remarks>
/// VisualPanel registers on <c>StartRun</c> and unregisters on
/// <c>StopRun</c>/<c>Dispose</c>. At most one coordinator per project id is
/// tracked — a new registration for the same project replaces the previous
/// entry (matches VisualPanel's own "dispose prior _coord before starting a
/// new one" invariant).
/// </remarks>
public sealed class VisualRunRegistry
{
    private readonly ConcurrentDictionary<int, VisualRunCoordinator> _byProject = new();

    /// <summary>Registers <paramref name="coord"/> as the live visual sim for <paramref name="projectId"/>, replacing any prior entry.</summary>
    public void Register(int projectId, VisualRunCoordinator coord)
    {
        _byProject[projectId] = coord;
    }

    /// <summary>Drops the registration if <paramref name="coord"/> is still the current entry — no-op if a fresh Start already replaced it.</summary>
    public void Unregister(int projectId, VisualRunCoordinator coord)
    {
        _byProject.TryRemove(new KeyValuePair<int, VisualRunCoordinator>(projectId, coord));
    }

    /// <summary>Looks up the live coordinator for a project. Callers should treat the returned instance as read-only apart from Pause/Resume/Stop.</summary>
    public bool TryGet(int projectId, out VisualRunCoordinator? coord)
    {
        if (_byProject.TryGetValue(projectId, out var c))
        {
            coord = c;
            return true;
        }
        coord = null;
        return false;
    }
}
