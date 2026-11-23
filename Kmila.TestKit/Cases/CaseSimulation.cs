using Kmila.Shared.Services.Api;
using Kmila.Shared.Services.Vcd;
using Kmila.TestKit.Reference;
using Kmila.TestKit.Runners;

namespace Kmila.TestKit.Cases;

/// <summary>
/// How a case variant is simulated and judged, in one place for every consumer
/// (the bench, the xUnit fixture cases): the reference request, the sources Kmila
/// reads, and Kmila's run of the author's testbench itself (v3.1.9, G86), with every
/// signal of the testbench compared strictly against the reference golden.
/// </summary>
public static class CaseSimulation
{
    /// <summary>The reference simulator's run of the variant's testbench.</summary>
    public static ReferenceRequest Reference(CaseSpec spec, CaseVariant v) =>
        new(v.SourcePaths, v.Top, spec.DurationNs, v.Standard);

    /// <summary>Files read verbatim, by name.</summary>
    public static List<SourceFile> Read(IEnumerable<string> paths) =>
        paths.Select(p => new SourceFile(Path.GetFileName(p), File.ReadAllText(p))).ToList();

    /// <summary>
    /// Kmila simulates the author's testbench (<see cref="CaseVariant.Top"/>, its generic
    /// maps and stimulus as written) over <paramref name="sources"/>, and every signal of
    /// the golden's outermost scope is compared strictly (nine values, t=0 included).
    /// </summary>
    public static async Task<(SimulateResponse Response, List<SignalDiff> Diff)> RunAsync(
        IKmilaRunner kmila, CaseSpec spec, CaseVariant v, IReadOnlyList<SourceFile> sources, VcdDocument golden)
    {
        var response = await kmila.SimulateAsync(sources, new SimulateRequest
        {
            EntryPoint = v.Top,
            TimeScale = spec.TimeScale,
            Duration = spec.DurationNs,
            SystemClockMhz = spec.SystemClockMhz,
            TickResNs = spec.TickResNs,
        });
        var diff = WaveformComparer.Compare(golden, VcdReader.Parse(response.Vcd), CompareOptions.Strict(golden.TopLevelNames, ignoreT0: false));
        return (response, diff);
    }
}
