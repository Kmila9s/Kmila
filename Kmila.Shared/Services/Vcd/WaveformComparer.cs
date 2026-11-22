namespace Kmila.Shared.Services.Vcd;

/// <summary>How two sampled values are judged equal.</summary>
public enum ValueEquality
{
    /// <summary>
    /// The nine values compared exactly (case-insensitive); a vector written
    /// without its leading zeros is padded to the signal width. The bench and
    /// test-kit mode (strict since v3.1.5: U/X/Z and t=0 count).
    /// </summary>
    PaddedExact,

    /// <summary>
    /// Case-insensitive text, except that two clean binary buses compare by
    /// numeric value (<c>0011</c> = <c>11</c>). The <c>kmila diff</c> mode.
    /// </summary>
    NumericBuses,
}

/// <summary>What <see cref="WaveformComparer.Compare"/> compares and how.</summary>
public sealed record CompareOptions
{
    /// <summary>Signals to compare by leaf name; null compares every name present in either dump.</summary>
    public IReadOnlyCollection<string>? Signals { get; init; }

    /// <summary>
    /// Bucket in femtoseconds each change is snapped to (nearest), keeping the
    /// last value per bucket; 0 compares raw times. The bench snaps to 1 ns:
    /// GHDL folds delta cycles into the instant while Kmila writes them as
    /// +1 ps offsets.
    /// </summary>
    public long SnapFs { get; init; }

    /// <summary>Do not count a mismatch at t=0 (a documented initial-value divergence).</summary>
    public bool IgnoreT0 { get; init; }

    /// <summary>Value assumed before a signal's first change; null means "no value".</summary>
    public string? InitialValue { get; init; }

    /// <summary>Equality rule for sampled values.</summary>
    public ValueEquality Equality { get; init; } = ValueEquality.PaddedExact;

    /// <summary>Mismatches recorded per signal (all of them are counted).</summary>
    public int MaxExamples { get; init; } = 5;

    /// <summary>The bench / test-kit comparison: exact nine values, 1 ns snap, values start at U.</summary>
    public static CompareOptions Strict(IEnumerable<string> ports, bool ignoreT0) => new()
    {
        Signals = ports.ToList(),
        SnapFs = 1_000_000,
        IgnoreT0 = ignoreT0,
        InitialValue = "U",
    };
}

/// <summary>Which dump lacks a compared signal.</summary>
public enum MissingSide { None, Expected, Actual, Both }

/// <summary>One mismatch: the time and the two values as compared.</summary>
public readonly record struct ValueMismatch(long TimeFs, string? Expected, string? Actual);

/// <summary>Comparison result for one signal.</summary>
public sealed class SignalDiff
{
    /// <summary>Leaf name compared.</summary>
    public required string Signal { get; init; }

    /// <summary>Width in the expected dump (the actual one's when the expected lacks the signal).</summary>
    public int Width { get; init; } = 1;

    /// <summary>Width in the actual dump.</summary>
    public int ActualWidth { get; init; } = 1;

    /// <summary>Which dump lacks the signal; when not <see cref="MissingSide.None"/> nothing was sampled.</summary>
    public MissingSide Missing { get; init; }

    /// <summary>Instants sampled (union of both sides' change times).</summary>
    public int Samples { get; set; }

    /// <summary>Instants whose values differ.</summary>
    public int Mismatches { get; set; }

    /// <summary>The first <see cref="CompareOptions.MaxExamples"/> mismatches.</summary>
    public List<ValueMismatch> Examples { get; } = new();

    /// <summary>True when both dumps have the signal and every sample matches.</summary>
    public bool Equal => Missing == MissingSide.None && Mismatches == 0;

    /// <summary>Which dump lacks the signal, in words (<paramref name="expected"/> and <paramref name="actual"/> name the dumps); null when both have it.</summary>
    public string? MissingFrom(string expected, string actual) => Missing switch
    {
        MissingSide.Expected => $"missing from {expected}",
        MissingSide.Actual => $"missing from {actual}",
        MissingSide.Both => $"missing from {expected} and {actual}",
        _ => null,
    };
}

