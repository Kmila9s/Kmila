using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents a complete FPGA (Field Programmable Gate Array) device specification.
/// This model is deserialized from JSON files containing device specifications
/// from various manufacturers (Xilinx, Altera/Intel, Lattice, etc.).
/// </summary>
/// <remarks>
/// FPGA specifications are loaded from JSON files in the wwwroot/Devices folder
/// and used to validate design resource requirements during simulation.
/// </remarks>
public class FPGA
{
    /// <summary>
    /// Gets or sets the FPGA model identifier.
    /// </summary>
    /// <example>XC6SLX9, EP4CE6, iCE40HX1K, LFE5U-25</example>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FPGA family name.
    /// </summary>
    /// <example>Spartan-6, Cyclone IV E, iCE40, ECP5</example>
    [JsonPropertyName("family")]
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FPGA manufacturer name.
    /// </summary>
    /// <example>Xilinx, Altera, Lattice</example>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FPGA resource specifications including logic units,
    /// memory blocks, flip-flops, and DSP slices.
    /// </summary>
    [JsonPropertyName("resources")]
    public FPGAResources? Resources { get; set; }

    /// <summary>
    /// Gets or sets the FPGA clocking specifications including PLL count,
    /// DCM/MMCM count, and maximum clock frequency.
    /// </summary>
    [JsonPropertyName("clocking")]
    public FPGAClocking? Clocking { get; set; }

    /// <summary>
    /// Gets or sets the FPGA power specifications including core voltage.
    /// </summary>
    [JsonPropertyName("power")]
    public FPGAPower? Power { get; set; }

    /// <summary>
    /// Gets or sets the FPGA I/O specifications including maximum user I/O pins,
    /// I/O banks count, and supported I/O standards.
    /// </summary>
    [JsonPropertyName("io")]
    public FPGAsIO? IO { get; set; }

    /// <summary>
    /// Gets or sets the list of available package options for this FPGA model.
    /// </summary>
    /// <example>["TQG144", "CSG225", "FTG256"]</example>
    [JsonPropertyName("package_options")]
    public List<string>? PackageOptions { get; set; }
}