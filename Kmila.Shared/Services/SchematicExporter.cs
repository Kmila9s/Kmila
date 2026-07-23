using System.Globalization;
using System.Net;
using System.Text;
using Interpreter.Synthesis;
using Interpreter.Synthesis.Layout;
using Kmila.Shared.Components.Schematic;
using Kmila.Shared.Models;

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
    /// When <see cref="SchematicExportRequest.Mode"/> is
    /// <see cref="SchematicRenderMode.Human"/>, the layout is first
    /// piped through <see cref="SchematicSimplifier.Simplify"/> and
    /// several rendering knobs shift toward readability: padding
    /// grows, fonts grow, wire strokes thicken, and muted internal
    /// output-net labels are hidden so trivial diagrams don't look
    /// noisy.
    /// </summary>
    public static string BuildSvg(
        FilteredLayout filtered,
        SchematicExportRequest req,
        string title,
        SchematicColourPalette? palette = null)
    {
        var ci = CultureInfo.InvariantCulture;
        bool human = req.Mode == SchematicRenderMode.Human;
        if (human)
        {
            filtered = SchematicSimplifier.Simplify(filtered, palette);
        }
        int padding = human ? HumanPadding : RawPadding;
        int labelFont = human ? 14 : 11;
        int titleFont = human ? 16 : 13;
        int portLabelFont = human ? 14 : 11;
        int busStroke = human ? 4 : 3;
        int wireStroke = human ? 3 : 2;

        var bounds = filtered.Bounds;
        bool trim = req.TrimToBounds;
        bool useColor = req.ColorMode == SchematicExportColorMode.Color && palette != null;

        // When trimming, translate everything so (bounds.X, bounds.Y)
        // becomes the export's origin. Otherwise paint everything at
        // its original layout coordinates inside the full canvas.
        double offsetX = trim ? -bounds.X + padding : 0;
        double offsetY = trim ? -bounds.Y + padding + (req.IncludeTitle ? TitleHeight : 0) : 0;

        double exportW = trim
            ? bounds.Width + padding * 2
            : filtered.Layout.CanvasWidth + padding * 2;
        double exportH = trim
            ? bounds.Height + padding * 2 + (req.IncludeTitle ? TitleHeight : 0)
            : filtered.Layout.CanvasHeight + padding * 2 + (req.IncludeTitle ? TitleHeight : 0);

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

        // Wires first so cells render on top of them at junctions.
        bool darkBg = req.Background == SchematicExportBackground.Dark;
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
            string d = BuildWirePath(wire, offsetX, offsetY);
            string stroke = wireDefault;
            if (useColor && palette != null)
            {
                // Wire colour priority: producer cell's stroke (so a
                // fan-out tree shares one hue), then the input port's
                // hue when the source is an entity port, then the
                // monochrome default.
                if (wire.FromCellId != null
                    && palette.Cells.TryGetValue(wire.FromCellId, out var producerCol))
                    stroke = producerCol.Stroke;
                else if (palette.Ports.TryGetValue(wire.NetId, out var portCol))
                    stroke = portCol;
            }
            sb.Append(ci, $"<path d=\"{d}\" fill=\"none\" stroke=\"{stroke}\" ")
              .Append(ci, $"stroke-width=\"{sw}\" stroke-linejoin=\"round\" />\n");
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
}
