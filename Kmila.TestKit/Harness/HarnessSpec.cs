using Interpreter.Models;
using Parser.Models;
using static Parser.Models.Constants;

namespace Kmila.TestKit.Harness;

/// <summary>A DUT port as the harness drives or observes it: name, direction, width and whether it is a vector.</summary>
public sealed record HarnessPort(string Name, PortType Direction, int Width, bool IsVector);

/// <summary>A clock the harness toggles: initial level at t=0, then every half period.</summary>
public sealed record HarnessClock(string Port, long HalfPeriodNs, bool InitialHigh);

/// <summary>An input value applied at an absolute time (events need not be ordered).</summary>
public sealed record HarnessEvent(long TimeNs, string Port, string Value);

/// <summary>
/// Language-neutral description of a testbench that reproduces one Kmila run on
/// another simulator: the DUT's ports, clocks, timed input values, initial
/// values (before t=0) and the run length. Built from the very
/// <see cref="SimulationConfig"/> Kmila executes (<see cref="From"/>), so both
/// simulators see the same stimulus by construction.
/// </summary>
public sealed record HarnessSpec(
    string Top,
    IReadOnlyList<HarnessPort> Ports,
    IReadOnlyList<HarnessClock> Clocks,
    IReadOnlyList<HarnessEvent> Events,
    IReadOnlyDictionary<string, string> Initials,
    long DurationNs)
{
    /// <summary>The harness for <paramref name="config"/> on <paramref name="top"/> (ticks of <paramref name="tickResNs"/> ns).</summary>
    public static HarnessSpec From(string top, IEnumerable<Variable> ports, SimulationConfig config, int tickResNs)
    {
        long Ns(ulong ticks) => (long)ticks * tickResNs;
        if (config.Stimuli.Any(s => s.Target is not null))
            throw new NotSupportedException("harness: stimulus on part of a port (v(3), p.f) is not reproduced");
        return new HarnessSpec(
            top,
            ports.Select(p => new HarnessPort(p.Name, p.PortType, Math.Max(1, p.Size),
                p.Type == DATATYPES.STD_LOGIC_VECTOR || p.Size > 1)).ToList(),
            config.Clocks.Select(c => new HarnessClock(c.Signal.Name, Ns(c.HalfPeriodTicks), c.InitialHigh)).ToList(),
            config.Stimuli.Select(s => new HarnessEvent(Ns(s.Tick), s.Signal.Name, s.Value)).ToList(),
            config.PortInitials.ToDictionary(s => s.Signal.Name, s => s.Value, StringComparer.OrdinalIgnoreCase),
            Ns(config.TotalTicks));
    }
}
