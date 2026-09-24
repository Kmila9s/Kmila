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
        int tickResNs = TimeClock.NS_PER_TICK,
        int systemClockMhz = 50)
        => BuildConfigNs(entity, portMoments,
            totalNs: (long)Math.Round(duration.TotalSeconds * 1e9),
            tickResNs: tickResNs,
            systemClockMhz: systemClockMhz);

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
        int systemClockMhz = 50)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Convert duration to ticks
        ulong totalTicks = (ulong)(totalNs / Math.Max(1, tickResNs));
        if (totalTicks == 0) totalTicks = 1;

        var clocks = new List<ClockConfig>();
        var stimuli = new List<StimulusEvent>();

        // Build a lookup for ports by name. Defensive against null entries
        // in the HashSet and against ports with a null Name — both have
        // been observed coming out of the parser on certain malformed or
        // incomplete port blocks, and the previous direct `ToDictionary`
        // call threw a bare NullReferenceException that masked the real
        // parse issue.
        var allPorts = new Dictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        if (entity.Ports != null)
        {
            foreach (var p in entity.Ports)
            {
                if (p is null || string.IsNullOrEmpty(p.Name)) continue;
                allPorts[p.Name] = p;
            }
        }

        if (portMoments != null)
        {
            foreach (var pm in portMoments)
            {
                if (string.IsNullOrEmpty(pm.PortName) || !allPorts.TryGetValue(pm.PortName, out var signal))
                    continue;

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
                        Value = pmv.Value
                    });
                }
            }
        }

        // System clock — ALWAYS injected when the entity declares a clk/clock
        // IN port. Frequency comes from the caller (Run-bar override OR the
        // selected FPGA's default). Students no longer mark clocks manually.
        if (systemClockMhz <= 0) systemClockMhz = 50;
        double halfPeriodNs = 1_000.0 / systemClockMhz / 2.0; // ns
        ulong halfPeriodTicks = (ulong)Math.Max(1, (long)Math.Round(halfPeriodNs / tickResNs));

        // Auto-clock detection. Defensive iteration because some code paths
        // have seeded `entity.Ports` with null entries on the HashSet — any
        // direct LINQ lambda that reads `p.PortType` before guarding against
        // null p would throw the cryptic NRE students were hitting.
        Variable? autoClkPort = null;
        try
        {
            if (entity.Ports != null)
            {
                // Pass 1 — prefer an explicit IN port with a clock-like name.
                foreach (var p in entity.Ports)
                {
                    if (p is null) continue;
                    Constants.PortType dir;
                    try { dir = p.PortType; } catch { continue; }
                    if (dir != Constants.PortType.IN) continue;
                    if (!IsClockPortName(p.Name)) continue;
                    autoClkPort = p;
                    break;
                }
                // Pass 2 — some parsers leave PortType at the default when
                // direction isn't spelled out. Fall back to any port with a
                // clock-like name so a mis-typed port still lights up.
                // `is null` bypasses Variable's overloaded `==` operators,
                // which resolve to the (Variable, string) overload for a
                // null literal and wrongly return false when the LHS is null.
                if (autoClkPort is null)
                {
                    foreach (var p in entity.Ports)
                    {
                        if (p is null) continue;
                        if (!IsClockPortName(p.Name)) continue;
                        autoClkPort = p;
                        break;
                    }
                }
            }
        }
        catch (Exception scanEx)
        {
            Console.WriteLine($"[StimulusBuilder] Auto-clock scan threw {scanEx.GetType().Name}: {scanEx.Message}");
            autoClkPort = null;
        }

        // Synthesize a virtual clock when the entity doesn't declare one.
        // Previously this path just left `clocks` empty, which meant
        // combinational designs produced no clock trace at all. The user
        // asked for a clock *always* to be present in the waveform, so we
        // fabricate one here and splice it into the entity's port set so
        // SimulationRunner registers + records it alongside real signals.
        // `is null` bypasses Variable's overloaded `==` — see pass 2 note.
        if (autoClkPort is null)
        {
            var synth = new Variable("_clk")
            {
                Type     = Constants.DATATYPES.STD_LOGIC,
                Size     = 1,
                Value    = "0",
                IsPort   = true,
                PortType = Constants.PortType.IN
            };
            try { entity.Ports?.Add(synth); } catch { /* frozen collection — still drive it via clocks list */ }
            autoClkPort = synth;
        }
        else if (autoClkPort.PortType != Constants.PortType.IN)
        {
            // Real port, but mis-directed. Force input since we're driving it.
            try { autoClkPort.PortType = Constants.PortType.IN; } catch { /* read-only proxy — harmless */ }
        }

        // If the caller has already provided explicit stimulus for the
        // clock signal (e.g. SimulatorBench's autoStimulus feeding clk
        // transitions derived from a GHDL VCD), don't ALSO auto-inject a
        // clock — that would race with the externally-driven schedule and
        // produce double-edge artifacts. Bench Phase F bug #13 fix.
        //
        // Exception: a stimulus with value "CLOCK" is a marker that the
        // caller WANTS the auto-clock — it isn't a real value the runner
        // can apply. Drop those entries before deciding, then proceed with
        // auto-injection as if no clock stim were provided. The "CLOCK"
        // marker comes from sample-config builders and a handful of legacy
        // tests; with this guard, both stop suppressing the auto-clock.
        if (autoClkPort is not null)
        {
            stimuli.RemoveAll(s =>
                (ReferenceEquals(s.Signal, autoClkPort) ||
                 string.Equals(s.Signal?.Name, autoClkPort.Name, StringComparison.OrdinalIgnoreCase))
                && string.Equals(s.Value, "CLOCK", StringComparison.OrdinalIgnoreCase));
        }
        bool clkStimProvided = autoClkPort is not null &&
            stimuli.Any(s => ReferenceEquals(s.Signal, autoClkPort) ||
                             string.Equals(s.Signal?.Name, autoClkPort.Name, StringComparison.OrdinalIgnoreCase));
        if (!clkStimProvided)
        {
            clocks.Add(new ClockConfig
            {
                Signal = autoClkPort,
                HalfPeriodTicks = halfPeriodTicks,
                InitialHigh = false
            });
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
    /// True when a port name reads like a clock — exact <c>clk</c>/<c>clock</c>
    /// plus common variants (<c>clk_i</c>, <c>sys_clk</c>, <c>aclk</c>,
    /// <c>master_clock</c>, etc). Case-insensitive. Keeping this broad is
    /// safer than narrow: a stray match still works because the Run-bar owns
    /// the frequency and the user can inspect the injected clock in
    /// Waveforms; a missed match means the sim runs without a clock and the
    /// student sees an empty waveform with no obvious cause.
    /// </summary>
    public static bool IsClockPortName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim().ToLowerInvariant();
        if (n == "clk" || n == "clock") return true;
        // Token-based scan — split on common separators and check for a clk /
        // clock token rather than raw substring so "blockade" or "clocking"
        // don't falsely register.
        foreach (var token in n.Split(new[] { '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token == "clk" || token == "clock") return true;
        }
        // Prefixed / suffixed single-token forms: aclk, hclk, pclk, clki,
        // clkn, clkp, clk1, clk2 …
        if (n.Length >= 4 && n.EndsWith("clk") && n.Length <= 6) return true;
        if (n.StartsWith("clk") && n.Length <= 6) return true;
        return false;
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
