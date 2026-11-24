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
    /// <param name="tickResNs">Requested tick in nanoseconds (default 10); the run narrows it to the design's static delays, in fs.</param>
    /// <param name="systemClockMhz">Effective system clock frequency (MHz) for this run.</param>
    /// <param name="options">Clocks, port initial values and initial-value policy (null: propose the clock by name, LRM).</param>
    /// <returns>A fully configured <see cref="SimulationConfig"/> ready for execution by SimulationRunner.</returns>
    public static SimulationConfig BuildConfig(
        Entity entity,
        List<PortMomentConfiguration>? portMoments,
        TimeSpan duration,
        int tickResNs = TimeClock.NS_PER_TICK,
        int systemClockMhz = 50,
        StimulusOptions? options = null,
        ElaborationLineMap? lineMap = null)
        => BuildConfigNs(entity, portMoments,
            totalNs: (long)Math.Round(duration.TotalSeconds * 1e9),
            tickResNs: tickResNs,
            systemClockMhz: systemClockMhz,
            options: options,
            lineMap: lineMap);

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
        StimulusOptions? options = null,
        ElaborationLineMap? lineMap = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Stimuli and clock edges, first in femtoseconds: the tick is chosen to land on them.
        var timedStimuli = new List<(long Fs, Variable Signal, string Value, AssignmentTarget? Part)>();
        var clockSpecs = new List<(Variable Port, long HalfFs, bool InitialHigh)>();

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
                    timedStimuli.Add((FsOf(pmv.Time, pmv.TimeScale), signal, pmv.Value, part));
            }
        }

        // Clocks: the declared ones, or (null) the clock-named port proposed at the
        // system frequency — unless the caller already drives that port (e.g. the
        // bench feeding clk transitions taken from a GHDL VCD).
        if (systemClockMhz <= 0) systemClockMhz = 50;
        double systemPeriodNs = 1_000.0 / systemClockMhz;
        timedStimuli.RemoveAll(st => string.Equals(st.Value, "CLOCK", StringComparison.OrdinalIgnoreCase));
        IEnumerable<ClockSpec> declared = options?.Clocks
            ?? (ClockPortPolicy.Suggest(entity) is { } suggested && !timedStimuli.Any(st => ReferenceEquals(st.Signal, suggested))
                ? [new ClockSpec(suggested.Name, systemPeriodNs)]
                : []);
        foreach (var spec in declared)
        {
            var port = portSet.FirstOrDefault(p => p.Name.Equals(spec.Port, StringComparison.OrdinalIgnoreCase)
                    && p.PortType is Constants.PortType.IN or Constants.PortType.INOUT)
                ?? throw new InvalidOperationException($"clock \"{spec.Port}\" is not an input port of {entity.Name}");
            double halfPeriodNs = (spec.PeriodNs > 0 ? spec.PeriodNs : systemPeriodNs) / 2.0;
            clockSpecs.Add((port, Math.Max(1, (long)Math.Round(halfPeriodNs * PhysicalTime.FsPerNs)), spec.InitialHigh));
        }

        // The tick lands exactly on every static delay of the design, every stimulus and
        // every clock edge, down to 1 fs (`wait for 500 ps` or an input at 5 ns under a
        // 10 ns request, G103).
        long tickFs = StaticDelays.TickFs(entity, tickResNs > 0 ? tickResNs * PhysicalTime.FsPerNs : PhysicalTime.DefaultTickFs);
        foreach (long fs in timedStimuli.Select(st => st.Fs).Concat(clockSpecs.Select(c => c.HalfFs)))
            if (fs > 0) tickFs = StaticDelays.Gcd(tickFs, fs);

        ulong totalTicks = (ulong)Math.Max(1, totalNs * PhysicalTime.FsPerNs / tickFs);
        // History instants are tick*1000+delta in a long: refuse a run that would overflow them.
        if (totalTicks > long.MaxValue / 1000)
            throw new InvalidOperationException(
                $"a run of {totalNs} ns at a tick of {tickFs} fs exceeds the engine's time range; shorten the run or use rounder delays");

        var stimuli = timedStimuli
            .Select(st => new StimulusEvent { Tick = (ulong)(st.Fs / tickFs), Signal = st.Signal, Value = st.Value, Target = st.Part })
            .OrderBy(st => st.Tick)
            .ToList();
        var clocks = clockSpecs
            .Select(c => new ClockConfig { Signal = c.Port, HalfPeriodTicks = (ulong)(c.HalfFs / tickFs), InitialHigh = c.InitialHigh })
            .ToList();

        // Initial values of input ports (the actual's initial value in a testbench).
        var portInitials = new List<StimulusEvent>();
        foreach (var (name, value) in options?.PortInitials ?? [])
        {
            if (AssignmentTarget.Resolve(name, portSet) is not { } target)
                throw new InvalidOperationException($"initial value for unknown port \"{name}\"");
            portInitials.Add(new StimulusEvent { Signal = target.Base, Value = value, Target = target.IsPartial ? target : null });
        }

        return new SimulationConfig
        {
            Entity = entity,
            TotalTicks = totalTicks,
            TickFs = tickFs,
            LineMap = lineMap,
            Clocks = clocks,
            Stimuli = stimuli,
            PortInitials = portInitials,
            InitialValues = options?.InitialValues ?? InitialValuePolicy.Lrm,
        };
    }

    /// <summary>The time <paramref name="time"/> <paramref name="timeScale"/> (NS, US, MS, S, M, H; NS by default) in femtoseconds.</summary>
    private static long FsOf(int time, string timeScale) =>
        time * PhysicalTime.UnitFs[timeScale.ToUpperInvariant() switch
        {
            "US" => "us", "MS" => "ms", "S" => "sec", "M" => "min", "H" => "hr",
            _ => "ns",
        }];
}
