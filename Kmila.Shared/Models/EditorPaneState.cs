using BlazorMonaco.Editor;

namespace Kmila.Shared.Models;

/// <summary>
/// Per-tab editor state. Each open file gets one of these — its own Monaco
/// <see cref="TextModel"/> (so cursor / scroll / undo are preserved when
/// switching tabs), its own dirty flag, its own language and markdown
/// preview. The Editor page never reads "the current file" directly any
/// more; it asks the active <see cref="EditorPane"/> for its
/// <see cref="EditorPane.ActiveTab"/>.
/// </summary>
public class OpenFileTab
{
    /// <summary>The project file this tab represents.</summary>
    public ProjectFile File { get; set; } = null!;

    /// <summary>
    /// Snapshot of the file content the tab was opened with. Compared
    /// against the live Monaco buffer to compute <see cref="HasUnsavedChanges"/>;
    /// rewritten on save so subsequent diff comparisons start fresh.
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>True when the buffer differs from <see cref="OriginalContent"/>.</summary>
    public bool HasUnsavedChanges { get; set; }

    /// <summary>1-based caret line as last reported by Monaco. Mirrored
    /// here so tab switches restore it on the next render.</summary>
    public int CursorLine { get; set; } = 1;

    /// <summary>1-based caret column matching <see cref="CursorLine"/>.</summary>
    public int CursorColumn { get; set; } = 1;

    /// <summary>Monaco language id. Drives syntax highlighting on tab activation.</summary>
    public string Language { get; set; } = "vhdl";

    /// <summary>
    /// Current markdown source for the live-preview pane. Only meaningful
    /// when <see cref="IsMarkdown"/> is true; tracked per-tab so switching
    /// between two open .md files doesn't show one's preview against the
    /// other's source.
    /// </summary>
    public string MarkdownPreview { get; set; } = string.Empty;

    /// <summary>
    /// Monaco TextModel backing this tab. Created on tab open with a
    /// per-tab unique URI (so two panes showing the same file don't
    /// collide in Monaco's global model registry) and disposed on tab
    /// close. Null between construction and the first OnAfterRender pass
    /// that materialises the model.
    /// </summary>
    public TextModel? Model { get; set; }

    /// <summary>
    /// URI of the model in Monaco's registry. Cached here so the
    /// disposal-fallback path can target the model by URI when the
    /// strong reference is gone (some BlazorMonaco builds don't expose
    /// DisposeModel and we have to drop down to JS interop).
    /// </summary>
    public string? ModelUri { get; set; }

    /// <summary>True if the file extension is .md or .markdown.</summary>
    public bool IsMarkdown =>
        File != null && (
            File.FileName.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase) ||
            File.FileName.EndsWith(".markdown", System.StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Drag-and-drop result for the tab strip. Carries both source pane +
/// tab and destination pane + tab so the same callback handles same-pane
/// reorder (Src.PaneIndex == Dest.PaneIndex) and cross-pane move (else).
/// </summary>
public record TabMoveRequest(
    int SrcPaneIndex,
    int SrcTabIndex,
    int DestPaneIndex,
    int DestTabIndex);

/// <summary>
/// One Monaco editor + its open tabs. Phase 1 wires a single pane in the
/// Editor page; Phase 2 will lift this into a list so two panes can sit
/// side-by-side. Keeping the per-pane fields here (not in a flat record on
/// the page) means the Phase 2 refactor is mechanical.
/// </summary>
public class EditorPane
{
    /// <summary>BlazorMonaco's editor wrapper. Set by `@ref` once Monaco mounts.</summary>
    public StandaloneCodeEditor Monaco { get; set; } = new();

    /// <summary>True after Monaco has emitted its OnDidInit callback.</summary>
    public bool Initialized { get; set; }

    /// <summary>The pane's open tabs, in display order. Reorderable by the
    /// tab-strip's drag-and-drop.</summary>
    public List<OpenFileTab> Tabs { get; } = new();

    /// <summary>Index of the active tab in <see cref="Tabs"/>, or -1 when
    /// the pane has no open tabs.</summary>
    public int ActiveIndex { get; set; } = -1;

    /// <summary>The currently active tab, or null when the pane is empty.</summary>
    public OpenFileTab? ActiveTab =>
        ActiveIndex >= 0 && ActiveIndex < Tabs.Count ? Tabs[ActiveIndex] : null;

    /// <summary>Locates the tab for a given file id; returns -1 if the file
    /// isn't already open in this pane.</summary>
    public int IndexOf(int fileId)
    {
        for (int i = 0; i < Tabs.Count; i++)
            if (Tabs[i].File != null && Tabs[i].File.Id == fileId)
                return i;
        return -1;
    }
}
