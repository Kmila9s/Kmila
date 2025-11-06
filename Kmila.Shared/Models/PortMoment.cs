using Parser.Models;

namespace Kmila.Shared.Models;
public class PortMoment
{
    public Variable Variable { get; set; } = default!;
    public int TimeInput { get; set; } = 0;
    public bool IsClock { get; set; } = false;
    public string Value { get; set; } = string.Empty;
    public TimeScale TimeScale { get; set; } = TimeScale.US;
}