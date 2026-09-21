using Interpreter.Services;
using Parser.Models;
using static Parser.Models.Constants;

namespace Kmila.Shared.Models;

/// <summary>
/// View-model for waveform display. Contains signal data arrays and time labels
/// ready for Chart.js consumption via BlazorBootstrap LineChart.
/// </summary>
public class WaveformData
{
    /// <summary>
    /// All signals with their chart-ready data.
    /// </summary>
    public List<WaveformSignal> Signals { get; init; } = new();

    /// <summary>
    /// (Deprecated.) Time labels for the legacy Chart.js categorical X-axis.
    /// The SVG viewer derives its own ruler from <see cref="TotalTicks"/> +
    /// <see cref="TimescaleNs"/>; kept as an empty list for JSON-shape
    /// compatibility with older snapshot payloads.
    /// </summary>
    public List<string> TimeLabels { get; init; } = new();

    /// <summary>
    /// Total number of ticks in the simulation. Mutable because the live
    /// merge path grows the X-axis as new transitions arrive — see
    /// <see cref="MergeFrom"/>.
    /// </summary>
    public ulong TotalTicks { get; set; }

    /// <summary>
    /// Per-signal append watermark used by the live merge path. Indexed by
    /// the original <see cref="Variable"/> reference (the simulator keeps a
    /// stable instance per signal for the run's lifetime). Private, so
    /// System.Text.Json's default public-only contract leaves it alone —
    /// snapshots round-trip through <see cref="SerializedForm"/> only.
    /// </summary>
    private readonly Dictionary<Variable, int> _liveWatermarks = new();

    /// <summary>
    /// Name → signal index for O(1) lookup during the live <see cref="MergeFrom"/>
    /// path. Built lazily from <see cref="Signals"/> on first merge and kept in
    /// sync as new signals are discovered mid-run. Replaces the previous
    /// per-signal <c>Signals.FirstOrDefault(s =&gt; s.Name == name)</c> scan, which
    /// was O(signals²) every 200 ms tick and a contributor to the live-render
    /// stall on designs with many signals. Purely a visualization optimisation —
    /// the merged data is identical.
    /// </summary>
    private Dictionary<string, WaveformSignal>? _byName;

    /// <summary>
    /// Nanoseconds per tick (timescale).
    /// </summary>
    public int TimescaleNs { get; init; }

    // SourceHistory used to live here as a back-reference to the simulator's
    // SignalHistory for VCD export, but it's never actually read — VCD export
    // goes through Builder.SimulationHistory directly. Holding it kept the
    // entire raw signal history (multi-MB per run) rooted for as long as the
    // WaveformData lived; removed to stop the leak.

