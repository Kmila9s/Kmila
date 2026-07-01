using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Walks a kmila-blocks workspace serialization (the JSON produced by
/// <c>window.kmilaBlocklySerialize</c>) and emits canonical VHDL. Phase 0
/// vertical-slice covers:
///   • entity with port declarations (in/out/inout, std_logic)
///   • architecture with signal declarations + concurrent body
///   • process with sensitivity list
///   • if / elsif / else (sequential)
///   • sequential signal assignment (&lt;=)
///   • expression atoms: identifier, '0'/'1' literal, equality, rising_edge()
///
/// Unrecognized block types degrade gracefully — they emit a VHDL comment
/// flagging the gap rather than crashing. Reverse direction
/// (VHDL → block workspace) lives in a separate VhdlBlockBuilder (TBD).
/// </summary>
/// <remarks>
/// Example input (counter): top-level array contains one entity + one
/// architecture. The entity has 3 ports chained off PORTS; the
/// architecture has one signal_decl under DECLS and one process under
/// BODY. The process body holds an if with `rst = '1'` as COND, an
/// assign in THEN_BODY, and a nested if in ELSE_BODY whose COND is
/// `rising_edge(clk)` — the emitter flattens this nested-if into a
/// proper VHDL `elsif … then` chain.
///
/// The emitter is deliberately tolerant: missing slots emit a comment
/// placeholder, the JSON walk uses try-properties throughout, and a
/// malformed top-level returns the empty string (same contract as
/// <see cref="BlockProgram.Fingerprint"/>).
/// </remarks>
public class VhdlBlockEmitter
{
    private readonly ILogger _logger;

    /// <summary>
    /// Maps 1-based VHDL source line numbers to the block id that
    /// produced the line, populated by the most recent <see cref="Emit"/>
    /// call. Used by the Editor's Step 7 (#16) error-highlighting path:
    /// when a simulator diagnostic points at "line N" of the emitted
    /// source, the matching block gets a kmila-block--error decoration.
    /// </summary>
    public Dictionary<int, string> LastBlockLineMap { get; private set; } = new();

    private int _currentLine;

    public VhdlBlockEmitter(ILogger<VhdlBlockEmitter>? logger = null)
    {
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Bind the upcoming line to the block id carried by <paramref name="node"/>.
    /// Idempotent — if the same line gets two block ids (rare, but
    /// possible for back-to-back single-line blocks), the first wins.
    /// </summary>
    private void Track(JsonElement node)
    {
        if (node.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String)
        {
            var s = id.GetString();
            if (!string.IsNullOrEmpty(s) && !LastBlockLineMap.ContainsKey(_currentLine))
            {
                LastBlockLineMap[_currentLine] = s;
            }
        }
    }

    /// <summary>Append-and-count: every <c>AppendLine</c> bumps the line cursor.</summary>
    private void Line(StringBuilder sb, string text = "")
    {
        sb.AppendLine(text);
        _currentLine++;
    }

    /// <summary>
    /// Palette layout the Editor's Blocks tab feeds into kmila-blocks.
    /// Two sections separated by <c>{"kind":"sep"}</c>: structural blocks
    /// (entity / arch / port / signal / process / if / assign) and
    /// expression atoms (id / literal '0' '1' / equality / rising_edge).
    /// Mirrors the catalog registered in kmila-blocks-vhdl.js — block
    /// types missing from the engine registry are silently skipped by
    /// the palette renderer, so adding new constructs only requires
    /// updating both files in lockstep.
    /// </summary>
    public const string ToolboxJson = """
        {
          "kind": "categoryToolbox",
          "contents": [
            { "kind": "block", "type": "kmila_vhdl_entity" },
            { "kind": "block", "type": "kmila_vhdl_architecture" },
            { "kind": "block", "type": "kmila_vhdl_port" },
            { "kind": "block", "type": "kmila_vhdl_signal_decl" },
            { "kind": "block", "type": "kmila_vhdl_assign_concurrent" },
            { "kind": "block", "type": "kmila_vhdl_process" },
            { "kind": "block", "type": "kmila_vhdl_if" },
            { "kind": "block", "type": "kmila_vhdl_assign_seq" },
            { "kind": "block", "type": "kmila_vhdl_case" },
            { "kind": "sep" },
            { "kind": "block", "type": "kmila_vhdl_id" },
            { "kind": "block", "type": "kmila_vhdl_literal_bit" },
            { "kind": "block", "type": "kmila_vhdl_literal_vec" },
            { "kind": "block", "type": "kmila_vhdl_eq" },
            { "kind": "block", "type": "kmila_vhdl_binop" },
            { "kind": "block", "type": "kmila_vhdl_rising_edge" },
            { "kind": "block", "type": "kmila_vhdl_falling_edge" },
            { "kind": "block", "type": "kmila_vhdl_not" }
          ]
        }
        """;

    /// <summary>
    /// Emit VHDL source for the given workspace JSON. Returns the empty
    /// string when the workspace is empty or the JSON is malformed.
    /// </summary>
    public string Emit(string? workspaceJson)
    {
        LastBlockLineMap = new Dictionary<int, string>();
        _currentLine = 1;
        if (string.IsNullOrWhiteSpace(workspaceJson)) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(workspaceJson);
            if (!doc.RootElement.TryGetProperty("blocks", out var b1)) return string.Empty;
            if (!b1.TryGetProperty("blocks", out var topArray)) return string.Empty;
            if (topArray.ValueKind != JsonValueKind.Array || topArray.GetArrayLength() == 0)
                return string.Empty;

            var sb = new StringBuilder();
            Line(sb, "library ieee;");
            Line(sb, "use ieee.std_logic_1164.all;");
            Line(sb);

            bool first = true;
            foreach (var node in topArray.EnumerateArray())
            {
                if (!first) Line(sb);
                first = false;
                EmitTopLevel(sb, node);
            }
            return sb.ToString().TrimEnd() + "\n";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VhdlBlockEmitter] Emit failed");
            return string.Empty;
        }
    }

    /* ── Top level ─────────────────────────────────────────────────── */

    private void EmitTopLevel(StringBuilder sb, JsonElement node)
    {
        var type = GetType(node);
        switch (type)
        {
            case "kmila_vhdl_entity":       EmitEntity(sb, node);       break;
            case "kmila_vhdl_architecture": EmitArchitecture(sb, node); break;
            case "kmila_vhdl_raw_top":      EmitRaw(sb, node, indent: 0); break;
            default:
                sb.Append("-- unsupported top-level block: ");
                Line(sb, type ?? "?");
                break;
        }
    }

    private void EmitEntity(StringBuilder sb, JsonElement node)
    {
        Track(node);
        var name = GetField(node, "NAME") ?? "anon";
        sb.Append("entity ").Append(name);
        Line(sb, " is");

        var ports = GetStatementChain(node, "PORTS").ToList();
        if (ports.Count > 0)
        {
            Line(sb, "    port (");
            for (int i = 0; i < ports.Count; i++)
            {
                Track(ports[i]);
                sb.Append("        ");
                EmitPort(sb, ports[i]);
                Line(sb, i == ports.Count - 1 ? "" : ";");
            }
            Line(sb, "    );");
        }
        sb.Append("end entity ").Append(name);
        Line(sb, ";");
    }

    private void EmitPort(StringBuilder sb, JsonElement node)
    {
        var name = GetField(node, "NAME") ?? "anon";
        var dir  = GetField(node, "DIR")  ?? "in";
        var type = GetField(node, "TYPE") ?? "std_logic";
        sb.Append(name).Append(" : ").Append(dir).Append(' ').Append(type);
    }

    private void EmitArchitecture(StringBuilder sb, JsonElement node)
    {
        Track(node);
        var name = GetField(node, "NAME") ?? "rtl";
        var of   = GetField(node, "OF")   ?? "anon";
        sb.Append("architecture ").Append(name).Append(" of ").Append(of);
        Line(sb, " is");

        foreach (var decl in GetStatementChain(node, "DECLS"))
        {
            Track(decl);
            sb.Append("    ");
            EmitDecl(sb, decl);
        }
        Line(sb, "begin");

        foreach (var stmt in GetStatementChain(node, "BODY"))
        {
            EmitConcurrent(sb, stmt, indent: 1);
        }

        sb.Append("end architecture ").Append(name);
        Line(sb, ";");
    }

    /* ── Declarations ──────────────────────────────────────────────── */

    private void EmitDecl(StringBuilder sb, JsonElement node)
    {
        var type = GetType(node);
        if (type == "kmila_vhdl_signal_decl")
        {
            var name = GetField(node, "NAME") ?? "anon";
            var dt   = GetField(node, "TYPE") ?? "std_logic";
            sb.Append("signal ").Append(name).Append(" : ").Append(dt);
            Line(sb, ";");
        }
        else
        {
            sb.Append("-- unsupported declaration block: ");
            Line(sb, type ?? "?");
        }
    }

    /* ── Concurrent statements (in architecture BODY) ──────────────── */

    private void EmitConcurrent(StringBuilder sb, JsonElement node, int indent)
    {
        var pad  = new string(' ', indent * 4);
        var type = GetType(node);
        if (type == "kmila_vhdl_process")
        {
            Track(node);
            var sens = GetField(node, "SENS") ?? "";
            sb.Append(pad).Append("process");
            if (!string.IsNullOrWhiteSpace(sens))
                sb.Append(" (").Append(sens.Trim()).Append(')');
            Line(sb, " is");
            sb.Append(pad);
            Line(sb, "begin");
            foreach (var stmt in GetStatementChain(node, "BODY"))
            {
                EmitSequential(sb, stmt, indent + 1);
            }
            sb.Append(pad);
            Line(sb, "end process;");
        }
        else if (type == "kmila_vhdl_assign_concurrent")
        {
            Track(node);
            var target = GetField(node, "TARGET") ?? "anon";
            var value  = GetValueInput(node, "VALUE");
            sb.Append(pad).Append(target).Append(" <= ").Append(EmitExpr(value));
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_raw_concurrent")
        {
            EmitRaw(sb, node, indent);
        }
        else
        {
            sb.Append(pad).Append("-- unsupported concurrent block: ");
            Line(sb, type ?? "?");
        }
    }

    /* ── Sequential statements (in process BODY / if branches) ─────── */

    private void EmitSequential(StringBuilder sb, JsonElement node, int indent)
    {
        var pad  = new string(' ', indent * 4);
        var type = GetType(node);
        switch (type)
        {
            case "kmila_vhdl_assign_seq":
                {
                    Track(node);
                    var target = GetField(node, "TARGET") ?? "anon";
                    var value  = GetValueInput(node, "VALUE");
                    sb.Append(pad).Append(target).Append(" <= ").Append(EmitExpr(value));
                    Line(sb, ";");
                    break;
                }
            case "kmila_vhdl_if":
                EmitIfChain(sb, node, indent);
                break;
            // F-4 (2026-06-28) — case / when emission.
            case "kmila_vhdl_case":
                EmitCase(sb, node, indent);
                break;
            case "kmila_vhdl_raw_seq":
                EmitRaw(sb, node, indent);
                break;
            default:
                sb.Append(pad).Append("-- unsupported sequential block: ");
                Line(sb, type ?? "?");
                break;
        }
    }

    // F-4 (2026-06-28) — emit `case <subj> is when … => … end case;`. Each
    // ALT in the ALTS chain is either a kmila_vhdl_case_when (with CHOICE
    // text field + BODY) or kmila_vhdl_case_others (BODY only, no CHOICE).
    private void EmitCase(StringBuilder sb, JsonElement node, int indent)
    {
        Track(node);
        var pad  = new string(' ', indent * 4);
        var pad2 = new string(' ', (indent + 1) * 4);
        var subj = GetValueInput(node, "SUBJECT");
        sb.Append(pad).Append("case ").Append(EmitExpr(subj));
        Line(sb, " is");
        foreach (var alt in GetStatementChain(node, "ALTS"))
        {
            Track(alt);
            var altType = GetType(alt);
            if (altType == "kmila_vhdl_case_when")
            {
                var choice = GetField(alt, "CHOICE") ?? "?";
                sb.Append(pad2).Append("when ").Append(choice).Append(" =>");
                Line(sb);
                foreach (var s in GetStatementChain(alt, "BODY"))
                    EmitSequential(sb, s, indent + 2);
            }
            else if (altType == "kmila_vhdl_case_others")
            {
                sb.Append(pad2);
                Line(sb, "when others =>");
                foreach (var s in GetStatementChain(alt, "BODY"))
                    EmitSequential(sb, s, indent + 2);
            }
        }
        sb.Append(pad);
        Line(sb, "end case;");
    }

    private void EmitRaw(StringBuilder sb, JsonElement node, int indent)
    {
        Track(node);
        var text = GetField(node, "TEXT") ?? string.Empty;
        var pad  = new string(' ', indent * 4);
        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        foreach (var line in lines)
        {
            if (line.Length == 0) Line(sb);
            else
            {
                sb.Append(pad);
                Line(sb, line);
            }
        }
    }

    /// <summary>
    /// Emit a full if / elsif* / else? / end if chain. Detects the common
    /// pattern where ELSE_BODY contains exactly one if block (with no
    /// next sibling) and flattens it into <c>elsif … then</c> instead of
    /// the literal <c>else if … end if; end if;</c> nesting the block
    /// representation actually carries.
    /// </summary>
    private void EmitIfChain(StringBuilder sb, JsonElement node, int indent)
    {
        var pad = new string(' ', indent * 4);
        var cursor = node;

        // Opening branch.
        Track(cursor);
        var cond = GetValueInput(cursor, "COND");
        sb.Append(pad).Append("if ").Append(EmitExpr(cond));
        Line(sb, " then");
        foreach (var stmt in GetStatementChain(cursor, "THEN_BODY"))
            EmitSequential(sb, stmt, indent + 1);

        // Walk down nested elses, promoting solitary-if children to elsif.
        while (true)
        {
            var elseStmts = GetStatementChain(cursor, "ELSE_BODY").ToList();
            if (elseStmts.Count == 1
                && GetType(elseStmts[0]) == "kmila_vhdl_if"
                && !HasNext(elseStmts[0]))
            {
                cursor = elseStmts[0];
                Track(cursor);
                var ec = GetValueInput(cursor, "COND");
                sb.Append(pad).Append("elsif ").Append(EmitExpr(ec));
                Line(sb, " then");
                foreach (var stmt in GetStatementChain(cursor, "THEN_BODY"))
                    EmitSequential(sb, stmt, indent + 1);
                continue;
            }

            if (elseStmts.Count > 0)
            {
                sb.Append(pad);
                Line(sb, "else");
                foreach (var stmt in elseStmts)
                    EmitSequential(sb, stmt, indent + 1);
            }
            break;
        }

        sb.Append(pad);
        Line(sb, "end if;");
    }

    /* ── Expressions ───────────────────────────────────────────────── */

    private string EmitExpr(JsonElement? node)
    {
        if (node == null) return "/* missing */";
        var n = node.Value;
        var type = GetType(n);
        switch (type)
        {
            case "kmila_vhdl_id":
                return GetField(n, "NAME") ?? "/*id*/";
            case "kmila_vhdl_literal_bit":
                return "'" + (GetField(n, "BIT") ?? "0") + "'";
            case "kmila_vhdl_eq":
                {
                    var l = GetValueInput(n, "LHS");
                    var r = GetValueInput(n, "RHS");
                    return EmitExpr(l) + " = " + EmitExpr(r);
                }
            case "kmila_vhdl_rising_edge":
                {
                    var s = GetValueInput(n, "SIG");
                    return "rising_edge(" + EmitExpr(s) + ")";
                }
            // F-7 (2026-06-28)
            case "kmila_vhdl_falling_edge":
                {
                    var s = GetValueInput(n, "SIG");
                    return "falling_edge(" + EmitExpr(s) + ")";
                }
            case "kmila_vhdl_not":
                {
                    var s = GetValueInput(n, "OPERAND");
                    return "not " + EmitExpr(s);
                }
            // F-2 (2026-06-28) — binary boolean op. The OP field holds the
            // VHDL keyword (and/or/xor/nand/nor/xnor) verbatim.
            case "kmila_vhdl_binop":
                {
                    var l  = GetValueInput(n, "LHS");
                    var r  = GetValueInput(n, "RHS");
                    var op = (GetField(n, "OP") ?? "and").ToLowerInvariant();
                    return EmitExpr(l) + " " + op + " " + EmitExpr(r);
                }
            // F-5 (2026-06-28) — vector / bit-string literal. TEXT carries
            // the literal including its surrounding quotes (`"00"`, `X"FF"`).
            case "kmila_vhdl_literal_vec":
                return GetField(n, "TEXT") ?? "\"\"";
            case "kmila_vhdl_raw_expr":
                return GetField(n, "TEXT") ?? string.Empty;
            default:
                return "/* unsupported expr: " + (type ?? "?") + " */";
        }
    }

    /* ── JSON helpers ─────────────────────────────────────────────── */

    private static string? GetType(JsonElement node)
    {
        return node.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString()
            : null;
    }

    private static string? GetField(JsonElement node, string name)
    {
        if (!node.TryGetProperty("fields", out var fields)) return null;
        if (fields.ValueKind != JsonValueKind.Object) return null;
        if (!fields.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.True   => "true",
            JsonValueKind.False  => "false",
            _                    => null
        };
    }

    private static IEnumerable<JsonElement> GetStatementChain(JsonElement parent, string slotName)
    {
        if (!parent.TryGetProperty("inputs", out var inputs)) yield break;
        if (inputs.ValueKind != JsonValueKind.Object) yield break;
        if (!inputs.TryGetProperty(slotName, out var slot)) yield break;
        if (!slot.TryGetProperty("block", out var head)) yield break;

        var cur = head;
        while (true)
        {
            yield return cur;
            if (!cur.TryGetProperty("next", out var n)) yield break;
            if (!n.TryGetProperty("block", out var next)) yield break;
            cur = next;
        }
    }

    private static JsonElement? GetValueInput(JsonElement parent, string slotName)
    {
        if (!parent.TryGetProperty("inputs", out var inputs)) return null;
        if (inputs.ValueKind != JsonValueKind.Object) return null;
        if (!inputs.TryGetProperty(slotName, out var slot)) return null;
        if (!slot.TryGetProperty("block", out var inner)) return null;
        return inner;
    }

    private static bool HasNext(JsonElement node)
    {
        return node.TryGetProperty("next", out var n)
            && n.TryGetProperty("block", out _);
    }
}
