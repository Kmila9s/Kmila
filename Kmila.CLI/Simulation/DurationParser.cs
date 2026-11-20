using System.Globalization;
using System.Text.RegularExpressions;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Parses human time strings ("50ns", "1 us", "2000") into nanoseconds, the
/// unit the engine's <c>StimulusBuilder.BuildConfigNs</c> and tick math work
/// in. A bare number is interpreted as nanoseconds.
/// </summary>
public static partial class DurationParser
{
    [GeneratedRegex(@"^\s*(\d+(?:\.\d+)?)\s*(fs|ps|ns|us|µs|ms|sec|s|m|h)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TimeRegex();

    /// <summary>
    /// Converts a duration string to nanoseconds (rounded to the nearest ns, never
    /// below 1). Throws <see cref="FormatException"/> on an unparseable value.
    /// </summary>
    public static long ParseToNs(string text) => Math.Max(1, ParseInstantNs(text));

    /// <summary>
    /// Converts a point in time to nanoseconds (rounded to the nearest ns; <c>0</c>
    /// is t = 0). Throws <see cref="FormatException"/> on an unparseable value.
    /// </summary>
    public static long ParseInstantNs(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new FormatException("empty duration");

        var m = TimeRegex().Match(text);
        if (!m.Success)
            throw new FormatException($"invalid time '{text}' (use e.g. 50ns, 1us, 2000)");

        double magnitude = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        string unit = m.Groups[2].Success ? m.Groups[2].Value.ToLowerInvariant() : "ns";

        double ns = unit switch
        {
            "fs"          => magnitude / 1_000_000d,
            "ps"          => magnitude / 1_000d,
            "ns"          => magnitude,
            "us" or "µs"  => magnitude * 1_000d,
            "ms"          => magnitude * 1_000_000d,
            "s" or "sec"  => magnitude * 1_000_000_000d,
            "m"           => magnitude * 60d * 1_000_000_000d,
            "h"           => magnitude * 3600d * 1_000_000_000d,
            _             => magnitude,
        };

        return (long)Math.Round(ns, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Parses a <c>port@time=value</c> stimulus token into its parts, with the
    /// time converted to an integer number of nanoseconds. Example:
    /// <c>rst@20ns=1</c> → ("rst", 20, "1").
    /// </summary>
    public static (string Port, int TimeNs, string Value) ParseStimulus(string token)
    {
        var at = token.IndexOf('@');
        var eq = token.IndexOf('=');
        if (at <= 0 || eq <= at + 1)
            throw new FormatException($"invalid --stimulus '{token}' (use port@time=value, e.g. rst@20ns=1)");

        var port = token[..at].Trim();
        var timePart = token[(at + 1)..eq].Trim();
        var value = token[(eq + 1)..].Trim();
        var timeNs = (int)Math.Min(int.MaxValue, ParseInstantNs(timePart));
        return (port, timeNs, value);
    }
}
