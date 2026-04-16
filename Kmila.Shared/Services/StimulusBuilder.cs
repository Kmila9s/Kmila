using Interpreter.Models;
using Interpreter.Services;
using Kmila.Shared.Models;
using Parser.Models;
using TimeMachine.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Converts UI-level port moment configurations into a SimulationConfig
/// that SimulationRunner can execute.
/// </summary>
public static class StimulusBuilder
{
    /// <summary>
    /// Builds a SimulationConfig from UI parameters.
    /// </summary>
    /// <param name="entity">The VHDL entity to simulate.</param>
    /// <param name="portMoments">Port moment configurations from the UI (may be null).</param>
    /// <param name="duration">Total simulation duration.</param>
    /// <param name="tickResNs">Nanoseconds per tick (default: TimeClock.NS_PER_TICK = 10).</param>
    /// <returns>A fully configured <see cref="SimulationConfig"/> ready for execution by SimulationRunner.</returns>
    public static SimulationConfig BuildConfig(
        Entity entity,
        List<PortMomentConfiguration>? portMoments,
        TimeSpan duration,
        int tickResNs = TimeClock.NS_PER_TICK)
    {
        // Convert duration to ticks
        double totalNs = duration.TotalSeconds * 1e9;
        ulong totalTicks = (ulong)(totalNs / tickResNs);
        if (totalTicks == 0) totalTicks = 1;

        var clocks = new List<ClockConfig>();
        var stimuli = new List<StimulusEvent>();

        // Build a lookup for ports by name
        var allPorts = entity.Ports.ToDictionary(
            p => p.Name,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        if (portMoments != null)
        {
            foreach (var pm in portMoments)
            {
                if (string.IsNullOrEmpty(pm.PortName) || !allPorts.TryGetValue(pm.PortName, out var signal))
                    continue;

                if (pm.Values == null || pm.Values.Count == 0)
                    continue;

                // Check if this port is configured as a clock
                // A port is a clock if it has a single value entry with TimeScale == "CLK"
                var firstVal = pm.Values[0];
                if (firstVal.TimeScale.Equals("CLK", StringComparison.OrdinalIgnoreCase))
                {
                    // Clock configuration: Time field is frequency in MHz, Value is ignored
                    // Formula: freq(MHz) -> period(ns) = 1000/freq -> half-period(ticks) = period/(2*tickRes)
                    double freqMhz = firstVal.Time > 0 ? firstVal.Time : 50; // default 50 MHz
                    double periodNs = 1_000.0 / freqMhz; // full period in ns
                    ulong halfPeriodTicks = (ulong)(periodNs / 2.0 / tickResNs);
                    if (halfPeriodTicks == 0) halfPeriodTicks = 1; // minimum 1 tick to avoid zero-period clock

                    clocks.Add(new ClockConfig
                    {
                        Signal = signal,
                        HalfPeriodTicks = halfPeriodTicks,
                        InitialHigh = false
                    });
                }
                else
                {
                    // Stimulus events: each PortMomentValue is a (time, value) pair
                    foreach (var pmv in pm.Values)
                    {
                        ulong tick = ConvertToTicks(pmv.Time, pmv.TimeScale, tickResNs);
                        stimuli.Add(new StimulusEvent
                        {
                            Tick = tick,
                            Signal = signal,
                            Value = pmv.Value
                        });
                    }
                }
            }
        }

        // Auto-detect clocks: if no clock was configured but there's a port named clk/clock,
        // add a default clock at half-period = 1 tick (toggles every tick)
        if (clocks.Count == 0)
        {
            var clkSignal = entity.Ports.FirstOrDefault(p =>
                p.PortType == Constants.PortType.IN &&
                (p.Name.Equals("clk", StringComparison.OrdinalIgnoreCase) ||
                 p.Name.Equals("clock", StringComparison.OrdinalIgnoreCase)));

            if (clkSignal != null)
            {
                clocks.Add(new ClockConfig
                {
                    Signal = clkSignal,
                    HalfPeriodTicks = 1,
                    InitialHigh = false
                });
            }
        }

        // Sort stimuli by tick
        stimuli.Sort((a, b) => a.Tick.CompareTo(b.Tick));

        return new SimulationConfig
        {
            Entity = entity,
            TotalTicks = totalTicks,
            Clocks = clocks,
            Stimuli = stimuli
        };
    }

    /// <summary>
    /// Converts a time value + scale string to simulation ticks.
    /// </summary>
    /// <param name="time">The numeric time value to convert.</param>
    /// <param name="timeScale">The time scale unit (NS, US, MS, S, M, H).</param>
    /// <param name="tickResNs">The tick resolution in nanoseconds.</param>
    /// <returns>The equivalent number of simulation ticks.</returns>
    private static ulong ConvertToTicks(int time, string timeScale, int tickResNs)
    {
        // Convert the time value to nanoseconds based on the scale unit
        double ns = timeScale.ToUpperInvariant() switch
        {
            "NS" => time,                       // nanoseconds (1:1)
            "US" => time * 1_000.0,             // microseconds -> ns
            "MS" => time * 1_000_000.0,         // milliseconds -> ns
            "S" => time * 1_000_000_000.0,      // seconds -> ns
            "M" => time * 60_000_000_000.0,     // minutes -> ns
            "H" => time * 3_600_000_000_000.0,  // hours -> ns
            _ => time                            // default: assume NS
        };
        // Divide nanoseconds by tick resolution to get tick count
        ulong ticks = (ulong)(ns / tickResNs);
        return ticks;
    }
}
