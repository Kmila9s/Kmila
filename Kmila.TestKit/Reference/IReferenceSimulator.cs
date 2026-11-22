using Kmila.Frontend;

namespace Kmila.TestKit.Reference;

/// <summary>What a reference simulator runs: the author's sources in analysis order, the top unit and the stop time.</summary>
/// <param name="Sources">Source files, dependencies first; never modified on disk.</param>
/// <param name="Top">Top unit to elaborate (the testbench).</param>
/// <param name="DurationNs">Simulation stop time in nanoseconds.</param>
/// <param name="Standard">Language standard (GHDL <c>--std</c>), or null for the simulator's default.</param>
public sealed record ReferenceRequest(IReadOnlyList<string> Sources, string Top, long DurationNs, string? Standard = null);

/// <summary>Outcome of a reference run: the dump on success, and the tool log either way.</summary>
/// <param name="Ok">True when every step succeeded and a dump was written.</param>
/// <param name="Vcd">The VCD text, or null on failure.</param>
/// <param name="Log">Commands and their output.</param>
/// <param name="ExitCode">Exit code of the last step run.</param>
public sealed record ReferenceRun(bool Ok, string? Vcd, string Log, int ExitCode);

/// <summary>
/// An external simulator that produces the golden waveform for one HDL
/// language (GHDL for VHDL; Icarus/Verilator for SystemVerilog in Fase 3).
/// </summary>
public interface IReferenceSimulator
{
    /// <summary>Language this simulator runs.</summary>
    HdlLanguage Language { get; }

    /// <summary>Whether the tool is installed, and its version line.</summary>
    Task<(bool Installed, string Version)> VersionAsync(CancellationToken ct = default);

    /// <summary>Analyzes, elaborates and runs the sources in a private temporary folder and returns the dump.</summary>
    Task<ReferenceRun> RunAsync(ReferenceRequest request, CancellationToken ct = default);
}
