using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Severity of a <see cref="GateAnimationFinding"/>.
/// </summary>
public enum GateFindingSeverity
{
    /// <summary>A hard logic contradiction: an authored value the gate network
    /// cannot produce for the step's inputs. These make a wire light wrong.</summary>
    Error,

    /// <summary>A staging smell that renders misleadingly but isn't a logic lie
    /// (a pulsing gate that outputs 0, a wire carrying 1 that can never glow).</summary>
    Warning,
}

/// <summary>
/// One issue found while reconciling a hand-authored gate-animation sidecar
/// (<c>*.gates.json</c>) against the Boolean logic its own node graph implies.
/// </summary>
/// <param name="File">Sidecar label (relative path) the finding belongs to.</param>
/// <param name="Severity">Error (logic lie) or Warning (staging smell).</param>
/// <param name="Kind">Short machine tag, e.g. <c>value-mismatch</c>.</param>
/// <param name="StepIndex">Zero-based step index.</param>
/// <param name="StepT">The step's authored <c>t</c> ordinal.</param>
/// <param name="Target">Net / node / wire id the finding is about.</param>
/// <param name="Authored">Value the sidecar declares (or empty).</param>
/// <param name="Expected">Value the gate logic computes (or empty).</param>
/// <param name="Message">Human-readable explanation.</param>
public record GateAnimationFinding(
    string File,
    GateFindingSeverity Severity,
    string Kind,
    int StepIndex,
    long StepT,
    string Target,
    string Authored,
    string Expected,
    string Message);

