using System.Globalization;
using System.Net;
using System.Text;
using Kmila.Shared.Models.Schematic;

namespace Kmila.Shared.Components.Schematic;

/// <summary>
/// Renders a canvas annotation card (<see cref="SchematicAnnotation"/>) as pure
/// SVG in the card's LOCAL coordinate space (origin at its top-left). The caller
/// wraps the output in a <c>&lt;g transform="translate(x y)"&gt;</c>, so the exact
/// same markup serves the on-screen viewer and the SVG/PNG exporter — the card
/// can never drift between screen and export (same convention as BlockSkins).
/// </summary>
public static class AnnotationSkins
{
    private const double Pad = 10;
    private const double TitleFont = 13;
    private const double BodyFont = 11;
    private const double TtFont = 10;
    private const double Line = 15;

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>Inner SVG body for one card, drawn at local (0,0)..(W,H).</summary>
    public static string Build(SchematicAnnotation a)
    {
        var sb = new StringBuilder();
        string fg = ReadableTextColor(a.Color);
        string stroke = Shade(a.Color, -0.25);
        string clip = "annclip-" + a.Id;

        sb.Append($"<clipPath id=\"{clip}\"><rect x=\"0\" y=\"0\" width=\"{F(a.W)}\" height=\"{F(a.H)}\" rx=\"8\" /></clipPath>");
        sb.Append($"<rect class=\"ann-bg\" x=\"0\" y=\"0\" width=\"{F(a.W)}\" height=\"{F(a.H)}\" rx=\"8\" ")
          .Append($"fill=\"{a.Color}\" stroke=\"{stroke}\" stroke-width=\"1.25\" />");

        sb.Append($"<g clip-path=\"url(#{clip})\">");
        double y = Pad + TitleFont;

        if (!string.IsNullOrEmpty(a.Title))
        {
            sb.Append(Text(Pad, y, a.Title, TitleFont, 700, fg));
            y += Line + 2;
            sb.Append($"<line x1=\"{F(Pad)}\" y1=\"{F(y - Line + 2)}\" x2=\"{F(a.W - Pad)}\" ")
              .Append($"y2=\"{F(y - Line + 2)}\" stroke=\"{stroke}\" stroke-width=\"0.75\" opacity=\"0.6\" />");
        }

        if (a.Kind == SchematicAnnotationKind.Note)
        {
            foreach (var ln in Wrap(a.Body, CharsFor(a.W, BodyFont)))
            {
                sb.Append(Text(Pad, y, ln, BodyFont, 400, fg));
                y += Line;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(a.Summary))
            {
                foreach (var ln in Wrap(a.Summary!, CharsFor(a.W, BodyFont)))
                {
                    sb.Append(Text(Pad, y, ln, BodyFont, 600, fg));
                    y += Line;
                }
                y += 2;
            }
            foreach (var row in a.Fields)
            {
                string label = row.Length > 0 ? row[0] : "";
                string val = row.Length > 1 ? row[1] : "";
                foreach (var ln in Wrap($"{label}: {val}", CharsFor(a.W, BodyFont)))
                {
                    sb.Append(Text(Pad, y, ln, BodyFont, 400, fg));
                    y += Line;
                }
            }
            if (a.TruthTable is { Count: > 0 } rows)
            {
                y += 4;
                y = AppendTruthTable(sb, rows, a.W, y, fg, stroke);
            }
        }

        sb.Append("</g>");
        return sb.ToString();
    }

    /// <summary>Height the card needs to show all of its content at width W —
    /// used to size a freshly created card. Mirrors Build's layout.</summary>
    public static double ContentHeight(SchematicAnnotation a)
    {
        double y = Pad + TitleFont;
        if (!string.IsNullOrEmpty(a.Title)) y += Line + 2;

        if (a.Kind == SchematicAnnotationKind.Note)
        {
            y += Math.Max(1, Wrap(a.Body, CharsFor(a.W, BodyFont)).Count) * Line;
        }
        else
        {
            if (!string.IsNullOrEmpty(a.Summary))
                y += Wrap(a.Summary!, CharsFor(a.W, BodyFont)).Count * Line + 2;
            foreach (var row in a.Fields)
            {
                string label = row.Length > 0 ? row[0] : "";
                string val = row.Length > 1 ? row[1] : "";
                y += Wrap($"{label}: {val}", CharsFor(a.W, BodyFont)).Count * Line;
            }
            if (a.TruthTable is { Count: > 0 } rows)
                y += 4 + rows.Count * (Line - 1) + 4;
        }
        return Math.Ceiling(y + Pad);
    }

