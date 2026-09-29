using Interpreter.Synthesis;

namespace Kmila.Shared.Models.Schematic;

/// <summary>
/// Educational doc for a single <see cref="CellKind"/>. Rendered in
/// the schematic info panel when the user clicks a cell, modeled on
/// Monaco's hover docs — the reader gets a one-line summary plus a
/// few sentences of detail, an optional truth table for binary
/// gates, and a VHDL snippet showing the operator in source.
/// </summary>
public sealed record CellDoc(
    string Title,
    string Summary,
    string Description,
    IReadOnlyList<string[]>? TruthTable,
    string? VhdlExample,
    string? Note = null
);

/// <summary>
/// Static catalog of cell documentation keyed by <see cref="CellKind"/>.
///
/// The catalog now stores translation KEYS (e.g. <c>"GATE_AND_TITLE"</c>)
/// instead of literal English text — the consumer passes a translator
/// function (typically <c>Traductor.T</c>) when reading. The previous
/// hard-coded English strings were the reason every locale showed the
/// schematic info panel in English regardless of language preference.
///
/// Truth-table headers / cells stay as raw symbols ("a", "0", "y") since
/// they're universal across locales. VHDL snippets stay raw — they're
/// code, not prose.
/// </summary>
public static class GateDocumentation
{
    /// <summary>
    /// Returns a localized doc for <paramref name="kind"/> using
    /// <paramref name="t"/> as the translator. Returns null when the
    /// kind isn't documented.
    /// </summary>
    public static CellDoc? Get(CellKind kind, Func<string, string> t)
    {
        if (!_entries.TryGetValue(kind, out var e)) return null;
        return new CellDoc(
            Title: t(e.TitleKey),
            Summary: t(e.SummaryKey),
            Description: t(e.DescriptionKey),
            TruthTable: e.TruthTable,
            VhdlExample: e.VhdlExample,
            Note: string.IsNullOrEmpty(e.NoteKey) ? null : t(e.NoteKey!));
    }

    /// <summary>
    /// Convenience overload preserving the pre-i18n call sites that
    /// don't have a translator handy. Returns the un-translated keys
    /// straight through — keeps anything still on the old API working
    /// (just renders as raw <c>GATE_*</c> placeholders) while we migrate
    /// every consumer to the locale-aware variant.
    /// </summary>
    public static CellDoc? Get(CellKind kind) => Get(kind, k => k);

    /// <summary>
    /// Stored shape — translation keys + the pieces we never translate
    /// (truth tables, VHDL examples).
    /// </summary>
    private sealed record Entry(
        string TitleKey,
        string SummaryKey,
        string DescriptionKey,
        IReadOnlyList<string[]>? TruthTable,
        string? VhdlExample,
        string? NoteKey = null);

