using Kmila.Shared.Services.Api;
using Interpreter.Models;
using Kmila.Shared.Services.Vcd;

namespace Kmila.TestKit.Stimulus;

/// <summary>
/// Stimulus taken from a golden dump: every change of the named input ports,
/// as port moments in whole nanoseconds. Lets a case whose testbench drives
/// inputs with loops or computed values run in Kmila without transcribing it.
/// </summary>
public static class StimulusDeriver
{
    /// <summary>Input-port changes of <paramref name="golden"/>, ordered by time (stable per port).</summary>
    public static List<ApiPortMoment> FromGolden(VcdDocument golden, IEnumerable<string> inputPorts)
    {
        var inputs = inputPorts.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var moments = new List<ApiPortMoment>();
        foreach (var sig in golden.ByName.Values)
        {
            if (!inputs.Contains(sig.Name)) continue;
            foreach (var change in sig.Transitions)
                moments.Add(new ApiPortMoment
                {
                    Port = sig.Name,
                    // Nearest nanosecond, not floor: testbenches that toggle at
                    // 7.5 + 5n ns would otherwise move every edge 0.5 ns early.
                    Time = (int)((change.TimeFs + PhysicalTime.FsPerNs / 2) / PhysicalTime.FsPerNs),
                    Scale = "NS",
                    Value = change.Value,
                });
        }
        return moments.OrderBy(m => m.Time).ToList();
    }
}
