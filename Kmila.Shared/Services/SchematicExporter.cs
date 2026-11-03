using System.Globalization;
using System.Net;
using System.Text;
using Interpreter.Synthesis;
using Interpreter.Synthesis.Layout;
using Kmila.Shared.Components.Schematic;
using Kmila.Shared.Models;
using Kmila.Shared.Models.Schematic;

namespace Kmila.Shared.Services;

/// <summary>
/// Builds a standalone SVG document of a schematic — gates, wires,
/// and ports — for the Export-to-SVG/PNG flow. Distinct from the
/// SchematicViewer's on-screen renderer because exported files have
/// to render identically wherever they're pasted, with no external
/// CSS or DOM scaffolding.
/// </summary>
/// <remarks>
/// Reuses the on-screen layout's coordinates so exporting "looks
/// like" cropping the live diagram. When the request asks to trim,
/// we translate every coordinate by the bounds origin instead of
/// re-laying-out the visible elements; that keeps the diagram
/// deterministic and matches the user's expectation that the export
/// matches the screen.
/// </remarks>
public static class SchematicExporter
{
    private const int RawPadding = 24;
    private const int HumanPadding = 40;
    private const int TitleHeight = 28;

    /// <summary>
    /// Builds the SVG for a filtered layout. <paramref name="title"/>
    /// is rendered into a top title bar when
    /// <see cref="SchematicExportRequest.IncludeTitle"/> is true.
    /// When <paramref name="palette"/> is non-null and
    /// <see cref="SchematicExportRequest.ColorMode"/> is
    /// <see cref="SchematicExportColorMode.Color"/>, the on-screen
    /// palette is honoured cell-by-cell and wire-by-wire so the
    /// exported file matches the live diagram pixel-for-pixel
    /// (modulo trim and background).
    ///
    /// When <see cref="SchematicExportRequest.Mode"/> is one of the human
    /// styles, several rendering knobs shift toward readability: padding
    /// grows, fonts grow, wire strokes thicken, and muted internal
    /// output-net labels are hidden so trivial diagrams don't look noisy.
    /// The block styles (Rtl, Nodes) additionally draw each cell as a box
    /// or card via <see cref="BlockSkins"/> instead of gate glyphs.
    /// </summary>
    public static string BuildSvg(
        FilteredLayout filtered,
        SchematicExportRequest req,
        string title,
        SchematicColourPalette? palette = null,
        IReadOnlyList<SchematicAnnotation>? annotations = null)
    {
        var ci = CultureInfo.InvariantCulture;
        bool human = req.Mode.IsHuman();
        bool block = req.Mode.IsBlock();
        bool rtl = req.Mode == SchematicRenderMode.Rtl;
        bool nodes = req.Mode == SchematicRenderMode.Nodes;
        bool grouped = req.Mode == SchematicRenderMode.Grouped;
        if (human && !block)
        {
            filtered = SchematicSimplifier.Simplify(filtered, palette);
        }
        int padding = human ? HumanPadding : RawPadding;
        int labelFont = human ? 14 : 11;
        int titleFont = human ? 16 : 13;
        int portLabelFont = human ? 14 : 11;
        int busStroke = nodes ? 3 : human ? 4 : 3;
        int wireStroke = nodes ? 2 : human ? 3 : 2;

        var bounds = filtered.Bounds;
        // Fold annotation cards into the bounding box so trimming never clips a
        // note/pinned card that sits outside the diagram's own extent.
        if (annotations is { Count: > 0 })
        {
            foreach (var a in annotations)
            {
                double x0 = Math.Min(bounds.X, a.X);
                double y0 = Math.Min(bounds.Y, a.Y);
                double x1 = Math.Max(bounds.X + bounds.Width, a.X + a.W);
                double y1 = Math.Max(bounds.Y + bounds.Height, a.Y + a.H);
                bounds = new Interpreter.Synthesis.Layout.Rect(x0, y0, x1 - x0, y1 - y0);
            }
        }
        // Grouped style: compute the titled statement containers up front (in
        // layout space) so the canvas bounds include them. A narrow single-gate
        // group widens to fit its title (see BlockSkins.GroupContainer); folding
        // that widened extent into the bounds here keeps the box — and its label
        // — from being clipped at the canvas edge. The draw loop below reuses
        // these exact boxes so on-screen and exported geometry stay identical.
        string[] groupHues = { "60,130,246", "245,158,11", "16,185,129",
            "168,85,247", "239,68,68", "6,182,212", "236,72,153" };
        var groupBoxes = new List<(double X, double Y, double W, double H, string Title, string Hue)>();
        double groupMaxX = double.NegativeInfinity, groupMaxY = double.NegativeInfinity;
        if (grouped)
        {
            var groups = filtered.Layout.Netlist.Cells
                .Where(c => filtered.VisibleCellIds.Contains(c.Id) && !string.IsNullOrEmpty(c.SourceLine))
                .GroupBy(c => c.SourceLine!)
                .ToList();
            for (int gi = 0; gi < groups.Count; gi++)
            {
                var rects = groups[gi]
                    .Select(c => filtered.Layout.CellBounds.TryGetValue(c.Id, out var r) ? r : default)
                    .Where(r => r.Width > 0).ToList();
                if (rects.Count == 0) continue;
                double gx0 = rects.Min(r => r.X) - 20;
                double gy0 = rects.Min(r => r.Y) - 34;
                double gx1 = rects.Max(r => r.X + r.Width) + 20;
                double gy1 = rects.Max(r => r.Y + r.Height) + 16;
                string gkey = groups[gi].Key;
                string gtext = gkey.Length > 64 ? gkey.Substring(0, 61) + "…" : gkey;
                double gw = Math.Max(gx1 - gx0, BlockSkins.GroupTitleMinWidth(gtext));
                double gh = gy1 - gy0;
                groupBoxes.Add((gx0, gy0, gw, gh, gtext, groupHues[gi % groupHues.Length]));
                groupMaxX = Math.Max(groupMaxX, gx0 + gw);
                groupMaxY = Math.Max(groupMaxY, gy0 + gh);
                double bx0 = Math.Min(bounds.X, gx0);
                double by0 = Math.Min(bounds.Y, gy0);
                double bx1 = Math.Max(bounds.X + bounds.Width, gx0 + gw);
                double by1 = Math.Max(bounds.Y + bounds.Height, gy0 + gh);
                bounds = new Interpreter.Synthesis.Layout.Rect(bx0, by0, bx1 - bx0, by1 - by0);
            }
        }

        bool trim = req.TrimToBounds;
        bool useColor = req.ColorMode == SchematicExportColorMode.Color && palette != null;

        // When trimming, translate everything so (bounds.X, bounds.Y)
        // becomes the export's origin. Otherwise paint everything at
        // its original layout coordinates inside the full canvas.
        double offsetX = trim ? -bounds.X + padding : 0;
        double offsetY = trim ? -bounds.Y + padding + (req.IncludeTitle ? TitleHeight : 0) : 0;

        // Non-trim keeps layout coordinates, so the canvas must still stretch to
        // cover any grouped box that a widened title pushed past the layout's own
        // extent (otherwise that box clips at the right/bottom edge).
        double fullW = Math.Max(filtered.Layout.CanvasWidth, groupMaxX);
        double fullH = Math.Max(filtered.Layout.CanvasHeight, groupMaxY);
        double exportW = trim
            ? bounds.Width + padding * 2
            : fullW + padding * 2;
        double exportH = trim
            ? bounds.Height + padding * 2 + (req.IncludeTitle ? TitleHeight : 0)
            : fullH + padding * 2 + (req.IncludeTitle ? TitleHeight : 0);

        var sb = new StringBuilder(8192);
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append(ci, $"<svg xmlns=\"http://www.w3.org/2000/svg\" ")
          .Append(ci, $"width=\"{exportW.ToString("0.##", ci)}\" height=\"{exportH.ToString("0.##", ci)}\" ")
          .Append(ci, $"viewBox=\"0 0 {exportW.ToString("0.##", ci)} {exportH.ToString("0.##", ci)}\" ")
          .Append("font-family=\"ui-monospace, Menlo, Consolas, monospace\">\n");

        // Background.
        string bgColour = req.Background switch
        {
            SchematicExportBackground.White => "#ffffff",
            SchematicExportBackground.Dark => "#0f172a",
            _ => "transparent",
        };
        if (bgColour != "transparent")
        {
            sb.Append(ci, $"<rect x=\"0\" y=\"0\" width=\"{exportW.ToString("0.##", ci)}\" ")
              .Append(ci, $"height=\"{exportH.ToString("0.##", ci)}\" fill=\"{bgColour}\" />\n");
        }

        // Title bar.
        if (req.IncludeTitle && !string.IsNullOrEmpty(title))
        {
            string titleColour = req.Background == SchematicExportBackground.Dark ? "#f1f5f9" : "#1f2937";
            sb.Append(ci, $"<text x=\"{padding}\" y=\"{padding + 4}\" font-size=\"{titleFont}\" font-weight=\"600\" ")
              .Append(ci, $"fill=\"{titleColour}\">")
              .Append(WebUtility.HtmlEncode(title))
              .Append("</text>\n");
        }

        bool darkBg = req.Background == SchematicExportBackground.Dark;

        // RTL arrowhead marker (inherits each wire's stroke via context-stroke).
        if (rtl)
        {
            sb.Append("<defs><marker id=\"schematic-arrow\" markerWidth=\"8\" markerHeight=\"8\" ")
              .Append("refX=\"6.5\" refY=\"4\" orient=\"auto-start-reverse\" markerUnits=\"userSpaceOnUse\">")
              .Append("<path d=\"M0,1 L7,4 L0,7 Z\" fill=\"context-stroke\" /></marker></defs>\n");
        }

        // Grouped style: titled containers behind the gates, one per statement.
        // Geometry (including the title-fitting width) was computed up front and
        // folded into the bounds; here we just place each box at the draw offset.
        if (grouped)
        {
            string groupText = darkBg ? "#f1f5f9" : "#1f2937";
            foreach (var gb in groupBoxes)
            {
                sb.Append(BlockSkins.GroupContainer(gb.X + offsetX, gb.Y + offsetY, gb.W, gb.H,
                    gb.Title, $"rgba({gb.Hue},.09)", $"rgba({gb.Hue},.55)", groupText)).Append('\n');
            }
        }

        // Wires first so cells render on top of them at junctions.
        string wireDefault = darkBg ? "#94a3b8" : "#475569";
        foreach (var wire in filtered.Layout.Wires)
        {
            if (!filtered.VisibleNetIds.Contains(wire.NetId)) continue;
            // Per-branch hide: if either endpoint cell is outside the
            // visible cell set, drop this branch (matches the on-
            // screen Naive isolation behaviour).
            if (wire.FromCellId != null && !filtered.VisibleCellIds.Contains(wire.FromCellId)) continue;
            if (wire.ToCellId != null && !filtered.VisibleCellIds.Contains(wire.ToCellId)) continue;

            var net = filtered.Layout.Netlist.Nets.FirstOrDefault(n => n.Id == wire.NetId);
            if (net == null) continue;
            int sw = net.Kind == NetKind.Bus ? busStroke : wireStroke;
            string d = nodes
                ? BuildWireBezier(wire, offsetX, offsetY)
                : BuildWirePath(wire, offsetX, offsetY);
            string stroke = wireDefault;
            if (useColor && palette != null)
            {
                // Wire colour priority: the per-net wire hue (spaced per driver
                // so sibling signals stay distinct), then the producer cell's
                // stroke as a fallback, then the input port's hue when the
                // source is an entity port, then the monochrome default.
                if (palette.Wires.TryGetValue(wire.NetId, out var wireCol))
                    stroke = wireCol;
                else if (wire.FromCellId != null
                    && palette.Cells.TryGetValue(wire.FromCellId, out var producerCol))
                    stroke = producerCol.Stroke;
                else if (palette.Ports.TryGetValue(wire.NetId, out var portCol))
                    stroke = portCol;
            }
            string marker = rtl ? " marker-end=\"url(#schematic-arrow)\"" : "";
            sb.Append(ci, $"<path d=\"{d}\" fill=\"none\" stroke=\"{stroke}\" ")
              .Append(ci, $"stroke-width=\"{sw}\" stroke-linejoin=\"round\"{marker} />\n");
        }

        // Cells.
        string cellBodyDefault = darkBg ? "#1e293b" : "#f8fafc";
        string cellStrokeDefault = darkBg ? "#cbd5e1" : "#1e293b";
        string labelDefault = darkBg ? "#f1f5f9" : "#1e293b";
        foreach (var c in filtered.Layout.Netlist.Cells)
        {
            if (!filtered.VisibleCellIds.Contains(c.Id)) continue;
            if (!filtered.Layout.CellBounds.TryGetValue(c.Id, out var b)) continue;

            // Per-cell palette lookup with sensible monochrome fallback.
            string body = cellBodyDefault, stroke = cellStrokeDefault, lbl = labelDefault;
            if (useColor && palette != null && palette.Cells.TryGetValue(c.Id, out var col))
            {
                body = col.Body;
                stroke = col.Stroke;
                lbl = col.Label;
            }

            double x = b.X + offsetX;
            double y = b.Y + offsetY;
            sb.Append(ci, $"<g transform=\"translate({x.ToString("0.##", ci)} {y.ToString("0.##", ci)})\">");

            // Block styles draw a box/card instead of gate glyphs.
            if (block && c.Attrs != null && c.Attrs.ContainsKey(BlockGraphBuilder.BlockAttr))
            {
                var bcolors = new BlockSkins.Colors(
                    Fill: body, Stroke: stroke, HeaderFill: stroke, HeaderText: darkBg ? "#0f172a" : "#ffffff",
                    Title: lbl, Sub: darkBg ? "#94a3b8" : "#64748b", Glyph: lbl,
                    PortFill: cellBodyDefault, PortStroke: stroke, PortLabel: labelDefault);
                bool IsBus(string netId)
                {
                    var n = filtered.Layout.Netlist.Nets.FirstOrDefault(x => x.Id == netId);
                    return n is { Kind: NetKind.Bus };
                }
                sb.Append(BlockSkins.Build(c, b.Width, b.Height, req.Mode, bcolors, IsBus));
                sb.Append("</g>\n");
                continue;
            }

            // Gate-level component / black-box instances draw as a label-fit box
            // (sized by SchematicCellSizing) instead of the fixed 70×50 glyph, so
            // the instance name sits inside its own box rather than overflowing.
            if (SchematicCellSizing.IsBoxCell(c))
            {
                sb.Append(BlockSkins.ComponentBox(b.Width, b.Height,
                    SchematicCellSizing.LabelOf(c), BlockSkins.Glyph(c.Kind), body, stroke, lbl));
                sb.Append("</g>\n");
                continue;
            }

            foreach (var path in GateGlyphs.Paths(c.Kind))
            {
                sb.Append(ci, $"<path d=\"{path}\" fill=\"{body}\" ")
                  .Append(ci, $"stroke=\"{stroke}\" stroke-width=\"1.5\" />");
            }
            string label = (c.Attrs != null && c.Attrs.TryGetValue("Op", out var op) && !string.IsNullOrEmpty(op))
                ? op.ToLowerInvariant()
                : GateGlyphs.Label(c.Kind);
            if (!string.IsNullOrEmpty(label))
            {
                sb.Append(ci, $"<text x=\"{GateGlyphs.LabelX(c.Kind).ToString("0.##", ci)}\" ")
                  .Append(ci, $"y=\"{(GateGlyphs.Height / 2 + 4).ToString("0.##", ci)}\" ")
                  .Append(ci, $"text-anchor=\"middle\" font-size=\"{labelFont}\" font-weight=\"600\" ")
                  .Append(ci, $"fill=\"{lbl}\">")
                  .Append(WebUtility.HtmlEncode(label))
                  .Append("</text>");
            }
            // Output net name above the cell (skip auto-named internals
            // so the diagram doesn't get noisy with "_n0"-style stubs).
            // Human mode suppresses these tiny labels entirely — students
            // don't need to see every intermediate net name; Raw mode
            // still shows them for detailed inspection.
            if (!human && c.Outputs.Count > 0)
            {
                var outNet = filtered.Layout.Netlist.Nets.FirstOrDefault(n => n.Id == c.Outputs[0].NetId);
                if (outNet != null && !outNet.Name.StartsWith("_n", StringComparison.Ordinal))
                {
                    string mutedColour = darkBg ? "#94a3b8" : "#64748b";
                    sb.Append(ci, $"<text x=\"{(GateGlyphs.Width / 2).ToString("0.##", ci)}\" y=\"-6\" ")
                      .Append("text-anchor=\"middle\" font-size=\"9\" ")
                      .Append(ci, $"fill=\"{mutedColour}\">")
                      .Append(WebUtility.HtmlEncode(outNet.Name))
                      .Append("</text>");
                }
            }
            sb.Append("</g>\n");
        }

        // Ports.
        foreach (var port in filtered.Layout.Netlist.Ports)
        {
            if (!filtered.VisiblePortNetIds.Contains(port.NetId)) continue;
            if (!filtered.Layout.PortPositions.TryGetValue(port.NetId, out var pos)) continue;
            double x = pos.Item1 + offsetX;
            double y = pos.Item2 + offsetY;
            string fill = port.Direction switch
            {
                PortDir.In => "#3c82f6",
                PortDir.Out => "#f59e0b",
                _ => "#94a3b8",
            };
            if (useColor && palette != null && palette.Ports.TryGetValue(port.NetId, out var portCol))
                fill = portCol;
            sb.Append(ci, $"<circle cx=\"{x.ToString("0.##", ci)}\" cy=\"{y.ToString("0.##", ci)}\" ")
              .Append(ci, $"r=\"6\" fill=\"{fill}\" stroke=\"{cellStrokeDefault}\" stroke-width=\"1.5\" />");
            bool isInput = port.Direction is PortDir.In or PortDir.InOut;
            double labelX = isInput ? x + 12 : x - 12;
            string anchor = isInput ? "start" : "end";
            sb.Append(ci, $"<text x=\"{labelX.ToString("0.##", ci)}\" y=\"{(y + 4).ToString("0.##", ci)}\" ")
              .Append(ci, $"text-anchor=\"{anchor}\" font-size=\"{portLabelFont}\" font-weight=\"600\" ")
              .Append(ci, $"fill=\"{labelDefault}\">")
              .Append(WebUtility.HtmlEncode(port.Name))
              // Use digital-design bit-range notation for multi-bit ports
              // (`reg_out[7:0]`, not `reg_out[8]`) so students see the
              // convention they'll meet in every textbook and vendor tool.
              .Append(port.Width > 1 ? $"[{port.Width - 1}:0]" : "")
              .Append("</text>\n");
        }

        // Annotation cards (notes + pinned info) float on top of the diagram,
        // at the same canvas coordinates they occupy on screen.
        if (annotations is { Count: > 0 })
        {
            foreach (var a in annotations)
            {
                double x = a.X + offsetX;
                double y = a.Y + offsetY;
                sb.Append(ci, $"<g transform=\"translate({x.ToString("0.##", ci)} {y.ToString("0.##", ci)})\">");
                sb.Append(AnnotationSkins.Build(a));
                sb.Append("</g>\n");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BuildWirePath(WirePath wire, double offsetX, double offsetY)
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        for (int i = 0; i < wire.Points.Count; i++)
        {
            var (x, y) = wire.Points[i];
            sb.Append(i == 0 ? "M " : "L ");
            sb.Append((x + offsetX).ToString("0.##", ci));
            sb.Append(' ');
            sb.Append((y + offsetY).ToString("0.##", ci));
            sb.Append(' ');
        }
        return sb.ToString();
    }

    // Node-editor link (Nodos style): a smooth curve following the routed
    // polyline with rounded corners — mirrors the on-screen viewer.
    private static string BuildWireBezier(WirePath wire, double offsetX, double offsetY)
    {
        if (wire.Points.Count < 2) return BuildWirePath(wire, offsetX, offsetY);
        var shifted = wire.Points.Select(p => (p.X + offsetX, p.Y + offsetY)).ToList();
        return BlockSkins.RoundedPolyline(shifted);
    }
}
