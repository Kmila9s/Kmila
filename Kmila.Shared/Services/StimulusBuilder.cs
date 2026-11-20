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
    /// <param name="systemClockMhz">Effective system clock frequency (MHz) for this run.</param>
    /// <param name="options">Clocks, port initial values and initial-value policy (null: propose the clock by name, LRM).</param>
    /// <returns>A fully configured <see cref="SimulationConfig"/> ready for execution by SimulationRunner.</returns>
    public static SimulationConfig BuildConfig(
        Entity entity,
        List<PortMomentConfiguration>? portMoments,
        TimeSpan duration,
        int tickResNs = TimeClock.NS_PER_TICK,
        int systemClockMhz = 50,
        StimulusOptions? options = null)
        => BuildConfigNs(entity, portMoments,
            totalNs: (long)Math.Round(duration.TotalSeconds * 1e9),
            tickResNs: tickResNs,
            systemClockMhz: systemClockMhz,
            options: options);

    /// <summary>
    /// Direct nanosecond-precision overload for callers that need sub-100ns
    /// durations (the API/SimulatorBench). TimeSpan ticks are 100ns, so the
    /// regular <see cref="BuildConfig"/> rounds anything under 100ns to 0
    /// and silently truncates the run.
    /// </summary>
    public static SimulationConfig BuildConfigNs(
        Entity entity,
        List<PortMomentConfiguration>? portMoments,
        long totalNs,
        int tickResNs = TimeClock.NS_PER_TICK,
        int systemClockMhz = 50,
        StimulusOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Convert duration to ticks
        ulong totalTicks = (ulong)(totalNs / Math.Max(1, tickResNs));
        if (totalTicks == 0) totalTicks = 1;

        var clocks = new List<ClockConfig>();
        var stimuli = new List<StimulusEvent>();

        // Ports stimuli resolve against. Defensive against null entries in
        // the HashSet and against ports with a null Name — both have been
        // observed coming out of the parser on certain malformed or
        // incomplete port blocks.
        var portSet = new HashSet<Variable>(
            entity.Ports?.Where(p => p is not null && !string.IsNullOrEmpty(p.Name)) ?? []);
        if (portMoments != null)
        {
            foreach (var pm in portMoments)
            {
                if (string.IsNullOrEmpty(pm.PortName)) continue;
                // A port name with selectors (`p.lo`, `v(3)`) drives that part of the port.
                if (AssignmentTarget.Resolve(pm.PortName, portSet) is not { } target) continue;
                var signal = target.Base;
                var part = target.IsPartial ? target : null;

                if (pm.Values == null || pm.Values.Count == 0)
                    continue;

                // Clock-marker rows (TimeScale == "CLK") are a legacy format
                // from when students configured clocks manually. We no longer
                // expose that UI — a system clock is always auto-injected
                // below from the FPGA/override frequency. Silently skip so
                // saved lessons that still carry CLK markers don't double
                // the clock or produce noisy stimulus events.
                var firstVal = pm.Values[0];
                if (firstVal.TimeScale.Equals("CLK", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Stimulus events: each PortMomentValue is a (time, value) pair.
                // SimulationRunner now applies tick-0 events naturally so we
                // no longer pre-seed signal.Value here.
                foreach (var pmv in pm.Values)
                {
                    ulong tick = ConvertToTicks(pmv.Time, pmv.TimeScale, tickResNs);
                    stimuli.Add(new StimulusEvent
                    {
                        Tick = tick,
                        Signal = signal,
                        Value = pmv.Value,
                        Target = part,
                    });
                }
            }
        }

        // Clocks: the declared ones, or (null) the clock-named port proposed at the
        // system frequency — unless the caller already drives that port (e.g. the
        // bench feeding clk transitions taken from a GHDL VCD).
        if (systemClockMhz <= 0) systemClockMhz = 50;
        double systemPeriodNs = 1_000.0 / systemClockMhz;
        stimuli.RemoveAll(st => string.Equals(st.Value, "CLOCK", StringComparison.OrdinalIgnoreCase));
        IEnumerable<ClockSpec> declared = options?.Clocks
            ?? (ClockPortPolicy.Suggest(entity) is { } suggested && !stimuli.Any(st => ReferenceEquals(st.Signal, suggested))
                ? [new ClockSpec(suggested.Name, systemPeriodNs)]
                : []);
        foreach (var spec in declared)
        {
            var port = portSet.FirstOrDefault(p => p.Name.Equals(spec.Port, StringComparison.OrdinalIgnoreCase)
                    && p.PortType is Constants.PortType.IN or Constants.PortType.INOUT)
                ?? throw new InvalidOperationException($"clock \"{spec.Port}\" is not an input port of {entity.Name}");
            double halfPeriodNs = (spec.PeriodNs > 0 ? spec.PeriodNs : systemPeriodNs) / 2.0;
            clocks.Add(new ClockConfig
            {
                Signal = port,
                HalfPeriodTicks = (ulong)Math.Max(1, (long)Math.Round(halfPeriodNs / tickResNs)),
                InitialHigh = spec.InitialHigh,
            });
        }

        // Initial values of input ports (the actual's initial value in a testbench).
        var portInitials = new List<StimulusEvent>();
        foreach (var (name, value) in options?.PortInitials ?? [])
        {
            if (AssignmentTarget.Resolve(name, portSet) is not { } target)
                throw new InvalidOperationException($"initial value for unknown port \"{name}\"");
            portInitials.Add(new StimulusEvent { Signal = target.Base, Value = value, Target = target.IsPartial ? target : null });
        }

        // Sort stimuli by tick
        stimuli.Sort((a, b) => a.Tick.CompareTo(b.Tick));

        return new SimulationConfig
        {
            Entity = entity,
            TotalTicks = totalTicks,
            Clocks = clocks,
            Stimuli = stimuli,
            PortInitials = portInitials,
            InitialValues = options?.InitialValues ?? InitialValuePolicy.Lrm,
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
