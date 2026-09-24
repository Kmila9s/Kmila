using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents a Kmila simulation configuration that can be saved and loaded.
/// Contains all settings needed to restore a simulation state including FPGA selection,
/// port configurations, and time settings.
/// </summary>
/// <remarks>
/// Configuration files use the .k-config extension and are stored as JSON.
/// </remarks>
public class KmilaConfiguration
{
    /// <summary>
    /// Gets or sets the configuration file format version for compatibility checking.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the user-friendly name for this configuration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what this configuration is for.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this configuration was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the FPGA configuration section.
    /// </summary>
    public FPGAConfiguration? FPGA { get; set; }

    /// <summary>
    /// Gets or sets the simulation time configuration.
    /// </summary>
    public TimeConfiguration? Time { get; set; }

    /// <summary>
    /// Gets or sets the port moment input configurations.
    /// </summary>
    public List<PortMomentConfiguration> PortMoments { get; set; } = new();

    /// <summary>
    /// Serializes this configuration to a JSON string.
    /// </summary>
    /// <returns>JSON representation of the configuration.</returns>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes a configuration from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized configuration, or null if parsing fails.</returns>
    public static KmilaConfiguration? FromJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<KmilaConfiguration>(json, options);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents the FPGA configuration section of a Kmila configuration.
/// </summary>
public class FPGAConfiguration
{
    /// <summary>
    /// Gets or sets the FPGA model name (e.g., "EP4CE10").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FPGA family (e.g., "Cyclone IV E").
    /// </summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FPGA manufacturer (e.g., "Intel").
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path/identifier used to load this FPGA.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Represents the time configuration section of a Kmila configuration.
/// </summary>
public class TimeConfiguration
{
    /// <summary>
    /// Gets or sets the simulation time value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the time scale (NS, US, MS, S, M, H).
    /// </summary>
    public string Scale { get; set; } = "NS";
}

/// <summary>
/// Represents a single port moment input configuration.
/// </summary>
public class PortMomentConfiguration
{
    /// <summary>
    /// Gets or sets the port/variable name.
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of time-value pairs for this port.
    /// </summary>
    public List<PortMomentValue> Values { get; set; } = new();
}

/// <summary>
/// Represents a single time-value pair for a port moment.
/// </summary>
public class PortMomentValue
{
    /// <summary>
    /// Gets or sets the time at which to set this value.
    /// </summary>
    public int Time { get; set; }

    /// <summary>
    /// Gets or sets the time scale for this moment.
    /// </summary>
    public string TimeScale { get; set; } = "NS";

    /// <summary>
    /// Gets or sets the value to set at this time.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