    private static readonly string[][] AndTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","0"},
        new[]{"0","1","0"},
        new[]{"1","0","0"},
        new[]{"1","1","1"},
    };

    private static readonly string[][] NandTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","1"},
        new[]{"0","1","1"},
        new[]{"1","0","1"},
        new[]{"1","1","0"},
    };

    private static readonly string[][] OrTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","0"},
        new[]{"0","1","1"},
        new[]{"1","0","1"},
        new[]{"1","1","1"},
    };

    private static readonly string[][] NorTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","1"},
        new[]{"0","1","0"},
        new[]{"1","0","0"},
        new[]{"1","1","0"},
    };

    private static readonly string[][] XorTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","0"},
        new[]{"0","1","1"},
        new[]{"1","0","1"},
        new[]{"1","1","0"},
    };

    private static readonly string[][] XnorTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","1"},
        new[]{"0","1","0"},
        new[]{"1","0","0"},
        new[]{"1","1","1"},
    };

    private static readonly string[][] NotTruthTable = new string[][]
    {
        new[]{"a","y"},
        new[]{"0","1"},
        new[]{"1","0"},
    };

    private static readonly string[][] BufTruthTable = new string[][]
    {
        new[]{"a","y"},
        new[]{"0","0"},
        new[]{"1","1"},
    };

    private static readonly string[][] MuxTruthTable = new string[][]
    {
        new[]{"s","d0","d1","y"},
        new[]{"0", "x","-","d0"},
        new[]{"1", "-","x","d1"},
    };

    private static readonly string[][] EqTruthTable = new string[][]
    {
        new[]{"a","b","y"},
        new[]{"0","0","1"},
        new[]{"0","1","0"},
        new[]{"1","0","0"},
        new[]{"1","1","1"},
    };

    private static readonly Dictionary<CellKind, Entry> _entries = new()
    {
        // ─── Combinational primitives ───────────────────────────

        [CellKind.AND] = new Entry(
            "GATE_AND_TITLE", "GATE_AND_SUMMARY", "GATE_AND_DESC",
            AndTruthTable,
            "y <= a and b;"
        ),
        [CellKind.NAND] = new Entry(
            "GATE_NAND_TITLE", "GATE_NAND_SUMMARY", "GATE_NAND_DESC",
            NandTruthTable,
            "y <= a nand b;"
        ),
        [CellKind.OR] = new Entry(
            "GATE_OR_TITLE", "GATE_OR_SUMMARY", "GATE_OR_DESC",
            OrTruthTable,
            "y <= a or b;"
        ),
        [CellKind.NOR] = new Entry(
            "GATE_NOR_TITLE", "GATE_NOR_SUMMARY", "GATE_NOR_DESC",
            NorTruthTable,
            "y <= a nor b;"
        ),
        [CellKind.XOR] = new Entry(
            "GATE_XOR_TITLE", "GATE_XOR_SUMMARY", "GATE_XOR_DESC",
            XorTruthTable,
            "y <= a xor b;"
        ),
        [CellKind.XNOR] = new Entry(
            "GATE_XNOR_TITLE", "GATE_XNOR_SUMMARY", "GATE_XNOR_DESC",
            XnorTruthTable,
            "y <= a xnor b;"
        ),
        [CellKind.NOT] = new Entry(
            "GATE_NOT_TITLE", "GATE_NOT_SUMMARY", "GATE_NOT_DESC",
            NotTruthTable,
            "y <= not a;"
        ),
        [CellKind.BUF] = new Entry(
            "GATE_BUF_TITLE", "GATE_BUF_SUMMARY", "GATE_BUF_DESC",
            BufTruthTable,
            "y <= a;",
            "GATE_BUF_NOTE"
        ),

        // ─── Storage / control ───────────────────────────────────

        [CellKind.MUX] = new Entry(
            "GATE_MUX_TITLE", "GATE_MUX_SUMMARY", "GATE_MUX_DESC",
            MuxTruthTable,
            "y <= d1 when s = '1' else d0;"
        ),
        [CellKind.DFF] = new Entry(
            "GATE_DFF_TITLE", "GATE_DFF_SUMMARY", "GATE_DFF_DESC",
            null,
            "process(clk)\nbegin\n  if rising_edge(clk) then\n    q <= d;\n  end if;\nend process;",
            "GATE_DFF_NOTE"
        ),
        [CellKind.CONST] = new Entry(
            "GATE_CONST_TITLE", "GATE_CONST_SUMMARY", "GATE_CONST_DESC",
            null,
            "y <= '1';"
        ),

        // ─── Arithmetic ──────────────────────────────────────────

        [CellKind.ADD] = new Entry(
            "GATE_ADD_TITLE", "GATE_ADD_SUMMARY", "GATE_ADD_DESC",
            null, "y <= a + b;"
        ),
        [CellKind.SUB] = new Entry(
            "GATE_SUB_TITLE", "GATE_SUB_SUMMARY", "GATE_SUB_DESC",
            null, "y <= a - b;"
        ),
        [CellKind.MUL] = new Entry(
            "GATE_MUL_TITLE", "GATE_MUL_SUMMARY", "GATE_MUL_DESC",
            null, "y <= a * b;"
        ),
        [CellKind.DIV] = new Entry(
            "GATE_DIV_TITLE", "GATE_DIV_SUMMARY", "GATE_DIV_DESC",
            null, "y <= a / b;"
        ),
        [CellKind.MOD] = new Entry(
            "GATE_MOD_TITLE", "GATE_MOD_SUMMARY", "GATE_MOD_DESC",
            null, "y <= a mod b;"
        ),
        [CellKind.POW] = new Entry(
            "GATE_POW_TITLE", "GATE_POW_SUMMARY", "GATE_POW_DESC",
            null, "y <= a ** 2;"
        ),

        // ─── Shifts / rotates ────────────────────────────────────

        [CellKind.SHL] = new Entry(
            "GATE_SHL_TITLE", "GATE_SHL_SUMMARY", "GATE_SHL_DESC",
            null, "y <= a sll 2;"
        ),
        [CellKind.SHR] = new Entry(
            "GATE_SHR_TITLE", "GATE_SHR_SUMMARY", "GATE_SHR_DESC",
            null, "y <= a srl 2;"
        ),
        [CellKind.ROL] = new Entry(
            "GATE_ROL_TITLE", "GATE_ROL_SUMMARY", "GATE_ROL_DESC",
            null, "y <= a rol 1;"
        ),
        [CellKind.ROR] = new Entry(
            "GATE_ROR_TITLE", "GATE_ROR_SUMMARY", "GATE_ROR_DESC",
            null, "y <= a ror 1;"
        ),

        // ─── Comparators ─────────────────────────────────────────

        [CellKind.EQ] = new Entry(
            "GATE_EQ_TITLE", "GATE_EQ_SUMMARY", "GATE_EQ_DESC",
            EqTruthTable,
            "y <= '1' when a = b else '0';"
        ),
        [CellKind.NEQ] = new Entry(
            "GATE_NEQ_TITLE", "GATE_NEQ_SUMMARY", "GATE_NEQ_DESC",
            null, "y <= '1' when a /= b else '0';"
        ),
        [CellKind.LT] = new Entry(
            "GATE_LT_TITLE", "GATE_LT_SUMMARY", "GATE_LT_DESC",
            null, "y <= '1' when a < b else '0';"
        ),
        [CellKind.LTE] = new Entry(
            "GATE_LTE_TITLE", "GATE_LTE_SUMMARY", "GATE_LTE_DESC",
            null, "y <= '1' when a <= b else '0';"
        ),
        [CellKind.GT] = new Entry(
            "GATE_GT_TITLE", "GATE_GT_SUMMARY", "GATE_GT_DESC",
            null, "y <= '1' when a > b else '0';"
        ),
        [CellKind.GTE] = new Entry(
            "GATE_GTE_TITLE", "GATE_GTE_SUMMARY", "GATE_GTE_DESC",
            null, "y <= '1' when a >= b else '0';"
        ),

        // ─── Fallback ────────────────────────────────────────────

        [CellKind.BLACKBOX_PROCESS] = new Entry(
            "GATE_BLACKBOX_TITLE", "GATE_BLACKBOX_SUMMARY", "GATE_BLACKBOX_DESC",
            null,
            "process(clk)\n  variable count : integer := 0;\nbegin\n  for i in 0 to 7 loop\n    count := count + 1;\n  end loop;\nend process;",
            "GATE_BLACKBOX_NOTE"
        ),
    };
}
