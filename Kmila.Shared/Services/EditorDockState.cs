namespace Kmila.Shared.Services;

/// <summary>
/// The eight tabs in the Editor's bottom dock. Ordering mirrors the user-facing
/// left-to-right tab strip: Entities (parsed design) · Schematic · Flow · Ports
/// (inputs) · Waveforms (outputs) · Runs · Files · Output (build/run log).
/// </summary>
public enum EditorTab
{
    Entities,
    Schematic,
    Flow,
    Ports,
    Waveforms,
    Runs,
    Files,
    Output
}

/// <summary>
/// Shared state for the Editor's bottom dock — active tab + collapse flag.
/// The dock component and anyone wanting to programmatically focus a tab
/// (e.g. "switch to Waveforms after Run completes") subscribes to
/// <see cref="OnChange"/> and drives state through the setters here.
/// </summary>
/// <remarks>
/// Currently in-memory only. Persistence to localStorage lives in a later
/// chunk so the user's last-used tab + dock size survive page reloads.
/// </remarks>
public class EditorDockState
{
    /// <summary>Which tab is currently visible.</summary>
    public EditorTab Active { get; private set; } = EditorTab.Entities;

    /// <summary>When true, only the tab strip is shown (panel collapsed to ~40px).</summary>
    public bool IsCollapsed { get; private set; }

    /// <summary>Expanded dock height in pixels, driven by the drag-grabber.</summary>
    public int Height { get; private set; } = 320;

    /// <summary>Fires whenever any property changes — components re-render on this.</summary>
    public event Action? OnChange;

    /// <summary>Sets the dock height (clamped to 200..1200) and fires OnChange.</summary>
    public void SetHeight(int px)
    {
        px = Math.Clamp(px, 200, 1200);
        if (px == Height) return;
        Height = px;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Selects which dock tab is currently shown. Also un-collapses the dock so
    /// clicking a tab while collapsed feels responsive (the panel pops back open).
    /// </summary>
    /// <param name="tab">The tab to focus.</param>
    public void SetActive(EditorTab tab)
    {
        // Clicking a tab while the dock is collapsed should pop it open —
        // otherwise the click feels broken (nothing visible changes).
        var changed = Active != tab || IsCollapsed;
        Active = tab;
        IsCollapsed = false;
        if (changed) OnChange?.Invoke();
    }

    /// <summary>Flips the dock between collapsed (header-only) and expanded.</summary>
    public void ToggleCollapsed()
    {
        IsCollapsed = !IsCollapsed;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Applies state loaded from localStorage without firing OnChange — used
    /// during first render to avoid a render storm before the component has
    /// mounted.
    /// </summary>
    internal void HydrateFromStorage(EditorTab? tab, bool collapsed, int? height = null)
    {
        if (tab.HasValue) Active = tab.Value;
        IsCollapsed = collapsed;
        if (height.HasValue && height.Value >= 200 && height.Value <= 1200)
            Height = height.Value;
    }
}
