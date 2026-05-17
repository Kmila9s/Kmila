using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Scoped holder for the most recent step-replay timeline. Editor.razor
/// pushes a fresh <see cref="ExecutionTimelineDoc"/> here after every
/// simulation run; <c>StepReplayPanel</c> (the new Replay dock tab)
/// subscribes to <see cref="OnChange"/> and re-renders the
/// <c>ExecutionPlayer</c> with the new doc.
/// </summary>
/// <remarks>
/// Scoped (per circuit) so different projects open in different tabs
/// don't trample each other's replay state. The store is in-memory only
/// — a Refresh of the page wipes it intentionally, since the runtime
/// signal history is also gone.
/// </remarks>
public class ReplayState
{
    /// <summary>The active replay document, or null when no simulation has run yet.</summary>
    public ExecutionTimelineDoc? Current { get; private set; }

    /// <summary>Fires whenever <see cref="Current"/> is set (or cleared).</summary>
    public event Action? OnChange;

    /// <summary>
    /// Publish a fresh replay. Pass <c>null</c> to clear (e.g. when the
    /// user switches projects and the previous replay no longer applies).
    /// </summary>
    public void Set(ExecutionTimelineDoc? doc)
    {
        Current = doc;
        OnChange?.Invoke();
    }
}
