using System.Text.Json;
using Kmila.CLI.Cli;
using Kmila.Shared.Models;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Resolves a <c>--fpga &lt;model&gt;</c> name against the device specs shipped in
/// <c>Kmila.Shared/wwwroot/Devices/*.json</c> and derives the default simulation
/// clock from the device (the shared headless engine accepts a
/// <c>Fpga</c> field but ignores it, so the CLI applies the device's clock
/// itself via <see cref="FPGAClocking.GetDefaultSimClockMhz"/>).
/// </summary>
public static class FpgaResolver
{
    /// <summary>Lists the known device model names (file stems), sorted. Empty if the folder is missing.</summary>
    public static IReadOnlyList<string> KnownModels()
    {
        var dir = ResourcePaths.Devices();
        if (dir == null) return Array.Empty<string>();
        return Directory.EnumerateFiles(dir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Loads the device spec for <paramref name="model"/> (case-insensitive),
    /// or null if unknown / unreadable.
    /// </summary>
    public static FPGA? Load(string model)
    {
        var dir = ResourcePaths.Devices();
        if (dir == null || string.IsNullOrWhiteSpace(model)) return null;

        // Exact file first, then case-insensitive match on the stem.
        string? path = Path.Combine(dir, model + ".json");
        if (!File.Exists(path))
        {
            path = Directory.EnumerateFiles(dir, "*.json")
                .FirstOrDefault(f => string.Equals(
                    Path.GetFileNameWithoutExtension(f), model, StringComparison.OrdinalIgnoreCase));
        }
        if (path == null || !File.Exists(path)) return null;

        try
        {
            using var fs = File.OpenRead(path);
            return JsonSerializer.Deserialize<FPGA>(fs);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the default simulation clock (MHz) for a device model, or null
    /// when the model is unknown. Falls back to the device's heuristic default
    /// when it declares no explicit <c>system_clock_mhz</c>.
    /// </summary>
    public static int? DefaultClockMhz(string model)
    {
        var fpga = Load(model);
        if (fpga == null) return null;
        return fpga.Clocking?.GetDefaultSimClockMhz() ?? 50;
    }
}
