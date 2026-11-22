using Kmila.Frontend;

namespace Kmila.TestKit.Harness;

/// <summary>
/// Writes a testbench, in one HDL language, that instantiates the DUT and
/// applies a <see cref="HarnessSpec"/> — so a reference simulator runs the
/// design under exactly the stimulus Kmila used. The DUT is never modified.
/// </summary>
public interface IHarnessGenerator
{
    /// <summary>Language of the generated testbench (and of the DUT it instantiates).</summary>
    HdlLanguage Language { get; }

    /// <summary>Name of the generated top unit (what the reference simulator elaborates).</summary>
    string TopName { get; }

    /// <summary>The testbench source for <paramref name="spec"/>.</summary>
    string Generate(HarnessSpec spec);
}

/// <summary>The harness generator of each HDL language; a new language registers its own here.</summary>
public static class HarnessGenerators
{
    private static readonly IReadOnlyList<IHarnessGenerator> All = [new VhdlHarnessGenerator()];

    /// <summary>The generator for <paramref name="language"/>, or null when none is registered.</summary>
    public static IHarnessGenerator? For(HdlLanguage language) => All.FirstOrDefault(g => g.Language == language);
}
