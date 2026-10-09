namespace Kmila.Shared.Models.Schematic;

/// <summary>What a canvas annotation card carries.</summary>
public enum SchematicAnnotationKind
{
    /// <summary>A free post-it: editable title + body + colour.</summary>
    Note,
    /// <summary>A pinned snapshot of a component's info panel (read-only content).</summary>
    Info,
}

/// <summary>
/// A user-placed card on the schematic canvas, stored in canvas (viewBox)
/// coordinates so it pans/zooms with the diagram and exports at the same spot.
/// Serialized to per-user localStorage (keyed by design) and rendered by the
/// shared <c>AnnotationSkins</c> on screen and in the exporter alike.
/// </summary>
public sealed class SchematicAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SchematicAnnotationKind Kind { get; set; } = SchematicAnnotationKind.Note;

    // Canvas-coordinate rectangle.
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 220;
    public double H { get; set; } = 140;

    /// <summary>Card background colour (hex, e.g. "#fde68a").</summary>
    public string Color { get; set; } = "#fde68a";

    public string Title { get; set; } = "";

    /// <summary>Free body text (Note kind).</summary>
    public string Body { get; set; } = "";

    // ── Info-kind snapshot (captured when pinning a component's info card) ──
    /// <summary>Key/value rows, each a two-element array [label, value].</summary>
    public List<string[]> Fields { get; set; } = new();
    public string? Summary { get; set; }
    public string? Description { get; set; }
    /// <summary>Truth table rows; row[0] is the header. Null when none.</summary>
    public List<string[]>? TruthTable { get; set; }
}
