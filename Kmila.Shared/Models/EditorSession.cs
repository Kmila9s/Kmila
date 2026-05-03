namespace Kmila.Shared.Models;

/// <summary>
/// Persistable snapshot of the editor's open-file state for one project.
/// Stored in localStorage keyed <c>kmila-editor-session-{projectId}</c> so
/// reopening a project restores the user's exact tab + split state from
/// the last session. Only file IDs and indices are saved — the actual
/// buffer content lives in the project files themselves, so we never
/// duplicate authoritative source-of-truth data into the session blob.
/// </summary>
public class EditorSession
{
    /// <summary>
    /// Per-pane snapshots in display order. <c>Panes.Count == 1</c> means
    /// the pane was unsplit; <c>== 2</c> means the user had it split.
    /// We don't store an explicit "isSplit" flag because deriving it from
    /// list length removes a class of inconsistencies during partial
    /// migrations.
    /// </summary>
    public List<EditorPaneSnapshot> Panes { get; set; } = new();

    /// <summary>Which pane was focused last. Clamped on restore so a
    /// stale value (e.g. session was split, file got deleted, restore
    /// trimmed to 1 pane) can't yield an out-of-range index.</summary>
    public int FocusedPaneIndex { get; set; }
}

/// <summary>One pane's persisted state — tab file IDs in order, plus
/// the active tab's index within that list.</summary>
public class EditorPaneSnapshot
{
    /// <summary>Project-scoped file IDs in tab order. IDs whose files
    /// no longer exist (deleted between sessions) are silently dropped
    /// during restore.</summary>
    public List<int> TabFileIds { get; set; } = new();

    /// <summary>Active tab index within <see cref="TabFileIds"/>. -1
    /// means "no active tab", which the restorer treats as 0 once at
    /// least one tab has been opened.</summary>
    public int ActiveIndex { get; set; } = -1;
}
