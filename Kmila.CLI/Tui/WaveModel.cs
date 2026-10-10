namespace Kmila.CLI.Tui;

/// <summary>
/// A terminal-friendly waveform model. Both TUI data sources funnel through
/// this one shape: a live simulation (its engine VCD) and an opened
/// <c>.vcd</c> file are parsed by <see cref="Kmila.CLI.Simulation.VcdReader"/>
/// into a <see cref="WaveModel"/>, so the renderer only ever deals with one
/// thing. Times are kept in femtoseconds (VCD's finest unit) and converted to
/// ns only for display.
/// </summary>
public sealed class WaveModel
{
    /// <summary>Signals in display order.</summary>
    public List<WaveSignal> Signals { get; } = new();

    /// <summary>Last transition time across all signals, in femtoseconds.</summary>
    public long MaxTimeFs { get; set; }

    /// <summary>Module name from the VCD scope, if any.</summary>
    public string Module { get; set; } = "";

    /// <summary>Femtoseconds per nanosecond.</summary>
    public const long FsPerNs = 1_000_000L;

    /// <summary>True when there are no signals to show.</summary>
    public bool IsEmpty => Signals.Count == 0;
}

/// <summary>One signal track: a name, a width (&gt;1 ⇒ bus), and its value-change list.</summary>
public sealed class WaveSignal
{
    /// <summary>Display name (leaf of any hierarchical VCD name).</summary>
    public required string Name { get; init; }
    /// <summary>Bit width; 1 for a scalar wire, &gt;1 for a bus.</summary>
    public int Width { get; init; } = 1;

    /// <summary>True for multi-bit buses (drawn as labelled value segments).</summary>
    public bool IsBus => Width > 1;

    /// <summary>Time-ascending value changes. Value is a scalar char
    /// ("0"/"1"/"x"/"z") for a wire, or a bit string for a bus.</summary>
    public List<WaveTransition> Transitions { get; } = new();

    /// <summary>Returns the value in effect at time <paramref name="fs"/> (last change at or before it), or null before the first.</summary>
    public string? ValueAtFs(long fs)
    {
        if (Transitions.Count == 0) return null;
        // Binary search for the last transition with TimeFs <= fs.
        int lo = 0, hi = Transitions.Count - 1, found = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (Transitions[mid].TimeFs <= fs) { found = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return found >= 0 ? Transitions[found].Value : null;
    }
}

/// <summary>A single value change at a point in time.</summary>
/// <param name="TimeFs">Absolute time in femtoseconds.</param>
/// <param name="Value">Scalar char or bus bit string.</param>
public readonly record struct WaveTransition(long TimeFs, string Value);
