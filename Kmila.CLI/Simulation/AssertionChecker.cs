using System.Globalization;
using Kmila.CLI.Tui;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Checks post-run signal values against <c>signal@time=value</c> assertions.
/// Used by <c>kmila sim --assert</c> to turn a simulation into a pass/fail gate
/// for CI and auto-grading. Values are read from the same <see cref="WaveModel"/>
/// the viewer uses, so scalars and buses compare consistently.
/// </summary>
public static class AssertionChecker
{
    /// <summary>One assertion's outcome.</summary>
    public readonly record struct Result(bool Passed, string Signal, long TimeNs, string Expected, string Actual);

    /// <summary>
    /// Evaluates each <c>signal@time=value</c> token against <paramref name="model"/>.
    /// Time accepts a unit (default ns). Expected values may be scalar
    /// (<c>0/1/x/z</c>), hex (<c>0x1f</c>), binary (<c>0b1010</c>) or decimal.
    /// </summary>
    public static List<Result> Evaluate(WaveModel model, IEnumerable<string> tokens)
    {
        var results = new List<Result>();
        foreach (var tok in tokens)
        {
            var (name, timeNs, expected) = Parse(tok);
            var sig = model.Signals.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (sig == null)
            {
                results.Add(new Result(false, name, timeNs, expected, "(no such signal)"));
                continue;
            }

            var raw = sig.ValueAtFs(timeNs * WaveModel.FsPerNs);
            var actual = raw ?? "(none)";
            bool ok = raw != null && Matches(expected, raw, sig.Width);
            // Present a bus actual as hex for readability.
            var shown = sig.IsBus && raw != null ? WaveformRenderer.FormatBus(raw, sig.Width) : actual;
            results.Add(new Result(ok, name, timeNs, expected, shown));
        }
        return results;
    }

    /// <summary>Parses a <c>signal@time=value</c> token (time may carry a unit).</summary>
    public static (string Signal, long TimeNs, string Expected) Parse(string token)
    {
        var at = token.IndexOf('@');
        var eq = token.IndexOf('=');
        if (at <= 0 || eq <= at + 1)
            throw new FormatException($"invalid --assert '{token}' (use signal@time=value, e.g. cnt@100ns=0x5)");
        var name = token[..at].Trim();
        var timePart = token[(at + 1)..eq].Trim();
        var value = token[(eq + 1)..].Trim();
        long timeNs = DurationParser.ParseToNs(timePart);
        return (name, timeNs, value);
    }

    private static bool Matches(string expected, string actual, int width)
    {
        if (width <= 1)
        {
            // Scalar: accept 0/1/x/z, case-insensitive; also tolerate "0b1"/"1".
            var e = Normalize1(expected);
            var a = actual.Trim().ToLowerInvariant();
            return e == a;
        }
        // Bus: compare numerically when both resolve to a number.
        if (TryNumber(expected, out var ne) && TryBinary(actual, out var na))
            return ne == na;
        // Fallback: textual (handles x/z buses).
        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize1(string v)
    {
        v = v.Trim().ToLowerInvariant();
        if (v.StartsWith("0b")) v = v[2..];
        return v;
    }

    /// <summary>Parses an expected value as hex (0x), binary (0b) or decimal.</summary>
    private static bool TryNumber(string v, out long value)
    {
        value = 0;
        v = v.Trim();
        if (v.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return long.TryParse(v[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        if (v.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            return TryBinary(v[2..], out value);
        return long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryBinary(string bits, out long value)
    {
        value = 0;
        if (string.IsNullOrEmpty(bits) || !bits.All(c => c is '0' or '1')) return false;
        foreach (var c in bits) value = (value << 1) | (c == '1' ? 1L : 0L);
        return true;
    }
}
