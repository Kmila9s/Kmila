using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;
public class FPGAClocking
{
    [JsonPropertyName("pll_count")]
    public int? PllCount { get; set; }

    [JsonPropertyName("dcm_mmcm_count")]
    public int? DcmMmcmCount { get; set; }

    [JsonPropertyName("max_pll_output_mhz")]
    public int? MaxPllOutputMhz { get; set; }
}
