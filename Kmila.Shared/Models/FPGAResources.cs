using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;
public class FPGAResources
{
    [JsonPropertyName("logic_units")]
    public int? LogicUnits { get; set; }

    [JsonPropertyName("logic_units_type")]
    public string? LogicUnitsType { get; set; }

    [JsonPropertyName("registers_flip_flops")]
    public int? RegistersFlipFlops { get; set; }

    [JsonPropertyName("memory_blocks")]
    public int? MemoryBlocks { get; set; }

    [JsonPropertyName("memory_block_type")]
    public string? MemoryBlockType { get; set; }

    [JsonPropertyName("memory_total_kbits")]
    public int? MemoryTotalKbits { get; set; }

    [JsonPropertyName("dsp_slices")]
    public int? DspSlices { get; set; }

    [JsonPropertyName("dsp_slice_type")]
    public string? DspSliceType { get; set; }
}
