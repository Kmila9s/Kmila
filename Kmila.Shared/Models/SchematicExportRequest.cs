namespace Kmila.Shared.Models;

/// <summary>
/// Output format for the schematic export. SVG keeps the diagram
/// vector-perfect for embedding in slides / docs; PNG goes through
/// the same canvas-rasterising JS helper the waveform export uses,
/// at the chosen scale.
/// </summary>
public enum SchematicExportFormat
{
    Svg,
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
    /// <see cref="ByKind"/>.</summary>
    Kind,
}

/// <summary>
/// Validated request submitted from the export modal back to the
/// SchematicViewer. The viewer turns this into a SelectionFilter +
/// renders + downloads.
/// </summary>
public class SchematicExportRequest
{
    public SchematicExportFormat Format { get; set; } = SchematicExportFormat.Svg;
    public int PngScale { get; set; } = 2;
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

    /// <summary>Render simplification mode. Human = education-friendly
    /// (bigger padding + fonts, muted internal names hidden, thicker
    /// buses, planned-for-v2 structural collapse). Raw = every glyph
    /// and label the netlist produces. Human is the default so the
    /// diagram opens looking readable; Raw is one click away for the
    /// full picture.</summary>
    public SchematicRenderMode Mode { get; set; } = SchematicRenderMode.Human;
}

/// <summary>
/// How much simplification the schematic renderer applies before
/// emission. Ships in v1 as two modes; a future version may expose a
/// slider for intermediate levels of detail.
/// </summary>
public enum SchematicRenderMode
{
    /// <summary>Everything the netlist produces, per-instance, per-bit.
    /// Same output the viewer + exporter shipped before this mode was
    /// introduced.</summary>
    Raw,

    /// <summary>Education-friendly view: bigger padding + fonts, muted
    /// internal net names suppressed, thicker bus strokes. Structural
    /// simplification passes (bus collapse, register consolidation,
    /// buffer elision) are planned for v2 and live in
    /// <see cref="Kmila.Shared.Services.SchematicSimplifier"/>.</summary>
    Human,
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
    Monochrome,
    Color,
}
