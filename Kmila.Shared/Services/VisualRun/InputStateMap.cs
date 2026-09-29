namespace Kmila.Shared.Services.VisualRun;

// Visual-mode bridge: design input ports → current driven value. Widgets
// (buttons, switches) write here; VisualRunCoordinator drains the map into
// the engine's signal table once per tick. Single source of truth so a
// widget's last write wins regardless of repaint ordering.
/// <summary>
/// Thread-safe map of design input port names to their current driven value.
/// Widgets write to it and the run coordinator drains it into the engine once
/// per tick, so the last write for a port wins regardless of repaint order.
/// </summary>
public sealed class InputStateMap
{
    private readonly Dictionary<string, string> _values =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>Raised with the port name whenever a port's value changes.</summary>
    public event Action<string>? OnPortChanged;

    /// <summary>Returns the current value driven onto a port, or "0" if unset.</summary>
    /// <param name="port">Port name to read.</param>
    /// <returns>The driven value, or "0" when no value has been set.</returns>
    public string Get(string port)
    {
        lock (_gate)
        {
            return _values.TryGetValue(port, out var v) ? v : "0";
        }
    }

    /// <summary>Sets a port's driven value, firing <see cref="OnPortChanged"/> only when it actually changes.</summary>
    /// <param name="port">Port name to write.</param>
    /// <param name="value">New value to drive onto the port.</param>
    public void Set(string port, string value)
    {
        bool fire = false;
        lock (_gate)
        {
            if (!_values.TryGetValue(port, out var existing) || existing != value)
            {
                _values[port] = value;
                fire = true;
            }
        }
        if (fire) OnPortChanged?.Invoke(port);
    }

    /// <summary>Returns a copy of the current port-to-value map.</summary>
    /// <returns>A snapshot dictionary of port names to driven values.</returns>
    public IReadOnlyDictionary<string, string> Snapshot()
    {
        lock (_gate)
        {
            return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Removes all driven port values.</summary>
    public void Clear()
    {
        lock (_gate) _values.Clear();
    }
}
