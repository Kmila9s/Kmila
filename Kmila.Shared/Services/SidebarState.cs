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
    /// <summary>Desktop "rail" collapsed mode — icons-only, narrow column.</summary>
    public bool IsCollapsed { get; private set; }

    /// <summary>Mobile drawer open state — off-canvas slide-in below 641 px.</summary>
    public bool IsMobileOpen { get; private set; }

    /// <summary>Fires whenever either flag changes so subscribers can re-render.</summary>
    public event Action? OnChange;

    /// <summary>Toggles the desktop rail mode and notifies subscribers.</summary>
    public void ToggleCollapsed()
    {
        IsCollapsed = !IsCollapsed;
        OnChange?.Invoke();
    }

    /// <summary>Toggles the mobile drawer (open ↔ closed) and notifies subscribers.</summary>
    public void ToggleMobile()
    {
        IsMobileOpen = !IsMobileOpen;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Closes the mobile drawer if it is currently open. No-op when already closed,
    /// so callers (nav-link handlers, backdrop clicks) can call this unconditionally
    /// without producing spurious change notifications.
    /// </summary>
    public void CloseMobile()
    {
        if (!IsMobileOpen) return;
        IsMobileOpen = false;
        OnChange?.Invoke();
    }
}
