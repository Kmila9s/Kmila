using System.Text.Json.Serialization;
using Kmila.Shared.Services.Api;

namespace Kmila.TestKit.Cases;

/// <summary>
/// The language-neutral part of a test case (<c>case.json</c>): provenance, the
/// DUT's ports, timing and stimulus. What each HDL contributes (sources, top
/// units) lives in its <see cref="CaseVariant"/>.
/// </summary>
public sealed class CaseSpec
{
    /// <summary>Content schema version (<c>ContentSchema.Version</c>).</summary>
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("sourceUrl")] public string SourceUrl { get; set; } = "";
    [JsonPropertyName("license")] public string License { get; set; } = "";
    [JsonPropertyName("fetchedAt")] public string FetchedAt { get; set; } = "";
    [JsonPropertyName("problemDescription")] public string ProblemDescription { get; set; } = "";

    /// <summary>DUT ports; <c>out</c>/<c>inout</c> ones are compared.</summary>
    [JsonPropertyName("ports")] public List<CasePort> Ports { get; set; } = new();

    [JsonPropertyName("timeScale")] public string TimeScale { get; set; } = "NS";
    [JsonPropertyName("durationNs")] public int DurationNs { get; set; } = 1000;
    [JsonPropertyName("tickResNs")] public int TickResNs { get; set; } = 10;
    [JsonPropertyName("systemClockMhz")] public int SystemClockMhz { get; set; } = 50;

    /// <summary>Hand-written port moments (appended after the derived ones when <see cref="AutoStimulus"/>).</summary>
    [JsonPropertyName("stimulus")] public List<ApiPortMoment> Stimulus { get; set; } = new();

    /// <summary>Clocks driven on input ports (an auto-stimulus case takes them from the golden dump).</summary>
    [JsonPropertyName("clocks")] public List<ApiClock> Clocks { get; set; } = new();

    /// <summary>Derive the stimulus from the golden dump's input changes instead of transcribing the testbench.</summary>
    [JsonPropertyName("autoStimulus")] public bool AutoStimulus { get; set; }

    /// <summary>Why the case is not run (reported as SKIP), or null.</summary>
    [JsonPropertyName("skipReason")] public string? SkipReason { get; set; }

    /// <summary>Synthesis diagnostic kinds that are expected (permanent) for this design.</summary>
    [JsonPropertyName("expectedSynthDiagnostics")] public List<string> ExpectedSynthDiagnostics { get; set; } = new();

    /// <summary>Input ports.</summary>
    [JsonIgnore] public IEnumerable<CasePort> Inputs => Ports.Where(p => p.Dir.Equals("in", StringComparison.OrdinalIgnoreCase));

    /// <summary>Compared port names (<c>out</c> and <c>inout</c>).</summary>
    [JsonIgnore] public IEnumerable<string> Outputs => Ports
        .Where(p => p.Dir.Equals("out", StringComparison.OrdinalIgnoreCase) || p.Dir.Equals("inout", StringComparison.OrdinalIgnoreCase))
        .Select(p => p.Name);
}

/// <summary>A DUT port: name, direction (<c>in</c>/<c>out</c>/<c>inout</c>) and bit width.</summary>
public sealed class CasePort
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("dir")] public string Dir { get; set; } = "in";
    [JsonPropertyName("width")] public int Width { get; set; } = 1;
}
