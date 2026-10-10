namespace Kmila.Shared.Models;

/// <summary>
/// Output format for the schematic export. SVG keeps the diagram
/// vector-perfect for embedding in slides / docs; PNG goes through
/// the same canvas-rasterising JS helper the waveform export uses,
/// at the chosen scale.
/// </summary>
public enum SchematicExportFormat
{
    /// <summary>Vector SVG output.</summary>
    Svg,
    /// <summary>Rasterised PNG output.</summary>
    Png,
}

/// <summary>
/// What to include in the exported diagram. The renderer reads this
/// to compute the visible cell/net/port set.
/// </summary>
/// <remarks>
/// The picker is intentionally explicit so the modal can show one
/// radio per option — the user said "full personalization", and
/// hiding choices behind a "scope" enum that we then re-interpret
/// would be the wrong kind of clever.
/// </remarks>
public enum SchematicExportScope
{
    /// <summary>Whole entity — every gate, wire, and port.</summary>
    Full,

    /// <summary>Only the cells of the currently-focused source-line
    /// group (Naive view). Disabled in the modal when no Naive focus
    /// is active.</summary>
    NaiveBlock,

    /// <summary>Whatever the user manually checked into the basket
    /// (Select-mode + Ctrl/Cmd clicks). Disabled when the basket is
    /// empty.</summary>
    Selection,

    /// <summary>Every input port + their first hop into the gates
    /// they drive — useful for "show me the input fan-out".</summary>
    Inputs,

    /// <summary>Every output port + the last gate driving each one.</summary>
    Outputs,

    /// <summary>Internal nets (signals + auto-named intermediate
    /// nets) and the cells that drive/consume them — the "guts"
    /// minus the entity's external interface.</summary>
    MidTerms,

    /// <summary>All cells of one specific kind (e.g. every AND gate)
    /// + their connecting wires. The kind comes from
    /// <see cref="SchematicExportRequest.ByKind"/>.</summary>
    Kind,
}

/// <summary>
/// Validated request submitted from the export modal back to the
/// SchematicViewer. The viewer turns this into a SelectionFilter +
/// renders + downloads.
/// </summary>
public class SchematicExportRequest
{
    /// <summary>Output file format for the export.</summary>
    public SchematicExportFormat Format { get; set; } = SchematicExportFormat.Svg;
    /// <summary>Rasterisation scale factor applied when exporting as PNG.</summary>
    public int PngScale { get; set; } = 2;
    /// <summary>Which elements of the diagram to include.</summary>
    public SchematicExportScope Scope { get; set; } = SchematicExportScope.Full;

    /// <summary>Cell-kind filter, used when <see cref="Scope"/> is
    /// <see cref="SchematicExportScope.Kind"/>. Stored as the cell
    /// kind's enum name (case-sensitive).</summary>
    public string? ByKind { get; set; }

    /// <summary>When true, picking a cell auto-includes its
    /// connected wires (and vice versa) one hop out, so a "selection"
    /// export doesn't render dangling pins. Off → strict include
    /// only what the user picked.</summary>
    public bool AutoIncludeEndpoints { get; set; } = true;

    /// <summary>When true, the canvas is trimmed to the bounding box
    /// of the visible elements. Off → keep the full canvas (matches
    /// what the user sees on screen, including empty regions).</summary>
    public bool TrimToBounds { get; set; } = true;

    /// <summary>Background colour of the exported SVG. Light mode
    /// pastes onto white-page documents; transparent suits dark
    /// presentation slides.</summary>
    public SchematicExportBackground Background { get; set; } = SchematicExportBackground.White;

    /// <summary>When true, the title bar at the top of the SVG
    /// shows the entity name + scope description. Off for a pure
    /// diagram with no chrome.</summary>
    public bool IncludeTitle { get; set; } = true;

