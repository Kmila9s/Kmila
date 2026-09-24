using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents the I/O (Input/Output) specifications of an FPGA device.
/// I/O resources determine how the FPGA can interface with external components.
/// </summary>
/// <remarks>
/// FPGA I/O is organized into banks, each with configurable voltage levels.
/// The number of available user I/O pins depends on the specific package option.
/// </remarks>
public class FPGAsIO
{
    /// <summary>
    /// Gets or sets the maximum number of user-configurable I/O pins.
    /// </summary>
    /// <remarks>
    /// This represents the maximum possible user I/O pins for the largest package option.
    /// Actual available I/O depends on the specific package selected.
    /// Some pins may be reserved for configuration, power, or dedicated functions.
    /// </remarks>
    [JsonPropertyName("max_user_io")]
    public int? MaxUserIO { get; set; }

    /// <summary>
    /// Gets or sets the number of I/O banks in the FPGA.
    /// </summary>
    /// <remarks>
    /// Each I/O bank can be configured with a different I/O voltage standard,
    /// allowing the FPGA to interface with multiple voltage domains simultaneously.
    /// </remarks>
    [JsonPropertyName("io_banks_count")]
    public int? IoBanksCount { get; set; }

    /// <summary>
    /// Gets or sets the list of supported I/O standards.
    /// </summary>
    /// <remarks>
    /// Common I/O standards include:
    /// - LVCMOS (Low Voltage CMOS): 1.2V, 1.5V, 1.8V, 2.5V, 3.3V
    /// - LVTTL (Low Voltage TTL)
    /// - LVDS (Low Voltage Differential Signaling)
    /// - SSTL (Stub Series Terminated Logic) for DDR memory
    /// - HSTL (High Speed Transceiver Logic)
    /// </remarks>
    /// <example>["LVCMOS33", "LVCMOS25", "LVDS_25", "SSTL18"]</example>
    [JsonPropertyName("supported_io_standards")]
    public List<string>? SupportedIoStandards { get; set; }
}
