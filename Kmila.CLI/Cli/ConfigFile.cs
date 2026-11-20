using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kmila.CLI.Cli;

/// <summary>
/// Optional defaults for <c>kmila sim</c> loaded from a <c>kmila.json</c> file in
/// the current directory (or a path given with <c>--config</c>). Explicit CLI
/// flags always win over the file. Keeps students from retyping the same
/// duration/clock/fpga every run — the terminal analogue of the GUI's saved
/// "Configurations".
/// </summary>
public sealed class ConfigFile
{
    /// <summary>Default simulated duration string (e.g. "200ns").</summary>
    [JsonPropertyName("duration")] public string? Duration { get; set; }
    /// <summary>Default system clock in MHz.</summary>
    [JsonPropertyName("clockMHZ")] public int? ClockMhz { get; set; }
    /// <summary>Default FPGA device model.</summary>
    [JsonPropertyName("fpga")] public string? Fpga { get; set; }
    /// <summary>Default tick resolution in ns.</summary>
    [JsonPropertyName("tickResNs")] public int? TickResNs { get; set; }
    /// <summary>Default output format (vcd|txt|csv|json|svg).</summary>
    [JsonPropertyName("format")] public string? Format { get; set; }
    /// <summary>Default UI/message language (es|en).</summary>
    [JsonPropertyName("lang")] public string? Lang { get; set; }
    /// <summary>Initial values: <c>lrm</c> (default) or <c>didactic</c>.</summary>
    [JsonPropertyName("initialValues")] public string? InitialValues { get; set; }
    /// <summary>Clocks to drive, as in <c>--clock</c> (<c>"clk"</c>, <c>"clk:20ns"</c>, <c>"clk:50MHz"</c>).</summary>
    [JsonPropertyName("clocks")] public List<string>? Clocks { get; set; }

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads config from <paramref name="explicitPath"/> if given, else from
    /// <c>kmila.json</c> in the current directory. Returns an empty config when
    /// no file exists; throws <see cref="FormatException"/> only for a bad
    /// explicit path.
    /// </summary>
    public static ConfigFile Load(string? explicitPath)
    {
        string? path = null;
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            path = Path.GetFullPath(explicitPath);
            if (!File.Exists(path))
                throw new FileNotFoundException($"config file not found: {path}");
        }
        else
        {
            var candidate = Path.Combine(Directory.GetCurrentDirectory(), "kmila.json");
            if (File.Exists(candidate)) path = candidate;
        }

        if (path == null) return new ConfigFile();
        try
        {
            return JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText(path), Opts) ?? new ConfigFile();
        }
        catch (JsonException ex)
        {
            throw new FormatException($"invalid config file {path}: {ex.Message}");
        }
    }
}
