using System.Globalization;
using System.Text;

namespace Kmila.CLI.Tui;

/// <summary>
/// Draws a <see cref="WaveModel"/> into a rectangle of a <see cref="ScreenBuffer"/>
/// as ASCII/Unicode digital waveforms: scalar wires as two-level square waves,
/// buses as labelled value segments, plus a time ruler and a cursor column.
/// </summary>
public static class WaveformRenderer
{
    /// <summary>Renders the waveform area.</summary>
    /// <param name="buf">Target buffer.</param>
    /// <param name="x">Left column of the drawing area.</param>
    /// <param name="y">Top row of the drawing area.</param>
    /// <param name="w">Area width.</param>
    /// <param name="h">Area height (last row is the ruler).</param>
    /// <param name="model">Waveform data.</param>
    /// <param name="viewStartNs">Left-edge time, ns.</param>
    /// <param name="nsPerCol">Nanoseconds represented by one column.</param>
    /// <param name="cursorNs">Cursor time, ns.</param>
    /// <param name="topSignal">Index of the first visible signal (vertical scroll).</param>
    /// <param name="selected">Index of the highlighted signal.</param>
    /// <param name="unicode">Use Unicode glyphs when true, ASCII when false.</param>
    public static void Draw(ScreenBuffer buf, int x, int y, int w, int h,
        WaveModel model, long viewStartNs, double nsPerCol, long cursorNs,
        int topSignal, int selected, bool unicode)
    {
        if (w < 6 || h < 2) return;

        int labelW = Math.Clamp(w / 4, 8, 20);
        int trackX = x + labelW;
        int trackW = w - labelW;
        if (trackW < 2) return;

        int signalRows = h - 1;                 // reserve the last row for the ruler
        long FsPerNs = WaveModel.FsPerNs;
        if (nsPerCol <= 0) nsPerCol = 1;

        char hi  = unicode ? '▀' : '-';
        char lo  = unicode ? '▄' : '_';
        char edge = unicode ? '█' : '|';
        char unk = unicode ? '▓' : 'x';
        char band = unicode ? '─' : '=';
        char xing = unicode ? '╳' : 'X';

        int cursorCol = (int)Math.Round((cursorNs - viewStartNs) / nsPerCol);

        for (int r = 0; r < signalRows; r++)
        {
            int si = topSignal + r;
            if (si >= model.Signals.Count) break;
            var sig = model.Signals[si];
            int row = y + r;
            bool sel = si == selected;

            // Label column: name (highlighted when selected).
            buf.FillRow(row, x, trackX, ' ', sel ? Attr.Selected : Attr.Default);
            buf.TextClipped(x, row, sig.Name, labelW - 1, sel ? Attr.Selected : Attr.Default);

            // Precompute the per-column value.
            var vals = new string?[trackW];
            for (int c = 0; c < trackW; c++)
            {
                long t = viewStartNs + (long)(c * nsPerCol);
                vals[c] = sig.ValueAtFs(t * FsPerNs);
            }

            if (!sig.IsBus)
            {
                for (int c = 0; c < trackW; c++)
                {
                    var v = vals[c];
                    char g;
                    Attr a;
                    if (v == null) { g = unicode ? '·' : '.'; a = Attr.Dim; }
                    else if (v is "1" or "h") { g = hi; a = Attr.Green; }
                    else if (v is "0" or "l") { g = lo; a = Attr.Green; }
                    else { g = unk; a = Attr.Red; }
                    if (c > 0 && v != null && vals[c - 1] != null && v != vals[c - 1])
                        g = edge;
                    buf.Set(trackX + c, row, g, a);
                }
            }
            else
            {
                // Band + boundary markers.
                for (int c = 0; c < trackW; c++)
                {
                    bool changed = c > 0 && vals[c] != vals[c - 1];
                    buf.Set(trackX + c, row, changed ? xing : band,
                        vals[c] == null ? Attr.Dim : Attr.Cyan);
                }
                // Segment value labels.
                for (int c = 0; c < trackW; c++)
                {
                    if (vals[c] == null) continue;
                    if (c == 0 || vals[c] != vals[c - 1])
                    {
                        var lab = FormatBus(vals[c]!, sig.Width);
                        for (int k = 0; k < lab.Length && (c + 1 + k) < trackW; k++)
                            buf.Set(trackX + c + 1 + k, row, lab[k], Attr.Cyan);
                    }
                }
            }

            // Cursor overlay for this row — recolor the existing glyph (or draw a
            // thin bar where the track is blank) so the value stays readable.
            if (cursorCol >= 0 && cursorCol < trackW)
            {
                int i = trackX + cursorCol;
                char cur = buf.CharAt(i, row);
                buf.Set(i, row, cur == ' ' ? (unicode ? '│' : '|') : cur, Attr.Yellow);
            }
        }

        // Ruler.
        int rulerRow = y + signalRows;
        buf.FillRow(rulerRow, x, trackX, ' ', Attr.Default);
        buf.TextClipped(x, rulerRow, "ns", labelW - 1, Attr.Dim);
        for (int c = 0; c < trackW; c++)
            buf.Set(trackX + c, rulerRow, unicode ? '─' : '-', Attr.Dim);

        int step = Math.Max(8, trackW / 8);
        for (int c = 0; c < trackW; c += step)
        {
            long nsAt = viewStartNs + (long)(c * nsPerCol);
            buf.Set(trackX + c, rulerRow, unicode ? '┬' : '+', Attr.Dim);
            var lab = nsAt.ToString(CultureInfo.InvariantCulture);
            for (int k = 0; k < lab.Length && (c + 1 + k) < trackW; k++)
                buf.Set(trackX + c + 1 + k, rulerRow, lab[k], Attr.Dim);
        }
        if (cursorCol >= 0 && cursorCol < trackW)
            buf.Set(trackX + cursorCol, rulerRow, unicode ? '▲' : '^', Attr.Yellow);
    }

    /// <summary>Formats a bus bit-string (MSB first) as hex, or raw when it carries x/z/u.</summary>
    public static string FormatBus(string bits, int width)
    {
        if (string.IsNullOrEmpty(bits)) return "?";
        bool clean = bits.All(ch => ch is '0' or '1');
        if (!clean)
            return bits.Length <= 8 ? bits : bits[..8];

        // Pad to a nibble boundary and convert to hex.
        int pad = (4 - bits.Length % 4) % 4;
        var padded = new string('0', pad) + bits;
        var sb = new StringBuilder("0x");
        for (int i = 0; i < padded.Length; i += 4)
        {
            int nib = Convert.ToInt32(padded.Substring(i, 4), 2);
            sb.Append("0123456789ABCDEF"[nib]);
        }
        return sb.ToString();
    }
}