/// <summary>
/// Reconciles a <see cref="GateAnimationDoc"/>'s hand-authored per-step values
/// against the Boolean logic implied by its own node/wire graph, so a lesson
/// animation can't silently show a wire ON (or OFF) in a way the circuit it
/// draws would never produce.
///
/// <para>Scope: fully evaluates the combinational primitives
/// (AND/OR/XOR/NAND/NOR/XNOR/NOT/BUFFER). Sequential or opaque primitives
/// (DFF/LATCH/REGISTER/MUX/ADDER/TRISTATE/BLOCK) depend on state or on inputs
/// the sidecar doesn't fully encode, so their output nets are left
/// <i>unchecked</i> rather than guessed — the checker never emits a false
/// positive for a value it cannot derive.</para>
///
/// <para>The gate truth tables here are the canonical <c>std_logic</c>
/// semantics (what GHDL / the Kmila Interpreter would compute for the same
/// primitive), evaluated directly because spinning up the full VHDL pipeline
/// for two-input gates would buy nothing.</para>
/// </summary>
public static class GateAnimationChecker
{
    private static readonly HashSet<string> CombinationalKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "xor", "nand", "nor", "xnor", "not", "buffer",
    };

    /// <summary>Tri-state logic value used during evaluation.</summary>
    private enum Tri { Zero, One, Unknown }

    /// <summary>
    /// Checks one sidecar document and returns every finding (empty when clean).
    /// </summary>
    /// <param name="doc">The parsed sidecar.</param>
    /// <param name="fileLabel">A label (relative path) used in findings.</param>
    public static List<GateAnimationFinding> Check(GateAnimationDoc doc, string fileLabel)
    {
        var findings = new List<GateAnimationFinding>();
        if (doc == null || doc.Steps.Count == 0) return findings;

        // --- Build the static graph once -------------------------------------
        // node id -> node
        var nodes = new Dictionary<string, GateNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in doc.Nodes)
            if (!string.IsNullOrEmpty(n.Id)) nodes[n.Id] = n;

        // gate id -> output net id (the `source` of the wire leaving gate.out)
        var gateOutNet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // gate id -> input net ids (the `source` of each wire entering gate.inX)
        var gateInNets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in doc.Wires)
        {
            var fromNode = StripPort(w.From);
            var toNode = StripPort(w.To);
            if (!string.IsNullOrEmpty(fromNode) && nodes.TryGetValue(fromNode, out var fn)
                && IsGateKind(fn.Kind) && !string.IsNullOrEmpty(w.Source))
            {
                // A gate may fan out: all out-wires share the same source net.
                gateOutNet[fromNode] = w.Source;
            }
            if (!string.IsNullOrEmpty(toNode) && nodes.TryGetValue(toNode, out var tn)
                && IsGateKind(tn.Kind) && !string.IsNullOrEmpty(w.Source))
            {
                if (!gateInNets.TryGetValue(toNode, out var list))
                    gateInNets[toNode] = list = new List<string>();
                list.Add(w.Source);
            }
        }

        // net id -> producing gate id (inverse of gateOutNet)
        var netProducer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in gateOutNet)
            netProducer[kv.Value] = kv.Key; // net -> gate

        // Which node ids are primary inputs (free variables we read from values).
        var inputIds = new HashSet<string>(
            doc.Nodes.Where(n => string.Equals(n.Kind, "input", StringComparison.OrdinalIgnoreCase))
                     .Select(n => n.Id),
            StringComparer.OrdinalIgnoreCase);

        // --- Walk every step --------------------------------------------------
        for (var si = 0; si < doc.Steps.Count; si++)
        {
            var step = doc.Steps[si];
            var values = step.Values ?? new Dictionary<string, string>();
            var active = step.Active ?? new List<string>();

            // Memoized evaluation for this step (net id -> computed Tri).
            var memo = new Dictionary<string, Tri>(StringComparer.OrdinalIgnoreCase);

            Tri EvalNet(string net, HashSet<string> visiting)
            {
                if (string.IsNullOrEmpty(net)) return Tri.Unknown;
                if (memo.TryGetValue(net, out var cached)) return cached;
                if (!visiting.Add(net)) return Tri.Unknown; // feedback loop -> give up

                Tri result;
                if (netProducer.TryGetValue(net, out var gateId)
                    && nodes.TryGetValue(gateId, out var gate)
                    && CombinationalKinds.Contains(gate.Kind))
                {
                    var ins = gateInNets.TryGetValue(gateId, out var l) ? l : new List<string>();
                    var inVals = ins.Select(i => EvalNet(i, visiting)).ToList();
                    result = ApplyGate(gate.Kind, inVals);
                }
                else if (values.TryGetValue(net, out var raw))
                {
                    // A primary input, an output terminal fed by a primary, or a
                    // net produced by an opaque/sequential gate: trust the sheet.
                    result = Parse(raw);
                }
                else
                {
                    result = Tri.Unknown;
                }

                visiting.Remove(net);
                memo[net] = result;
                return result;
            }

            // 1) Value contradictions: for every net the sheet assigns AND whose
            //    producer is a combinational gate, compare authored vs computed.
            foreach (var kv in values)
            {
                var net = kv.Key;
                if (!netProducer.TryGetValue(net, out var gateId)) continue;
                if (!nodes.TryGetValue(gateId, out var gate) || !CombinationalKinds.Contains(gate.Kind))
                    continue;

                var authored = Parse(kv.Value);
                var computed = EvalNet(net, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                if (computed == Tri.Unknown || authored == Tri.Unknown) continue; // can't adjudicate
                if (authored != computed)
                {
                    findings.Add(new GateAnimationFinding(
                        fileLabel, GateFindingSeverity.Error, "value-mismatch", si, step.T,
                        net, Show(authored), Show(computed),
                        $"net '{net}' (out of {gate.Kind.ToUpperInvariant()} '{gateId}') is authored {Show(authored)} but the gate logic computes {Show(computed)} for this step's inputs."));
                }
            }

            // 2) Stale pulses: a gate in `active` whose output is 0 can't pulse —
            //    the outline implies firing that isn't happening.
            foreach (var id in active)
            {
                if (!gateOutNet.TryGetValue(id, out var outNet)) continue;
                if (!nodes.TryGetValue(id, out var gate) || !CombinationalKinds.Contains(gate.Kind))
                    continue;
                var outVal = EvalNet(outNet, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                if (outVal == Tri.Zero)
                {
                    findings.Add(new GateAnimationFinding(
                        fileLabel, GateFindingSeverity.Warning, "stale-active", si, step.T,
                        id, "active", "0",
                        $"gate '{id}' is listed in step.active but outputs 0 this step — it won't pulse; the narrative may mislead."));
                }
            }

            // 3) Dead hot wires: a wire whose source carries 1 but whose
            //    `activeFor` gate never fires this step stays cold despite the 1.
            foreach (var w in doc.Wires)
            {
                if (w.ActiveFor == null || w.ActiveFor.Count == 0) continue;
                var src = EvalNet(w.Source, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                if (src != Tri.One) continue; // only matters when the wire truly carries 1
                var open = w.ActiveFor.Any(g =>
                    active.Contains(g, StringComparer.OrdinalIgnoreCase)
                    && gateOutNet.TryGetValue(g, out var gn)
                    && EvalNet(gn, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) == Tri.One);
                if (!open)
                {
                    var wireId = string.IsNullOrEmpty(w.Id) ? $"{w.From}->{w.To}" : w.Id;
                    findings.Add(new GateAnimationFinding(
                        fileLabel, GateFindingSeverity.Warning, "dead-hot-wire", si, step.T,
                        wireId, "1", "cold",
                        $"wire '{wireId}' (source '{w.Source}') carries 1 but renders cold: its activeFor gate(s) [{string.Join(",", w.ActiveFor)}] are not firing this step."));
                }
            }
        }

        return findings;
    }

    // --- gate evaluation ----------------------------------------------------

    private static Tri ApplyGate(string kind, List<Tri> ins)
    {
        switch (kind.ToLowerInvariant())
        {
            case "not": return ins.Count == 1 ? Invert(ins[0]) : Tri.Unknown;
            case "buffer": return ins.Count == 1 ? ins[0] : Tri.Unknown;
            case "and": return And(ins);
            case "nand": return Invert(And(ins));
            case "or": return Or(ins);
            case "nor": return Invert(Or(ins));
            case "xor": return Xor(ins);
            case "xnor": return Invert(Xor(ins));
            default: return Tri.Unknown;
        }
    }

    // AND: a dominant 0 forces 0 even if another input is unknown.
    private static Tri And(List<Tri> ins)
    {
        if (ins.Count == 0) return Tri.Unknown;
        if (ins.Any(t => t == Tri.Zero)) return Tri.Zero;
        if (ins.Any(t => t == Tri.Unknown)) return Tri.Unknown;
        return Tri.One;
    }

    // OR: a dominant 1 forces 1 even if another input is unknown.
    private static Tri Or(List<Tri> ins)
    {
        if (ins.Count == 0) return Tri.Unknown;
        if (ins.Any(t => t == Tri.One)) return Tri.One;
        if (ins.Any(t => t == Tri.Unknown)) return Tri.Unknown;
        return Tri.Zero;
    }

    // XOR: any unknown input makes the parity undecidable.
    private static Tri Xor(List<Tri> ins)
    {
        if (ins.Count == 0) return Tri.Unknown;
        if (ins.Any(t => t == Tri.Unknown)) return Tri.Unknown;
        var ones = ins.Count(t => t == Tri.One);
        return (ones % 2 == 1) ? Tri.One : Tri.Zero;
    }

    private static Tri Invert(Tri t) => t switch
    {
        Tri.Zero => Tri.One,
        Tri.One => Tri.Zero,
        _ => Tri.Unknown,
    };

    // --- value parsing ------------------------------------------------------

    /// <summary>
    /// Parses an authored display value to a tri-state logic level. Scalars
    /// '1'/'H' are One, '0'/'L' are Zero; Z/X/U/'-' and anything non-scalar
    /// (vectors, decimal tokens) are Unknown so the checker never adjudicates a
    /// value it doesn't understand.
    /// </summary>
    private static Tri Parse(string? value)
    {
        if (string.IsNullOrEmpty(value)) return Tri.Unknown;
        if (value.Length != 1) return Tri.Unknown;
        return value[0] switch
        {
            '1' or 'H' or 'h' => Tri.One,
            '0' or 'L' or 'l' => Tri.Zero,
            _ => Tri.Unknown,
        };
    }

    private static string Show(Tri t) => t switch
    {
        Tri.Zero => "0",
        Tri.One => "1",
        _ => "?",
    };

    private static bool IsGateKind(string? kind) =>
        !string.IsNullOrEmpty(kind)
        && !string.Equals(kind, "input", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(kind, "output", StringComparison.OrdinalIgnoreCase);

    private static string StripPort(string? spec)
    {
        if (string.IsNullOrEmpty(spec)) return string.Empty;
        var dot = spec.IndexOf('.');
        return dot < 0 ? spec : spec.Substring(0, dot);
    }
}
