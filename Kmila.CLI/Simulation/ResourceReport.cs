using Interpreter.Services;
using Kmila.Shared.Models;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Compares an estimated resource usage (<see cref="Resources"/>) against an
/// optional FPGA device budget (<see cref="FPGAResources"/>) and formats a
/// utilization table. The shared library has an estimator and the device specs
/// but no "used vs available %" service, so the CLI computes it here.
/// </summary>
public static class ResourceReport
{
    /// <summary>One resource row: estimated usage vs the device's capacity.</summary>
    public readonly record struct Row(string Name, double Used, int? Available)
    {
        /// <summary>Utilization fraction (0..1+) when the device declares a capacity.</summary>
        public double? Fraction => Available is > 0 ? Used / Available.Value : null;
        /// <summary>Status: ok (&lt;80%), warn (80-100%), over (&gt;100%), or unknown (no device cap).</summary>
        public string Status => Fraction is not { } f ? "-" : f > 1.0 ? "OVER" : f >= 0.8 ? "WARN" : "ok";
    }

    /// <summary>Builds the comparison rows. <paramref name="device"/> may be null (estimate-only).</summary>
    public static List<Row> Build(Resources used, FPGAResources? device)
    {
        return new List<Row>
        {
            new("LUTs",       used.LUTs,       device?.LogicUnits),
            new("FlipFlops",  used.FlipFlops,  device?.RegistersFlipFlops),
            new("BRAM",       used.BRAMBlocks, device?.MemoryBlocks),
            new("DSP",        used.DSPSlices,  device?.DspSlices),
            new("IO",         used.IOPins,     null),
            new("Clocks",     used.ClockDomains, null),
        };
    }

    /// <summary>True when any row exceeds the device capacity.</summary>
    public static bool AnyOverBudget(IEnumerable<Row> rows) =>
        rows.Any(r => r.Fraction is > 1.0);

    /// <summary>Renders the rows as an aligned text table.</summary>
    public static string Format(IReadOnlyList<Row> rows, string entity, string? deviceName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(deviceName == null
            ? $"Resource estimate for '{entity}':"
            : $"Resource estimate for '{entity}' on {deviceName}:");
        sb.AppendLine($"  {"Resource",-10} {"Used",8} {"Avail",8} {"Util",7}  Status");
        foreach (var r in rows)
        {
            var used = r.Name is "BRAM" ? r.Used.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                                        : ((long)r.Used).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var avail = r.Available?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-";
            var util = r.Fraction is { } f ? (f * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%" : "-";
            sb.AppendLine($"  {r.Name,-10} {used,8} {avail,8} {util,7}  {r.Status}");
        }
        return sb.ToString().TrimEnd('\n');
    }
}
