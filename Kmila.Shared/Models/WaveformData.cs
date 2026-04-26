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
    /// Total number of ticks in the simulation.
    /// </summary>
    public ulong TotalTicks { get; init; }

    /// <summary>
    /// Nanoseconds per tick (timescale).
    /// </summary>
    public int TimescaleNs { get; init; }

    /// <summary>
    /// The underlying SignalHistory for VCD export.
    /// </summary>
    public SignalHistory? SourceHistory { get; init; }

    /// <summary>
    /// JSON round-trip used by <c>SimulationSnapshotStore</c> to persist and
    /// replay runs. Drops <see cref="SourceHistory"/> — it references live
    /// Interpreter objects that can't serialize — but keeps every rendered
    /// transition, so the stored run replays pixel-identically to the
    /// original.
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
                TimescaleNs = payload.TimescaleNs,
                SourceHistory = null
            };
        }
        catch
        {
            return null;
        }
    }

    private class SerializedForm
    {
        public ulong TotalTicks { get; set; }
        public int TimescaleNs { get; set; }
        public List<WaveformSignal>? Signals { get; set; }
    }

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

        // Sample stride for the legacy uniform-sample arrays (ChartValues +
        // DisplayLabels). The SVG viewer reads from Transitions directly
        // and ignores these, but snapshot JSON round-trip still serialises
        // them for forward-compat. ~2000 samples to prevent aliasing when
        // a legacy renderer is ever re-enabled.
        int sampleStep = Math.Max(1, (int)(totalTicks / 2000));

        var waveformSignals = new List<WaveformSignal>();

        // Process each signal sorted by port type (IN first, then OUT, INOUT, INTERNAL)
        // and alphabetically within each group
        foreach (var signal in trackedSignals.OrderBy(s => GetSortOrder(s)).ThenBy(s => s.Name))
        {
            var rawHistory = history.GetSignalHistory(signal);
            if (rawHistory.Count == 0) continue;

            // Classify the signal for chart rendering
            var dataType = GetSignalDataType(signal);
            int bitWidth = GetBitWidth(signal);
            string portType = signal.IsPort ? signal.PortType.ToString() : "INTERNAL";

            // Collapse delta-level events to tick-level: when multiple deltas occur
            // within the same tick, only the last value is kept for charting
            var chartValues = new List<double?>();
            var displayLabels = new List<string?>();

            // Build a tick->value map from the raw history.
            // Raw encoding: timeMoment = tick * 1000 + delta, so dividing by 1000
            // extracts the tick number. Overwriting ensures last-delta-wins semantics.
            var tickValues = new Dictionary<ulong, object>();
            foreach (var entry in rawHistory)
            {
                ulong tick = (ulong)(entry.Item1 / 1000);
                tickValues[tick] = entry.Item2; // last delta wins
            }

            // Fill chart arrays at the high-density sample resolution so fast
            // signals (e.g. a 50 MHz clock with a 2-tick period) don't alias
            // into a flat line.
            object? lastValue = null;
            for (ulong t = 0; t < totalTicks; t += (ulong)sampleStep)
            {
                // Search backwards from the current tick to find the most recent
                // value assignment (signals hold their value until changed)
                object? value = null;
                for (ulong search = t; ; search--)
                {
                    if (tickValues.TryGetValue(search, out var v))
                    {
                        value = v;
                        break;
                    }
                    if (search == 0) break;
                }

                // If no value found searching backwards, use the initial value
                // from the raw history as a fallback (e.g., initial signal state)
                if (value == null && rawHistory.Count > 0)
                {
                    value = rawHistory[0].Item2;
                }

                double? chartVal = ConvertToChartValue(value, dataType, bitWidth);
                chartValues.Add(chartVal);

                // For non-std_logic signals (vectors, integers), generate display labels
                // only at transition points to avoid cluttering the chart
                if (dataType != "std_logic" && value != null)
                {
                    string? label = null;
                    bool isTransition = lastValue == null || !Equals(value, lastValue);
                    if (isTransition)
                    {
                        label = FormatDisplayLabel(value, dataType, bitWidth);
                    }
                    displayLabels.Add(label);
                }
                else
                {
                    displayLabels.Add(null);
                }

                lastValue = value;
            }

            // Build the transition list from the (already-collapsed) tick
            // map. One entry per tick with a recorded value, in tick order,
            // duplicates collapsed — the SVG renderer emits a horizontal
            // segment between consecutive entries and a vertical step at
            // each. No downsampling: fast signals like a 50 MHz clock show
            // up as an honest square wave instead of aliasing flat.
            var transitions = new List<WaveformTransition>(tickValues.Count);
            double? prevVal = null;
            foreach (var kv in tickValues.OrderBy(kv => kv.Key))
            {
                long ns = (long)kv.Key * timescaleNs;
                double? val = ConvertToChartValue(kv.Value, dataType, bitWidth);
                string? lbl = (dataType != "std_logic" && kv.Value != null)
                    ? FormatDisplayLabel(kv.Value, dataType, bitWidth)
                    : null;

                // Skip consecutive duplicates to keep the list small. The
                // step-function render only cares about actual changes.
                if (transitions.Count > 0 && Equals(val, prevVal) &&
                    string.Equals(lbl, transitions[^1].Label, StringComparison.Ordinal))
                {
                    continue;
                }

                transitions.Add(new WaveformTransition(ns, val, lbl));
                prevVal = val;
            }

            waveformSignals.Add(new WaveformSignal
            {
                Name = signal.Name,
                DataType = dataType,
                BitWidth = bitWidth,
                PortType = portType,
                ChartValues = chartValues,
                DisplayLabels = displayLabels,
                Transitions = transitions
            });
        }

        return new WaveformData
        {
            Signals = waveformSignals,
            TotalTicks = totalTicks,
            TimescaleNs = timescaleNs,
            SourceHistory = history
        };
    }

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
    /// Chart values: one per time label.
    /// For std_logic: 0.0 or 1.0. For vectors: integer value. Null for undefined.
    /// </summary>
    public List<double?> ChartValues { get; init; } = new();

    /// <summary>
    /// Display labels for transition points (used by datalabels plugin).
    /// Null for std_logic, hex/decimal string at transitions for vectors.
    /// </summary>
    public List<string?> DisplayLabels { get; init; } = new();

    /// <summary>
    /// Raw transitions in (time-ns, numeric-value) form — one entry per real
    /// change in the signal's history, with no downsampling. This is what
    /// the SVG waveform renderer plots: a step function from one transition
    /// to the next. Authoritative over <see cref="ChartValues"/>, which
    /// remains for any legacy code still expecting a uniform sample grid.
    /// </summary>
    public List<WaveformTransition> Transitions { get; init; } = new();
}

/// <summary>
/// A single step-function transition: at <see cref="TimeNs"/> the signal
/// becomes <see cref="Value"/>. For multi-bit signals we also carry the
/// human-readable formatted label (hex / decimal) so the SVG can stamp it
/// inside the plateau.
/// </summary>
public readonly record struct WaveformTransition(long TimeNs, double? Value, string? Label);
