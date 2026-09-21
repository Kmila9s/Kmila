using Interpreter.Synthesis;
using Kmila.Shared.Models;
// On the MAUI heads Microsoft.Maui.Controls is globally imported and also
// defines a `Cell` type, which collides with the netlist Cell. Alias it so
// this file always means the synthesis Cell (the net9.0 head has no clash,
// but the alias keeps every target building).
using Cell = Interpreter.Synthesis.Cell;

namespace Kmila.Shared.Services;

/// <summary>
/// Human-mode simplification pass over a <see cref="FilteredLayout"/>.
/// Called by <see cref="SchematicExporter.BuildSvg"/> (and by the
/// on-screen viewer when the user selects Human mode) BEFORE emission.
///
/// v1 is intentionally conservative — it returns the layout unchanged.
/// The visible "Human vs. Raw" differentiation is delivered by exporter
/// knobs (padding, font-sizes, thicker bus strokes, muted-label
/// suppression) which do not need to touch the immutable layout graph.
///
/// v2 (backlog) will layer in structural passes:
///   1. Bus collapse — parallel bit-slice wires sharing a signal name
///      and endpoints → one bus wire labelled [N-1:0].
///   2. Register consolidation — N ≥ 2 DFF cells sharing a clock and
///      participating in the same bus → one boxed register glyph.
///   3. Buffer / passthrough elision — CellKind.BUF cells with one
///      input and one output → the cell is removed and the two nets
///      are stitched into one.
///
/// Those passes require creating new cells, nets, wires, AND fresh
/// layout coordinates — a synthesis-adjacent rewrite that is not safe
/// to attempt inside a visibility filter. v1 ships the toggle + visual
/// differentiation so users have the two-mode UX today; v2 delivers
/// structural collapse without further UI surgery.
/// </summary>
public static class SchematicSimplifier
{
    /// <summary>
    /// Returns the simplified layout for a Human-mode export. v1 is a
    /// pass-through — the visible differentiation lives in the exporter.
    /// Callers should invoke this unconditionally when mode is Human;
    /// keeping the call-site stable lets v2 land without touching the
    /// exporter or the viewer.
    /// </summary>
    public static FilteredLayout Simplify(FilteredLayout layout, SchematicColourPalette? palette)
    {
        _ = palette;
        return layout;
    }

    /// <summary>
    /// Pre-layout structural simplification for the clean (non-Naive) view.
    /// Returns a NEW display netlist; the caller's original
    /// <see cref="Synthesizer.Synthesize"/> output is left untouched (so the
    /// literal Naive view and the synthesis tests keep the faithful graph).
    ///
    /// Runs, in order:
    ///  (a)+(b) passthrough-BUF elision, then
    ///  (c) case-cascade collapse (EQ column + MUX chain → one ROM/MUX box).
    /// Naive keeps the literal graph; this is only used by the clean modes.
    /// </summary>
    public static Netlist SimplifyNetlist(Netlist netlist)
        => CollapseCaseCascades(ElideBuffers(netlist));

    /// <summary>
    /// Pass (a)+(b): every 1-in/1-out scalar <see cref="CellKind.BUF"/> is
    /// removed and its two nets unified, so signal-rename buffers collapse
    /// and each constant that used to sit behind a BUF becomes a value
    /// carried on the wire that feeds the real consumer (the renderer tags it
    /// as a small literal chip). We never merge two entity-port nets together
    /// — that would fuse distinct external pins — so such a BUF is kept.
    /// </summary>
    private static Netlist ElideBuffers(Netlist netlist)
    {
        // Union-find over net ids (path-compressed).
        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var n in netlist.Nets) parent[n.Id] = n.Id;
        string Find(string x)
        {
            if (!parent.TryGetValue(x, out _)) parent[x] = x;
            var r = x;
            while (parent[r] != r) r = parent[r];
            while (parent[x] != r) { var nx = parent[x]; parent[x] = r; x = nx; }
            return r;
        }
        void Union(string a, string b)
        {
            var ra = Find(a); var rb = Find(b);
            if (ra != rb) parent[rb] = ra;
        }

        var netById = netlist.Nets.ToDictionary(n => n.Id, StringComparer.Ordinal);
        bool IsPort(string id) => netById.TryGetValue(id, out var n)
            && n.Origin is NetOrigin.PortIn or NetOrigin.PortOut or NetOrigin.PortInOut;

