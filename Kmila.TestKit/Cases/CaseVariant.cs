using System.Text.Json.Serialization;
using Kmila.Frontend;

namespace Kmila.TestKit.Cases;

/// <summary>
/// One HDL's realisation of a case (<c>variants/&lt;lang&gt;/variant.json</c>):
/// the author's sources (in <c>reference/</c>, never modified), the testbench
/// the reference simulator runs and the DUT Kmila enters at.
/// </summary>
public sealed class CaseVariant
{
    /// <summary>HDL language id (<see cref="HdlLanguage.Id"/>).</summary>
    [JsonPropertyName("language")] public string LanguageId { get; set; } = "";

    /// <summary>Source files under <c>reference/</c>, in analysis order.</summary>
    [JsonPropertyName("sources")] public List<string> Sources { get; set; } = new();

    /// <summary>Testbench unit the reference simulator elaborates.</summary>
    [JsonPropertyName("top")] public string Top { get; set; } = "";

    /// <summary>DUT unit Kmila simulates.</summary>
    [JsonPropertyName("entity")] public string Entity { get; set; } = "";

    /// <summary>Language standard the reference simulator analyzes with (e.g. VHDL <c>93c</c>), or null for its default.</summary>
    [JsonPropertyName("standard")] public string? Standard { get; set; }

    /// <summary>Declared type of each DUT port in this language (documentation).</summary>
    [JsonPropertyName("portTypes")] public Dictionary<string, string> PortTypes { get; set; } = new();

    /// <summary>The variant's folder (<c>variants/&lt;lang&gt;</c>), set by the loader.</summary>
    [JsonIgnore] public string Dir { get; internal set; } = "";

    /// <summary>The language, set by the loader.</summary>
    [JsonIgnore] public HdlLanguage Language { get; internal set; } = null!;

    /// <summary>The committed reference golden of a unit-test fixture (<c>variants/&lt;lang&gt;/expected.vcd</c>).</summary>
    [JsonIgnore] public string ExpectedVcdPath => Path.Combine(Dir, "expected.vcd");

    /// <summary>Absolute paths of <see cref="Sources"/>, in order (set by the loader).</summary>
    [JsonIgnore] public IReadOnlyList<string> SourcePaths { get; internal set; } = [];
}
