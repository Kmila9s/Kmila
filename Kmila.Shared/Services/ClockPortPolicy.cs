using Interpreter.Ir;

namespace Kmila.Shared.Services;

/// <summary>
/// The clock a run proposes when the caller does not declare one: the input port whose name
/// reads like a clock (<c>clk</c>, <c>clock</c>, <c>sys_clk</c>, <c>aclk</c>…). The editor and the
/// practices show it as a visible stimulus (the «auto clock» badge); the API and the CLI use it
/// only in didactic mode, otherwise the clock is declared explicitly.
/// </summary>
public static class ClockPortPolicy
{
    private static readonly char[] Separators = { '_', '-', '.' };

    /// <summary>The clock-named port of <paramref name="design"/> (inputs first), or null.</summary>
    public static IrSignal? Suggest(IrDesign design) =>
        design.Ports.FirstOrDefault(p => p.PortMode == IrPortMode.In && IsClockPortName(p.Name))
            ?? design.Ports.FirstOrDefault(p => IsClockPortName(p.Name));

    /// <summary>
    /// True when a port name reads like a clock — exact <c>clk</c>/<c>clock</c>, a <c>clk</c>/<c>clock</c>
    /// token (<c>sys_clk</c>, <c>clk_i</c>) or a short prefixed/suffixed form (<c>aclk</c>, <c>clk1</c>).
    /// Case-insensitive; <c>clocking</c> or <c>blockade</c> do not match.
    /// </summary>
    public static bool IsClockPortName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim().ToLowerInvariant();
        if (n == "clk" || n == "clock") return true;
        if (n.Split(Separators, StringSplitOptions.RemoveEmptyEntries).Any(t => t is "clk" or "clock")) return true;
        return n.Length <= 6 && ((n.Length >= 4 && n.EndsWith("clk", StringComparison.Ordinal)) || n.StartsWith("clk", StringComparison.Ordinal));
    }
}
