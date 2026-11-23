using Kmila.Shared.Services.Api;
using Kmila.Shared.Services.Vcd;
using Kmila.TestKit.Reference;
using Kmila.TestKit.Runners;
using Kmila.TestKit.Stimulus;

namespace Kmila.TestKit.Cases;

/// <summary>
/// How a case variant is simulated and judged, in one place for every consumer
/// (the bench, the xUnit fixture cases): the reference request, the sources Kmila
/// reads, and the two Kmila runs — the author's testbench itself (verbatim, every
/// testbench signal compared) or the design driven by the case's stimulus (legacy,
/// output ports compared).
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
    /// The simulate request: the golden dump's input changes (autoStimulus)
    /// followed by the hand-written moments, the declared clocks, and each input's
    /// initial value taken from the testbench signal that drives it.
    /// </summary>
    public static SimulateRequest Request(CaseSpec spec, CaseVariant v, VcdDocument golden)
    {
        var moments = spec.AutoStimulus ? StimulusDeriver.FromGolden(golden, spec.Inputs.Select(p => p.Name)) : new List<ApiPortMoment>();
        moments.AddRange(spec.Stimulus);
        var request = BaseRequest(spec, v.Entity);
        request.PortMoments = moments;
        request.Clocks = spec.Clocks;
        request.PortInitials = TestbenchInitials.Read(v.SourcePaths, spec.Inputs.Select(p => (p.Name, p.Width)));
        return request;
    }

    /// <summary>The timing every run of the case shares, entering at <paramref name="entryPoint"/>.</summary>
    private static SimulateRequest BaseRequest(CaseSpec spec, string entryPoint) => new()
    {
        EntryPoint = entryPoint,
        TimeScale = spec.TimeScale,
        Duration = spec.DurationNs,
        SystemClockMhz = spec.SystemClockMhz,
        TickResNs = spec.TickResNs,
    };

    /// <summary>
    /// The verbatim run (v3.1.9, G86): Kmila simulates the author's testbench itself
    /// (<see cref="CaseVariant.Top"/>, its generic maps and stimulus as written) and
    /// every signal of the golden's outermost scope is compared strictly.
    /// </summary>
    public static async Task<(SimulateResponse Response, List<SignalDiff> Diff)> RunTestbenchAsync(
        IKmilaRunner kmila, CaseSpec spec, CaseVariant v, IReadOnlyList<SourceFile> sources, VcdDocument golden)
    {
        var response = await kmila.SimulateAsync(sources, BaseRequest(spec, v.Top));
        var diff = WaveformComparer.Compare(golden, VcdReader.Parse(response.Vcd), CompareOptions.Strict(golden.TopLevelNames, ignoreT0: false));
        return (response, diff);
    }

    /// <summary>Simulates <paramref name="sources"/> in Kmila and compares the output ports against <paramref name="golden"/>.</summary>
    public static async Task<(SimulateResponse Response, List<SignalDiff> Diff)> RunAsync(
        IKmilaRunner kmila, CaseSpec spec, CaseVariant v, IReadOnlyList<SourceFile> sources, VcdDocument golden, bool ignoreT0 = false)
    {
        var response = await kmila.SimulateAsync(sources, Request(spec, v, golden));
        var diff = WaveformComparer.Compare(golden, VcdReader.Parse(response.Vcd), CompareOptions.Strict(spec.Outputs, ignoreT0));
        return (response, diff);
    }
}
