using System.Text.Json.Serialization;

namespace Kmila.Shared.Models
{
    public class FPGAsIO
    {
        [JsonPropertyName("max_user_io")]
        public int? MaxUserIO { get; set; }

        [JsonPropertyName("io_banks_count")]
        public int? IoBanksCount { get; set; }

        [JsonPropertyName("supported_io_standards")]
        public List<string>? SupportedIoStandards { get; set; }
    }
}
