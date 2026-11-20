using Interpreter.Models;

namespace Kmila.Shared.Services;

/// <summary>A clock driven on an input port: period (0 = the system clock) and initial level.</summary>
public sealed record ClockSpec(string Port, double PeriodNs = 0, bool InitialHigh = false);

/// <summary>How a run is driven besides the timed port moments.</summary>
public sealed class StimulusOptions
{
    /// <summary>Initial values of objects declared without <c>:=</c>.</summary>
    public InitialValuePolicy InitialValues { get; init; } = InitialValuePolicy.Lrm;

    /// <summary>
    /// Clocks to drive; null proposes the clock-named port at the system frequency
    /// (<see cref="ClockPortPolicy.Suggest"/>), an empty list drives none.
    /// </summary>
    public IReadOnlyList<ClockSpec>? Clocks { get; init; }

    /// <summary>
    /// Initial values of input ports (<c>port</c> → value), as the actual's initial value
    /// in a testbench would give them: set before time 0, not a transaction.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>>? PortInitials { get; init; }

    /// <summary>
    /// Options for a run driven from outside the app (API, CLI): without declared clocks
    /// there is none under LRM, and the clock-named port is proposed only in didactic mode.
    /// </summary>
    public static StimulusOptions Declared(InitialValuePolicy policy, IReadOnlyList<ClockSpec>? clocks,
        IReadOnlyList<KeyValuePair<string, string>>? portInitials) => new()
    {
        InitialValues = policy,
        Clocks = clocks is { Count: > 0 } ? clocks : policy == InitialValuePolicy.Didactic ? null : [],
        PortInitials = portInitials,
    };

    /// <summary>The policy named <paramref name="name"/> (<c>lrm</c>, <c>didactic</c>; null or empty = LRM); false when unknown.</summary>
    public static bool TryParsePolicy(string? name, out InitialValuePolicy policy)
    {
        policy = InitialValuePolicy.Lrm;
        switch (name?.Trim().ToLowerInvariant())
        {
            case null or "" or "lrm": return true;
            case "didactic": policy = InitialValuePolicy.Didactic; return true;
            default: return false;
        }
    }

    /// <summary>The policy named <paramref name="name"/>.</summary>
    /// <exception cref="NotSupportedException">Unknown name.</exception>
    public static InitialValuePolicy ParsePolicy(string? name) => TryParsePolicy(name, out var p) ? p
        : throw new NotSupportedException($"Unknown initial-value policy '{name}' (use \"lrm\" or \"didactic\").");

    /// <summary>The policy named <paramref name="name"/>, LRM when unknown (a stored setting never blocks a run).</summary>
    public static InitialValuePolicy ParsePolicyOrLrm(string? name) => TryParsePolicy(name, out var p) ? p : InitialValuePolicy.Lrm;

    /// <summary>The setting/API token of <paramref name="policy"/> (<c>lrm</c>, <c>didactic</c>).</summary>
    public static string ToToken(InitialValuePolicy policy) => policy == InitialValuePolicy.Didactic ? "didactic" : "lrm";
}
