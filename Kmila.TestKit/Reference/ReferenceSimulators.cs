using Kmila.Frontend;

namespace Kmila.TestKit.Reference;

/// <summary>
/// The reference simulator of each HDL language (GHDL for VHDL); a new language
/// registers its own here, the way <see cref="Frontends"/> registers front-ends.
/// </summary>
public static class ReferenceSimulators
{
    private static readonly IReadOnlyList<IReferenceSimulator> All = [new GhdlReferenceSimulator()];

    /// <summary>Every registered reference simulator.</summary>
    public static IReadOnlyList<IReferenceSimulator> Registered => All;

    /// <summary>The simulator for <paramref name="language"/>, or null when none is registered.</summary>
    public static IReferenceSimulator? For(HdlLanguage language) => All.FirstOrDefault(s => s.Language == language);
}