    /// <summary>
    /// JSON round-trip used by <c>SimulationSnapshotStore</c> to persist and
    /// replay runs. Keeps every rendered transition so the stored run replays
    /// pixel-identically to the original.
    /// </summary>
    public string ToJson()
    {
        var payload = new SerializedForm
        {
            TotalTicks  = TotalTicks,
            TimescaleNs = TimescaleNs,
            Signals     = Signals
        };
        return System.Text.Json.JsonSerializer.Serialize(payload,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    }

    public static WaveformData? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize<SerializedForm>(json);
            if (payload == null) return null;
            return new WaveformData
            {
                Signals     = payload.Signals ?? new(),
                TimeLabels  = new(),
                TotalTicks  = payload.TotalTicks,
                TimescaleNs = payload.TimescaleNs
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// JSON-serialisation DTO for <see cref="WaveformData"/>. Decoupled from the public
    /// shape so the on-disk format can evolve without breaking the in-memory contract.
    /// </summary>
    private class SerializedForm
    {
        /// <summary>Total simulated ticks captured by the waveform.</summary>
        public ulong TotalTicks { get; set; }
        /// <summary>Timescale resolution in nanoseconds per tick (canonical 10).</summary>
        public int TimescaleNs { get; set; }
        /// <summary>Per-signal sampled transitions.</summary>
        public List<WaveformSignal>? Signals { get; set; }
    }

    /// <summary>
    /// Creates an empty live waveform that callers grow incrementally by
    /// calling <see cref="MergeFrom"/> on each progress tick. The Editor
    /// uses this for real-time display — the SVG viewer reads
    /// <see cref="Signals"/> and <see cref="TotalTicks"/> on every render
    /// and so picks up appended transitions automatically.
    /// </summary>
    public static WaveformData CreateEmpty(int timescaleNs)
        => new WaveformData { TimescaleNs = timescaleNs };

    /// <summary>
    /// Appends transitions that have arrived in <paramref name="history"/>
    /// since the last call. New signals discovered mid-run are inserted
    /// (signals are then re-sorted by port direction so the Y-order stays
    /// stable across renders). Idempotent: re-running with no new entries
    /// is a cheap no-op via the per-signal watermarks.
    ///
    /// Returns <c>true</c> when the merge appended at least one transition
    /// or added a new signal — the live-update path uses this to skip
    /// <see cref="Microsoft.AspNetCore.Components.ComponentBase.StateHasChanged"/>
    /// on no-op fires (e.g. periodic ticks while the simulator is paused).
    /// </summary>
    public bool MergeFrom(SignalHistory history)
    {
        var trackedSignals = history.GetTrackedSignals();
        bool addedSignal = false;
        bool appendedAny = false;
        long maxTimeMoment = 0;

        // Lazily build the name→signal index from whatever is already in
        // Signals (e.g. after a snapshot reload), then keep it in sync below.
        if (_byName == null)
        {
            _byName = new Dictionary<string, WaveformSignal>(Signals.Count);
            foreach (var s in Signals) _byName[s.Name] = s;
        }

        foreach (var signal in trackedSignals)
        {
            var name = signal.Name;
            if (!_byName.TryGetValue(name, out var sig))
            {
                sig = new WaveformSignal
                {
                    Name = name,
                    DataType = GetSignalDataType(signal),
                    BitWidth = GetBitWidth(signal),
                    PortType = signal.IsPort ? signal.PortType.ToString() : "INTERNAL"
                };
                Signals.Add(sig);
                _byName[name] = sig;
                addedSignal = true;
            }

            int from = _liveWatermarks.GetValueOrDefault(signal, 0);
            var transitions = sig.Transitions;
            // Capture so the inner closure can flip the outer-scope flag.
            bool localAppended = false;
            int newCount = history.ReadSince(signal, from, (timeMoment, value) =>
            {
                if (timeMoment > maxTimeMoment) maxTimeMoment = timeMoment;
                ulong tick = (ulong)(timeMoment / 1000);
                long ns = (long)tick * TimescaleNs;
                double? val = ConvertToChartValue(value, sig.DataType, sig.BitWidth);
                string? lbl = (sig.DataType != "std_logic" && value != null)
                    ? FormatDisplayLabel(value, sig.DataType, sig.BitWidth)
                    : null;
                // Display-only per-bit string for buses — see the static path in
                // FromSignalHistory and WaveformTransition.Bits. Not exported.
                string? bits = sig.DataType == "std_logic_vector" ? value?.ToString() : null;

                // Same-tick collapse: deltas inside one tick → keep last.
                if (transitions.Count > 0 && transitions[^1].TimeNs == ns)
                {
                    transitions[^1] = new WaveformTransition(ns, val, lbl, 0, bits);
                    localAppended = true;
                    return;
                }
                // Same-state skip: the step function only cares about real changes.
                if (transitions.Count > 0 && Equals(val, transitions[^1].Value) &&
                    string.Equals(lbl, transitions[^1].Label, StringComparison.Ordinal))
                {
                    return;
                }
                transitions.Add(new WaveformTransition(ns, val, lbl, 0, bits));
                localAppended = true;
            });
            _liveWatermarks[signal] = newCount;
            if (localAppended) appendedAny = true;
        }

        // Grow the X-axis to whatever we've now seen.
        ulong maxTick = maxTimeMoment > 0 ? (ulong)(maxTimeMoment / 1000) : 0;
        ulong observed = maxTick + 1;
        if (observed > TotalTicks) TotalTicks = observed;

        // Re-sort the signal list when new entries appeared so the Y-order
        // (IN → OUT → INOUT → INTERNAL, alphabetical inside each group)
        // stays stable as the simulator registers signals lazily.
        if (addedSignal)
        {
            Signals.Sort((a, b) =>
            {
                int oa = SortOrderForPortString(a.PortType);
                int ob = SortOrderForPortString(b.PortType);
                int c = oa.CompareTo(ob);
                return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }

        return appendedAny || addedSignal;
    }

    private static int SortOrderForPortString(string port) => port switch
    {
        "IN"    => 0,
        "OUT"   => 1,
        "INOUT" => 2,
        _       => 3
    };

    /// <summary>
    /// Creates WaveformData from a SignalHistory produced by the simulation engine.
    /// Collapses the tick*1000+delta encoding to tick-level transitions.
    /// </summary>
    /// <param name="history">The signal history produced by the VHDL simulation engine.</param>
    /// <param name="timescaleNs">Nanoseconds per tick used for time label formatting. Defaults to 10.</param>
    /// <returns>A new <see cref="WaveformData"/> instance with chart-ready signal data and time labels.</returns>
    public static WaveformData FromSignalHistory(SignalHistory history, int timescaleNs = 10)
    {
        // Retrieve all signals being tracked and determine the simulation time boundaries
        var trackedSignals = history.GetTrackedSignals();
        var (minTime, maxTime) = history.GetTimeRange();

        // Determine tick range from the time encoding (timeMoment = tick * 1000 + delta).
        // The engine encodes each time moment as tick*1000+delta to support multiple
        // signal changes within the same tick (delta events).
        ulong maxTick = maxTime > 0 ? (ulong)(maxTime / 1000) : 0;
        ulong totalTicks = maxTick + 1;

        var waveformSignals = new List<WaveformSignal>();

        // Process each signal sorted by port type (IN first, then OUT, INOUT, INTERNAL)
        // and alphabetically within each group
        foreach (var signal in trackedSignals.OrderBy(s => GetSortOrder(s)).ThenBy(s => s.Name))
        {
            // Phase B+ Deep AST: pull the detailed history including per-
            // transition source lines. Falls back to a 0 source line when
            // the underlying recording wasn't tagged (synthetic / legacy).
            var rawHistory = history.GetSignalHistoryDetailed(signal);
            if (rawHistory.Count == 0) continue;

            // Classify the signal for chart rendering
            var dataType = GetSignalDataType(signal);
            int bitWidth = GetBitWidth(signal);
            string portType = signal.IsPort ? signal.PortType.ToString() : "INTERNAL";

            // Collapse delta-level events to tick-level: when multiple deltas
            // occur within the same tick we only keep the last value. The raw
            // history is roughly tick-ordered already, but a final OrderBy
            // guarantees monotonic time even if the engine flushed deltas out
            // of order during a stop.
            //
            // Build the transition list in a single forward pass over the raw
            // history. The previous implementation iterated every Nth tick and
            // searched backwards through a tick→value dictionary, which scaled
            // with simulation length even when the signal barely changed.
            var transitions = new List<WaveformTransition>();
            ulong? lastTick = null;
            double? prevVal = null;
            string? prevLbl = null;
            foreach (var entry in rawHistory)
            {
                ulong tick = (ulong)(entry.TimeMoment / 1000);
                long ns = (long)tick * timescaleNs;
                double? val = ConvertToChartValue(entry.Value, dataType, bitWidth);
                string? lbl = (dataType != "std_logic" && entry.Value != null)
                    ? FormatDisplayLabel(entry.Value, dataType, bitWidth)
                    : null;
                // Display-only: keep the raw per-bit string for buses so the
                // viewer can expand into per-bit waves (preserving U/Z/X). Not
                // read by any export path — see WaveformTransition.Bits.
                string? bits = dataType == "std_logic_vector" ? entry.Value?.ToString() : null;
                int srcLine = entry.SourceLine;

                if (lastTick == tick && transitions.Count > 0)
                {
                    // Multiple deltas inside the same tick — keep only the last
                    // value (matches the previous tick→value dictionary's
                    // last-write-wins semantics). The source line from the
                    // winning (last) delta wins too.
                    transitions[^1] = new WaveformTransition(ns, val, lbl, srcLine, bits);
                    prevVal = val;
                    prevLbl = lbl;
                    continue;
                }

                if (transitions.Count > 0 && Equals(val, prevVal) &&
                    string.Equals(lbl, prevLbl, StringComparison.Ordinal))
                {
                    // Same logical state as the previous tick — skip to keep
                    // the step-function list compact.
                    continue;
                }

                transitions.Add(new WaveformTransition(ns, val, lbl, srcLine, bits));
                lastTick = tick;
                prevVal = val;
                prevLbl = lbl;
            }

            waveformSignals.Add(new WaveformSignal
            {
                Name = signal.Name,
                DataType = dataType,
                BitWidth = bitWidth,
                PortType = portType,
                Transitions = transitions
            });
        }

        return new WaveformData
        {
            Signals = waveformSignals,
            TotalTicks = totalTicks,
            TimescaleNs = timescaleNs
        };
    }

    /// <summary>
    /// Resolves a signal's value at an absolute simulation <paramref name="tick"/>
    /// using last-write-wins step semantics (the nearest transition at or before
    /// the cursor). Shared by the schematic's Debug-mode wire colouring and the
    /// waveform viewer's value column so both read values identically. Returns
    /// the numeric value and the pre-formatted label (hex/decimal for buses),
    /// both <c>null</c> when the signal has no sample yet at that time.
    /// </summary>
    public (double? Value, string? Label) ValueAt(WaveformSignal signal, ulong tick)
        => signal.ValueAtNs((long)tick * Math.Max(1, TimescaleNs));

    /// <summary>
    /// Determines the sort order for a signal based on its port type.
    /// Inputs are displayed first, then outputs, then bidirectional, and finally internal signals.
    /// </summary>
    /// <param name="signal">The variable/signal to evaluate.</param>
    /// <returns>An integer sort key: 0 for IN, 1 for OUT, 2 for INOUT, 3 for internal/unknown.</returns>
    private static int GetSortOrder(Variable signal)
    {
        if (!signal.IsPort) return 3; // internal signals last
        return signal.PortType switch
        {
            PortType.IN => 0,
            PortType.OUT => 1,
            PortType.INOUT => 2,
            _ => 3
        };
    }

    /// <summary>
    /// Maps a VHDL variable's type to a simplified chart data type string.
    /// </summary>
    /// <param name="signal">The variable/signal whose type to classify.</param>
    /// <returns>
    /// "std_logic" for single-bit types (STD_LOGIC, BIT, BOOLEAN),
    /// "std_logic_vector" for multi-bit types, or "integer" for numeric types.
    /// Falls back to size-based detection for unknown types.
    /// </returns>
    private static string GetSignalDataType(Variable signal)
    {
        return signal.Type switch
        {
            DATATYPES.STD_LOGIC or DATATYPES.BIT => "std_logic",
            DATATYPES.STD_LOGIC_VECTOR => "std_logic_vector",
            DATATYPES.INTEGER or DATATYPES.NATURAL => "integer",
            DATATYPES.BOOLEAN => "std_logic",
            _ => signal.Size > 1 ? "std_logic_vector" : "std_logic"
        };
    }

    /// <summary>
    /// Gets the bit width of a signal, defaulting to 1 for signals with no explicit size.
    /// </summary>
    /// <param name="signal">The variable/signal to evaluate.</param>
    /// <returns>The bit width of the signal, minimum of 1.</returns>
    private static int GetBitWidth(Variable signal)
    {
        if (signal.Size > 0) return signal.Size;
        return 1;
    }

    /// <summary>
    /// Converts a signal value to a numeric chart value suitable for Chart.js rendering.
    /// </summary>
    /// <param name="value">The raw signal value from the simulation history.</param>
    /// <param name="dataType">The simplified data type: "std_logic", "std_logic_vector", or "integer".</param>
    /// <param name="bitWidth">The bit width of the signal (used for vector conversions).</param>
    /// <returns>
    /// A double value for charting: 0.0/1.0 for std_logic, the integer value for integers,
    /// the binary-to-long conversion for vectors, or null if the value is undefined or unparseable.
    /// </returns>
    private static double? ConvertToChartValue(object? value, string dataType, int bitWidth)
    {
        if (value == null) return null;

        string str = value.ToString() ?? "";

        // Single-bit signals: map to 0.0 or 1.0 for digital waveform display
        if (dataType == "std_logic")
        {
            return str switch
            {
                "1" or "true" or "True" => 1.0,
                "0" or "false" or "False" => 0.0,
                _ => null
            };
        }

        // Integer signals: parse directly as numeric value
        if (dataType == "integer")
        {
            if (int.TryParse(str, out int intVal))
                return intVal;
            return null;
        }

        // std_logic_vector: convert binary string to integer for charting
        if (str.All(c => c == '0' || c == '1'))
        {
            try
            {
                long longVal = Convert.ToInt64(str, 2);
                return longVal;
            }
            catch
            {
                return null;
            }
        }

        // Fallback: attempt direct integer parse for non-binary string representations
        if (int.TryParse(str, out int parsed))
            return parsed;

        return null;
    }

    /// <summary>
    /// Formats a signal value into a human-readable display label for chart annotations.
    /// Integers are shown as-is; vectors are converted to zero-padded hexadecimal.
    /// </summary>
    /// <param name="value">The raw signal value to format.</param>
    /// <param name="dataType">The simplified data type: "std_logic", "std_logic_vector", or "integer".</param>
    /// <param name="bitWidth">The bit width of the signal, used to determine hex digit padding.</param>
    /// <returns>A formatted string label, or null if the value is null.</returns>
    private static string? FormatDisplayLabel(object? value, string dataType, int bitWidth)
    {
        if (value == null) return null;
        string str = value.ToString() ?? "";

        if (dataType == "integer")
            return str;

        // For vectors, convert binary string to zero-padded hexadecimal
        if (str.All(c => c == '0' || c == '1'))
        {
            try
            {
                long longVal = Convert.ToInt64(str, 2);
                // Calculate the number of hex digits needed to represent the full bit width
                int hexDigits = (bitWidth + 3) / 4;
                return $"0x{longVal:X}".PadLeft(hexDigits + 2);
            }
            catch
            {
                return str;
            }
        }

        return str;
    }

    /// <summary>
    /// Formats a time value in nanoseconds into the most appropriate human-readable unit.
    /// Automatically scales to ns, us, ms, or s depending on the magnitude.
    /// </summary>
    /// <param name="ns">The time value in nanoseconds.</param>
    /// <returns>A formatted time string with the appropriate unit suffix (e.g., "100ns", "1.5us", "10ms", "1s").</returns>
    private static string FormatTimeLabel(long ns)
    {
        if (ns < 1_000) return $"{ns}ns";
        if (ns < 1_000_000) return $"{ns / 1_000.0:G4}us";
        if (ns < 1_000_000_000) return $"{ns / 1_000_000.0:G4}ms";
        return $"{ns / 1_000_000_000.0:G4}s";
    }
}

/// <summary>
/// Represents a single signal's waveform data for chart display.
/// </summary>
public class WaveformSignal
{
    /// <summary>
    /// Signal name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Data type: "std_logic", "std_logic_vector", or "integer".
    /// </summary>
    public string DataType { get; init; } = "std_logic";

    /// <summary>
    /// Bit width of the signal.
    /// </summary>
    public int BitWidth { get; init; } = 1;

    /// <summary>
    /// Port direction: "IN", "OUT", "INOUT", or "INTERNAL".
    /// </summary>
    public string PortType { get; init; } = "INTERNAL";

    /// <summary>
    /// Raw transitions in (time-ns, numeric-value) form — one entry per real
    /// change in the signal's history, with no downsampling. This is what
    /// the SVG waveform renderer plots: a step function from one transition
    /// to the next. Always ordered ascending by <see cref="WaveformTransition.TimeNs"/>,
    /// which the binary-search helpers below rely on.
    /// </summary>
    public List<WaveformTransition> Transitions { get; init; } = new();

    /// <summary>
    /// Index of the last transition whose <see cref="WaveformTransition.TimeNs"/>
    /// is at or before <paramref name="ns"/>, or -1 when none exists (the cursor
    /// sits before the first transition). O(log N) binary search — replaces the
    /// O(N) linear scans the viewer/schematic did from index 0 on every render.
    /// </summary>
    public int IndexAtOrBefore(long ns)
    {
        var t = Transitions;
        int lo = 0, hi = t.Count - 1, res = -1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (t[mid].TimeNs <= ns) { res = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return res;
    }

    /// <summary>
    /// Value + formatted label at absolute time <paramref name="ns"/> using
    /// last-write-wins step semantics (nearest sample at or before). Returns
    /// (null, null) when the signal has no sample yet at that time.
    /// </summary>
    public (double? Value, string? Label) ValueAtNs(long ns)
    {
        int idx = IndexAtOrBefore(ns);
        if (idx < 0) return (null, null);
        var t = Transitions[idx];
        return (t.Value, t.Label);
    }

    /// <summary>
    /// Returns only the transitions needed to draw the [<paramref name="startNs"/>,
    /// <paramref name="endNs"/>] window, decimated to roughly one point per pixel
    /// (<paramref name="maxPoints"/> ≈ visible pixel width). The window carries the
    /// entry level (last transition at or before the left edge) so a clipped signal
    /// starts at its real value. This bounds render cost by the viewport width
    /// instead of the total history length — the essence of the windowed viewer.
    ///
    /// Decimation preserves the first and last transition of every pixel bucket
    /// plus the bucket's min- and max-valued samples, so narrow pulses (e.g. a fast
    /// clock at zoomed-out scales) still show up as activity rather than vanishing.
    /// Purely visual: the underlying <see cref="Transitions"/> data is never mutated.
    /// </summary>
    public WaveformWindow GetWindow(long startNs, long endNs, int maxPoints)
    {
        var t = Transitions;
        if (t.Count == 0)
            return new WaveformWindow(default, false, Array.Empty<WaveformTransition>());

        int entryIdx = IndexAtOrBefore(startNs);
        bool haveEntry = entryIdx >= 0;
        WaveformTransition entry = haveEntry ? t[entryIdx] : t[0];

        int first = entryIdx + 1;                 // first transition strictly after startNs
        int last = IndexAtOrBefore(endNs);        // last transition at or before endNs
        if (last < first)
            return new WaveformWindow(entry, haveEntry, Array.Empty<WaveformTransition>());

        int count = last - first + 1;
        if (maxPoints <= 0 || count <= maxPoints)
        {
            var slice = new List<WaveformTransition>(count);
            for (int i = first; i <= last; i++) slice.Add(t[i]);
            return new WaveformWindow(entry, haveEntry, slice);
        }

        // Bucket the visible span into ~maxPoints columns and keep at most four
        // representative samples per bucket (first, min, max, last).
        long span = endNs - startNs;
        if (span <= 0) span = 1;
        int buckets = Math.Max(1, maxPoints);
        var result = new List<WaveformTransition>(Math.Min(count, buckets * 4));
        int lastIdx = first - 1;
        void Emit(int idx) { if (idx > lastIdx) { result.Add(t[idx]); lastIdx = idx; } }

        int i2 = first;
        for (int b = 0; b < buckets && i2 <= last; b++)
        {
            long bucketEnd = startNs + (long)((double)(b + 1) / buckets * span);
            int bStart = i2;
            int bEnd = -1;
            int minIdx = -1, maxIdx = -1;
            while (i2 <= last && t[i2].TimeNs <= bucketEnd)
            {
                bEnd = i2;
                double? v = t[i2].Value;
                if (v.HasValue)
                {
                    if (minIdx < 0 || v.Value < (t[minIdx].Value ?? double.MaxValue)) minIdx = i2;
                    if (maxIdx < 0 || v.Value > (t[maxIdx].Value ?? double.MinValue)) maxIdx = i2;
                }
                i2++;
            }
            if (bEnd < 0) continue; // empty bucket
            Emit(bStart);
            if (minIdx >= 0 && maxIdx >= 0)
            {
                Emit(Math.Min(minIdx, maxIdx));
                Emit(Math.Max(minIdx, maxIdx));
            }
            Emit(bEnd);
        }
        return new WaveformWindow(entry, haveEntry, result);
    }
}

/// <summary>
/// A decimated slice of a signal's transitions for one viewport window, as
/// produced by <see cref="WaveformSignal.GetWindow"/>. <see cref="Entry"/> is
/// the level at the left edge (valid only when <see cref="HaveEntry"/> is true);
/// <see cref="Points"/> are the in-window transitions to draw, time-ascending.
/// </summary>
public readonly record struct WaveformWindow(
    WaveformTransition Entry,
    bool HaveEntry,
    IReadOnlyList<WaveformTransition> Points);

/// <summary>
/// A single step-function transition: at <see cref="TimeNs"/> the signal
/// becomes <see cref="Value"/>. For multi-bit signals we also carry the
/// human-readable formatted label (hex / decimal) so the SVG can stamp it
/// inside the plateau.
/// <para>
/// <see cref="Bits"/> is a display-only mirror of the raw per-bit string
/// ("0","1","U","Z","X",..., MSB-first), populated only for
/// <c>std_logic_vector</c> signals (<c>null</c> for std_logic / integer).
/// It lets the viewer expand a bus into one wave per bit while preserving the
/// undefined states (U/Z/X) that the aggregated <see cref="Value"/> collapses
/// away. It is storage for visualization, not simulation state: nothing in the
/// value/label/export path reads it, so the VCD/CSV output is unaffected.
/// </para>
/// </summary>
public readonly record struct WaveformTransition(long TimeNs, double? Value, string? Label, int SourceLine = 0, string? Bits = null);
