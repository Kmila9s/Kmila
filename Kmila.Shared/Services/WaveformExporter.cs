using System.Globalization;
using System.Net;
using System.Text;
using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Builds a self-contained SVG document of a waveform — labels, traces, and
/// ruler all in one file — for the Export-to-PDF/PNG/SVG flow. Distinct from
/// <c>WaveformViewer</c>'s on-screen renderer (which mixes HTML labels with
/// per-row SVG tracks) because exported files have to render identically
/// wherever they're pasted, with no external CSS or DOM scaffolding.
///
/// The clipping/X-mapping logic mirrors the viewer's windowed mode so a
/// "10 µs to 30 µs" export looks the same as scrolling that window in the
/// live UI: transitions outside the window are skipped, and each clipped
/// signal enters at its real level (taken from the most recent transition
/// at or before <c>viewStartNs</c>) instead of re-baselining to whatever
/// its first stored sample happened to be.
/// </summary>
public static class WaveformExporter
{
    private const int LabelWidth  = 200;
    private const int TitleHeight = 28;
    private const int RowHeight   = 40;
    private const int RulerHeight = 22;
    private const int Pad         = 8;

    /// <summary>
    /// Builds the SVG document for a single page (one time slice). Multi-page
    /// exports call this once per slice.
    /// </summary>
    /// <param name="data">Source waveform.</param>
    /// <param name="viewStartNs">Inclusive window start, ns.</param>
    /// <param name="viewEndNs">Exclusive window end, ns.</param>
    /// <param name="trackWidthPx">Pixel width of the trace column.</param>
    /// <param name="includeClock">When false, signals named clk/clock are
    /// skipped — matches the viewer's eye-toggle so exported pages reflect
    /// what the user is currently looking at.</param>
    /// <param name="title">Top-line title (project name + range).</param>
    public static string BuildSvg(
        WaveformData data,
        long viewStartNs,
        long viewEndNs,
        int trackWidthPx,
        bool includeClock,
        string title)
    {
        // Filter clocks early so layout dimensions match what we draw.
        var signals = data.Signals
            .Where(s => includeClock || !IsClockName(s.Name))
            .ToList();

        long spanNs = viewEndNs - viewStartNs;
        if (spanNs <= 0) spanNs = 1; // safety: avoid divide-by-zero on degenerate ranges

        int totalWidth  = Pad * 2 + LabelWidth + trackWidthPx;
        int totalHeight = Pad * 2 + TitleHeight + RowHeight * signals.Count + RulerHeight;

        var sb = new StringBuilder(8192);
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{totalWidth}\" height=\"{totalHeight}\" ");
        sb.Append(CultureInfo.InvariantCulture,
            $"viewBox=\"0 0 {totalWidth} {totalHeight}\" font-family=\"ui-monospace, Menlo, Consolas, monospace\">\n");

