using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents the power specifications of an FPGA device.
/// Power requirements are important for hardware design and board selection.
/// </summary>
/// <remarks>
/// Modern FPGAs typically require multiple voltage rails:
/// - VCCINT: Core logic voltage (internal operations)
/// - VCCAUX: Auxiliary voltage (I/O and configuration)
/// - VCCO: I/O bank voltages (configurable per bank)
/// </remarks>
public class FPGAPower
{
    /// <summary>
    /// Gets or sets the core voltage (VCCINT) in volts.
    /// </summary>
    /// <remarks>
    /// VCCINT powers the internal logic of the FPGA. Lower voltages generally
    /// result in lower power consumption but may limit maximum operating frequency.
    /// Common values: 1.0V, 1.2V, 1.8V depending on the FPGA family and process node.
    /// </remarks>
    /// <example>1.2, 1.0, 0.9</example>
    [JsonPropertyName("core_voltage_vccint_v")]
    public double? CoreVoltageVccintV { get; set; }
}
