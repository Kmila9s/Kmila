using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Walks a kmila-blocks workspace serialization (the JSON produced by
/// <c>window.kmilaBlocklySerialize</c>) and emits canonical VHDL.
///
/// Post the 2026-07-19 "1:1 audit" + Phase 19 (see
/// <c>Documentacion/HANDOFF_block_editor_1to1_2026-07-19.md</c>) the
/// emitter covers ~70 block types across five dispatches:
/// <list type="bullet">
/// <item><see cref="EmitTopLevel"/>: entity/architecture/library/use/package/package_body + comment_top + raw_top.</item>
/// <item><see cref="EmitDecl"/>: signal_decl/constant/subtype/alias/type_enum/type_array/type_record/variable_decl/attribute_decl/attribute_spec + <b>component_decl/procedure_decl/function_decl (Phase 19)</b> + comment_decl + raw_decl.</item>
/// <item><see cref="EmitConcurrent"/>: process(with label)/assign_concurrent/cond_assign/component_inst/generate_for/generate_if/with_select + comment_concurrent + raw_concurrent.</item>
/// <item><see cref="EmitSequential"/>: assign_seq/variable_assign/if-chain/case/loops/wait/exit/next/return/null/report/assert + <b>procedure_call (Phase 19)</b> + comment_seq + raw_seq.</item>
/// <item><see cref="EmitExpr"/>: id/literal_bit/literal_vec/eq/binop/rising_edge/falling_edge/not(with precedence parens)/slice/index/call/aggregate/others + raw_expr.</item>
/// </list>
///
/// Unrecognized block types degrade gracefully — they emit a VHDL
/// comment flagging the gap rather than crashing. Reverse direction
/// (VHDL → block workspace) lives in <see cref="VhdlBlockBuilder"/>.
/// </summary>
/// <remarks>
/// The emitter is deliberately tolerant: missing slots emit a comment
/// placeholder, the JSON walk uses try-properties throughout, and a
/// malformed top-level returns the empty string.
///
/// Post-Phase-22/23 additions (2026-07-19): mixed-boolean binop wrapping
/// (<see cref="NeedsParensForBinop"/>) so <c>(not q) and c</c> survives
/// round-trip without collapsing to <c>not q and c</c>; process arms no
/// longer emit VHDL-2008 <c>is</c> keyword (Kmila's parser is VHDL-93 for
/// process headers); JSON walk uses <c>MaxDepth = 512</c> to handle
/// deep next-pointer case chains (33-arm case = 150+ nesting);
/// <see cref="ToolboxJson"/> reshaped into a real 6-category
/// <c>categoryToolbox</c>.
///
/// Two invariants worth knowing (from the 2026-07-19 audit):
/// <list type="bullet">
/// <item>Silent-loss defaults are ALWAYS visible. Missing OP on a binop,
/// missing TYPE on a port, missing VAL on an aggregate — all emit
/// a <c>/* MISSING X */</c> marker AND record a diagnostic in
/// <see cref="LastDiagnostics"/> for Editor.razor to surface as a
/// Danger-severity toast. Never a silent LOGIC-FLIP.</item>
/// <item>Backward-compat prelude: the hardcoded <c>library ieee; use
/// ieee.std_logic_1164.all;</c> at the top of every emit is ONLY
/// generated when the workspace has no kmila_vhdl_library/use blocks.
/// New builder output includes them explicitly so no double-emission;
/// legacy pre-Phase-8 sidecars still get the auto-prelude.</item>
/// </list>
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

    /// <summary>
    /// Silent-loss diagnostics from the most recent <see cref="Emit"/>
    /// call. Each entry describes a hand-built workspace field that was
    /// missing at emit time — the emitter fills a visible marker
    /// (e.g. <c>/* MISSING OP */</c>) rather than a silent default that
    /// would flip semantics (like an <c>or</c> block silently becoming
    /// <c>and</c>). Editor.razor surfaces these as a distinct Error
    /// toast (<c>EDITOR_BLOCKS_SILENT_LOSS_*</c>) and, post 2026-07-20
    /// Phase 23a, projects each entry into a Monaco marker at
    /// <see cref="EmitterDiagnostic.Line"/> with owner
    /// <c>"kmila-vhdl-emit"</c>.
    /// </summary>
    public List<EmitterDiagnostic> LastDiagnostics { get; private set; } = new();

    private int _currentLine;

    /// <summary>Creates the emitter with an optional logger.</summary>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
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

    /// <summary>
    /// 2026-07-20 Phase 23a — same guarded read as <see cref="Track"/> but
    /// returns the id (or null). Used by silent-loss diagnostic sites to
    /// attach the offending block's id to the emitted
    /// <see cref="EmitterDiagnostic"/> without side-effecting
    /// <see cref="LastBlockLineMap"/>.
    /// </summary>
    private static string? GetBlockId(JsonElement node)
    {
        if (node.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String)
        {
            var s = id.GetString();
            return string.IsNullOrEmpty(s) ? null : s;
        }
        return null;
    }

    /// <summary>Append-and-count: every <c>AppendLine</c> bumps the line cursor.</summary>
    private void Line(StringBuilder sb, string text = "")
    {
        sb.AppendLine(text);
        _currentLine++;
    }

    /// <summary>
    /// Palette layout the Editor's Blocks tab feeds into kmila-blocks.
    /// 2026-07-20 Phase 23: real <c>categoryToolbox</c> — six named
    /// categories (Top-level / Declarations / Concurrent / Sequential /
    /// Expressions / Comments) rendered as section headers by
    /// <c>kmila-blocks.js::_renderPaletteItem</c>. Mirrors the catalog
    /// registered in <c>kmila-blocks-vhdl.js</c>; block types missing from
    /// the engine registry are silently skipped by the palette renderer,
    /// so adding new constructs only requires updating both files in
    /// lockstep. Within each category, the block ordering matches the
    /// pre-P23 flat list (no reordering; only regrouping).
    /// </summary>
    public const string ToolboxJson = """
        {
          "kind": "categoryToolbox",
          "contents": [
            {
              "kind": "category",
              "name": "Top-level",
              "contents": [
                { "kind": "block", "type": "kmila_vhdl_library" },
                { "kind": "block", "type": "kmila_vhdl_use" },
                { "kind": "block", "type": "kmila_vhdl_entity" },
                { "kind": "block", "type": "kmila_vhdl_generic" },
                { "kind": "block", "type": "kmila_vhdl_architecture" },
                { "kind": "block", "type": "kmila_vhdl_package" },
                { "kind": "block", "type": "kmila_vhdl_package_body" }
              ]
            },
            {
              "kind": "category",
              "name": "Declarations",
              "contents": [
                { "kind": "block", "type": "kmila_vhdl_port" },
                { "kind": "block", "type": "kmila_vhdl_signal_decl" },
                { "kind": "block", "type": "kmila_vhdl_constant" },
                { "kind": "block", "type": "kmila_vhdl_subtype" },
                { "kind": "block", "type": "kmila_vhdl_alias" },
                { "kind": "block", "type": "kmila_vhdl_type_array" },
                { "kind": "block", "type": "kmila_vhdl_type_record" },
                { "kind": "block", "type": "kmila_vhdl_record_field" },
                { "kind": "block", "type": "kmila_vhdl_variable_decl" },
                { "kind": "block", "type": "kmila_vhdl_variable_assign" },
                { "kind": "block", "type": "kmila_vhdl_attribute_decl" },
                { "kind": "block", "type": "kmila_vhdl_attribute_spec" },
                { "kind": "block", "type": "kmila_vhdl_type_enum" },
                { "kind": "block", "type": "kmila_vhdl_component_decl" },
                { "kind": "block", "type": "kmila_vhdl_procedure_decl" },
                { "kind": "block", "type": "kmila_vhdl_function_decl" },
                { "kind": "block", "type": "kmila_vhdl_param" }
              ]
            },
            {
              "kind": "category",
              "name": "Concurrent",
              "contents": [
                { "kind": "block", "type": "kmila_vhdl_assign_concurrent" },
                { "kind": "block", "type": "kmila_vhdl_cond_assign" },
                { "kind": "block", "type": "kmila_vhdl_process" },
                { "kind": "block", "type": "kmila_vhdl_component_inst" },
                { "kind": "block", "type": "kmila_vhdl_port_assoc" },
                { "kind": "block", "type": "kmila_vhdl_generate_for" },
                { "kind": "block", "type": "kmila_vhdl_generate_if" },
                { "kind": "block", "type": "kmila_vhdl_with_select" },
                { "kind": "block", "type": "kmila_vhdl_with_when" },
                { "kind": "block", "type": "kmila_vhdl_with_others" }
              ]
            },
            {
              "kind": "category",
              "name": "Sequential",
              "contents": [
                { "kind": "block", "type": "kmila_vhdl_if" },
                { "kind": "block", "type": "kmila_vhdl_elsif" },
                { "kind": "block", "type": "kmila_vhdl_assign_seq" },
                { "kind": "block", "type": "kmila_vhdl_case" },
                { "kind": "block", "type": "kmila_vhdl_wait" },
                { "kind": "block", "type": "kmila_vhdl_exit" },
                { "kind": "block", "type": "kmila_vhdl_next" },
                { "kind": "block", "type": "kmila_vhdl_return" },
                { "kind": "block", "type": "kmila_vhdl_null" },
                { "kind": "block", "type": "kmila_vhdl_report" },
                { "kind": "block", "type": "kmila_vhdl_assert" },
                { "kind": "block", "type": "kmila_vhdl_procedure_call" },
                { "kind": "block", "type": "kmila_vhdl_for_loop" },
                { "kind": "block", "type": "kmila_vhdl_while_loop" },
                { "kind": "block", "type": "kmila_vhdl_loop" }
              ]
            },
            {
              "kind": "category",
              "name": "Expressions",
              "contents": [
                { "kind": "block", "type": "kmila_vhdl_id" },
                { "kind": "block", "type": "kmila_vhdl_literal_bit" },
                { "kind": "block", "type": "kmila_vhdl_literal_vec" },
                { "kind": "block", "type": "kmila_vhdl_eq" },
                { "kind": "block", "type": "kmila_vhdl_binop" },
                { "kind": "block", "type": "kmila_vhdl_rising_edge" },
                { "kind": "block", "type": "kmila_vhdl_falling_edge" },
                { "kind": "block", "type": "kmila_vhdl_not" },
                { "kind": "block", "type": "kmila_vhdl_slice" },
                { "kind": "block", "type": "kmila_vhdl_index" },
                { "kind": "block", "type": "kmila_vhdl_call" },
                { "kind": "block", "type": "kmila_vhdl_others" },
                { "kind": "block", "type": "kmila_vhdl_aggregate" },
                { "kind": "block", "type": "kmila_vhdl_agg_elem" }
              ]
            },
            {
              "kind": "category",
              "name": "Comments",
              "contents": [
                { "kind": "block", "type": "kmila_vhdl_comment_top" },
                { "kind": "block", "type": "kmila_vhdl_comment_decl" },
                { "kind": "block", "type": "kmila_vhdl_comment_concurrent" },
                { "kind": "block", "type": "kmila_vhdl_comment_seq" }
              ]
            }
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
        LastDiagnostics = new List<EmitterDiagnostic>();
        _currentLine = 1;
        if (string.IsNullOrWhiteSpace(workspaceJson)) return string.Empty;

        try
        {
            // 2026-07-20 Phase 22b — MaxDepth=512 (vs default 64) so
            // workspaces with long chain-next pointers (e.g. a 32-arm
            // case block on 016-demux-1to32, ~150 nesting levels once
            // every arm's BODY + VALUE tree is walked) parse cleanly.
            // Must match the Builder's serialize-side MaxDepth.
            using var doc = JsonDocument.Parse(workspaceJson,
                new JsonDocumentOptions { MaxDepth = 512 });
            if (!doc.RootElement.TryGetProperty("blocks", out var b1)) return string.Empty;
            if (!b1.TryGetProperty("blocks", out var topArray)) return string.Empty;
            if (topArray.ValueKind != JsonValueKind.Array || topArray.GetArrayLength() == 0)
                return string.Empty;

            var sb = new StringBuilder();

            // 2026-07-19 Phase 8 — only emit the hard-coded `library ieee;
            // use ieee.std_logic_1164.all;` prelude when the workspace does
            // NOT already contain library/use blocks. Legacy .bvhdl sidecars
            // (pre-Phase-8) have no such blocks so they keep the prelude for
            // backward compatibility. New builder output includes the
            // library/use blocks explicitly so no double-emission.
            bool hasLibraryBlock = false;
            foreach (var n in topArray.EnumerateArray())
            {
                var t = GetType(n);
                if (t == "kmila_vhdl_library" || t == "kmila_vhdl_use")
                {
                    hasLibraryBlock = true;
                    break;
                }
            }
            if (!hasLibraryBlock)
            {
                Line(sb, "library ieee;");
                Line(sb, "use ieee.std_logic_1164.all;");
                Line(sb);
            }

            bool first = true;
            string? prevType = null;
            foreach (var node in topArray.EnumerateArray())
            {
                var t = GetType(node);
                // Group library/use clauses tightly (no blank line between
                // consecutive library or use blocks); insert a blank line
                // when transitioning out of a library/use cluster or between
                // major declarations (entity/architecture/package).
                bool tightCluster = (t == "kmila_vhdl_library" || t == "kmila_vhdl_use")
                    && (prevType == "kmila_vhdl_library" || prevType == "kmila_vhdl_use");
                if (!first && !tightCluster) Line(sb);
                first = false;
                EmitTopLevel(sb, node);
                prevType = t;
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
            // 2026-07-19 Phase 8 — library/use/package/package_body
            case "kmila_vhdl_library":
                Track(node);
                sb.Append("library ").Append(GetField(node, "NAMES") ?? "ieee");
                Line(sb, ";");
                break;
            case "kmila_vhdl_use":
                Track(node);
                sb.Append("use ").Append(GetField(node, "PATH") ?? "ieee.std_logic_1164.all");
                Line(sb, ";");
                break;
            case "kmila_vhdl_package":
                EmitPackage(sb, node, isBody: false);
                break;
            case "kmila_vhdl_package_body":
                EmitPackage(sb, node, isBody: true);
                break;
            // 2026-07-19 Phase 9 — top-level comment chain block
            case "kmila_vhdl_comment_top":
                Track(node);
                sb.Append("-- ").Append(GetField(node, "TEXT") ?? "");
                Line(sb);
                break;
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

        // 2026-07-19 Phase 8 — optional generic clause. Only emitted when
        // GENERICS chain has at least one child; keeps byte-identity for
        // pre-Phase-8 entities that have no generic block.
        var generics = GetStatementChain(node, "GENERICS").ToList();
        if (generics.Count > 0)
        {
            Line(sb, "    generic (");
            for (int i = 0; i < generics.Count; i++)
            {
                Track(generics[i]);
                sb.Append("        ")
                  .Append(GetField(generics[i], "NAME") ?? "N")
                  .Append(" : ")
                  .Append(GetField(generics[i], "TYPE") ?? "integer");
                var def = GetField(generics[i], "DEFAULT") ?? "";
                if (!string.IsNullOrWhiteSpace(def))
                    sb.Append(" := ").Append(def.Trim());
                Line(sb, i == generics.Count - 1 ? "" : ";");
            }
            Line(sb, "    );");
        }

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

    // 2026-07-19 Phase 8 — package / package body emitter.
    private void EmitPackage(StringBuilder sb, JsonElement node, bool isBody)
    {
        Track(node);
        var name = GetField(node, "NAME") ?? "my_pkg";
        sb.Append(isBody ? "package body " : "package ").Append(name);
        Line(sb, " is");
        var slot = isBody ? "BODY" : "DECLS";
        foreach (var decl in GetStatementChain(node, slot))
        {
            Track(decl);
            sb.Append("    ");
            EmitDecl(sb, decl);
        }
        sb.Append(isBody ? "end package body " : "end package ").Append(name);
        Line(sb, ";");
    }

    private void EmitPort(StringBuilder sb, JsonElement node)
    {
        var name = GetField(node, "NAME") ?? "anon";
        var dir  = GetField(node, "DIR")  ?? "in";
        var typeRaw = GetField(node, "TYPE");
        string type;
        if (string.IsNullOrWhiteSpace(typeRaw))
        {
            LastDiagnostics.Add(new EmitterDiagnostic(
                _currentLine,
                $"port '{name}' missing TYPE — emitting std_logic with visible marker",
                BlockId: GetBlockId(node)));
            type = "std_logic /* MISSING TYPE */";
        }
        else type = typeRaw;
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
            // F-18 (2026-07-06 batch 2): emit `:= <init>` when the
            // signal_decl block carries an INIT field. Blank/absent =>
            // clean `signal x : t;` — no noisy trailing `:=`.
            var init = GetField(node, "INIT");
            if (!string.IsNullOrWhiteSpace(init))
            {
                sb.Append(" := ").Append(init.Trim());
            }
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_type_enum")
        {
            // F-N (2026-07-06 batch 2): `type <name> is (M1, M2, …);`.
            // MEMBERS is a chain of kmila_vhdl_enum_member blocks.
            Track(node);
            var name = GetField(node, "NAME") ?? "anon_t";
            sb.Append("type ").Append(name).Append(" is (");
            bool first = true;
            foreach (var member in GetStatementChain(node, "MEMBERS"))
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(GetField(member, "NAME") ?? "M");
            }
            sb.Append(')');
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_comment_decl")
        {
            // 2026-07-19 Phase 9 — decl-slot comment chain block
            Track(node);
            sb.Append("-- ").Append(GetField(node, "TEXT") ?? "");
            Line(sb);
        }
        // 2026-07-19 Phase 7 — Tier D declarations
        else if (type == "kmila_vhdl_constant")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "K";
            var dt   = GetField(node, "TYPE") ?? "integer";
            var val  = GetField(node, "VALUE") ?? "0";
            sb.Append("constant ").Append(name).Append(" : ").Append(dt)
              .Append(" := ").Append(val);
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_subtype")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "S";
            var dt   = GetField(node, "TYPE_EXPR") ?? "integer";
            sb.Append("subtype ").Append(name).Append(" is ").Append(dt);
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_alias")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "a";
            var tgt  = GetField(node, "TARGET") ?? "x";
            sb.Append("alias ").Append(name).Append(" is ").Append(tgt);
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_type_array")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "mem_t";
            var rng  = GetField(node, "RANGE") ?? "0 to 15";
            var elem = GetField(node, "ELEM_TYPE") ?? "std_logic";
            sb.Append("type ").Append(name).Append(" is array (").Append(rng)
              .Append(") of ").Append(elem);
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_type_record")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "bus_t";
            sb.Append("type ").Append(name);
            Line(sb, " is record");
            foreach (var f in GetStatementChain(node, "FIELDS"))
            {
                Track(f);
                sb.Append("        ")
                  .Append(GetField(f, "NAME") ?? "f")
                  .Append(" : ")
                  .Append(GetField(f, "TYPE") ?? "std_logic");
                Line(sb, ";");
            }
            sb.Append("    end record ").Append(name);
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_variable_decl")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "v";
            var dt   = GetField(node, "TYPE") ?? "integer";
            var init = GetField(node, "INIT") ?? "";
            sb.Append("variable ").Append(name).Append(" : ").Append(dt);
            if (!string.IsNullOrWhiteSpace(init)) sb.Append(" := ").Append(init.Trim());
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_attribute_decl")
        {
            Track(node);
            sb.Append("attribute ").Append(GetField(node, "NAME") ?? "a")
              .Append(" : ").Append(GetField(node, "TYPE") ?? "string");
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_attribute_spec")
        {
            Track(node);
            sb.Append("attribute ").Append(GetField(node, "NAME") ?? "a")
              .Append(" of ").Append(GetField(node, "TARGET") ?? "x")
              .Append(" : ").Append(GetField(node, "CLASS") ?? "signal")
              .Append(" is ").Append(GetField(node, "VALUE") ?? "\"…\"");
            Line(sb, ";");
        }
        // 2026-07-19 Phase 19 — component declaration (arch/package DECLS).
        //   component <NAME> is
        //       [generic ( … );]
        //       port ( … );
        //   end component;
        // Canonical form emits `is` (VHDL-93+ preferred). Byte-idempotency
        // is measured V2 vs V3, not V1 vs V2, so the canonicalization is
        // safe.
        else if (type == "kmila_vhdl_component_decl")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "my_component";
            sb.Append("component ").Append(name);
            Line(sb, " is");

            var generics = GetStatementChain(node, "GENERICS").ToList();
            if (generics.Count > 0)
            {
                sb.Append("        generic (");
                Line(sb);
                for (int i = 0; i < generics.Count; i++)
                {
                    Track(generics[i]);
                    sb.Append("            ")
                      .Append(GetField(generics[i], "NAME") ?? "N")
                      .Append(" : ")
                      .Append(GetField(generics[i], "TYPE") ?? "integer");
                    var def = GetField(generics[i], "DEFAULT") ?? "";
                    if (!string.IsNullOrWhiteSpace(def))
                        sb.Append(" := ").Append(def.Trim());
                    Line(sb, i == generics.Count - 1 ? "" : ";");
                }
                sb.Append("        );");
                Line(sb);
            }

            var ports = GetStatementChain(node, "PORTS").ToList();
            if (ports.Count > 0)
            {
                sb.Append("        port (");
                Line(sb);
                for (int i = 0; i < ports.Count; i++)
                {
                    Track(ports[i]);
                    sb.Append("            ")
                      .Append(GetField(ports[i], "NAME") ?? "?")
                      .Append(" : ")
                      .Append(GetField(ports[i], "DIR") ?? "in").Append(' ')
                      .Append(GetField(ports[i], "TYPE") ?? "std_logic");
                    Line(sb, i == ports.Count - 1 ? "" : ";");
                }
                sb.Append("        );");
                Line(sb);
            }
            else
            {
                LastDiagnostics.Add(new EmitterDiagnostic(
                    _currentLine,
                    "kmila_vhdl_component_decl \"" + name + "\" missing PORTS",
                    BlockId: GetBlockId(node)));
                sb.Append("        /* MISSING PORTS */;");
                Line(sb);
            }

            sb.Append("    end component;");
            Line(sb);
        }
        // 2026-07-19 Phase 19 — procedure declaration. Reuses EmitDecl for
        // the local-decls chain and EmitSequential for the body chain, so
        // nested variables/case/loop/etc. all round-trip correctly.
        else if (type == "kmila_vhdl_procedure_decl")
        {
            Track(node);
            var name = GetField(node, "NAME") ?? "my_proc";
            sb.Append("procedure ").Append(name);
            EmitSubprogParams(sb, node);
            Line(sb, " is");

            foreach (var d in GetStatementChain(node, "DECLS"))
            {
                sb.Append("        ");
                EmitDecl(sb, d);
            }
            sb.Append("    begin");
            Line(sb);
            foreach (var s in GetStatementChain(node, "BODY"))
                EmitSequential(sb, s, indent: 2);
            sb.Append("    end procedure;");
            Line(sb);
        }
        // 2026-07-19 Phase 19 — function declaration. Same shape as
        // procedure_decl plus `return <RTYPE>`.
        else if (type == "kmila_vhdl_function_decl")
        {
            Track(node);
            var name  = GetField(node, "NAME")        ?? "my_func";
            var rtype = GetField(node, "RETURN_TYPE") ?? "";
            sb.Append("function ").Append(name);
            EmitSubprogParams(sb, node);
            if (!string.IsNullOrWhiteSpace(rtype))
            {
                sb.Append(" return ").Append(rtype.Trim());
            }
            else
            {
                LastDiagnostics.Add(new EmitterDiagnostic(
                    _currentLine,
                    "kmila_vhdl_function_decl \"" + name + "\" missing RETURN_TYPE",
                    BlockId: GetBlockId(node)));
                sb.Append(" return /* MISSING RETURN_TYPE */");
            }
            Line(sb, " is");

            foreach (var d in GetStatementChain(node, "DECLS"))
            {
                sb.Append("        ");
                EmitDecl(sb, d);
            }
            sb.Append("    begin");
            Line(sb);
            foreach (var s in GetStatementChain(node, "BODY"))
                EmitSequential(sb, s, indent: 2);
            sb.Append("    end function;");
            Line(sb);
        }
        else if (type == "kmila_vhdl_raw_decl")
        {
            // Verbatim preservation of any decl statement the recognizer
            // didn't cover (never silently dropped).
            Track(node);
            sb.Append(GetField(node, "TEXT") ?? "");
            Line(sb);
        }
        else
        {
            sb.Append("-- unsupported declaration block: ");
            Line(sb, type ?? "?");
        }
    }

    // 2026-07-19 Phase 19 — emit the parameter list of a procedure_decl /
    // function_decl. Renders `(a : in T; b : in T; …)` inline or nothing
    // when PARAMS chain is empty. CLASS is omitted when blank (LRM
    // default). Silent-loss records a diagnostic when NAME or TYPE
    // are missing on any param wrapper.
    private void EmitSubprogParams(StringBuilder sb, JsonElement node)
    {
        var ps = GetStatementChain(node, "PARAMS").ToList();
        if (ps.Count == 0) return;
        sb.Append('(');
        for (int i = 0; i < ps.Count; i++)
        {
            Track(ps[i]);
            var cls  = GetField(ps[i], "CLASS") ?? "";
            var pnm  = GetField(ps[i], "NAME")  ?? "";
            var mode = GetField(ps[i], "MODE")  ?? "in";
            var ptp  = GetField(ps[i], "TYPE")  ?? "";
            if (string.IsNullOrWhiteSpace(pnm))
            {
                LastDiagnostics.Add(new EmitterDiagnostic(
                    _currentLine,
                    "kmila_vhdl_param missing NAME",
                    BlockId: GetBlockId(ps[i])));
                pnm = "/* MISSING NAME */";
            }
            if (string.IsNullOrWhiteSpace(ptp))
            {
                LastDiagnostics.Add(new EmitterDiagnostic(
                    _currentLine,
                    "kmila_vhdl_param \"" + pnm + "\" missing TYPE",
                    BlockId: GetBlockId(ps[i])));
                ptp = "/* MISSING TYPE */";
            }
            if (i > 0) sb.Append("; ");
            if (!string.IsNullOrWhiteSpace(cls)) sb.Append(cls.Trim()).Append(' ');
            sb.Append(pnm).Append(" : ").Append(mode).Append(' ').Append(ptp);
        }
        sb.Append(')');
    }

    /* ── Concurrent statements (in architecture BODY) ──────────────── */

    private void EmitConcurrent(StringBuilder sb, JsonElement node, int indent)
    {
        var pad  = new string(' ', indent * 4);
        var type = GetType(node);
        if (type == "kmila_vhdl_process")
        {
            Track(node);
            var label = GetField(node, "LABEL") ?? "";
            var sens = GetField(node, "SENS") ?? "";
            sb.Append(pad);
            // 2026-07-19 Phase 4 (F-11): optional label prefix.
            if (!string.IsNullOrWhiteSpace(label))
                sb.Append(label.Trim()).Append(": ");
            sb.Append("process");
            if (!string.IsNullOrWhiteSpace(sens))
                sb.Append(" (").Append(sens.Trim()).Append(')');
            // 2026-07-20 Phase 21 — never emit the (optional, VHDL-2008)
            // `is` keyword after process. Kmila's own VHDL parser only
            // accepts the VHDL-93 form (`process (…)\n<decls>\nbegin`) —
            // `process (…) is\n…` fails to parse. GHDL accepts both, so
            // omitting `is` is safe for both simulator paths. The
            // ProcessRe (in VhdlBlockBuilder) still ACCEPTS the `is`
            // form on the parse side so source VHDL with the VHDL-2008
            // shape survives the round-trip.
            Line(sb);
            // 2026-07-19 Phase 20 — declarative part between the process
            // header and `begin`. Emits identically to arch DECLS one
            // indent deeper.
            var procPad = new string(' ', (indent + 1) * 4);
            foreach (var decl in GetStatementChain(node, "DECLS"))
            {
                Track(decl);
                sb.Append(procPad);
                EmitDecl(sb, decl);
            }
            sb.Append(pad);
            Line(sb, "begin");
            foreach (var stmt in GetStatementChain(node, "BODY"))
            {
                EmitSequential(sb, stmt, indent + 1);
            }
            sb.Append(pad).Append("end process");
            if (!string.IsNullOrWhiteSpace(label)) sb.Append(' ').Append(label.Trim());
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_component_inst")
        {
            // 2026-07-19 Phase 4 (F-11) — labeled component instantiation.
            //   <label>: <component> [generic map (…)] port map (…);
            Track(node);
            var label = GetField(node, "LABEL") ?? "u_anon";
            var comp  = GetField(node, "COMPONENT") ?? "component_anon";
            sb.Append(pad).Append(label).Append(": ").Append(comp);

            var generics = GetStatementChain(node, "GENERIC_MAP").ToList();
            if (generics.Count > 0)
            {
                Line(sb);
                sb.Append(pad).Append("    generic map (");
                Line(sb);
                for (int i = 0; i < generics.Count; i++)
                {
                    Track(generics[i]);
                    sb.Append(pad).Append("        ")
                      .Append(GetField(generics[i], "FORMAL") ?? "?")
                      .Append(" => ")
                      .Append(EmitExpr(GetValueInput(generics[i], "ACTUAL")));
                    Line(sb, i == generics.Count - 1 ? "" : ",");
                }
                sb.Append(pad).Append("    )");
            }

            var ports = GetStatementChain(node, "PORT_MAP").ToList();
            if (ports.Count > 0)
            {
                Line(sb);
                sb.Append(pad).Append("    port map (");
                Line(sb);
                for (int i = 0; i < ports.Count; i++)
                {
                    Track(ports[i]);
                    sb.Append(pad).Append("        ")
                      .Append(GetField(ports[i], "FORMAL") ?? "?")
                      .Append(" => ")
                      .Append(EmitExpr(GetValueInput(ports[i], "ACTUAL")));
                    Line(sb, i == ports.Count - 1 ? "" : ",");
                }
                sb.Append(pad).Append("    )");
            }
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_assign_concurrent")
        {
            Track(node);
            var target = GetField(node, "TARGET") ?? "anon";
            var value  = GetValueInput(node, "VALUE");
            sb.Append(pad).Append(target).Append(" <= ").Append(EmitExpr(value));
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_cond_assign")
        {
            // F-N conditional-assign (2026-07-06 batch 2):
            //   target <= v1 when c1 else v2 when c2 else vN;
            Track(node);
            var target = GetField(node, "TARGET") ?? "anon";
            sb.Append(pad).Append(target).Append(" <= ");
            bool first = true;
            foreach (var arm in GetStatementChain(node, "ARMS"))
            {
                var v = GetValueInput(arm, "VALUE");
                var c = GetValueInput(arm, "COND");
                if (!first) sb.Append(" else ");
                first = false;
                sb.Append(EmitExpr(v)).Append(" when ").Append(EmitExpr(c));
            }
            var elseValue = GetValueInput(node, "ELSE_VALUE");
            if (elseValue.HasValue)
                sb.Append(" else ").Append(EmitExpr(elseValue));
            Line(sb, ";");
        }
        // 2026-07-19 Phase 6 — with … select
        else if (type == "kmila_vhdl_with_select")
        {
            Track(node);
            var subj   = GetValueInput(node, "SUBJECT");
            var target = GetField(node, "TARGET") ?? "y";
            sb.Append(pad).Append("with ").Append(EmitExpr(subj))
              .Append(" select ").Append(target).Append(" <= ");
            var alts = GetStatementChain(node, "ALTS").ToList();
            for (int i = 0; i < alts.Count; i++)
            {
                Track(alts[i]);
                var altType = GetType(alts[i]);
                var value   = EmitExpr(GetValueInput(alts[i], "VALUE"));
                if (altType == "kmila_vhdl_with_others")
                    sb.Append(value).Append(" when others");
                else
                {
                    var choice = GetField(alts[i], "CHOICE") ?? "?";
                    sb.Append(value).Append(" when ").Append(choice);
                }
                if (i < alts.Count - 1) sb.Append(", ");
            }
            Line(sb, ";");
        }
        // 2026-07-19 Phase 5 (F-7) — for-generate
        else if (type == "kmila_vhdl_generate_for")
        {
            Track(node);
            var label = GetField(node, "LABEL") ?? "gen";
            var var   = GetField(node, "VAR") ?? "i";
            var dir   = (GetField(node, "DIR") ?? "to").ToLowerInvariant();
            var lo    = GetValueInput(node, "LO");
            var hi    = GetValueInput(node, "HI");
            sb.Append(pad).Append(label).Append(": for ").Append(var).Append(" in ")
              .Append(EmitExpr(lo)).Append(' ').Append(dir).Append(' ')
              .Append(EmitExpr(hi)).Append(' ');
            Line(sb, "generate");
            foreach (var s in GetStatementChain(node, "BODY"))
                EmitConcurrent(sb, s, indent + 1);
            sb.Append(pad).Append("end generate ").Append(label);
            Line(sb, ";");
        }
        // 2026-07-19 Phase 5 (F-7) — if-generate (with optional else)
        else if (type == "kmila_vhdl_generate_if")
        {
            Track(node);
            var label = GetField(node, "LABEL") ?? "g_if";
            var cond  = GetValueInput(node, "COND");
            sb.Append(pad).Append(label).Append(": if ").Append(EmitExpr(cond)).Append(' ');
            Line(sb, "generate");
            foreach (var s in GetStatementChain(node, "THEN_BODY"))
                EmitConcurrent(sb, s, indent + 1);
            var elseBody = GetStatementChain(node, "ELSE_BODY").ToList();
            if (elseBody.Count > 0)
            {
                sb.Append(pad);
                Line(sb, "else generate");
                foreach (var s in elseBody)
                    EmitConcurrent(sb, s, indent + 1);
            }
            sb.Append(pad).Append("end generate ").Append(label);
            Line(sb, ";");
        }
        else if (type == "kmila_vhdl_raw_concurrent")
        {
            EmitRaw(sb, node, indent);
        }
        else if (type == "kmila_vhdl_comment_concurrent")
        {
            // 2026-07-19 Phase 9 — concurrent-slot comment chain block
            Track(node);
            sb.Append(pad).Append("-- ").Append(GetField(node, "TEXT") ?? "");
            Line(sb);
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
            // 2026-07-19 Phase 7 — variable assignment `v := expr;`
            case "kmila_vhdl_variable_assign":
                {
                    Track(node);
                    var target = GetField(node, "TARGET") ?? "v";
                    var value  = GetValueInput(node, "VALUE");
                    sb.Append(pad).Append(target).Append(" := ").Append(EmitExpr(value));
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
            // 2026-07-19 Phase 19 — procedure call as sequential statement.
            //   NAME(a, b, c);   or   NAME;
            case "kmila_vhdl_procedure_call":
                {
                    Track(node);
                    var name = GetField(node, "NAME") ?? "";
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        LastDiagnostics.Add(new EmitterDiagnostic(
                            _currentLine,
                            "kmila_vhdl_procedure_call missing NAME",
                            BlockId: GetBlockId(node)));
                        name = "/* MISSING NAME */";
                    }
                    sb.Append(pad).Append(name);
                    var args = GetStatementChain(node, "ARGS").ToList();
                    if (args.Count > 0)
                    {
                        sb.Append('(');
                        for (int i = 0; i < args.Count; i++)
                        {
                            Track(args[i]);
                            if (i > 0) sb.Append(", ");
                            sb.Append(EmitExpr(GetValueInput(args[i], "VALUE")));
                        }
                        sb.Append(')');
                    }
                    Line(sb, ";");
                    break;
                }
            case "kmila_vhdl_raw_seq":
                EmitRaw(sb, node, indent);
                break;
            // F-8 (2026-07-05) — an elsif block only makes sense inside
            // kmila_vhdl_if's ELSIF_CHAIN. If one shows up at the top of a
            // sequential body (student dragged it out of place, malformed
            // .bvhdl) emit a comment instead of a syntactically invalid
            // `elsif` line.
            case "kmila_vhdl_elsif":
                sb.Append(pad).Append("-- orphan elsif (must live inside an if-block's ELSIF_CHAIN slot)");
                Line(sb);
                break;
            // 2026-07-19 Phase 2 — sequential single-statement primitives
            case "kmila_vhdl_wait":
                {
                    Track(node);
                    var mode = GetField(node, "MODE") ?? "";
                    var expr = GetField(node, "EXPR") ?? "";
                    sb.Append(pad).Append("wait");
                    if (!string.IsNullOrWhiteSpace(mode))
                    {
                        sb.Append(' ').Append(mode);
                        if (!string.IsNullOrWhiteSpace(expr)) sb.Append(' ').Append(expr.Trim());
                    }
                    Line(sb, ";");
                    break;
                }
            case "kmila_vhdl_exit":
            case "kmila_vhdl_next":
                {
                    Track(node);
                    var kw = type == "kmila_vhdl_exit" ? "exit" : "next";
                    var lbl  = GetField(node, "LABEL") ?? "";
                    var cond = GetField(node, "COND")  ?? "";
                    sb.Append(pad).Append(kw);
                    if (!string.IsNullOrWhiteSpace(lbl))  sb.Append(' ').Append(lbl.Trim());
                    if (!string.IsNullOrWhiteSpace(cond)) sb.Append(" when ").Append(cond.Trim());
                    Line(sb, ";");
                    break;
                }
            case "kmila_vhdl_return":
                {
                    Track(node);
                    var expr = GetField(node, "EXPR") ?? "";
                    sb.Append(pad).Append("return");
                    if (!string.IsNullOrWhiteSpace(expr)) sb.Append(' ').Append(expr.Trim());
                    Line(sb, ";");
                    break;
                }
            case "kmila_vhdl_null":
                Track(node);
                sb.Append(pad);
                Line(sb, "null;");
                break;
            case "kmila_vhdl_report":
                {
                    Track(node);
                    var msg = GetField(node, "MSG") ?? "\"\"";
                    var sev = GetField(node, "SEVERITY") ?? "";
                    sb.Append(pad).Append("report ").Append(msg);
                    if (!string.IsNullOrWhiteSpace(sev)) sb.Append(" severity ").Append(sev);
                    Line(sb, ";");
                    break;
                }
            case "kmila_vhdl_assert":
                {
                    Track(node);
                    var cond = GetValueInput(node, "COND");
                    var msg  = GetField(node, "MSG") ?? "";
                    var sev  = GetField(node, "SEVERITY") ?? "";
                    sb.Append(pad).Append("assert ").Append(EmitExpr(cond));
                    if (!string.IsNullOrWhiteSpace(msg)) sb.Append(" report ").Append(msg);
                    if (!string.IsNullOrWhiteSpace(sev)) sb.Append(" severity ").Append(sev);
                    Line(sb, ";");
                    break;
                }
            // 2026-07-19 Phase 3 — loops
            case "kmila_vhdl_for_loop":
                {
                    Track(node);
                    var var  = GetField(node, "VAR") ?? "i";
                    var dir  = (GetField(node, "DIR") ?? "to").ToLowerInvariant();
                    var lo   = GetValueInput(node, "LO");
                    var hi   = GetValueInput(node, "HI");
                    sb.Append(pad).Append("for ").Append(var).Append(" in ")
                      .Append(EmitExpr(lo)).Append(' ').Append(dir).Append(' ')
                      .Append(EmitExpr(hi)).Append(' ');
                    Line(sb, "loop");
                    foreach (var s in GetStatementChain(node, "BODY"))
                        EmitSequential(sb, s, indent + 1);
                    sb.Append(pad);
                    Line(sb, "end loop;");
                    break;
                }
            case "kmila_vhdl_while_loop":
                {
                    Track(node);
                    var cond = GetValueInput(node, "COND");
                    sb.Append(pad).Append("while ").Append(EmitExpr(cond)).Append(' ');
                    Line(sb, "loop");
                    foreach (var s in GetStatementChain(node, "BODY"))
                        EmitSequential(sb, s, indent + 1);
                    sb.Append(pad);
                    Line(sb, "end loop;");
                    break;
                }
            case "kmila_vhdl_loop":
                Track(node);
                sb.Append(pad);
                Line(sb, "loop");
                foreach (var s in GetStatementChain(node, "BODY"))
                    EmitSequential(sb, s, indent + 1);
                sb.Append(pad);
                Line(sb, "end loop;");
                break;
            // 2026-07-19 Phase 9 — comment chain block (sequential slot)
            case "kmila_vhdl_comment_seq":
                Track(node);
                sb.Append(pad).Append("-- ").Append(GetField(node, "TEXT") ?? "");
                Line(sb);
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
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');

        // Round-trip fidelity fix (2026-07-06): raw blocks captured from
        // multi-line sources retain the original leading whitespace on
        // every line except the first (which the builder Trim()s). If we
        // blindly prepend `pad`, indentation grows by 4 chars per
        // round-trip on lines 2+ — every fixture with a multi-line
        // comment block failed byte-idempotence because of it. Compute
        // the common leading whitespace across INDENTED lines (skipping
        // lines with zero leading, which are likely the Trim()-affected
        // first line of a captured range), strip it, THEN prepend pad.
        int commonIndent = int.MaxValue;
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            int lead = 0;
            while (lead < line.Length && line[lead] == ' ') lead++;
            if (lead == 0) continue; // skip zero-lead lines (trimmed head)
            if (lead < line.Length && commonIndent > lead) commonIndent = lead;
        }
        if (commonIndent == int.MaxValue) commonIndent = 0;

        foreach (var line in lines)
        {
            if (line.Length == 0) { Line(sb); continue; }
            // Only strip commonIndent chars when the line's first
            // commonIndent chars are ALL spaces. This protects the
            // Trim()ed first line whose leading was already stripped by
            // the builder — otherwise we chop into its content.
            bool canDedent = commonIndent > 0 && line.Length >= commonIndent;
            if (canDedent)
            {
                for (int k = 0; k < commonIndent; k++)
                {
                    if (line[k] != ' ') { canDedent = false; break; }
                }
            }
            var dedented = canDedent ? line.Substring(commonIndent) : line;
            sb.Append(pad);
            Line(sb, dedented);
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

        // F-8 (2026-07-05) — primary elsif path: linear chain in ELSIF_CHAIN.
        // Emits one `elsif <cond> then …` per node in the next-chain. New
        // builder output uses this shape exclusively.
        foreach (var arm in GetStatementChain(cursor, "ELSIF_CHAIN"))
        {
            if (GetType(arm) != "kmila_vhdl_elsif") continue;
            Track(arm);
            var armCond = GetValueInput(arm, "COND");
            sb.Append(pad).Append("elsif ").Append(EmitExpr(armCond));
            Line(sb, " then");
            foreach (var stmt in GetStatementChain(arm, "BODY"))
                EmitSequential(sb, stmt, indent + 1);
        }

        // Legacy fallback: walk down nested elses, promoting solitary-if
        // children to elsif. Preserves round-trip fidelity for pre-F-8
        // .bvhdl sidecars that encoded elsif as a nested-if chain inside
        // ELSE_BODY. Runs AFTER the ELSIF_CHAIN pass so mixed workspaces
        // emit `if / chain-arms / legacy-arms / else / end if;` in order.
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
                // Legacy shape can also carry its own ELSIF_CHAIN if a
                // student hand-mixed the forms — walk it before recursing.
                foreach (var arm in GetStatementChain(cursor, "ELSIF_CHAIN"))
                {
                    if (GetType(arm) != "kmila_vhdl_elsif") continue;
                    Track(arm);
                    var armCond = GetValueInput(arm, "COND");
                    sb.Append(pad).Append("elsif ").Append(EmitExpr(armCond));
                    Line(sb, " then");
                    foreach (var stmt in GetStatementChain(arm, "BODY"))
                        EmitSequential(sb, stmt, indent + 1);
                }
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

    /// <summary>
    /// 2026-07-20 Phase 22a — decide whether a binop operand needs
    /// parentheses in the emitted VHDL.
    /// <list type="bullet">
    /// <item>Same-op child binop (`a and b and c` chain) — NO wrap
    /// (associative per LRM §9.2, unambiguous).</item>
    /// <item>Different-op child binop (`(a or b) and c`) — WRAP. VHDL LRM
    /// §9.2 requires explicit parens for mixed same-precedence ops.</item>
    /// <item>`not X` child under a binop parent — WRAP. Not strictly
    /// required by LRM (`not` has highest precedence) but matches source
    /// author intent and avoids parser edge-cases (Kmila's simulator
    /// evaluates un-parenthesized `not q and c` differently from GHDL).</item>
    /// <item>Equality child (`(a = b) and c`) — WRAP for readability +
    /// consistency with the `not` case.</item>
    /// <item>Everything else (id/literal/call/index/slice/aggregate/
    /// rising_edge/…) — NO wrap.</item>
    /// </list>
    /// </summary>
    private static bool NeedsParensForBinop(JsonElement child, string parentOp)
    {
        var t = GetType(child);
        if (t == "kmila_vhdl_not") return true;
        if (t == "kmila_vhdl_eq")  return true;
        if (t == "kmila_vhdl_binop")
        {
            var childOp = (GetField(child, "OP") ?? "").ToLowerInvariant();
            return childOp != parentOp;
        }
        return false;
    }

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
                    // 2026-07-19 1:1 audit F-17: wrap the operand in parens
                    // when it's a binop/eq — VHDL precedence puts `not`
                    // above every binary operator, so `not a and b` parses
                    // as `(not a) and b`. Preserving `not (a and b)` from
                    // the source requires explicit parens on emit.
                    var opType = s.HasValue ? GetType(s.Value) : null;
                    bool needsParens = opType is "kmila_vhdl_binop"
                                              or "kmila_vhdl_eq";
                    return needsParens
                        ? "not (" + EmitExpr(s) + ")"
                        : "not " + EmitExpr(s);
                }
            // F-2 (2026-06-28) — binary boolean op. The OP field holds the
            // VHDL keyword (and/or/xor/nand/nor/xnor) verbatim.
            // 2026-07-20 Phase 22a — LHS/RHS wrapped in parens when the
            // operand is itself a binop with a different operator, a
            // `not`, or an equality expression. VHDL LRM §9.2 classifies
            // mixed and/or at the same precedence as ambiguous without
            // explicit parens; the paren-strip in BuildExpression drops
            // source parens, so the emitter has to re-add them for
            // semantic-preservation. See NeedsParensForBinop below.
            case "kmila_vhdl_binop":
                {
                    var l  = GetValueInput(n, "LHS");
                    var r  = GetValueInput(n, "RHS");
                    var opRaw = GetField(n, "OP");
                    string op;
                    if (string.IsNullOrWhiteSpace(opRaw))
                    {
                        LastDiagnostics.Add(new EmitterDiagnostic(
                            _currentLine,
                            "binop missing OP field — LOGIC-FLIP hazard, emitting visible marker",
                            BlockId: GetBlockId(n)));
                        op = "/* MISSING OP */";
                    }
                    else op = opRaw.ToLowerInvariant();
                    var lhsStr = EmitExpr(l);
                    var rhsStr = EmitExpr(r);
                    if (l.HasValue && NeedsParensForBinop(l.Value, op)) lhsStr = "(" + lhsStr + ")";
                    if (r.HasValue && NeedsParensForBinop(r.Value, op)) rhsStr = "(" + rhsStr + ")";
                    return lhsStr + " " + op + " " + rhsStr;
                }
            // F-5 (2026-06-28) — vector / bit-string literal. TEXT carries
            // the literal including its surrounding quotes (`"00"`, `X"FF"`).
            case "kmila_vhdl_literal_vec":
                return GetField(n, "TEXT") ?? "\"\"";
            // F-10 (2026-07-05) — vector slice `sig(hi downto lo)` or
            // `sig(lo to hi)`. DIR field carries the direction keyword.
            case "kmila_vhdl_slice":
                {
                    var sig = GetField(n, "SIG") ?? "/*sig*/";
                    var dir = (GetField(n, "DIR") ?? "downto").ToLowerInvariant();
                    var hi  = GetValueInput(n, "HI");
                    var lo  = GetValueInput(n, "LO");
                    return sig + "(" + EmitExpr(hi) + " " + dir + " " + EmitExpr(lo) + ")";
                }
            // F-10 (2026-07-05) — scalar index `sig(idx)`.
            case "kmila_vhdl_index":
                {
                    var sig = GetField(n, "SIG") ?? "/*sig*/";
                    var i   = GetValueInput(n, "IDX");
                    return sig + "(" + EmitExpr(i) + ")";
                }
            // F-N call (2026-07-06 batch 2) — `name(a1, a2, …)`.
            case "kmila_vhdl_call":
                {
                    var name = GetField(n, "NAME") ?? "/*fn*/";
                    var argParts = new List<string>();
                    foreach (var arg in GetStatementChain(n, "ARGS"))
                    {
                        var v = GetValueInput(arg, "VALUE");
                        argParts.Add(EmitExpr(v));
                    }
                    return name + "(" + string.Join(", ", argParts) + ")";
                }
            // 2026-07-19 Phase 10 — general aggregate. Walks ELEMS chain
            // and renders each element by its MODE.
            case "kmila_vhdl_aggregate":
                {
                    var parts = new List<string>();
                    foreach (var elem in GetStatementChain(n, "ELEMS"))
                    {
                        var mode   = GetField(elem, "MODE") ?? "positional";
                        var choice = GetField(elem, "CHOICE") ?? "";
                        var value  = EmitExpr(GetValueInput(elem, "VALUE"));
                        if (mode == "others")
                            parts.Add("others => " + value);
                        else if (mode == "indexed")
                            parts.Add(choice + " => " + value);
                        else
                            parts.Add(value);
                    }
                    return "(" + string.Join(", ", parts) + ")";
                }
            // F-12 (2026-07-05) — `(others => '0')` aggregate. VAL carries
            // the RHS literal including its quotes.
            case "kmila_vhdl_others":
                {
                    var vRaw = GetField(n, "VAL");
                    string v;
                    if (string.IsNullOrWhiteSpace(vRaw))
                    {
                        LastDiagnostics.Add(new EmitterDiagnostic(
                            _currentLine,
                            "(others => …) aggregate missing VAL — emitting '0' with visible marker",
                            BlockId: GetBlockId(n)));
                        v = "'0' /* MISSING VAL */";
                    }
                    else v = vRaw;
                    return "(others => " + v + ")";
                }
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

/// <summary>
/// 2026-07-20 Phase 23a — one silent-loss diagnostic entry. Emitted by
/// <see cref="VhdlBlockEmitter"/> at the arm that filled a
/// <c>/* MISSING X */</c> marker. Consumed by <c>Editor.razor</c>'s
/// <c>PublishEmitterDiagnosticsAsync</c>, which projects it into a
/// Monaco marker at <see cref="Line"/> with owner
/// <c>"kmila-vhdl-emit"</c>.
/// </summary>
/// <param name="Line">1-based source line the offending arm was emitting
/// when the diagnostic fired (from <c>_currentLine</c> at Add time).</param>
/// <param name="Message">Human-readable description — same string
/// content as the pre-P23a <c>List&lt;string&gt;</c> shape carried;
/// locale-invariant so existing xUnit predicates still fire.</param>
/// <param name="Severity">Monaco severity code: 8=Error, 4=Warning,
/// 2=Info. Defaults to Error since every existing site is a LOGIC-FLIP
/// hazard.</param>
/// <param name="BlockId">Block id (from workspace JSON) that produced
/// the diagnostic, when the arm has a <c>node</c> in scope; null on
/// param-loop entries that operate on chain children without a
/// containing node reference.</param>
public sealed record EmitterDiagnostic(
    int Line,
    string Message,
    int Severity = 8,
    string? BlockId = null);
