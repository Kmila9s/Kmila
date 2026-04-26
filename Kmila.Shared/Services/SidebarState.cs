namespace Kmila.Shared.Services;

/// <summary>
/// Shared state for the custom Kmila sidebar — lets the TopBar hamburger and
/// the sidebar itself coordinate without parent/child coupling.
/// </summary>
/// <remarks>
/// Two independent bits of state:
/// <list type="bullet">
///   <item><description><see cref="IsCollapsed"/> — desktop "rail" mode (icons only, wide hit area, tooltips on hover).</description></item>
///   <item><description><see cref="IsMobileOpen"/> — narrow-viewport drawer visibility; off-canvas with backdrop.</description></item>
/// </list>
/// Anyone can subscribe to <see cref="OnChange"/> to re-render when either flips.
/// </remarks>
public class SidebarState
{
    public bool IsCollapsed { get; private set; }
    public bool IsMobileOpen { get; private set; }

    public event Action? OnChange;

    public void ToggleCollapsed()
    {
        IsCollapsed = !IsCollapsed;
        OnChange?.Invoke();
    }

    public void ToggleMobile()
    {
        IsMobileOpen = !IsMobileOpen;
        OnChange?.Invoke();
    }

    public void CloseMobile()
    {
        if (!IsMobileOpen) return;
        IsMobileOpen = false;
        OnChange?.Invoke();
    }
}
