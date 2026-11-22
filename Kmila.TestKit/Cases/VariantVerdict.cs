using Kmila.Frontend;
using Kmila.Shared.Services.Vcd;

namespace Kmila.TestKit.Cases;

/// <summary>How one variant of a case ran: Kmila matched its golden or not, and the golden itself (null when the reference failed).</summary>
public sealed record VariantOutcome(bool Pass, VcdDocument? Golden);

/// <summary>Verdict of a whole case.</summary>
public enum CaseOutcome
{
    /// <summary>Every supported language has a variant, each passes, and their goldens agree.</summary>
    Pass,
    /// <summary>A supported language has no variant (a missing variant is a failure, never a skip).</summary>
    MissingVariant,
    /// <summary>A variant failed, or the variants' goldens disagree.</summary>
    Fail,
}

/// <summary>The case verdict with what caused it.</summary>
public sealed record CaseVerdict(CaseOutcome Outcome, IReadOnlyList<HdlLanguage> Missing, bool GoldensAgree);

/// <summary>
/// One logical case = one variant per supported HDL: it passes only when all of
/// them exist, all pass, and the reference simulators agree with each other on
/// the compared ports (the same design written in two languages behaves alike).
/// </summary>
public static class VariantVerdict
{
    /// <summary>Aggregates the per-language <paramref name="outcomes"/> against the <paramref name="required"/> languages.</summary>
    public static CaseVerdict Aggregate(IEnumerable<HdlLanguage> required, HdlVariants<VariantOutcome> outcomes, IEnumerable<string> ports)
    {
        var missing = outcomes.MissingFrom(required).ToList();
        var ran = outcomes.Languages.Select(l => outcomes.For(l)!).ToList();
        var goldens = ran.Select(o => o.Golden).OfType<VcdDocument>().ToList();
        var portList = ports.ToList();
        bool agree = goldens.Skip(1).All(g =>
            WaveformComparer.Compare(goldens[0], g, CompareOptions.Strict(portList, ignoreT0: false)).All(d => d.Equal));
        var outcome = missing.Count > 0 ? CaseOutcome.MissingVariant
            : ran.Any(o => !o.Pass) || !agree ? CaseOutcome.Fail
            : CaseOutcome.Pass;
        return new CaseVerdict(outcome, missing, agree);
    }
}