        // 1. Identify elidable BUFs and union their input/output nets.
        var elided = new HashSet<string>(StringComparer.Ordinal); // cell ids
        foreach (var c in netlist.Cells)
        {
            if (c.Kind != CellKind.BUF) continue;
            if (c.Inputs.Count != 1 || c.Outputs.Count != 1) continue;
            var inP = c.Inputs[0];
            var outP = c.Outputs[0];
            if (inP.BusBit != null || outP.BusBit != null) continue; // scalar passthrough only
            if (inP.NetId == outP.NetId) continue;
            if (IsPort(inP.NetId) && IsPort(outP.NetId)) continue; // don't fuse two ports
            elided.Add(c.Id);
            Union(inP.NetId, outP.NetId);
        }

        if (elided.Count == 0) return netlist; // nothing to do

        // 2. Pick a representative net per union class. Prefer a port net so
        //    entity pins keep their identity, then a named signal, then a
        //    constant, then an internal wire; ties break on id for
        //    determinism. Carry any ConstValue in the class onto the rep so
        //    the renderer can tag the surviving wire.
        static int Pri(Net n) => n.Origin switch
        {
            NetOrigin.PortIn or NetOrigin.PortOut or NetOrigin.PortInOut => 3,
            NetOrigin.Signal => 2,
            NetOrigin.Constant => 1,
            _ => 0,
        };
        var rep = new Dictionary<string, string>(StringComparer.Ordinal);
        var newNets = new List<Net>();
        foreach (var g in netlist.Nets.GroupBy(n => Find(n.Id)))
        {
            var members = g.ToList();
            var best = members.OrderByDescending(Pri).ThenBy(n => n.Id, StringComparer.Ordinal).First();
            var cv = best.ConstValue ?? members.FirstOrDefault(m => m.ConstValue != null)?.ConstValue;
            newNets.Add(cv != null && best.ConstValue == null ? best with { ConstValue = cv } : best);
            foreach (var m in members) rep[m.Id] = best.Id;
        }

        Pin Remap(Pin p) => rep.TryGetValue(p.NetId, out var r) && r != p.NetId ? p with { NetId = r } : p;

        // 3. Rebuild cells (drop elided BUFs, remap surviving pins) and ports.
        var newCells = netlist.Cells
            .Where(c => !elided.Contains(c.Id))
            .Select(c => c with
            {
                Inputs = c.Inputs.Select(Remap).ToList(),
                Outputs = c.Outputs.Select(Remap).ToList(),
            })
            .ToList();

        var newPorts = netlist.Ports
            .Select(p => rep.TryGetValue(p.NetId, out var r) && r != p.NetId ? p with { NetId = r } : p)
            .ToList();