    private static double AppendTruthTable(StringBuilder sb, List<string[]> rows,
        double w, double y, string fg, string stroke)
    {
        int ncols = rows.Max(r => r.Length);
        if (ncols == 0) return y;
        double colW = (w - 2 * Pad) / ncols;
        double rh = Line - 1;
        for (int r = 0; r < rows.Count; r++)
        {
            bool header = r == 0;
            for (int c = 0; c < ncols; c++)
            {
                string cell = c < rows[r].Length ? rows[r][c] : "";
                double cx = Pad + c * colW + colW / 2.0;
                sb.Append($"<text x=\"{F(cx)}\" y=\"{F(y)}\" text-anchor=\"middle\" ")
                  .Append($"font-size=\"{F(TtFont)}\" font-weight=\"{(header ? 700 : 400)}\" ")
                  .Append($"font-family=\"ui-monospace, Menlo, monospace\" fill=\"{fg}\">")
                  .Append(Esc(cell)).Append("</text>");
            }
            y += rh;
            if (header)
            {
                sb.Append($"<line x1=\"{F(Pad)}\" y1=\"{F(y - rh + 3)}\" x2=\"{F(w - Pad)}\" ")
                  .Append($"y2=\"{F(y - rh + 3)}\" stroke=\"{stroke}\" stroke-width=\"0.75\" opacity=\"0.6\" />");
            }
        }
        return y;
    }

    private static string Text(double x, double y, string s, double size, int weight, string fill)
        => $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{F(size)}\" font-weight=\"{weight}\" "
         + $"font-family=\"ui-monospace, Menlo, monospace\" fill=\"{fill}\">{Esc(s)}</text>";

    private static int CharsFor(double width, double font)
        => Math.Max(4, (int)((width - 2 * Pad) / (font * 0.62)));

    /// <summary>Greedy word-wrap; also splits on explicit newlines.</summary>
    public static List<string> Wrap(string text, int maxChars)
    {
        var outLines = new List<string>();
        if (string.IsNullOrEmpty(text)) return outLines;
        foreach (var para in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (para.Length == 0) { outLines.Add(""); continue; }
            var cur = new StringBuilder();
            foreach (var word in para.Split(' '))
            {
                string w = word;
                // Hard-break a single word longer than the line.
                while (w.Length > maxChars)
                {
                    if (cur.Length > 0) { outLines.Add(cur.ToString()); cur.Clear(); }
                    outLines.Add(w.Substring(0, maxChars));
                    w = w.Substring(maxChars);
                }
                if (cur.Length == 0) cur.Append(w);
                else if (cur.Length + 1 + w.Length <= maxChars) cur.Append(' ').Append(w);
                else { outLines.Add(cur.ToString()); cur.Clear(); cur.Append(w); }
            }
            if (cur.Length > 0) outLines.Add(cur.ToString());
        }
        return outLines;
    }

    /// <summary>Dark or light text depending on the background's luminance.</summary>
    public static string ReadableTextColor(string hex)
        => Luminance(hex) > 0.6 ? "#0f172a" : "#f8fafc";

    private static double Luminance(string hex)
    {
        var (r, g, b) = Parse(hex);
        return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0;
    }

    /// <summary>Darkens (amount &lt; 0) or lightens a hex colour for borders.</summary>
    private static string Shade(string hex, double amount)
    {
        var (r, g, b) = Parse(hex);
        int Adj(int c) => Math.Clamp((int)(c + amount * 255), 0, 255);
        return $"#{Adj(r):x2}{Adj(g):x2}{Adj(b):x2}";
    }

    private static (int R, int G, int B) Parse(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return (226, 232, 240);
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length < 6) return (226, 232, 240);
        try
        {
            return (Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16));
        }
        catch { return (226, 232, 240); }
    }
}