/// <summary>
/// The one waveform comparator: samples both step functions at every change
/// time of either side and counts the instants whose values differ. Used by the
/// bench and test kit (<see cref="CompareOptions.Strict"/>) and by <c>kmila diff</c>.
/// </summary>
public static class WaveformComparer
{
    /// <summary>Compares <paramref name="actual"/> against <paramref name="expected"/>, one result per signal.</summary>
    public static List<SignalDiff> Compare(VcdDocument expected, VcdDocument actual, CompareOptions options)
    {
        var names = options.Signals ?? expected.Signals.Select(s => s.Name)
            .Concat(actual.Signals.Select(s => s.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var result = new List<SignalDiff>();
        foreach (var name in names)
        {
            expected.ByName.TryGetValue(name, out var e);
            actual.ByName.TryGetValue(name, out var a);
            if (e is null || a is null)
            {
                result.Add(new SignalDiff
                {
                    Signal = name,
                    Width = (e ?? a)?.Width ?? 1,
                    ActualWidth = a?.Width ?? 1,
                    Missing = e is null && a is null ? MissingSide.Both : e is null ? MissingSide.Expected : MissingSide.Actual,
                });
                continue;
            }
            result.Add(CompareSignal(name, e, a, options));
        }
        return result;
    }

    private static SignalDiff CompareSignal(string name, VcdSignal e, VcdSignal a, CompareOptions o)
    {
        var diff = new SignalDiff { Signal = name, Width = e.Width, ActualWidth = a.Width };
        var es = Steps(e, o);
        var acts = Steps(a, o);
        int ei = 0, ai = 0;
        string? ev = o.InitialValue, av = o.InitialValue;
        // Merge the two ascending step lists: sample at every instant either side changes.
        while (ei < es.Count || ai < acts.Count)
        {
            long t = Math.Min(ei < es.Count ? es[ei].TimeFs : long.MaxValue, ai < acts.Count ? acts[ai].TimeFs : long.MaxValue);
            if (ei < es.Count && es[ei].TimeFs == t) ev = es[ei++].Value;
            if (ai < acts.Count && acts[ai].TimeFs == t) av = acts[ai++].Value;
            diff.Samples++;
            if (Same(ev, av, e.Width, a.Width, o.Equality) || (o.IgnoreT0 && t == 0)) continue;
            diff.Mismatches++;
            if (diff.Examples.Count < o.MaxExamples) diff.Examples.Add(new ValueMismatch(t, ev, av));
        }
        return diff;
    }

    /// <summary>
    /// The signal's changes snapped to the bucket and normalized for the equality
    /// rule, one per instant (the last value wins). VCD times only grow and snapping
    /// is monotonic, so a same-instant change can only follow the previous one.
    /// </summary>
    private static List<VcdChange> Steps(VcdSignal s, CompareOptions o)
    {
        var steps = new List<VcdChange>(s.Transitions.Count);
        foreach (var c in s.Transitions)
        {
            long t = o.SnapFs > 0 ? (c.TimeFs + o.SnapFs / 2) / o.SnapFs * o.SnapFs : c.TimeFs;
            var step = new VcdChange(t, o.Equality == ValueEquality.PaddedExact ? Padded(c.Value, s.Width) : c.Value);
            if (steps.Count > 0 && steps[^1].TimeFs == t) steps[^1] = step;
            else steps.Add(step);
        }
        return steps;
    }

    private static string Padded(string raw, int width)
    {
        string v = raw.ToUpperInvariant();
        return width > 1 && v.Length < width ? v.PadLeft(width, '0') : v;
    }

    private static bool Same(string? x, string? y, int wx, int wy, ValueEquality rule)
    {
        if (x is null || y is null) return x == y;
        if (rule == ValueEquality.NumericBuses && (wx > 1 || wy > 1) && IsBinary(x) && IsBinary(y))
            return x.TrimStart('0') == y.TrimStart('0');
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBinary(string bits) => bits.Length > 0 && bits.All(c => c is '0' or '1');
}
