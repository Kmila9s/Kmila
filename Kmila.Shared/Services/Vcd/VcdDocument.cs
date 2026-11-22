namespace Kmila.Shared.Services.Vcd;

/// <summary>
/// A parsed VCD dump (Kmila's own or a reference simulator's): signals in
/// declaration order, their value changes in femtoseconds, and a by-name index
/// that resolves the leaf name to the shallowest declaration. Produced by
/// <see cref="VcdReader"/>; compared by <see cref="WaveformComparer"/>.
/// </summary>
public sealed class VcdDocument
{
    /// <summary>Femtoseconds per VCD time unit (from <c>$timescale</c>; 1 ps when absent).</summary>
    public long TimescaleFs { get; internal set; } = 1_000;

    /// <summary>Outermost scope of the first declared variable (the design or testbench), or empty.</summary>
    public string Module { get; internal set; } = "";

    /// <summary>One entry per VCD identifier, in declaration order.</summary>
    public List<VcdSignal> Signals { get; } = new();

    /// <summary>
    /// Signal by leaf name (hierarchy and subscript removed), case-insensitive.
    /// The shallowest declaration wins (a testbench port over a same-named net
    /// inside an instance); among equally deep ones the last declared wins.
    /// </summary>
    public Dictionary<string, VcdSignal> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>One VCD variable: its names, width and time-ordered value changes.</summary>
public sealed class VcdSignal
{
    /// <summary>VCD identifier code.</summary>
    public required string Id { get; init; }

    /// <summary>Leaf name without hierarchy or subscript (<c>/tb/dut/y[3:0]</c> → <c>y</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Leaf name as declared, including any subscript or range token (<c>y[3:0]</c>).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Bit width (1 for a scalar).</summary>
    public int Width { get; init; } = 1;

    /// <summary>Value changes as written (scalar character or vector bits without the <c>b</c>), ascending in time.</summary>
    public List<VcdChange> Transitions { get; } = new();
}

/// <summary>A value change at an absolute time in femtoseconds.</summary>
public readonly record struct VcdChange(long TimeFs, string Value);
