using System.Text.Json.Serialization;

namespace Kmila.Shared.Models;

/// <summary>
/// Represents the computational resource specifications of an FPGA device.
/// These resources are used to validate whether a VHDL design fits within
/// the target FPGA's capacity before simulation.
/// </summary>
/// <remarks>
/// Resource types and naming conventions vary by manufacturer:
/// - Xilinx uses Slices/CLBs, Block RAM, DSP48 slices
/// - Altera/Intel uses Logic Elements (LEs), Memory blocks, DSP blocks
/// - Lattice uses LUTs, EBR blocks, DSP blocks
/// </remarks>
public class FPGAResources
{
    /// <summary>
    /// Gets or sets the number of logic units (LUTs, LEs, or Slices) available.
    /// </summary>
    /// <remarks>
    /// The meaning depends on the manufacturer:
    /// - Xilinx: Slices or CLBs
    /// - Altera: Logic Elements (LEs)
    /// - Lattice: LUTs
    /// </remarks>
    [JsonPropertyName("logic_units")]
    public int? LogicUnits { get; set; }

    /// <summary>
    /// Gets or sets the type/name of logic units for display purposes.
    /// </summary>
    /// <example>Slices, LEs, LUTs, CLBs</example>
    [JsonPropertyName("logic_units_type")]
    public string? LogicUnitsType { get; set; }

    /// <summary>
    /// Gets or sets the total number of flip-flops/registers available.
    /// </summary>
    /// <remarks>
    /// Flip-flops are used for storing state in sequential logic designs.
    /// Each registered signal in a VHDL design consumes one flip-flop.
    /// </remarks>
    [JsonPropertyName("registers_flip_flops")]
    public int? RegistersFlipFlops { get; set; }

    /// <summary>
    /// Gets or sets the number of memory blocks (BRAM, EBR) available.
    /// </summary>
    /// <remarks>
    /// Memory blocks are used for implementing RAMs, ROMs, FIFOs, and other
    /// memory structures in VHDL designs.
    /// </remarks>
    [JsonPropertyName("memory_blocks")]
    public int? MemoryBlocks { get; set; }

    /// <summary>
    /// Gets or sets the type/name of memory blocks for display purposes.
    /// </summary>
    /// <example>Block RAM, EBR, M9K, M10K</example>
    [JsonPropertyName("memory_block_type")]
    public string? MemoryBlockType { get; set; }

    /// <summary>
    /// Gets or sets the total memory capacity in kilobits.
    /// </summary>
    [JsonPropertyName("memory_total_kbits")]
    public int? MemoryTotalKbits { get; set; }

    /// <summary>
    /// Gets or sets the number of DSP (Digital Signal Processing) slices available.
    /// </summary>
    /// <remarks>
    /// DSP slices are optimized hardware blocks for multiplication, accumulation,
    /// and other mathematical operations commonly used in signal processing.
    /// </remarks>
    [JsonPropertyName("dsp_slices")]
    public int? DspSlices { get; set; }

    /// <summary>
    /// Gets or sets the type/name of DSP slices for display purposes.
    /// </summary>
    /// <example>DSP48E1, DSP48A1, 18x18 multipliers</example>
    [JsonPropertyName("dsp_slice_type")]
    public string? DspSliceType { get; set; }
}
