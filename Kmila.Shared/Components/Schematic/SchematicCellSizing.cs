using Interpreter.Synthesis;
using Interpreter.Synthesis.Layout;
// Disambiguate from Microsoft.Maui.Controls.Cell in the MAUI target build.
using Cell = Interpreter.Synthesis.Cell;

namespace Kmila.Shared.Components.Schematic;

/// <summary>
/// Per-cell box sizing shared by the on-screen <c>SchematicViewer</c>, the static
/// <c>SchematicExporter</c>, and the <c>kmila synth</c> CLI, so a cell is the same
/// size wherever it's drawn. These are the <c>sizeOf</c> functions handed to
/// <see cref="LayeredLayout.Layout"/>.
///
/// The one rule that matters: a cell whose label is drawn <em>inside</em> its box
/// (a component / black-box instance) must be wide enough to hold that label.
/// Logic gates keep the fixed glyph box — their labels are short ("and", "xor")
/// and the glyph art is drawn at fixed 70×50 coordinates, so they must not change.
/// </summary>
public static class SchematicCellSizing
{
    // Inner padding kept on each side of a boxed label.
    private const double GatePad = 14;   // gate-level label font ~11
    private const double BlockPad = 16;  // block-level title font ~13

    /// <summary>True for cells drawn as a titled box with the name inside it,
    /// which therefore must grow to fit that name.</summary>
    public static bool IsBoxCell(Cell c)
        => c.Kind is CellKind.BLACKBOX_COMPONENT or CellKind.BLACKBOX_PROCESS;

    /// <summary>The label drawn inside a cell — identical to what the viewer and
    /// exporter render (the <c>Op</c> attribute, else the kind's default tag).</summary>
    public static string LabelOf(Cell c)
        => c.Attrs != null && c.Attrs.TryGetValue("Op", out var op) && !string.IsNullOrEmpty(op)
            ? op.ToLowerInvariant()
            : GateGlyphs.Label(c.Kind);

    /// <summary>Gate-level (Grouped / Raw) sizing. Real gates keep the fixed
    /// glyph box so their art is unchanged bit-for-bit; only boxed instances grow
    /// to fit their name and port rows.</summary>
    public static (double Width, double Height) GateLevel(Cell c)
    {
        if (!IsBoxCell(c))
            return (GateGlyphs.Width, GateGlyphs.Height);
        double w = System.Math.Max(GateGlyphs.Width,
            BlockSkins.EstimateTextWidth(LabelOf(c), 11) + GatePad * 2);
        int rows = System.Math.Max(1, System.Math.Max(c.Inputs.Count, c.Outputs.Count));
        double h = System.Math.Max(GateGlyphs.Height, 22 + rows * 18);
        return (w, h);
    }

    /// <summary>Block-level (RTL / Nodos) sizing. Starts from the engine's
    /// port-count box (<see cref="BlockGraphBuilder.BlockSize"/>) and widens it so
    /// a long block title never overflows the card.</summary>
    public static (double Width, double Height) Block(Cell c)
    {
        var baseSize = BlockGraphBuilder.BlockSize(c);
        string title = c.Attrs != null && c.Attrs.TryGetValue(BlockGraphBuilder.LabelAttr, out var t) ? t : "";
        string sub = c.Attrs != null && c.Attrs.TryGetValue(BlockGraphBuilder.SubAttr, out var s) ? s : "";
        double textW = System.Math.Max(
            BlockSkins.EstimateTextWidth(title, 13),
            BlockSkins.EstimateTextWidth(sub, 10.5));
        double w = System.Math.Max(baseSize.Width, textW + BlockPad * 2);
        return (w, baseSize.Height);
    }
}
