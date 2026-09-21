using Interpreter.Synthesis;

namespace Kmila.Shared.Components.Schematic;

/// <summary>
/// Inline-SVG path strings for each <see cref="CellKind"/>. All glyphs
/// are drawn into a 70×50 box with the input pins on the left edge
/// (x=0) and the output pin on the right edge (x=70). Picked these
/// shapes to match what's in every digital-logic textbook so a student
/// reading the schematic doesn't need a legend.
/// </summary>
public static class GateGlyphs
{
    public const double Width = 70;
    public const double Height = 50;

    /// <summary>
    /// Returns one or more SVG <c>path d=""</c> bodies for the cell kind.
    /// Multiple paths are returned for kinds that have a small bubble or
    /// inversion mark (NAND, NOR, NOT, XNOR) — the renderer draws them
    /// one after another with a shared transform.
    /// </summary>
    public static IReadOnlyList<string> Paths(CellKind kind) => kind switch
    {
        CellKind.AND => new[] { "M 5 5 L 35 5 A 20 20 0 0 1 35 45 L 5 45 Z" },
        CellKind.NAND => new[] {
            "M 5 5 L 30 5 A 20 20 0 0 1 30 45 L 5 45 Z",
            "M 50 25 a 4 4 0 1 0 8 0 a 4 4 0 1 0 -8 0",
            "M 58 25 L 70 25",
        },
        CellKind.OR => new[] {
            "M 5 5 Q 25 5 50 25 Q 25 45 5 45 Q 18 25 5 5 Z",
        },
        CellKind.NOR => new[] {
            "M 5 5 Q 25 5 45 25 Q 25 45 5 45 Q 18 25 5 5 Z",
            "M 50 25 a 4 4 0 1 0 8 0 a 4 4 0 1 0 -8 0",
            "M 58 25 L 70 25",
        },
        CellKind.XOR => new[] {
            "M 0 5 Q 13 25 0 45",
            "M 8 5 Q 28 5 53 25 Q 28 45 8 45 Q 21 25 8 5 Z",
        },
        CellKind.XNOR => new[] {
            "M 0 5 Q 13 25 0 45",
            "M 8 5 Q 25 5 48 25 Q 25 45 8 45 Q 21 25 8 5 Z",
            "M 53 25 a 4 4 0 1 0 8 0 a 4 4 0 1 0 -8 0",
            "M 61 25 L 70 25",
        },
        CellKind.NOT => new[] {
            "M 5 5 L 50 25 L 5 45 Z",
            "M 50 25 a 4 4 0 1 0 8 0 a 4 4 0 1 0 -8 0",
            "M 58 25 L 70 25",
        },
        CellKind.BUF => new[] { "M 5 5 L 60 25 L 5 45 Z" },
        // MUX — the textbook trapezoid (wide input edge, narrow output
        // edge). Applies to 2:1 muxes and the N-way muxes the case-collapse
        // pass emits, so every selector reads as a mux at a glance.
        CellKind.MUX => new[] { "M 8 5 L 60 19 L 60 31 L 8 45 Z" },
        // ROM_LOOKUP — a memory block: the generic rectangle plus a divider
        // near the address (left) edge, the common lookup-table convention,
        // so the collapsed decoders stand apart from ordinary modules.
        CellKind.ROM_LOOKUP => new[] { "M 2 5 L 68 5 L 68 45 L 2 45 Z", "M 15 5 L 15 45" },
        // Rectangular blocks for everything else — opaque modules, kept
        // visually distinct from gate-shaped primitives so the user can
        // see at a glance "this is not a single gate".
        _ => new[] { "M 2 5 L 68 5 L 68 45 L 2 45 Z" },
    };

    /// <summary>
    /// Short label stamped inside every cell. For gate-shaped primitives
    /// the body is wide enough to fit the operator name (and/or/xor…)
    /// readable at the default zoom — being explicit about the gate's
    /// function beats relying on the silhouette alone, which is hard
    /// to distinguish on a busy diagram.
    /// </summary>
    public static string Label(CellKind kind) => kind switch
    {
        CellKind.AND => "and", CellKind.NAND => "nand",
        CellKind.OR  => "or",  CellKind.NOR  => "nor",
        CellKind.XOR => "xor", CellKind.XNOR => "xnor",
        CellKind.NOT => "not", CellKind.BUF  => "buf",
        CellKind.MUX => "mux",
        CellKind.DFF => "dff",
        CellKind.CONST => "const",
        CellKind.ADD => "+", CellKind.SUB => "−", CellKind.MUL => "×",
        CellKind.DIV => "÷", CellKind.MOD => "mod", CellKind.POW => "**",
        CellKind.SHL => "≪", CellKind.SHR => "≫",
        CellKind.ROL => "rol", CellKind.ROR => "ror",
        CellKind.EQ => "=", CellKind.NEQ => "≠",
        CellKind.LT => "<", CellKind.LTE => "≤",
        CellKind.GT => ">", CellKind.GTE => "≥",
        CellKind.BLACKBOX_PROCESS => "process",
        // Blackbox component ships with its entity name in `Attrs["Op"]`,
        // so `Label(kind)` is only reached when that attr is empty —
        // fall back to the generic "component" tag rather than the
        // verbose enum name.
        CellKind.BLACKBOX_COMPONENT => "component",
        // Signal-attribute cells emitted by the DeferredAttribute walker
        // (pass 12). Label uses the VHDL attribute-tick syntax so the
        // student reads the cell as the same construct they wrote.
        CellKind.LAST_VALUE => "'last",
        CellKind.STABLE     => "'stable",
        CellKind.EDGE_DETECT => "edge",
        CellKind.TO_INTEGER  => "→int",
        CellKind.TO_VECTOR   => "→vec",
        CellKind.RESIZE      => "resize",
        CellKind.ROM_LOOKUP  => "rom",
        CellKind.RAM_READ    => "ram",
        CellKind.RAM_WRITE   => "ram_wr",
        _ => kind.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// X position (in glyph-local coordinates) where a label should sit
    /// to be visually centred inside the body of the gate, accounting
    /// for the bubble that NAND/NOR/XNOR/NOT extend past their body.
    /// </summary>
    public static double LabelX(CellKind kind) => kind switch
    {
        CellKind.AND or CellKind.NAND => 22,
        CellKind.OR or CellKind.NOR or CellKind.XOR or CellKind.XNOR => 24,
        CellKind.NOT => 22,
        CellKind.BUF => 25,
        _ => Width / 2,
    };
}
