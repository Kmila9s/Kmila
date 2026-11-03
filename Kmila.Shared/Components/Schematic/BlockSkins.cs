using System.Globalization;
using System.Text;
using Interpreter.Synthesis;
using Interpreter.Synthesis.Layout;
using Kmila.Shared.Models;
// Disambiguate from Microsoft.Maui.Controls.Cell in the MAUI target build.
using Cell = Interpreter.Synthesis.Cell;

namespace Kmila.Shared.Components.Schematic;

/// <summary>
/// Inline-SVG builders for the block-level render styles (RTL datapath blocks
/// and Nodos node-cards) and the Grouped-style container boxes. Lives in one
/// place so the on-screen <c>SchematicViewer</c> and the static
/// <c>SchematicExporter</c> draw blocks identically — the colours differ per
/// caller, the geometry does not. All drawing is in the block's local space
/// (origin 0,0 to <c>w,h</c>); the caller wraps it in the placed
/// <c>&lt;g transform="translate(x y)"&gt;</c>.
/// </summary>
public static class BlockSkins
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;
    private static string F(double v) => v.ToString("0.##", Ci);
    private static string Esc(string s) => System.Net.WebUtility.HtmlEncode(s);

    /// <summary>Colours for one drawn block. Supplied by the caller so the
    /// viewer can use CSS custom properties and the exporter concrete hex.</summary>
    public readonly record struct Colors(
        string Fill, string Stroke, string HeaderFill, string HeaderText,
        string Title, string Sub, string Glyph, string PortFill, string PortStroke, string PortLabel);

    /// <summary>Local (within-box) port anchor, matching the fraction layout
    /// <see cref="LayeredLayout"/> uses so the dot sits exactly where wires
    /// connect.</summary>
    public readonly record struct Port(string Name, double X, double Y, bool IsInput, bool IsBus);

    /// <summary>Computes port anchors for a block using the same even-fraction
    /// distribution the layout engine places pins at.</summary>
    public static IReadOnlyList<Port> PortsOf(Cell block, double w, double h, Func<string, bool>? isBus = null)
    {
        var list = new List<Port>(block.Inputs.Count + block.Outputs.Count);
        for (int i = 0; i < block.Inputs.Count; i++)
        {
            double frac = (i + 1.0) / (block.Inputs.Count + 1.0);
            list.Add(new Port(block.Inputs[i].Name, 0, h * frac, true, isBus?.Invoke(block.Inputs[i].NetId) ?? false));
        }
        for (int j = 0; j < block.Outputs.Count; j++)
        {
            double frac = (j + 1.0) / (block.Outputs.Count + 1.0);
            list.Add(new Port(block.Outputs[j].Name, w, h * frac, false, isBus?.Invoke(block.Outputs[j].NetId) ?? false));
        }
        return list;
    }

    /// <summary>Draws a block in the given style's skin.</summary>
    public static string Build(Cell block, double w, double h, SchematicRenderMode style,
        in Colors c, Func<string, bool>? isBus = null)
    {
        string title = block.Attrs != null && block.Attrs.TryGetValue(BlockGraphBuilder.LabelAttr, out var t) ? t : "";
        string sub = block.Attrs != null && block.Attrs.TryGetValue(BlockGraphBuilder.SubAttr, out var s) ? s : "";
        var ports = PortsOf(block, w, h, isBus);
        return style == SchematicRenderMode.Nodes
            ? Node(w, h, title, sub, in c, ports)
            : Rtl(w, h, title, sub, Glyph(block.Kind), in c, ports);
    }

    // ── RTL datapath block: rounded rect, big kind glyph, name, subtitle,
    //    edge port dots with inset labels. ──────────────────────────────
    private static string Rtl(double w, double h, string title, string sub, string glyph,
        in Colors c, IReadOnlyList<Port> ports)
    {
        var sb = new StringBuilder();
        sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"10\" ")
          .Append($"fill=\"{c.Fill}\" stroke=\"{c.Stroke}\" stroke-width=\"2\" />");
        double midx = w / 2;
        if (!string.IsNullOrEmpty(glyph))
            sb.Append(Text(midx, h / 2 - 8, glyph, 22, 700, c.Glyph, "middle", "var(--kmila-font-display, 'Archivo', system-ui, sans-serif)"));
        sb.Append(Text(midx, h / 2 + 16, title, 13, 600, c.Title, "middle"));
        // Subtitle in the block's own dark label colour (legible on the
        // saturated body) rather than the muted theme grey that read as a
        // half-hidden ghost.
        if (!string.IsNullOrEmpty(sub))
            sb.Append(Text(midx, h - 10, sub, 10.5, 500, c.Title, "middle"));
        AppendPorts(sb, ports, in c, insetLabels: true);
        return sb.ToString();
    }

    // ── Nodos node-card: titled header bar, subtitle, typed port dots. ──
    private static string Node(double w, double h, string title, string sub,
        in Colors c, IReadOnlyList<Port> ports)
    {
        const double head = 26;
        var sb = new StringBuilder();
        // body
        sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"12\" ")
          .Append($"fill=\"{c.Fill}\" stroke=\"{c.Stroke}\" stroke-width=\"1.75\" />");
        // header (rounded top only: rect + a square patch under its lower half)
        sb.Append($"<path d=\"M0,{F(head)} L0,12 Q0,0 12,0 L{F(w - 12)},0 Q{F(w)},0 {F(w)},12 L{F(w)},{F(head)} Z\" ")
          .Append($"fill=\"{c.HeaderFill}\" />");
        sb.Append(Text(11, head - 8, title, 12.5, 700, c.HeaderText, "start"));
        // Node cards don't repeat the operation as a body subtitle — the
        // titled header already names the block and the inspector carries the
        // detail, so the body stays clean (the faint subtitle read as a label
        // that should have been hidden). `sub` is intentionally unused here.
        _ = sub;
        AppendPorts(sb, ports, in c, insetLabels: true);
        return sb.ToString();
    }

    private static void AppendPorts(StringBuilder sb, IReadOnlyList<Port> ports, in Colors c, bool insetLabels)
    {
        foreach (var p in ports)
        {
            sb.Append($"<circle cx=\"{F(p.X)}\" cy=\"{F(p.Y)}\" r=\"{(p.IsBus ? "4.5" : "4")}\" ")
              .Append($"fill=\"{c.PortFill}\" stroke=\"{c.PortStroke}\" stroke-width=\"1.75\" />");
            if (!insetLabels || string.IsNullOrEmpty(p.Name) || p.Name == "·") continue;
            double lx = p.IsInput ? p.X + 9 : p.X - 9;
            string anchor = p.IsInput ? "start" : "end";
            sb.Append(Text(lx, p.Y + 3.5, p.Name, 10, 500, c.PortLabel, anchor));
        }
    }

    // Title geometry for the Grouped container: font size, left/right inset of
    // the title from the box edges, and an estimated advance per character at
    // that size for the display/sans title font (weight 600). Kept slightly
    // generous so the box never ends up a hair too narrow for its title.
    private const double GroupTitleFont = 11;
    private const double GroupTitleInset = 12;
    private const double GroupTitleCharW = 6.6;

    /// <summary>Minimum box width that fits <paramref name="title"/> at the group
    /// title's font, so a narrow statement box still holds its whole label
    /// instead of letting the text spill past the right edge. There is no DOM to
    /// measure against here (the SVG is built as a string, shared by the viewer
    /// and the exporter), so this estimates the advance width.</summary>
    public static double GroupTitleMinWidth(string title)
        => string.IsNullOrEmpty(title)
            ? 0
            : title.Length * GroupTitleCharW + GroupTitleInset * 2;

    /// <summary>Titled, translucent container box for the Grouped style. The box
    /// grows to at least the title's width so the statement label stays inside
    /// it (narrow single-gate groups used to let a long line overflow the box
    /// and become unreadable).</summary>
    public static string GroupContainer(double x, double y, double w, double h,
        string title, string fill, string stroke, string textColor)
    {
        double drawW = Math.Max(w, GroupTitleMinWidth(title));
        var sb = new StringBuilder();
        sb.Append($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(drawW)}\" height=\"{F(h)}\" rx=\"12\" ")
          .Append($"fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1.5\" />");
        if (!string.IsNullOrEmpty(title))
            sb.Append($"<text x=\"{F(x + GroupTitleInset)}\" y=\"{F(y + 16)}\" font-size=\"{F(GroupTitleFont)}\" font-weight=\"600\" ")
              .Append("font-family=\"var(--kmila-font-display, 'Archivo', system-ui, sans-serif)\" ")
              .Append($"fill=\"{textColor}\">").Append(Esc(title)).Append("</text>");
        return sb.ToString();
    }

    /// <summary>Estimated rendered width of <paramref name="s"/> at the given
    /// font size, for the mono/sans label fonts used across the schematic. The
    /// SVG is built as a string with no DOM to measure against, so callers that
    /// must size a box to its text (group containers, component boxes) use this.
    /// Kept slightly generous so a box never ends up a hair too narrow.</summary>
    public static double EstimateTextWidth(string s, double fontSize)
        => string.IsNullOrEmpty(s) ? 0 : s.Length * fontSize * 0.62;

    /// <summary>Solid, label-fit box for a component / black-box instance in the
    /// gate-level styles (Grouped/Raw). Replaces the fixed 70×50 gate glyph so
    /// the instance name fits inside its own box instead of spilling out both
    /// sides. The layout sizes the box (<paramref name="w"/>×<paramref name="h"/>,
    /// via SchematicCellSizing) so wires still meet it exactly at its edges.</summary>
    public static string ComponentBox(double w, double h, string label, string tag,
        string fill, string stroke, string labelColor)
    {
        var sb = new StringBuilder();
        sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"8\" ")
          .Append($"fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1.5\" />");
        if (!string.IsNullOrEmpty(tag))
            sb.Append(Text(7, 13, tag, 9, 600, labelColor, "start",
                "var(--kmila-font-display, 'Archivo', system-ui, sans-serif)"));
        if (!string.IsNullOrEmpty(label))
            sb.Append(Text(w / 2, h / 2 + 4, label, 11, 600, labelColor, "middle"));
        return sb.ToString();
    }

    /// <summary>Cubic bezier from a source anchor (an output, exiting right) to
    /// a sink anchor (an input, entering from the left) — the node-editor link
    /// shape.</summary>
    public static string BezierPath(double x1, double y1, double x2, double y2)
    {
        // Forward edge (sink to the right of source): classic S-curve with
        // horizontal handles.
        if (x2 >= x1 - 12)
        {
            double dx = Math.Clamp(Math.Abs(x2 - x1) * 0.5, 40, 180);
            return $"M {F(x1)} {F(y1)} C {F(x1 + dx)} {F(y1)}, {F(x2 - dx)} {F(y2)}, {F(x2)} {F(y2)}";
        }
        // Backward (feedback) edge: the sink is to the LEFT of the source — a
        // plain horizontal-handle cubic curls back over the source block. Exit
        // the output rightward, bow the curve downward, and sweep into the
        // input's left side so it reads as a deliberate feedback loop, not a
        // knot overlapping itself.
        double back = x1 - x2;
        const double hx = 48;
        double bow = Math.Clamp(back * 0.28, 70, 230);
        return $"M {F(x1)} {F(y1)} C {F(x1 + hx)} {F(y1 + bow)}, {F(x2 - hx)} {F(y2 + bow)}, {F(x2)} {F(y2)}";
    }

    /// <summary>Smooth path that follows an orthogonal routed polyline, rounding
    /// each corner with a quadratic so Nodos keeps its soft node-editor look
    /// while honouring the obstacle-aware waypoints (instead of a straight
    /// endpoint-to-endpoint bezier that would cut across blocks).</summary>
    public static string RoundedPolyline(IReadOnlyList<(double X, double Y)> pts, double radius = 10)
    {
        if (pts.Count == 0) return "";
        if (pts.Count <= 2)
        {
            var a0 = pts[0];
            var b0 = pts[^1];
            return $"M {F(a0.X)} {F(a0.Y)} L {F(b0.X)} {F(b0.Y)}";
        }

        static (double X, double Y) Toward((double X, double Y) from, (double X, double Y) to, double dist)
        {
            double dx = to.X - from.X, dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return from;
            double t = Math.Min(dist, len / 2) / len;
            return (from.X + dx * t, from.Y + dy * t);
        }

        var sb = new StringBuilder();
        sb.Append($"M {F(pts[0].X)} {F(pts[0].Y)}");
        for (int i = 1; i < pts.Count - 1; i++)
        {
            var entry = Toward(pts[i], pts[i - 1], radius);
            var exit = Toward(pts[i], pts[i + 1], radius);
            sb.Append($" L {F(entry.X)} {F(entry.Y)} Q {F(pts[i].X)} {F(pts[i].Y)} {F(exit.X)} {F(exit.Y)}");
        }
        var last = pts[^1];
        sb.Append($" L {F(last.X)} {F(last.Y)}");
        return sb.ToString();
    }

    private static string Text(double x, double y, string s, double size, int weight, string fill,
        string anchor, string? family = null)
        => $"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{anchor}\" font-size=\"{F(size)}\" "
         + $"font-weight=\"{weight}\" font-family=\"{family ?? "ui-monospace, Menlo, monospace"}\" "
         + $"fill=\"{fill}\">{Esc(s)}</text>";

    /// <summary>Prominent symbol stamped on an RTL block for its kind.</summary>
    public static string Glyph(CellKind k) => k switch
    {
        CellKind.DFF => "DFF",
        CellKind.ADD => "Σ",
        CellKind.SUB => "−", CellKind.MUL => "×", CellKind.DIV => "÷",
        CellKind.MOD => "mod", CellKind.POW => "**",
        CellKind.MUX => "MUX",
        CellKind.EQ => "=", CellKind.NEQ => "≠", CellKind.LT => "<",
        CellKind.LTE => "≤", CellKind.GT => ">", CellKind.GTE => "≥",
        CellKind.SHL => "≪", CellKind.SHR => "≫", CellKind.ROL => "⟲", CellKind.ROR => "⟳",
        CellKind.ROM_LOOKUP => "ROM", CellKind.RAM_READ or CellKind.RAM_WRITE => "RAM",
        CellKind.TO_INTEGER => "→int", CellKind.TO_VECTOR => "→vec", CellKind.RESIZE => "⇲",
        CellKind.EDGE_DETECT => "edge", CellKind.LAST_VALUE => "'last", CellKind.STABLE => "'stable",
        CellKind.BLACKBOX_PROCESS => "proc", CellKind.BLACKBOX_COMPONENT => "▣",
        _ => "≡",
    };
}
