namespace Kmila.Shared.Models;

/// <summary>
/// Per-cell colour triple. Body fills the gate's silhouette; stroke
/// is the outline; label is the operator text colour. Same triple
/// used on screen and in the SVG export so the two views match.
/// </summary>
public sealed record SchematicCellColours(string Body, string Stroke, string Label);

/// <summary>
/// Bundle of colour assignments produced by the on-screen viewer's
/// random hue sweep. Carried into <c>SchematicExporter</c> when the
/// user picks the Color export mode so the exported diagram uses
/// the same palette they're looking at — including the result of
/// the Shuffle button.
/// </summary>
/// <remarks>
/// Empty maps are allowed and treated as "fall through to the
/// monochrome defaults" — keeps the exporter resilient to a viewer
/// that hasn't generated colours yet (e.g. mid-render or after a
/// hard reload).
/// </remarks>
public sealed class SchematicColourPalette
{
    public Dictionary<string, SchematicCellColours> Cells { get; init; } = new();
    public Dictionary<string, string> Ports { get; init; } = new();
    public Dictionary<string, string> Wires { get; init; } = new();
}
