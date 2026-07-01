namespace Kmila.Shared.Services.VisualRun;

// Visual-mode bridge: design input ports → current driven value. Widgets
// (buttons, switches) write here; VisualRunCoordinator drains the map into
// the engine's signal table once per tick. Single source of truth so a
// widget's last write wins regardless of repaint ordering.
public sealed class InputStateMap
{
    private readonly Dictionary<string, string> _values =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public event Action<string>? OnPortChanged;

    public string Get(string port)
    {
        lock (_gate)
        {
            return _values.TryGetValue(port, out var v) ? v : "0";
        }
    }

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

    public IReadOnlyDictionary<string, string> Snapshot()
    {
        lock (_gate)
        {
            return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Clear()
    {
        lock (_gate) _values.Clear();
    }
}
