using Parser.Models;

namespace Kmila.Shared.Models;
/// <summary>
/// Represents a moment in time for a port, capturing its state and value at a specific time.
/// </summary>
public class PortMoment
{
    /// <summary>
    /// Gets or sets the variable associated with this port moment.
    /// </summary>
    public Variable Variable { get; set; } = default!;
    /// <summary>
    /// Gets or sets the time input for this port moment.
    /// </summary>
    public int TimeInput { get; set; } = 0;
    /// <summary>
    /// Gets or sets a value indicating whether this port moment represents a clock signal.
    /// </summary>
    public bool IsClock { get; set; } = false;
    /// <summary>
    /// Gets or sets the value of the port at this moment.
    /// </summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the time scale for this port moment.
    /// </summary>
    public TimeScale TimeScale { get; set; } = TimeScale.US;
}