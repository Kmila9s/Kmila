using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;
public class FPGA
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("family")]
    public string Family { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("resources")]
    public FPGAResources? Resources { get; set; }

    [JsonPropertyName("clocking")]
    public FPGAClocking? Clocking { get; set; }

    [JsonPropertyName("power")]
    public FPGAPower? Power { get; set; }

    [JsonPropertyName("io")]
    public FPGAsIO? IO { get; set; }

    [JsonPropertyName("package_options")]
    public List<string>? PackageOptions { get; set; }
}