        return netlist with { Nets = newNets, Cells = newCells, Ports = newPorts };
    }

    /// <summary>
    /// Pass (c): collapse a synthesised <c>case</c> — an EQ column plus a
    /// per-target MUX cascade against ONE common selector — into a single
    /// box: a <see cref="CellKind.ROM_LOOKUP"/> when every branch value is a
    /// constant (e.g. a BCD→7-seg decoder) or an N-way <see cref="CellKind.MUX"/>
    /// when the branch values are signals. Only true common-selector cases
    /// collapse; <c>if/elsif</c> chains with heterogeneous conditions and
    /// plain 2:1 muxes are left untouched. Dead comparators left with no
    /// consumer are then swept.
    /// </summary>
    private static Netlist CollapseCaseCascades(Netlist nl)
    {
        var netById = nl.Nets.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var cellByOut = new Dictionary<string, Cell>(StringComparer.Ordinal);
        foreach (var c in nl.Cells)
            foreach (var o in c.Outputs)
                cellByOut[o.NetId] = c;

        static string? PinNet(Cell c, string name) =>
            c.Inputs.FirstOrDefault(p => p.Name == name)?.NetId;

        var muxes = nl.Cells.Where(c => c.Kind == CellKind.MUX).ToList();
        var isD0Of = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in muxes)
            if (PinNet(m, "D0") is { } d0) isD0Of.Add(d0);

        var collapsedByRoot = new Dictionary<string, Cell>(StringComparer.Ordinal);
        var removed = new HashSet<string>(StringComparer.Ordinal); // inner mux cell ids

        foreach (var root in muxes)
        {
            if (removed.Contains(root.Id) || collapsedByRoot.ContainsKey(root.Id)) continue;
            var outNet = root.Outputs[0].NetId;
            if (isD0Of.Contains(outNet)) continue; // not a cascade root

            // Walk the D0 chain: each level contributes (select, value); the
            // first non-mux D0 is the default/others value.
            var chain = new List<(string S, string Val)>();
            var inner = new List<Cell>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var cur = root;
            string? defaultNet = null;
            bool ok = true;
            while (true)
            {
                if (!seen.Add(cur.Id)) { ok = false; break; }
                var s = PinNet(cur, "S"); var d1 = PinNet(cur, "D1"); var d0 = PinNet(cur, "D0");
                if (s == null || d1 == null || d0 == null) { ok = false; break; }
                chain.Add((s, d1));
                inner.Add(cur);
                if (cellByOut.TryGetValue(d0, out var prod) && prod.Kind == CellKind.MUX)
                { cur = prod; continue; }
                defaultNet = d0;
                break;
            }
            if (!ok || defaultNet == null || chain.Count < 2) continue;

            // Every select must be a plain EQ(selector, const) sharing one
            // common selector. Anything else (OR/AND matching, mixed
            // selectors) → leave the cascade alone.
            string? commonSel = null;
            var choices = new List<string>();
            bool decoder = true;
            foreach (var (s, _) in chain)
            {
                if (!cellByOut.TryGetValue(s, out var eq) || eq.Kind != CellKind.EQ) { decoder = false; break; }
                var a = PinNet(eq, "A"); var b = PinNet(eq, "B");
                if (a == null || b == null) { decoder = false; break; }
                bool aConst = netById.TryGetValue(a, out var an) && an.ConstValue != null;
                bool bConst = netById.TryGetValue(b, out var bn) && bn.ConstValue != null;
                string sel;
                if (!aConst && bConst) sel = a;
                else if (aConst && !bConst) sel = b;
                else { decoder = false; break; }
                if (commonSel == null) commonSel = sel;
                else if (commonSel != sel) { decoder = false; break; }
                string label = eq.Attrs != null && eq.Attrs.TryGetValue("Choice", out var ch)
                    ? ch.Replace("\"", "").Replace("'", "").Trim()
                    : (netById.TryGetValue(aConst ? a : b, out var cn) ? cn.ConstValue ?? "?" : "?");
                choices.Add(label);
            }
            if (!decoder || commonSel == null) continue;

            foreach (var m in inner) removed.Add(m.Id);
            removed.Remove(root.Id); // root id is reused by the collapsed cell

            bool allConst = chain.All(x => netById.TryGetValue(x.Val, out var vn) && vn.ConstValue != null)
                            && netById.TryGetValue(defaultNet, out var dn) && dn.ConstValue != null;

            if (allConst)
            {
                var rows = new List<string>();
                for (int i = 0; i < chain.Count; i++)
                    rows.Add($"{choices[i]}=>{netById[chain[i].Val].ConstValue}");
                rows.Add($"others=>{netById[defaultNet].ConstValue}");
                collapsedByRoot[root.Id] = new Cell(
                    root.Id, CellKind.ROM_LOOKUP,
                    new[] { new Pin("A", commonSel) },
                    new[] { new Pin("Y", outNet) },
                    root.SourceLine,
                    new Dictionary<string, string> { ["rom"] = string.Join(" ; ", rows), ["sel"] = netById[commonSel].Name });
            }
            else
            {
                var ins = new List<Pin> { new Pin("S", commonSel) };
                for (int i = 0; i < chain.Count; i++) ins.Add(new Pin($"D{i}", chain[i].Val));
                ins.Add(new Pin("DEF", defaultNet));
                collapsedByRoot[root.Id] = new Cell(
                    root.Id, CellKind.MUX, ins,
                    new[] { new Pin("Y", outNet) },
                    root.SourceLine,
                    new Dictionary<string, string> { ["choices"] = string.Join(",", choices), ["sel"] = netById[commonSel].Name });
            }
        }

        if (collapsedByRoot.Count == 0) return nl;

        // Rebuild cells preserving original order (collapsed cell takes its
        // root's slot); then sweep comparators/logic left with no consumer.
        var kept = new List<Cell>();
        foreach (var c in nl.Cells)
        {
            if (collapsedByRoot.TryGetValue(c.Id, out var col)) kept.Add(col);
            else if (!removed.Contains(c.Id)) kept.Add(c);
        }

        var portNets = new HashSet<string>(nl.Ports.Select(p => p.NetId), StringComparer.Ordinal);
        bool changed = true;
        while (changed)
        {
            changed = false;
            var consumed = new HashSet<string>(kept.SelectMany(c => c.Inputs.Select(p => p.NetId)), StringComparer.Ordinal);
            var dead = kept.Where(c =>
                c.Kind is CellKind.EQ or CellKind.OR or CellKind.AND or CellKind.NOT
                && c.Outputs.All(o => !consumed.Contains(o.NetId) && !portNets.Contains(o.NetId))).ToList();
            if (dead.Count > 0)
            {
                var deadIds = new HashSet<string>(dead.Select(c => c.Id), StringComparer.Ordinal);
                kept = kept.Where(c => !deadIds.Contains(c.Id)).ToList();
                changed = true;
            }
        }

        return nl with { Cells = kept };
    }
}