        // Solid white background — guarantees the export looks the same when
        // pasted onto dark word-processor canvases or rendered by tools that
        // don't honour transparent SVG backgrounds.
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"0\" y=\"0\" width=\"{totalWidth}\" height=\"{totalHeight}\" fill=\"#ffffff\" />\n");

        // Title bar
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{Pad}\" y=\"{Pad + 18}\" font-size=\"13\" fill=\"#1f2937\" font-weight=\"600\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</text>\n");

        int bodyX = Pad;
        int bodyY = Pad + TitleHeight;

        // Per-signal rows
        for (int i = 0; i < signals.Count; i++)
        {
            var sig = signals[i];
            int rowY = bodyY + i * RowHeight;

            DrawLabel(sb, sig, bodyX, rowY);
            DrawTrace(sb, sig, bodyX + LabelWidth, rowY, trackWidthPx, viewStartNs, viewEndNs, spanNs);
        }

        // Ruler row
        int rulerY = bodyY + signals.Count * RowHeight;
        DrawRuler(sb, bodyX + LabelWidth, rulerY, trackWidthPx, viewStartNs, viewEndNs, spanNs);

        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>
    /// Generates the (start, end) windows for a multi-page split. When
    /// <paramref name="pageSpanNs"/> is null or ≥ the full range, returns
    /// a single window covering [<paramref name="rangeStartNs"/>,
    /// <paramref name="rangeEndNs"/>]. Otherwise emits consecutive
    /// non-overlapping pages of <paramref name="pageSpanNs"/> each, with
    /// the last page possibly shorter so the export matches the user's
    /// requested range exactly.
    /// </summary>
    public static IEnumerable<(long startNs, long endNs)> PaginateRange(
        long rangeStartNs, long rangeEndNs, long? pageSpanNs)
    {
        if (rangeEndNs <= rangeStartNs) yield break;

        long span = rangeEndNs - rangeStartNs;
        if (pageSpanNs == null || pageSpanNs.Value <= 0 || pageSpanNs.Value >= span)
        {
            yield return (rangeStartNs, rangeEndNs);
            yield break;
        }

        long page = pageSpanNs.Value;
        for (long start = rangeStartNs; start < rangeEndNs; start += page)
        {
            long end = Math.Min(start + page, rangeEndNs);
            yield return (start, end);
        }
    }

    // ─── Label column ──────────────────────────────────────────────────────

    private static void DrawLabel(StringBuilder sb, WaveformSignal signal, int x, int y)
    {
        // Port-direction chip (small rounded pill, mirrors the on-screen
        // .waveform-signal-chip styling — colours match so an exported page
        // and a screenshot of the live viewer read as siblings).
        var (chipBg, chipFg) = ChipColours(signal.PortType);
        const int chipW = 56;
        const int chipH = 16;
        int chipY = y + (RowHeight - chipH) / 2;

        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"{x}\" y=\"{chipY}\" width=\"{chipW}\" height=\"{chipH}\" rx=\"8\" fill=\"{chipBg}\" />\n");
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{x + chipW / 2}\" y=\"{chipY + chipH - 4}\" text-anchor=\"middle\" font-size=\"9\" font-weight=\"700\" fill=\"{chipFg}\">");
        sb.Append(WebUtility.HtmlEncode(signal.PortType));
        sb.Append("</text>\n");

        // Signal name (left-aligned next to chip)
        int nameX = x + chipW + 8;
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{nameX}\" y=\"{y + RowHeight / 2 + 4}\" font-size=\"12\" font-weight=\"600\" fill=\"#1f2937\">");
        sb.Append(WebUtility.HtmlEncode(signal.Name));
        sb.Append("</text>\n");

        // Bit-width hint for vectors (e.g. [8])
        if (signal.BitWidth > 1)
        {
            int widthX = nameX + EstimateTextWidth(signal.Name, 12) + 6;
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{widthX}\" y=\"{y + RowHeight / 2 + 4}\" font-size=\"10\" fill=\"#64748b\">[{signal.BitWidth}]</text>\n");
        }
    }

    private static (string bg, string fg) ChipColours(string portType) => portType switch
    {
        "IN"       => ("#dbeafe", "#1d4ed8"), // blue
        "OUT"      => ("#fef3c7", "#b45309"), // amber
        "INOUT"    => ("#ede9fe", "#7c3aed"), // purple
        "INTERNAL" => ("#f1f5f9", "#475569"), // slate
        _          => ("#f1f5f9", "#475569")
    };

    // Crude proportional-font width estimate for monospace-ish layout —
    // good enough for placing the bit-width hint without measuring text in
    // a real layout engine. Each char ≈ 0.55 × font-size for monospace.
    private static int EstimateTextWidth(string text, int fontSize)
        => (int)Math.Ceiling(text.Length * fontSize * 0.55);

    // ─── Trace column ──────────────────────────────────────────────────────

    private static void DrawTrace(StringBuilder sb, WaveformSignal signal,
        int trackX, int rowY, int widthPx,
        long viewStartNs, long viewEndNs, long spanNs)
    {
        const double PadY = 4;
        const double ValHi  = PadY;
        const double ValLo  = RowHeight - PadY;
        const double ValMid = RowHeight / 2.0;

        // Faint baseline so the row is visible even if the signal has no
        // transitions in this window.
        sb.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{trackX}\" y1=\"{rowY + ValMid}\" x2=\"{trackX + widthPx}\" y2=\"{rowY + ValMid}\" stroke=\"#e5e7eb\" stroke-dasharray=\"2 4\" />\n");

        // Transitions to draw. Long runs (on-disk archive) whose page starts before
        // the in-memory window read the page from the archive: the entry level plus
        // the in-range transitions — full fidelity, bounded by the page's range.
        IReadOnlyList<WaveformTransition> trs = signal.Transitions;
        if (signal.Archive != null &&
            (signal.Transitions.Count == 0 || viewStartNs < signal.Transitions[0].TimeNs))
        {
            var page = new List<WaveformTransition>();
            if (signal.TransitionAtOrBefore(viewStartNs) is { } pageEntry) page.Add(pageEntry);
            page.AddRange(signal.Archive.ReadRange(viewStartNs + 1, viewEndNs));
            trs = page;
        }

        if (trs.Count == 0) return;

        // X(ns) → absolute SVG x for this track.
        double X(long ns) => trackX + (double)(ns - viewStartNs) / spanNs * widthPx;

        // Find entry-level transition (last one at or before viewStart) so
        // a partially-clipped signal starts at its real value, not whatever
        // its first stored sample is.
        int firstInside = trs.Count;
        WaveformTransition entry = trs[0];
        bool haveEntry = false;
        for (int i = 0; i < trs.Count; i++)
        {
            var t = trs[i];
            if (t.TimeNs <= viewStartNs)
            {
                entry = t;
                haveEntry = true;
                continue;
            }
            firstInside = i;
            break;
        }
        if (!haveEntry) entry = trs[0];

        string color = SignalColor(signal);

        if (signal.DataType == "std_logic")
        {
            double LevelY(double? v) => v.HasValue ? (v.Value >= 0.5 ? rowY + ValHi : rowY + ValLo) : rowY + ValMid;

            var solid  = new StringBuilder();
            var dashed = new StringBuilder();
            bool onDashed = !entry.Value.HasValue;
            double curY = LevelY(entry.Value);
            var target  = onDashed ? dashed : solid;
            target.Append(CultureInfo.InvariantCulture, $"M {trackX} {curY:0.##} ");

            // U-040: coalesce. The previous emitter wrote one horizontal `L`
            // per transition even at unchanging level (so 100k+ ticks of an
            // idle signal each emitted their own L command). Only emit an
            // L when the level or dashed-ness actually changes — the rest
            // collapse into the trailing close-out L at end-of-window. On
            // the SimulatorBench counter (1 ms × 50 MHz × 4-bit) this drops
            // the rendered path from 6.2 MB → ~0.5 MB without changing
            // visual fidelity (consecutive same-level points lie on the
            // same horizontal line anyway).
            for (int i = firstInside; i < trs.Count; i++)
            {
                var t = trs[i];
                if (t.TimeNs > viewEndNs) break;
                bool nextDashed = !t.Value.HasValue;
                double nextY    = LevelY(t.Value);
                if (nextDashed == onDashed && Equals(nextY, curY))
                    continue; // same-level run — nothing to draw yet

                double tx = X(t.TimeNs);
                target.Append(CultureInfo.InvariantCulture, $"L {tx:0.##} {curY:0.##} ");
                if (nextDashed != onDashed)
                {
                    var other = nextDashed ? dashed : solid;
                    other.Append(CultureInfo.InvariantCulture, $"M {tx:0.##} {curY:0.##} ");
                    target   = other;
                    onDashed = nextDashed;
                }
                if (!Equals(nextY, curY))
                {
                    target.Append(CultureInfo.InvariantCulture, $"L {tx:0.##} {nextY:0.##} ");
                    curY = nextY;
                }
            }
            target.Append(CultureInfo.InvariantCulture, $"L {trackX + widthPx} {curY:0.##} ");

            if (solid.Length > 0)
                sb.Append(CultureInfo.InvariantCulture,
                    $"<path d=\"{solid}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" stroke-linejoin=\"miter\" stroke-linecap=\"butt\" />\n");
            if (dashed.Length > 0)
                sb.Append(CultureInfo.InvariantCulture,
                    $"<path d=\"{dashed}\" fill=\"none\" stroke=\"#f59e0b\" stroke-width=\"2\" stroke-dasharray=\"4 3\" />\n");
        }
        else
        {
            // Multi-bit bus: mid-row line with diamond markers + value labels.
            var d = new StringBuilder();
            d.Append(CultureInfo.InvariantCulture, $"M {trackX} {rowY + ValMid:0.##} ");
            for (int i = firstInside; i < trs.Count; i++)
            {
                var t = trs[i];
                if (t.TimeNs > viewEndNs) break;
                double tx = X(t.TimeNs);
                d.Append(CultureInfo.InvariantCulture, $"L {tx - 3:0.##} {rowY + ValMid:0.##} ");
                d.Append(CultureInfo.InvariantCulture, $"L {tx:0.##} {rowY + ValHi:0.##} ");
                d.Append(CultureInfo.InvariantCulture, $"L {tx + 3:0.##} {rowY + ValMid:0.##} ");
            }
            d.Append(CultureInfo.InvariantCulture, $"L {trackX + widthPx} {rowY + ValMid:0.##}");
            sb.Append(CultureInfo.InvariantCulture,
                $"<path d=\"{d}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" />\n");

            // Value labels in each visible plateau, including the entry
            // plateau when its transition fell before the window.
            int plateauStart = haveEntry ? firstInside - 1 : firstInside;
            if (plateauStart < 0) plateauStart = 0;
            for (int i = plateauStart; i < trs.Count; i++)
            {
                var t = trs[i];
                if (t.TimeNs > viewEndNs) break;
                if (string.IsNullOrEmpty(t.Label)) continue;
                double tx = i < firstInside ? trackX : X(t.TimeNs);
                double nextX = i + 1 < trs.Count && trs[i + 1].TimeNs <= viewEndNs
                    ? X(trs[i + 1].TimeNs)
                    : trackX + widthPx;
                double midX = (tx + nextX) / 2.0;
                if (nextX - tx < 24) continue;

                sb.Append(CultureInfo.InvariantCulture,
                    $"<text x=\"{midX:0.##}\" y=\"{rowY + ValMid + 4:0.##}\" text-anchor=\"middle\" font-size=\"10\" fill=\"#1f2937\">");
                sb.Append(WebUtility.HtmlEncode(t.Label));
                sb.Append("</text>\n");
            }
        }
    }

    private static string SignalColor(WaveformSignal signal)
    {
        if (IsClockName(signal.Name)) return "#10b981"; // green for clocks
        if (signal.BitWidth > 1) return "#a855f7";      // purple for buses
        return signal.PortType switch
        {
            "IN"  => "#3c82f6",
            "OUT" => "#f59e0b",
            _     => "#94a3b8"
        };
    }

    private static bool IsClockName(string name) => StimulusBuilder.IsClockPortName(name);

    // ─── Ruler ─────────────────────────────────────────────────────────────

    private static void DrawRuler(StringBuilder sb,
        int trackX, int rulerY, int widthPx,
        long viewStartNs, long viewEndNs, long spanNs)
    {
        const int LabelCount = 12;
        for (int i = 0; i <= LabelCount; i++)
        {
            double frac = (double)i / LabelCount;
            double x = trackX + frac * widthPx;
            long ns = viewStartNs + (long)(frac * spanNs);
            sb.Append(CultureInfo.InvariantCulture,
                $"<line x1=\"{x:0.##}\" y1=\"{rulerY}\" x2=\"{x:0.##}\" y2=\"{rulerY + 5}\" stroke=\"#94a3b8\" />\n");
            var anchor = i == 0 ? "start" : (i == LabelCount ? "end" : "middle");
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{x:0.##}\" y=\"{rulerY + 17}\" text-anchor=\"{anchor}\" font-size=\"10\" fill=\"#64748b\">");
            sb.Append(FormatAxisLabel(ns));
            sb.Append("</text>\n");
        }
    }

    private static string FormatAxisLabel(long ns)
    {
        if (Math.Abs(ns) >= 1_000_000_000) return $"{ns / 1_000_000_000.0:0.##} s";
        if (Math.Abs(ns) >= 1_000_000)     return $"{ns / 1_000_000.0:0.##} ms";
        if (Math.Abs(ns) >= 1_000)         return $"{ns / 1_000.0:0.##} µs";
        return $"{ns} ns";
    }
}
