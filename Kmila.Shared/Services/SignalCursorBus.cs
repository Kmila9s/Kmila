namespace Kmila.Shared.Services;

/// <summary>
/// Cross-component bus that ties the schematic's Debug-mode cursor to
/// the waveform viewer. Either side can publish a cursor tick or a
/// selected-net id; the other re-renders accordingly. Singleton-scoped
/// so every Editor pane sees the same cursor.
/// </summary>
/// <remarks>
/// The bus is intentionally thin — no value lookup happens here. The
/// schematic asks the active <c>WaveformData</c> for the value of a
/// given net at <see cref="CursorTick"/> when it needs to colour a
/// wire, and the waveform viewer reacts to <see cref="SelectedNet"/>
/// by scrolling its row into view. Keeping the bus value-free means
/// it has no dependency on simulation history at all.
/// </remarks>
public class SignalCursorBus
{
    /// <summary>
    /// Current cursor position in absolute simulation ticks. Driven by
    /// the waveform viewer when the user clicks/scrubs; consumed by the
    /// schematic in Debug mode.
    /// </summary>
    public ulong CursorTick { get; private set; }

    /// <summary>
    /// Currently-highlighted net by name (matches signal name in the
    /// waveform). Set by clicking a wire in the schematic; consumed by
    /// the waveform viewer to highlight the corresponding row.
    /// </summary>
    public string? SelectedNet { get; private set; }

    /// <summary>Fires when either <see cref="CursorTick"/> or <see cref="SelectedNet"/> changes.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Publishes a new cursor tick. No-op (and no event) when the value matches
    /// the current state, so callers can call this idempotently from animation
    /// frames without producing render storms.
    /// </summary>
    /// <param name="tick">Absolute simulation tick to position the cursor at.</param>
    public void SetCursorTick(ulong tick)
    {
        if (tick == CursorTick) return;
        CursorTick = tick;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Publishes a new selected-net name (or <c>null</c> to deselect). No-op when
    /// the name is unchanged.
    /// </summary>
    /// <param name="netName">Net/signal name to highlight; <c>null</c> clears.</param>
    public void SetSelectedNet(string? netName)
    {
        if (string.Equals(SelectedNet, netName, StringComparison.Ordinal)) return;
        SelectedNet = netName;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Resets both cursor and selection to their initial state. No-op (and no
    /// event) when both are already cleared.
    /// </summary>
    public void Clear()
    {
        if (CursorTick == 0 && SelectedNet == null) return;
        CursorTick = 0;
        SelectedNet = null;
        OnChange?.Invoke();
    }
}
