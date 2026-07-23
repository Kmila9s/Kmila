using Interpreter.Synthesis;
using Kmila.Shared.Models;

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
}
