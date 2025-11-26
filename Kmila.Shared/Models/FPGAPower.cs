using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;
public class FPGAPower
{
    [JsonPropertyName("core_voltage_vccint_v")]
    public double? CoreVoltageVccintV { get; set; }
}