    /// <summary>Mono = grayscale (print-friendly); Color = mirrors
    /// the on-screen palette including the result of any Shuffle
    /// button click since the viewer last rebuilt.</summary>
    public SchematicExportColorMode ColorMode { get; set; } = SchematicExportColorMode.Color;

    /// <summary>Render style. Chooses how the same netlist is drawn —
    /// see <see cref="SchematicRenderMode"/>. <c>Grouped</c> is the default
    /// so the diagram opens looking readable; <c>Raw</c> is one pick away
    /// for the full per-glyph picture.</summary>
    public SchematicRenderMode Mode { get; set; } = SchematicRenderMode.Grouped;
}

/// <summary>
/// Render style for the schematic — how the same synthesized netlist is
/// presented. <see cref="Raw"/> keeps the gate-level diagram exactly as it
/// has always been. The three "human" styles make the schematic readable in
/// different ways: <see cref="Grouped"/> stays gate-level but wraps each VHDL
/// statement in a titled container; <see cref="Rtl"/> and <see cref="Nodes"/>
/// collapse gates into named functional blocks (built by
/// <see cref="Interpreter.Synthesis.Layout.BlockGraphBuilder"/>) and differ
/// only in skin (datapath blocks vs node-editor cards).
/// </summary>
public enum SchematicRenderMode
{
    /// <summary>Every glyph and label the netlist produces, per-instance,
    /// per-bit. The gate-level view the viewer + exporter always shipped —
    /// unchanged.</summary>
    Raw,

    /// <summary>Gate-level, but each VHDL statement / process is wrapped in a
    /// titled, translucent container and drawn with human-sized labels. Keeps
    /// every cell (the full cell count).</summary>
    Grouped,

    /// <summary>Block-level datapath: gates are merged into named functional
    /// blocks (Registro, Sumador, MUX, Comparador, ROM…) drawn as rectangles
    /// with a glyph, a name, and bus arrows carrying bit-widths.</summary>
    Rtl,

    /// <summary>Block-level node-editor: the same merged blocks drawn as
    /// rounded cards with a titled header, typed input/output ports, and
    /// smooth bezier links colour-coded per net.</summary>
    Nodes,
}

/// <summary>Classification helpers for <see cref="SchematicRenderMode"/>.</summary>
public static class SchematicRenderModes
{
    /// <summary>Block-level styles (Rtl, Nodes) collapse gates into named
    /// functional blocks via <c>BlockGraphBuilder</c>; the gate-level styles
    /// (Raw, Grouped) keep every cell.</summary>
    public static bool IsBlock(this SchematicRenderMode m)
        => m is SchematicRenderMode.Rtl or SchematicRenderMode.Nodes;

    /// <summary>The three education-friendly styles. Raw is the odd one out:
    /// it keeps tiny internal net labels and the original tighter typography.</summary>
    public static bool IsHuman(this SchematicRenderMode m)
        => m is not SchematicRenderMode.Raw;
}

/// <summary>
/// Background fill applied to the exported SVG/PNG canvas. <c>Transparent</c>
/// is the right pick for slides; <c>White</c> for print; <c>Dark</c> mirrors
/// the dark-mode app theme.
/// </summary>
public enum SchematicExportBackground
{
    /// <summary>Solid white, ideal for print and reports.</summary>
    White,
    /// <summary>No background; lets the destination canvas show through.</summary>
    Transparent,
    /// <summary>Solid dark, matching the dark-theme app surface.</summary>
    Dark,
}

/// <summary>
/// Whether the SVG should preserve the on-screen colour palette
/// (per-cell-kind hues, port hues, wire colours) or emit a flat
/// grayscale diagram. Monochrome is the default for printable docs;
/// colour matches the live view including any palette shuffles.
/// </summary>
public enum SchematicExportColorMode
{
    /// <summary>Flat grayscale output, ideal for print.</summary>
    Monochrome,
    /// <summary>Preserves the on-screen colour palette.</summary>
    Color,
}
