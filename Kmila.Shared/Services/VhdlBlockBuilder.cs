using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Interpreter.Models;
using Parser.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Reverse direction of <see cref="VhdlBlockEmitter"/>: takes VHDL source
/// and produces a kmila-blocks workspace JSON in the same shape that
/// <c>window.kmilaBlocklySerialize</c> returns.
///
/// Recognition strategy: regex-based text scanning, not a full parser.
/// Post the 2026-07-19 "1:1 audit" + Phase 19 (see
/// <c>Documentacion/HANDOFF_block_editor_1to1_2026-07-19.md</c>) this
/// module recognises ~60 first-class constructs across five parse loops:
/// <list type="bullet">
/// <item>Top-level: library/use/entity(with generics)/architecture/package/package_body.</item>
/// <item>Declarations: signal_decl/constant/subtype/alias/type_enum/type_array/type_record/variable_decl/attribute_decl/attribute_spec + <b>component_decl/procedure_decl/function_decl (Phase 19)</b> + comment_decl + raw_decl safety-net.</item>
/// <item>Concurrent body: process (with optional label)/assign_concurrent/cond_assign/component_inst/generate_for/generate_if/with_select + comment_concurrent.</item>
/// <item>Sequential body: assign_seq/variable_assign/if-elsif-else/case/for_loop/while_loop/loop/wait/exit/next/return/null/report/assert + <b>procedure_call (Phase 19)</b> + comment_seq.</item>
/// <item>Expressions: id/literal_bit(9 std_ulogic values)/literal_vec/eq/binop/relational/adding/multiplying/rising_edge/falling_edge/not(with paren guard)/slice/index/call/aggregate/others + raw_expr.</item>
/// </list>
///
/// Comments are first-class chain blocks in every slot — the four
/// kmila_vhdl_comment_* variants preserve source comments verbatim.
/// Raw fallback blocks (kmila_vhdl_raw_*) remain as a safety net so
/// the source survives round-trip even when the recognizer misses a
/// construct.
/// </summary>
/// <remarks>
/// Regex hazards to know about (see the handoff § "Architectural gotchas"):
/// <list type="number">
/// <item>Comments are BLANKED to spaces in <c>scan</c> (via BlankLineComments)
/// but preserved in <c>original</c>. Header regexes that use <c>\s+</c> between
/// anchors will EAT blanked comments as whitespace — always use <c>\b</c>
/// boundaries when the capture region may contain comments (see
/// <see cref="ArchitectureHeaderRe"/> and <see cref="ProcessRe"/>).</item>
/// <item>Paren-strip in <see cref="BuildExpression"/> runs after
/// <see cref="OthersAggregateRe"/> + general aggregate detection (which need
/// their outer parens) but before other atoms. Reorder at your peril.</item>
/// <item>Two <c>FlushGap</c> closures exist (in <see cref="BuildArchitecture"/>
/// and <see cref="ParseDeclarations"/>). Post-P20 audit they are
/// functionally equivalent for the two emitted block kinds
/// (<c>kmila_vhdl_comment_decl</c>, <c>kmila_vhdl_raw_decl</c>). The
/// structural difference is that <c>BuildArchitecture</c> stages tuples
/// into a <c>finalDecls</c> list so it can interleave gap-derived blocks
/// with matched-decl blocks in source order during a second pass;
/// <c>ParseDeclarations</c> materializes directly into the head chain.
/// Keep the pair parallel when adding new Tier D constructs. Collapsing
/// into a shared helper was considered in Phase 23 and rejected — the
/// two-pass staging in <c>BuildArchitecture</c> is not portable to
/// <c>ParseDeclarations</c>'s single-pass shape without either doubling
/// state in packages or introducing a materializer callback.</item>
/// <item>Phase 19 — the architecture DECLS/BODY split is NOT regex-driven
/// anymore. <see cref="ArchitectureHeaderRe"/> only matches the header up
/// to <c>is</c>; the outermost <c>begin</c> is located by
/// <see cref="FindArchitectureBegin"/>, which skips over nested
/// procedure/function/component scopes. Walkers must also skip
/// double-quoted strings (via <see cref="SkipString"/>) — VHDL testbenches
/// commonly write <c>"End rst process"</c> inside strings, and the
/// unquoted <c>process</c> would corrupt process-depth tracking.</item>
/// <item>Phase 22 (2026-07-20) — <see cref="IndexRe"/> excludes commas from
/// the index-expression to keep <c>rgb(0)</c>-style subscripts distinct
/// from function-argument commas; <see cref="TimeLiteralRe"/> captures
/// <c>5 ns</c>/<c>10 us</c>-style delays; <see cref="PowerOps"/> array
/// preserves <c>**</c> operator. JSON build uses
/// <c>JsonSerializerOptions { MaxDepth = 512 }</c> — the default 64
/// throws a misleading "object cycle detected" on 33-arm case chains.</item>
/// <item>Phase 23a (2026-07-20) — silent-loss paths now emit Monaco
/// markers under owner <c>"kmila-vhdl-emit"</c> (line-anchored, distinct
/// from parser markers). See <c>Editor.razor.PublishEmitterDiagnosticsAsync</c>.</item>
/// <item>Phase 19 — <see cref="EntityRe"/> uses a <c>\k&lt;name&gt;</c>
/// backreference for the trailing name. The previous <c>|\w+\s*</c>
/// alternative greedily matched *any* <c>end &lt;word&gt;;</c> including
/// the architecture's own <c>end &lt;archname&gt;;</c>, swallowing the
/// whole architecture into the entity's body when the entity used the
/// bare <c>end &lt;entname&gt;;</c> form. Same pattern used in
/// <see cref="ProcedureDeclRe"/> / <see cref="FunctionDeclRe"/> /
/// <see cref="ComponentDeclRe"/>.</item>
/// </list>
/// </remarks>
public class VhdlBlockBuilder
{
    // Cached to avoid allocating a JsonSerializerOptions per Serialize call (CA1869).
    // MaxDepth=512: `next`-pointer chains produce linearly deep JSON; the default
    // MaxDepth=64 trips on 100+-arm cases and deeply nested processes.
    private static readonly System.Text.Json.JsonSerializerOptions WorkspaceJsonOptions = new() { MaxDepth = 512 };

    private readonly ILogger _logger;

    /// <summary>
    /// Count of raw-VHDL fallback blocks produced by the most recent
    /// <see cref="Build"/> call. Editor.razor uses this to surface a
    /// soft "N fragmentos preservados" toast (locale key
    /// <c>EDITOR_BLOCKS_RAW_TITLE</c>) on Code→Blocks switch when > 0.
    /// Reset on every Build. Post 2026-07-19 audit, ALL 11 real gallery
    /// fixtures round-trip at 0 raws; the toast only fires on genuine
    /// unrecognized constructs.
    /// </summary>
    public int LastRawCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VhdlBlockBuilder"/> class.
    /// </summary>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public VhdlBlockBuilder(ILogger<VhdlBlockBuilder>? logger = null)
    {
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Convert a VHDL source string into a kmila-blocks workspace JSON.
    /// Returns an empty workspace (<c>{"blocks":{"blocks":[]}}</c>) when
    /// the input is null / whitespace / contains nothing the recognizer
    /// can identify or wrap. Never throws on malformed input.
    /// </summary>
    public string Build(string? vhdlSource)
    {
        LastRawCount = 0;
        if (string.IsNullOrWhiteSpace(vhdlSource)) return EmptyWorkspace;

        try
        {
            // Two parallel views into the same byte offsets: `scan` has
            // line comments replaced by spaces (so regex / keyword peeks
            // never match VHDL keywords hiding inside comment bodies);
            // `vhdlSource` is the untouched original we copy from when
            // accumulating raw-VHDL block text. Step 6 of #16 — comments
            // round-trip safely when they fall into a raw block.
            var scan = VhdlText.BlankLineComments(vhdlSource);
            var topLevel = ParseTopLevel(scan, vhdlSource);
            LastRawCount = CountRawBlocks(topLevel);

            // Lay top-level blocks out vertically at a sensible starting
            // offset so the first thing the student sees is the entity at
            // top-left, architecture below it, raw blocks further down.
            int y = 20;
            foreach (var b in topLevel)
            {
                b["x"] = 20;
                b["y"] = y;
                y += 320;
            }

            var doc = new Dictionary<string, object?>
            {
                ["blocks"] = new Dictionary<string, object?>
                {
                    ["blocks"] = topLevel
                }
            };
            // 2026-07-20 Phase 22b — `next`-pointer chains produce
            // linearly deep JSON structures: a 32-arm case block hits the
            // default MaxDepth=64 on the workspace JSON output, and
            // `JsonSerializer.Serialize` throws an "object cycle detected"
            // error (misleading — it's depth, not a cycle). Bump to 512
            // to safely handle 100+-arm cases and deeply nested processes.
            return JsonSerializer.Serialize(doc, WorkspaceJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VhdlBlockBuilder] Build failed; returning empty workspace");
            return EmptyWorkspace;
        }
    }

    private const string EmptyWorkspace = """{"blocks":{"blocks":[]}}""";

    /* ── Top-level scan ───────────────────────────────────────────── */

    // 2026-07-19 Phase 19 — trailing name matches the header via `\k<name>`
    // backreference. The previous `\w+\s*` alternative greedily matched
    // *any* `end <word>;` — including the architecture's own `end
    // <archname>;` — and swallowed the whole architecture into the
    // entity's body when the entity used the bare `end <entname>;` form
    // (common in old testbench templates like 102-counter-arst).
    private static readonly Regex EntityRe = new(
        @"\bentity\s+(?<name>\w+)\s+is\s+(?<body>.*?)\bend\s+(?:entity(?:\s+\k<name>)?|\k<name>)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 2026-07-19 Phase 19: header-only match. The prior regex captured
    // `<decls>` non-greedily up to `\bbegin\b`, but a procedure_decl /
    // function_decl body inside DECLS itself contains a `begin`, so the
    // regex truncated the DECLS section at the FIRST nested begin. Now
    // the regex only anchors on the header (`architecture X of Y is`);
    // the call site uses FindArchitectureBegin to walk forward tracking
    // subprogram / component scopes and returning the OUTERMOST begin.
    //
    // Phase 17 preserved: `is\b` (not `is\s+`) so whitespace + blanked
    // comments between `is` and the first decl fall into the DECLS
    // region reconstructed by the call site.
    private static readonly Regex ArchitectureHeaderRe = new(
        @"\barchitecture\s+(?<name>\w+)\s+of\s+(?<entity>\w+)\s+is\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private List<Dictionary<string, object?>> ParseTopLevel(string scan, string original)
    {
        var result = new List<Dictionary<string, object?>>();

        int i = 0;
        var rawAcc = new StringBuilder();

        void FlushRaw()
        {
            var text = rawAcc.ToString().Trim();
            rawAcc.Clear();
            if (text.Length == 0) return;
            result.Add(BuildRaw("kmila_vhdl_raw_top", text));
        }

        while (i < scan.Length)
        {
            while (i < scan.Length && char.IsWhiteSpace(scan[i]))
            {
                // 2026-07-19 Phase 9 — comment detection at top level.
                // BlankLineComments has replaced comment chars with spaces
                // in `scan`, so a space in scan whose corresponding char in
                // `original` is `-` starts a `-- comment`. Consume through
                // end-of-line and emit a first-class kmila_vhdl_comment_top
                // block instead of accumulating into rawAcc.
                if (i < original.Length && original[i] == '-'
                    && i + 1 < original.Length && original[i + 1] == '-')
                {
                    FlushRaw();
                    int cmtStart = i + 2; // skip past "--"
                    // Skip a single leading space after "--" (idiomatic).
                    if (cmtStart < original.Length && original[cmtStart] == ' ') cmtStart++;
                    int cmtEnd = cmtStart;
                    while (cmtEnd < original.Length
                           && original[cmtEnd] != '\n' && original[cmtEnd] != '\r') cmtEnd++;
                    var text = original.Substring(cmtStart, cmtEnd - cmtStart).TrimEnd();
                    var c = MkBlock("kmila_vhdl_comment_top");
                    AddField(c, "TEXT", text);
                    result.Add(c);
                    // Advance i past the end-of-line so the next iteration
                    // starts on the following statement.
                    i = cmtEnd;
                    while (i < original.Length
                           && (original[i] == '\n' || original[i] == '\r')) i++;
                    continue;
                }
                rawAcc.Append(original[i]);
                i++;
            }
            if (i >= scan.Length) break;

            // 2026-07-19 Phase 8 — first-class library/use clauses.
            //   library <NAMES>;   → kmila_vhdl_library with comma-sep NAMES
            //   use <PATH>;        → kmila_vhdl_use with PATH
            // Previously silently consumed; now preserved as top-level
            // blocks. The emitter drops its hard-coded `library ieee; use
            // ieee.std_logic_1164.all;` prelude when the workspace already
            // has library blocks, so byte-idempotence is maintained.
            var keyword = PeekIdent(scan, i);
            if (string.Equals(keyword, "library", StringComparison.OrdinalIgnoreCase))
            {
                int p0 = NextNonWhitespace(scan, i);
                int semi = scan.IndexOf(';', p0);
                if (semi < 0) { i = scan.Length; break; }
                var namesText = scan.Substring(p0 + "library".Length, semi - (p0 + "library".Length)).Trim();
                FlushRaw();
                var lib = MkBlock("kmila_vhdl_library");
                AddField(lib, "NAMES", namesText);
                result.Add(lib);
                i = semi + 1;
                continue;
            }
            if (string.Equals(keyword, "use", StringComparison.OrdinalIgnoreCase))
            {
                int p0 = NextNonWhitespace(scan, i);
                int semi = scan.IndexOf(';', p0);
                if (semi < 0) { i = scan.Length; break; }
                var pathText = scan.Substring(p0 + "use".Length, semi - (p0 + "use".Length)).Trim();
                FlushRaw();
                var use = MkBlock("kmila_vhdl_use");
                AddField(use, "PATH", pathText);
                result.Add(use);
                i = semi + 1;
                continue;
            }

            // 2026-07-19 Phase 8 — package / package body at top level.
            //   package <NAME> is <DECLS> end package [<NAME>];
            //   package body <NAME> is <BODY> end package body [<NAME>];
            if (string.Equals(keyword, "package", StringComparison.OrdinalIgnoreCase))
            {
                int consumedPkg = TryParsePackage(scan, original, i, out var pkgBlock);
                if (consumedPkg > 0 && pkgBlock != null)
                {
                    FlushRaw();
                    result.Add(pkgBlock);
                    i += consumedPkg;
                    continue;
                }
            }

            if (string.Equals(keyword, "entity", StringComparison.OrdinalIgnoreCase))
            {
                var m = EntityRe.Match(scan, i);
                if (m.Success && m.Index == NextNonWhitespace(scan, i))
                {
                    FlushRaw();
                    result.Add(BuildEntity(m.Groups["name"].Value, m.Groups["body"].Value));
                    i = m.Index + m.Length;
                    continue;
                }
            }

            if (string.Equals(keyword, "architecture", StringComparison.OrdinalIgnoreCase))
            {
                var m = ArchitectureHeaderRe.Match(scan, i);
                if (m.Success && m.Index == NextNonWhitespace(scan, i))
                {
                    // 2026-07-19 Phase 19 — walk forward from just after
                    // `is` looking for the outermost `begin`, skipping
                    // any `procedure/function/component` scopes we cross.
                    // Without this, a subprogram body's `begin` would
                    // truncate DECLS at the FIRST nested begin.
                    int isEndIdx = m.Index + m.Length;
                    int beginIdx = FindArchitectureBegin(scan, isEndIdx);
                    if (beginIdx < 0) { i++; continue; }

                    int bodyStart = beginIdx + 5; // past "begin"
                    var archName  = m.Groups["name"].Value;
                    int endIdx    = FindArchitectureEnd(scan, bodyStart, archName, out int endLen);
                    if (endIdx >= 0)
                    {
                        FlushRaw();
                        int declsStart = isEndIdx;
                        int declsLen   = beginIdx - isEndIdx;
                        int bodyLen    = endIdx - bodyStart;
                        var declsScan  = scan.Substring(declsStart, declsLen);
                        var bodyScan   = scan.Substring(bodyStart, bodyLen);
                        var bodyOrig   = original.Substring(bodyStart, bodyLen);
                        result.Add(BuildArchitecture(
                            archName,
                            m.Groups["entity"].Value,
                            declsScan,
                            bodyScan,
                            original.Substring(declsStart, declsLen),
                            bodyOrig));
                        i = endIdx + endLen;
                        continue;
                    }
                }
            }

            rawAcc.Append(original[i]);
            i++;
        }

        FlushRaw();
        return result;
    }

    /* ── Entity ──────────────────────────────────────────────────── */

    private static readonly Regex PortBlockRe = new(
        // Round-trip fidelity fix (2026-07-06): `.*?` was LAZY and stopped
        // at the first `)`, which is the *inner* paren of vector types
        // like `std_logic_vector(N downto 0)`. All ports past that inner
        // paren were silently dropped. Every fixture in the audit failed
        // byte-idempotence because of it. Now we locate the opening
        // `port(` head with the regex and rely on ExtractParenBody to
        // walk paren depth to the matching close.
        @"\bport\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static Dictionary<string, object?> BuildEntity(string name, string body)
    {
        var b = MkBlock("kmila_vhdl_entity");
        AddField(b, "NAME", name);

        // 2026-07-19 Phase 8 — parse `generic (…)` clause first if present.
        // The clause always appears BEFORE `port (…)` per LRM §5.2.
        var genericMatch = GenericBlockRe.Match(body);
        if (genericMatch.Success)
        {
            int gOpen = genericMatch.Index + genericMatch.Length - 1; // points at `(`
            var genBody = ExtractParenBody(body, gOpen);
            if (genBody != null)
            {
                var generics = ParseGenericList(genBody);
                if (generics != null) AddInput(b, "GENERICS", generics);
            }
        }

        var portMatch = PortBlockRe.Match(body);
        if (portMatch.Success)
        {
            int openIdx = portMatch.Index + portMatch.Length - 1; // points at `(`
            var portBody = ExtractParenBody(body, openIdx);
            if (portBody != null)
            {
                var ports = ParsePortList(portBody);
                if (ports != null) AddInput(b, "PORTS", ports);
            }
        }
        return b;
    }

    // 2026-07-19 Phase 8 — entity generic clause head.
    private static readonly Regex GenericBlockRe = new(
        @"\bgeneric\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 2026-07-19 Phase 8 — one generic parameter:
    //   <NAMES> : <TYPE> [ := <DEFAULT> ]
    private static readonly Regex GenericLineRe = new(
        @"\s*(?<names>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\)|\s+range\s+[^;:]+)?)\s*(?::=\s*(?<def>.+?))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Dictionary<string, object?>? ParseGenericList(string body)
    {
        Dictionary<string, object?>? head = null;
        foreach (var partRaw in body.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var text = partRaw.Trim();
            if (text.Length == 0) continue;
            var m = GenericLineRe.Match(text);
            if (!m.Success) continue;
            var type = NormaliseType(m.Groups["type"].Value);
            var def  = m.Groups["def"].Success ? m.Groups["def"].Value.Trim() : "";
            foreach (var raw in m.Groups["names"].Value.Split(','))
            {
                var name = raw.Trim();
                if (name.Length == 0) continue;
                var g = MkBlock("kmila_vhdl_generic");
                AddField(g, "NAME", name);
                AddField(g, "TYPE", type);
                AddField(g, "DEFAULT", def);
                head = ChainAppend(head, g);
            }
        }
        return head;
    }

    /* ── Phase 19: subprogram parameters ─────────────────────────── */
    // <class>? <names> : <mode>? <type> [ := <default> ]
    // Class defaults to blank (LRM §4.2.2.1: `constant` for mode `in`,
    // `variable` for `out`/`inout`). Mode defaults to `in`. Type accepts
    // `range` clauses (unconstrained/ranged) up to the terminator.
    private static readonly Regex SubprogParamRe = new(
        @"^\s*(?:(?<class>constant|variable|signal|file)\s+)?(?<names>\w+(?:\s*,\s*\w+)*)\s*:\s*(?:(?<mode>in|out|inout|buffer)\s+)?(?<type>[^;]+?)\s*(?::=\s*(?<default>[^;]+?))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Dictionary<string, object?>? ParseSubprogParams(string paramsBody)
    {
        Dictionary<string, object?>? head = null;
        // VHDL allows both `;` and `,` between params but `;` is canonical
        // (LRM §4.2.2). Split on `;` at depth 0.
        foreach (var partRaw in paramsBody.Split(';'))
        {
            var text = partRaw.Trim();
            if (text.Length == 0) continue;
            var m = SubprogParamRe.Match(text);
            if (!m.Success) continue;
            var cls  = m.Groups["class"].Success ? m.Groups["class"].Value.ToLowerInvariant() : "";
            var mode = m.Groups["mode"].Success  ? m.Groups["mode"].Value.ToLowerInvariant()  : "in";
            var type = m.Groups["type"].Value.Trim();
            foreach (var raw in m.Groups["names"].Value.Split(','))
            {
                var name = raw.Trim();
                if (name.Length == 0) continue;
                var p = MkBlock("kmila_vhdl_param");
                AddField(p, "CLASS", cls);
                AddField(p, "NAME",  name);
                AddField(p, "MODE",  mode);
                AddField(p, "TYPE",  type);
                head = ChainAppend(head, p);
            }
        }
        return head;
    }

    // Phase 19 — component_decl parse (shared by ParseDeclarations +
    // BuildArchitecture). The regex-captured `<body>` region contains
    // optional `is`, optional `generic (…)`, and mandatory `port (…)`.
    // Uses ExtractParenBody + ParsePortList/ParseGenericList to avoid the
    // vector-paren nesting hazard.
    private static Dictionary<string, object?> BuildComponentDeclBlock(Match m)
    {
        var b = MkBlock("kmila_vhdl_component_decl");
        AddField(b, "NAME", m.Groups["name"].Value.Trim());

        var bodyText = m.Groups["body"].Value;
        int cur = 0;
        while (cur < bodyText.Length && char.IsWhiteSpace(bodyText[cur])) cur++;
        // Optional `is` keyword after the name.
        if (cur + 2 <= bodyText.Length
            && string.Compare(bodyText, cur, "is", 0, 2, StringComparison.OrdinalIgnoreCase) == 0
            && (cur + 2 == bodyText.Length || !(char.IsLetterOrDigit(bodyText[cur + 2]) || bodyText[cur + 2] == '_')))
        {
            cur += 2;
        }

        Dictionary<string, object?>? genericsHead = null;
        var genMatch = GenericBlockRe.Match(bodyText, cur);
        if (genMatch.Success)
        {
            int gOpen = genMatch.Index + genMatch.Length - 1; // points at `(`
            var genBody = ExtractParenBody(bodyText, gOpen);
            if (genBody != null) genericsHead = ParseGenericList(genBody);
        }

        Dictionary<string, object?>? portsHead = null;
        var portMatch = PortBlockRe.Match(bodyText, cur);
        if (portMatch.Success)
        {
            int pOpen = portMatch.Index + portMatch.Length - 1;
            var portBody = ExtractParenBody(bodyText, pOpen);
            if (portBody != null) portsHead = ParsePortList(portBody);
        }

        if (genericsHead != null) AddInput(b, "GENERICS", genericsHead);
        if (portsHead    != null) AddInput(b, "PORTS",    portsHead);
        return b;
    }

    // Phase 19 — procedure_decl / function_decl parse (shared by
    // ParseDeclarations + BuildArchitecture). The regex-captured `<body>`
    // region spans everything after the header name through the matching
    // `end procedure|end function|end <name>`. We split into optional
    // params paren, optional `return <rtype>` (function only), `is`,
    // inner decls, `begin`, inner body — then recurse into
    // ParseDeclarations + ParseSequentialBody for the two chains.
    private Dictionary<string, object?> BuildSubprogramBlock(
        string blockType, Match m, string declsOriginal)
    {
        var b = MkBlock(blockType);
        AddField(b, "NAME", m.Groups["name"].Value.Trim());

        var bodyText = m.Groups["body"].Value;
        int bodyIdx  = m.Groups["body"].Index;
        // BlankLineComments preserves length, so declsOriginal at the
        // same offsets is the original view of this body region.
        var bodyOrig = (bodyIdx >= 0 && bodyIdx + bodyText.Length <= declsOriginal.Length)
            ? declsOriginal.Substring(bodyIdx, bodyText.Length)
            : bodyText;

        int cur = 0;
        while (cur < bodyText.Length && char.IsWhiteSpace(bodyText[cur])) cur++;

        // Optional (params).
        Dictionary<string, object?>? paramsHead = null;
        if (cur < bodyText.Length && bodyText[cur] == '(')
        {
            var paramsBody = ExtractParenBody(bodyText, cur);
            if (paramsBody != null)
            {
                paramsHead = ParseSubprogParams(paramsBody);
                cur += paramsBody.Length + 2; // past `(` … `)`
            }
        }

        // For functions: `return <rtype>` before `is`.
        if (blockType == "kmila_vhdl_function_decl")
        {
            int retIdx = FindKeywordAtDepth(bodyText, cur, "return", 0);
            if (retIdx >= 0)
            {
                int retStart = retIdx + 6;
                int isIdx0   = FindKeywordAtDepth(bodyText, retStart, "is", 0);
                if (isIdx0 > 0)
                {
                    AddField(b, "RETURN_TYPE",
                        bodyText.Substring(retStart, isIdx0 - retStart).Trim());
                    cur = isIdx0 + 2;
                }
                else
                {
                    // Malformed — fall back to blank RETURN_TYPE.
                    AddField(b, "RETURN_TYPE", "");
                }
            }
            else
            {
                AddField(b, "RETURN_TYPE", "");
            }
        }
        else
        {
            int isIdx = FindKeywordAtDepth(bodyText, cur, "is", 0);
            if (isIdx < 0)
            {
                if (paramsHead != null) AddInput(b, "PARAMS", paramsHead);
                return b; // malformed but survive
            }
            cur = isIdx + 2;
        }

        // Find `begin`.
        int beginIdx = FindKeywordAtDepth(bodyText, cur, "begin", 0);
        if (beginIdx < 0)
        {
            if (paramsHead != null) AddInput(b, "PARAMS", paramsHead);
            return b; // malformed but survive
        }

        int innerDeclsStart = cur;
        int innerDeclsEnd   = beginIdx;
        int innerBodyStart  = beginIdx + 5;
        int innerBodyEnd    = bodyText.Length;

        var innerDeclsScan = bodyText.Substring(innerDeclsStart, innerDeclsEnd - innerDeclsStart);
        var innerDeclsOrig = bodyOrig.Substring(innerDeclsStart, innerDeclsEnd - innerDeclsStart);
        var innerDeclsHead = ParseDeclarations(innerDeclsScan, innerDeclsOrig);

        var innerBodyScan = bodyText.Substring(innerBodyStart, innerBodyEnd - innerBodyStart);
        var innerBodyOrig = bodyOrig.Substring(innerBodyStart, innerBodyEnd - innerBodyStart);
        var innerBodyHead = ParseSequentialBody(innerBodyScan, innerBodyOrig);

        if (paramsHead     != null) AddInput(b, "PARAMS", paramsHead);
        if (innerDeclsHead != null) AddInput(b, "DECLS",  innerDeclsHead);
        if (innerBodyHead  != null) AddInput(b, "BODY",   innerBodyHead);
        return b;
    }

    // 2026-07-19 Phase 8 — parse `package <NAME> is <DECLS> end package
    // [<NAME>];` or `package body <NAME> is <BODY> end package body
    // [<NAME>];`. Returns chars consumed (0 on malformed).
    private int TryParsePackage(
        string scan, string original, int start,
        out Dictionary<string, object?>? block)
    {
        block = null;
        int p = NextNonWhitespace(scan, start);
        if (!ConsumeKw(scan, ref p, "package")) return 0;
        // Detect `body`.
        int savedP = p;
        while (p < scan.Length && char.IsWhiteSpace(scan[p])) p++;
        bool isBody = false;
        var nextKw = PeekIdent(scan, p);
        if (string.Equals(nextKw, "body", StringComparison.OrdinalIgnoreCase))
        {
            isBody = true;
            p += "body".Length;
        }
        // <NAME>
        while (p < scan.Length && char.IsWhiteSpace(scan[p])) p++;
        var name = PeekIdent(scan, p);
        if (string.IsNullOrEmpty(name)) return 0;
        p += name.Length;
        while (p < scan.Length && char.IsWhiteSpace(scan[p])) p++;
        // `is`
        if (!ConsumeKw(scan, ref p, "is")) return 0;

        // Find matching `end package [body] [<NAME>];`
        int cur = p;
        int endIdx = -1;
        while (cur < scan.Length)
        {
            int e = FindKeywordAtDepth(scan, cur, "end", 0);
            if (e < 0) break;
            int q = e + 3;
            while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
            // Accept `end;` (LRM §4.7 allows bare `end;`) or `end package [body];`.
            if (q < scan.Length && scan[q] == ';')
            {
                endIdx = q + 1;
                break;
            }
            var kw2 = PeekIdent(scan, q);
            if (string.Equals(kw2, "package", StringComparison.OrdinalIgnoreCase))
            {
                q += "package".Length;
                while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
                if (isBody)
                {
                    var kw3 = PeekIdent(scan, q);
                    if (string.Equals(kw3, "body", StringComparison.OrdinalIgnoreCase))
                    {
                        q += "body".Length;
                        while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
                    }
                }
                // Optional trailing name
                var trailing = PeekIdent(scan, q);
                if (!string.IsNullOrEmpty(trailing) && trailing != ";")
                {
                    q += trailing.Length;
                    while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
                }
                if (q < scan.Length && scan[q] == ';')
                {
                    endIdx = q + 1;
                    break;
                }
            }
            cur = e + 3;
        }
        if (endIdx < 0) return 0;

        // Body span: from p (past `is`) to before the `end` we matched.
        // Locate the `end` keyword position (before endIdx).
        // Simpler: everything from p up to the last `end` matched — but we
        // don't have that position saved separately. Reconstruct by finding
        // the `end` keyword that endIdx completes; iterate backward.
        int endKwStart = endIdx - 1;
        while (endKwStart > p && scan[endKwStart] != ';') endKwStart--;
        // Walk back from `;` to find `end` — the `end` keyword starts 3 chars
        // before the first non-whitespace tokenPrefix. Cheap heuristic: scan
        // backward for the `end` word at depth 0.
        int walk = endKwStart;
        while (walk > p)
        {
            if (walk + 3 < scan.Length
                && string.Compare(scan, walk, "end", 0, 3, StringComparison.OrdinalIgnoreCase) == 0
                && (walk == 0 || !char.IsLetterOrDigit(scan[walk - 1]) && scan[walk - 1] != '_')
                && !char.IsLetterOrDigit(scan[walk + 3]) && scan[walk + 3] != '_')
            {
                break;
            }
            walk--;
        }
        int declsStart = p;
        int declsEnd   = walk;
        while (declsEnd > declsStart && char.IsWhiteSpace(scan[declsEnd - 1])) declsEnd--;

        // Parse the decls region using the same interleaved matcher used by
        // BuildArchitecture (Phase 7). Cheapest path: reuse BuildArchitecture's
        // logic by constructing a synthetic architecture wrapper. But that
        // couples the two. Simpler: emit each Tier D match directly here.
        // For now, package DECLS shares the DECLS chain slot (kmila_vhdl_decl),
        // so we recurse via the arch parser's core loop. Extract a helper.
        var declsScan = scan.Substring(declsStart, declsEnd - declsStart);
        var declsOrig = original.Substring(declsStart,
            Math.Min(declsEnd, original.Length) - declsStart);
        var declsHead = ParseDeclarations(declsScan, declsOrig);

        var pkgBlock = MkBlock(isBody ? "kmila_vhdl_package_body" : "kmila_vhdl_package");
        AddField(pkgBlock, "NAME", name);
        if (declsHead != null)
            AddInput(pkgBlock, isBody ? "BODY" : "DECLS", declsHead);
        block = pkgBlock;
        return endIdx - start;
    }

    // 2026-07-19 Phase 8 — reusable decls parser (Tier D + signal + type_enum
    // + raw_decl catch-all). Extracted from BuildArchitecture so packages
    // can share the same logic.
    private Dictionary<string, object?>? ParseDeclarations(string decls, string declsOriginal)
    {
        var declMatches = new List<(int Index, int Length, Match Match, string Kind)>();
        foreach (Match m in SignalDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "signal"));
        foreach (Match m in TypeArrayRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "type_array"));
        foreach (Match m in TypeRecordRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "type_record"));
        foreach (Match m in TypeEnumRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "type_enum"));
        foreach (Match m in ConstantRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "constant"));
        foreach (Match m in SubtypeRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "subtype"));
        foreach (Match m in AliasRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "alias"));
        foreach (Match m in VariableDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "variable"));
        foreach (Match m in AttributeSpecRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "attribute_spec"));
        foreach (Match m in AttributeDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "attribute_decl"));
        // 2026-07-19 Phase 19 — subprogram + component declarations.
        // These are multi-line envelopes; must sort ahead of Tier D
        // point declarations via the (Index,-Length) tie-break so they
        // win overlap resolution against any nested single-line construct
        // (e.g. a `variable v : T;` inside a procedure body would
        // otherwise be seen as a top-level match).
        foreach (Match m in ComponentDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "component_decl"));
        foreach (Match m in ProcedureDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "procedure_decl"));
        foreach (Match m in FunctionDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "function_decl"));

        declMatches.Sort((a, x) =>
        {
            int c = a.Index.CompareTo(x.Index);
            return c != 0 ? c : x.Length.CompareTo(a.Length);
        });
        var accepted = new List<(int Index, int Length, Match Match, string Kind)>();
        int lastEnd = -1;
        foreach (var dm in declMatches)
        {
            if (dm.Index < lastEnd) continue;
            accepted.Add(dm);
            lastEnd = dm.Index + dm.Length;
        }

        Dictionary<string, object?>? declsHead = null;
        int cursor = 0;
        void FlushGap(int end)
        {
            if (end <= cursor) return;
            var slice = decls.Substring(cursor, end - cursor);
            var origSlice = cursor < declsOriginal.Length
                ? declsOriginal.Substring(cursor,
                    Math.Min(end, declsOriginal.Length) - cursor)
                : slice;
            int gStart = 0;
            for (int k = 0; k < slice.Length; k++)
            {
                // 2026-07-19 Phase 9 — detect `--` in package decls too.
                if (k < origSlice.Length && origSlice[k] == '-'
                    && k + 1 < origSlice.Length && origSlice[k + 1] == '-')
                {
                    int cmtStart = k + 2;
                    if (cmtStart < origSlice.Length && origSlice[cmtStart] == ' ') cmtStart++;
                    int cmtEnd = cmtStart;
                    while (cmtEnd < origSlice.Length
                           && origSlice[cmtEnd] != '\n' && origSlice[cmtEnd] != '\r') cmtEnd++;
                    var cmtText = origSlice.Substring(cmtStart, cmtEnd - cmtStart).TrimEnd();
                    var c = MkBlock("kmila_vhdl_comment_decl");
                    AddField(c, "TEXT", cmtText);
                    declsHead = ChainAppend(declsHead, c);
                    k = cmtEnd;
                    while (k < slice.Length
                           && (origSlice[k] == '\n' || origSlice[k] == '\r')) k++;
                    gStart = k;
                    k--;
                    continue;
                }
                if (slice[k] == ';')
                {
                    var stmt = origSlice.Substring(gStart, k - gStart + 1).Trim();
                    if (stmt.Length > 0)
                    {
                        var raw = MkBlock("kmila_vhdl_raw_decl");
                        AddField(raw, "TEXT", stmt);
                        declsHead = ChainAppend(declsHead, raw);
                    }
                    gStart = k + 1;
                }
            }
        }
        foreach (var dm in accepted)
        {
            FlushGap(dm.Index);
            var m = dm.Match;
            var kind = dm.Kind;
            if (kind == "signal")
            {
                var names = m.Groups["names"].Value;
                var capturedType = NormaliseType(m.Groups["type"].Value);
                var initValue = m.Groups["init"].Success ? m.Groups["init"].Value.Trim() : null;
                foreach (var raw in names.Split(','))
                {
                    var nm = raw.Trim();
                    if (nm.Length == 0) continue;
                    var d = MkBlock("kmila_vhdl_signal_decl");
                    AddField(d, "NAME", nm);
                    AddField(d, "TYPE", capturedType);
                    if (!string.IsNullOrEmpty(initValue))
                        AddField(d, "INIT", initValue);
                    declsHead = ChainAppend(declsHead, d);
                }
            }
            else if (kind == "type_enum")
            {
                var typeName = m.Groups["name"].Value.Trim();
                var enumBlock = MkBlock("kmila_vhdl_type_enum");
                AddField(enumBlock, "NAME", typeName);
                Dictionary<string, object?>? memberHead = null;
                foreach (var raw in m.Groups["members"].Value.Split(','))
                {
                    var mn = raw.Trim();
                    if (mn.Length == 0) continue;
                    var mb = MkBlock("kmila_vhdl_enum_member");
                    AddField(mb, "NAME", mn);
                    memberHead = ChainAppend(memberHead, mb);
                }
                if (memberHead != null) AddInput(enumBlock, "MEMBERS", memberHead);
                declsHead = ChainAppend(declsHead, enumBlock);
            }
            else if (kind == "constant")
            {
                var d = MkBlock("kmila_vhdl_constant");
                AddField(d, "NAME",  m.Groups["name"].Value.Trim());
                AddField(d, "TYPE",  NormaliseType(m.Groups["type"].Value));
                AddField(d, "VALUE", m.Groups["value"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "subtype")
            {
                var d = MkBlock("kmila_vhdl_subtype");
                AddField(d, "NAME",      m.Groups["name"].Value.Trim());
                AddField(d, "TYPE_EXPR", m.Groups["type"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "alias")
            {
                var d = MkBlock("kmila_vhdl_alias");
                AddField(d, "NAME",   m.Groups["name"].Value.Trim());
                AddField(d, "TARGET", m.Groups["target"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "type_array")
            {
                var d = MkBlock("kmila_vhdl_type_array");
                AddField(d, "NAME",      m.Groups["name"].Value.Trim());
                AddField(d, "RANGE",     m.Groups["range"].Value.Trim());
                AddField(d, "ELEM_TYPE", m.Groups["elem"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "type_record")
            {
                var d = MkBlock("kmila_vhdl_type_record");
                AddField(d, "NAME", m.Groups["name"].Value.Trim());
                var bodyText = m.Groups["body"].Value;
                Dictionary<string, object?>? fieldsHead = null;
                var fieldRe = new Regex(
                    @"^\s*(?<n>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<t>[^;]+?)\s*$",
                    RegexOptions.Compiled);
                foreach (var partRaw in bodyText.Split(';'))
                {
                    var part = partRaw.Trim();
                    if (part.Length == 0) continue;
                    var fm = fieldRe.Match(part);
                    if (!fm.Success) continue;
                    var fType = fm.Groups["t"].Value.Trim();
                    foreach (var fRaw in fm.Groups["n"].Value.Split(','))
                    {
                        var fName = fRaw.Trim();
                        if (fName.Length == 0) continue;
                        var f = MkBlock("kmila_vhdl_record_field");
                        AddField(f, "NAME", fName);
                        AddField(f, "TYPE", fType);
                        fieldsHead = ChainAppend(fieldsHead, f);
                    }
                }
                if (fieldsHead != null) AddInput(d, "FIELDS", fieldsHead);
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "variable")
            {
                var names = m.Groups["names"].Value;
                var capturedType = NormaliseType(m.Groups["type"].Value);
                var initValue = m.Groups["init"].Success ? m.Groups["init"].Value.Trim() : "";
                foreach (var raw in names.Split(','))
                {
                    var nm = raw.Trim();
                    if (nm.Length == 0) continue;
                    var d = MkBlock("kmila_vhdl_variable_decl");
                    AddField(d, "NAME", nm);
                    AddField(d, "TYPE", capturedType);
                    AddField(d, "INIT", initValue);
                    declsHead = ChainAppend(declsHead, d);
                }
            }
            else if (kind == "attribute_spec")
            {
                var d = MkBlock("kmila_vhdl_attribute_spec");
                AddField(d, "NAME",   m.Groups["name"].Value.Trim());
                AddField(d, "TARGET", m.Groups["target"].Value.Trim());
                AddField(d, "CLASS",  m.Groups["class"].Value.Trim());
                AddField(d, "VALUE",  m.Groups["value"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "attribute_decl")
            {
                var d = MkBlock("kmila_vhdl_attribute_decl");
                AddField(d, "NAME", m.Groups["name"].Value.Trim());
                AddField(d, "TYPE", m.Groups["type"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            // 2026-07-19 Phase 19 — component / procedure / function
            // declarations. Each helper method handles inner splitting
            // (params, generic/port maps, decls/body regions).
            else if (kind == "component_decl")
            {
                declsHead = ChainAppend(declsHead, BuildComponentDeclBlock(m));
            }
            else if (kind == "procedure_decl")
            {
                declsHead = ChainAppend(declsHead,
                    BuildSubprogramBlock("kmila_vhdl_procedure_decl", m, declsOriginal));
            }
            else if (kind == "function_decl")
            {
                declsHead = ChainAppend(declsHead,
                    BuildSubprogramBlock("kmila_vhdl_function_decl", m, declsOriginal));
            }
            cursor = dm.Index + dm.Length;
        }
        FlushGap(decls.Length);
        return declsHead;
    }

    // Given a source string and the index of an opening `(`, walk paren
    // depth and return the inner body (without the outer parens) up to the
    // matching close. Returns null if no balanced close is found.
    private static string? ExtractParenBody(string src, int openParenIdx)
    {
        if (openParenIdx < 0 || openParenIdx >= src.Length || src[openParenIdx] != '(')
            return null;
        int depth = 1;
        int i = openParenIdx + 1;
        while (i < src.Length && depth > 0)
        {
            char c = src[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            if (depth == 0) break;
            i++;
        }
        if (depth != 0) return null;
        return src.Substring(openParenIdx + 1, i - openParenIdx - 1);
    }

    // 2026-07-19 1:1 audit — `<name>` widened to accept comma-separated
    // names (`y, z : out std_logic`). Splitting happens inside
    // ParsePortList so each comma-separated identifier becomes its own
    // kmila_vhdl_port block, matching signal_decl behavior and preventing
    // the previous synthetic-port data-corruption fallback.
    private static readonly Regex PortLineRe = new(
        @"\s*(?<name>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<dir>in|out|inout|buffer|linkage)\b\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\))?)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static Dictionary<string, object?>? ParsePortList(string body)
    {
        // Top-level split on ';' (no nesting expected at this depth).
        var parts = body.Split(';', StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, object?>? head = null;
        foreach (var part in parts)
        {
            var text = part.Trim();
            if (text.Length == 0) continue;
            var m = PortLineRe.Match(text);
            if (m.Success && m.Length == text.Length)
            {
                var dir = m.Groups["dir"].Value.ToLowerInvariant();
                var type = NormaliseType(m.Groups["type"].Value);
                var names = m.Groups["name"].Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in names)
                {
                    var name = raw.Trim();
                    if (name.Length == 0) continue;
                    var portB = MkBlock("kmila_vhdl_port");
                    AddField(portB, "NAME", name);
                    AddField(portB, "DIR",  dir);
                    // F-1 fix (2026-06-28): use the regex-captured type, not a
                    // hard-coded `std_logic`. PortLineRe already accepts the
                    // optional `(N downto 0)` suffix, so vector bus widths and
                    // unsigned/signed/integer types now round-trip intact.
                    AddField(portB, "TYPE", type);
                    head = ChainAppend(head, portB);
                }
            }
            else
            {
                // Unrecognized port line — keep verbatim as a no-op port
                // (Phase 0 doesn't have a kmila_vhdl_raw_port slot type).
                // Best fallback: emit a synthetic port whose name is the
                // raw text so the user still sees their data.
                var portB = MkBlock("kmila_vhdl_port");
                AddField(portB, "NAME", text);
                AddField(portB, "DIR",  "in");
                AddField(portB, "TYPE", "std_logic");
                head = ChainAppend(head, portB);
            }
        }
        return head;
    }

    /* ── Architecture ────────────────────────────────────────────── */

    // Accept the full common signal-decl forms:
    //   signal x : T;
    //   signal x : T := '0';
    //   signal a, b, c : T := '0';
    // The `names` capture is the entire comma-separated list; it gets
    // split into per-name signal_decl blocks below. F-18 fix (2026-07-06
    // batch 2): the `:= <init>` clause is now captured into the `init`
    // group and preserved as the block's INIT field, closing the silent
    // data loss on kitchen-sink's aggregate initializers and any
    // fixture that hand-authored a starting value.
    private static readonly Regex SignalDeclRe = new(
        // F-1 fix (2026-06-28): widen <type> to accept `std_logic_vector(N
        // downto 0)`, `unsigned(7 downto 0)`, etc. — same suffix shape as
        // PortLineRe. Without this, signals silently downgraded to scalar
        // `std_logic` and the round-trip emitted a structurally wrong
        // architecture.
        //
        // `init` supports the balanced-paren aggregate form `(others =>
        // '0')` because `[^;]` accepts parens; the regex is
        // non-greedy up to `;` so paren depth doesn't need explicit
        // tracking here.
        // 2026-07-19 Phase 7: type widened to accept subtype constraints
        // like `integer range 0 to 5` and `bit_vector(a to b)`. Range
        // clauses match through the next `;` / `:=` — never crossing a
        // statement boundary.
        @"\bsignal\s+(?<names>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\)|\s+range\s+[^;:]+)?)\s*(?::=\s*(?<init>[^;]+?))?\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // F-N enum type decl (2026-07-06 batch 2): `type state_t is (S_RED,
    // S_GREEN, S_YELLOW);`. Members are captured as one comma-separated
    // string; ParseArchitecture splits and builds one enum_member block
    // per name.
    private static readonly Regex TypeEnumRe = new(
        @"\btype\s+(?<name>\w+)\s+is\s*\(\s*(?<members>[^)]+)\)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 2026-07-19 Phase 7 — Tier D declarations (constant / subtype / alias
    // / type array / type record / variable / attribute decl+spec). Each
    // is a first-class block; anything else in the DECLS section that
    // doesn't match falls into kmila_vhdl_raw_decl (never silently dropped).

    // constant NAME : TYPE := VALUE;
    // TYPE accepts `integer range 0 to 5` like SignalDeclRe. VALUE is
    // paren-tolerant via [^;]+? (aggregate constants like `(others => '0')`
    // and `("00", "11", "00", "11")` fit; `;` is the only forbidden char).
    private static readonly Regex ConstantRe = new(
        @"\bconstant\s+(?<name>\w+)\s*:\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\)|\s+range\s+[^;:]+)?)\s*:=\s*(?<value>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // subtype NAME is TYPE_EXPR;   (TYPE_EXPR can contain range clauses,
    // so we swallow everything up to `;` at depth 0 — but no nested
    // structures require depth tracking here since subtypes are one-line.)
    private static readonly Regex SubtypeRe = new(
        @"\bsubtype\s+(?<name>\w+)\s+is\s+(?<type>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // alias NAME is TARGET;
    private static readonly Regex AliasRe = new(
        @"\balias\s+(?<name>\w+)\s+is\s+(?<target>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // type NAME is array (RANGE) of ELEM_TYPE;
    private static readonly Regex TypeArrayRe = new(
        @"\btype\s+(?<name>\w+)\s+is\s+array\s*\(\s*(?<range>[^)]+?)\s*\)\s+of\s+(?<elem>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // type NAME is record … end record [NAME];   (singleline mode so `.`
    // spans newlines — record bodies are almost always multi-line).
    private static readonly Regex TypeRecordRe = new(
        @"\btype\s+(?<name>\w+)\s+is\s+record\b(?<body>.*?)\bend\s+record\s*(?:\w+\s*)?;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // variable NAME : TYPE [ := INIT ];   (accepts multi-name form too)
    private static readonly Regex VariableDeclRe = new(
        @"\bvariable\s+(?<names>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\)|\s+range\s+[^;:]+)?)\s*(?::=\s*(?<init>[^;]+?))?\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // attribute NAME : TYPE;   (declaration form — no `of`)
    private static readonly Regex AttributeDeclRe = new(
        @"\battribute\s+(?<name>\w+)\s*:\s*(?<type>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // attribute NAME of TARGET : CLASS is VALUE;   (specification form)
    private static readonly Regex AttributeSpecRe = new(
        @"\battribute\s+(?<name>\w+)\s+of\s+(?<target>[^:]+?)\s*:\s*(?<class>\w+)\s+is\s+(?<value>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 2026-07-19 Phase 19 — component declaration (arch/package DECLS
    // section). Distinct from Phase 4's kmila_vhdl_component_inst (body-
    // level instantiation with LABEL + maps). Testbenches use this to
    // declare the UUT interface before instantiating below. The whole
    // scope is captured non-greedy up to `end component [name];`; the
    // parse branch uses ExtractParenBody to pull out the optional
    // `generic(...)` and mandatory `port(...)` clauses so nested vector
    // parens like `std_logic_vector(7 downto 0)` don't confuse capture.
    // Backreference `\k<name>` requires trailing name (if present) to
    // equal the header name.
    private static readonly Regex ComponentDeclRe = new(
        @"\bcomponent\s+(?<name>\w+)(?<body>.*?)\bend\s+component(?:\s+\k<name>)?\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 2026-07-19 Phase 19 — procedure declaration (arch/package DECLS
    // section). Full body form:
    //   procedure <name>[(<params>)] is <decls> begin <body>
    //   end [procedure [<name>] | <name>];
    // Params/decls/body are captured as one <body> region and split
    // inside the parse branch — the outer regex only anchors on the
    // procedure/end-procedure envelope. Nested `end if`/`end case`/
    // `end loop` inside the body don't collide with `end procedure` or
    // `end <name>` (VHDL reserved words can't be identifier names), so
    // the non-greedy `.*?` terminates on the correct token.
    private static readonly Regex ProcedureDeclRe = new(
        @"\bprocedure\s+(?<name>\w+)(?<body>.*?)\bend\s+(?:procedure(?:\s+\k<name>)?|\k<name>)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 2026-07-19 Phase 19 — function declaration (arch/package DECLS
    // section). Same envelope shape as procedure_decl; the parse branch
    // splits out the `return <rtype>` clause between the params paren
    // and the `is` keyword.
    private static readonly Regex FunctionDeclRe = new(
        @"\bfunction\s+(?<name>\w+)(?<body>.*?)\bend\s+(?:function(?:\s+\k<name>)?|\k<name>)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Normalize a captured VHDL type by collapsing internal whitespace and
    // lowercasing the base keyword. Keeps `std_logic_vector(7 downto 0)`
    // recognisable while tolerating sloppy input like `STD_LOGIC_VECTOR( 7
    // DOWNTO 0 )`.
    private static string NormaliseType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "std_logic";
        var t = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
        // Lowercase the leading identifier only; keep numeric ranges verbatim
        var paren = t.IndexOf('(', StringComparison.Ordinal);
        if (paren < 0) return t.ToLowerInvariant();
        return t.Substring(0, paren).ToLowerInvariant() + t.Substring(paren);
    }

    private Dictionary<string, object?> BuildArchitecture(
        string name, string entity, string decls, string body,
        string declsOriginal, string bodyOriginal)
    {
        var b = MkBlock("kmila_vhdl_architecture");
        AddField(b, "NAME", name);
        AddField(b, "OF",   entity);

        // Declarations (Phase 0: only signal declarations recognized).
        // Multi-name lines like `signal a, b, c : T;` expand into one
        // signal_decl block per name — the block catalog has no
        // multi-name variant.
        // F-N enum decls (2026-07-06 batch 2): interleave signal_decl
        // and type_enum matches by source position so declarations
        // appear in the emitted VHDL in the same order the student
        // wrote them. Without ordering, a `type state_t` written above
        // a `signal st : state_t := S_RED;` would emit AFTER the signal
        // that depends on it, producing a source-order regression.
        var declMatches = new List<(int Index, int Length, Match Match, string Kind)>();
        foreach (Match m in SignalDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "signal"));
        // 2026-07-19 Phase 7 — the type-* regexes are ordered by
        // specificity (array/record before generic enum) so `type mem_t
        // is array…` isn't misread as an enum head. In practice enums
        // start with `is (` and arrays/records start with `is array`/`is
        // record`, so overlap is minimal, but interleaving needs an
        // explicit priority just in case.
        foreach (Match m in TypeArrayRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "type_array"));
        foreach (Match m in TypeRecordRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "type_record"));
        foreach (Match m in TypeEnumRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "type_enum"));
        foreach (Match m in ConstantRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "constant"));
        foreach (Match m in SubtypeRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "subtype"));
        foreach (Match m in AliasRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "alias"));
        foreach (Match m in VariableDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "variable"));
        // attribute-spec has the more specific pattern (`of`) so match it
        // first; anything left over is the plain declaration form.
        foreach (Match m in AttributeSpecRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "attribute_spec"));
        foreach (Match m in AttributeDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "attribute_decl"));
        // 2026-07-19 Phase 19 — subprogram + component declarations.
        // Multi-line envelopes; (Index,-Length) tie-break puts them
        // ahead of any nested single-line decl they contain.
        foreach (Match m in ComponentDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "component_decl"));
        foreach (Match m in ProcedureDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "procedure_decl"));
        foreach (Match m in FunctionDeclRe.Matches(decls))
            declMatches.Add((m.Index, m.Length, m, "function_decl"));

        // Drop overlapping matches — later (longer/more-specific) wins
        // when two regexes both accept the same span. Sort by (Index,
        // -Length) so longer matches sort first at the same start; then
        // iterate and skip any match whose span overlaps a previously
        // accepted one.
        declMatches.Sort((a, x) =>
        {
            int c = a.Index.CompareTo(x.Index);
            return c != 0 ? c : x.Length.CompareTo(a.Length);
        });
        var accepted = new List<(int Index, int Length, Match Match, string Kind)>();
        int lastEnd = -1;
        foreach (var dm in declMatches)
        {
            if (dm.Index < lastEnd) continue; // overlaps previous
            accepted.Add(dm);
            lastEnd = dm.Index + dm.Length;
        }

        // 2026-07-19 Phase 7 — sweep every gap between accepted matches
        // that contains a `;` and turn each unrecognized statement into
        // a kmila_vhdl_raw_decl block. This closes the silent-drop hole
        // for constructs the recognizer doesn't cover.
        var finalDecls = new List<(int Index, int Length, Match? Match, string Kind, string RawText)>();
        int cursor = 0;
        void FlushGap(int end)
        {
            if (end <= cursor) return;
            var slice = decls.Substring(cursor, end - cursor);
            var origSlice = cursor < declsOriginal.Length
                ? declsOriginal.Substring(cursor, Math.Min(end, declsOriginal.Length) - cursor)
                : slice;
            // 2026-07-19 Phase 9 — treat `--` at line-start as a comment.
            // Walk the gap; a run of comment lines becomes one
            // kmila_vhdl_comment_decl per line, statements terminated by
            // `;` become raw_decl.
            int gStart = 0;
            for (int k = 0; k < slice.Length; k++)
            {
                // Detect `--` in original slice (scan slice has spaces).
                if (k < origSlice.Length && origSlice[k] == '-'
                    && k + 1 < origSlice.Length && origSlice[k + 1] == '-')
                {
                    // Emit any preceding whitespace-only prefix (nothing to
                    // do — we just advance gStart past this comment).
                    int cmtStart = k + 2;
                    if (cmtStart < origSlice.Length && origSlice[cmtStart] == ' ') cmtStart++;
                    int cmtEnd = cmtStart;
                    while (cmtEnd < origSlice.Length
                           && origSlice[cmtEnd] != '\n' && origSlice[cmtEnd] != '\r') cmtEnd++;
                    var cmtText = origSlice.Substring(cmtStart, cmtEnd - cmtStart).TrimEnd();
                    finalDecls.Add((cursor + k, cmtEnd - k, null, "comment_decl", cmtText));
                    k = cmtEnd; // continue past the line
                    while (k < slice.Length
                           && (origSlice[k] == '\n' || origSlice[k] == '\r')) k++;
                    gStart = k;
                    k--; // for-loop k++ will advance
                    continue;
                }
                if (slice[k] == ';')
                {
                    var stmt = origSlice.Substring(gStart, k - gStart + 1).Trim();
                    if (stmt.Length > 0)
                        finalDecls.Add((cursor + gStart, k + 1 - gStart, null, "raw_decl", stmt));
                    gStart = k + 1;
                }
            }
            // Trailing text with no `;` — skip (whitespace).
        }
        foreach (var dm in accepted)
        {
            FlushGap(dm.Index);
            finalDecls.Add((dm.Index, dm.Length, dm.Match, dm.Kind, ""));
            cursor = dm.Index + dm.Length;
        }
        FlushGap(decls.Length);

        Dictionary<string, object?>? declsHead = null;
        foreach (var (_, _, m, kind, rawText) in finalDecls)
        {
            if (kind == "signal")
            {
                var names = m!.Groups["names"].Value;
                var capturedType = NormaliseType(m.Groups["type"].Value);
                // F-18 (2026-07-06 batch 2): capture the `:= <init>` clause
                // if present. VHDL allows `signal a, b : T := <init>;` which
                // initializes every listed name to the same value, so all
                // split blocks share the same INIT string.
                var initGroup = m.Groups["init"];
                var initValue = initGroup.Success ? initGroup.Value.Trim() : null;
                foreach (var raw in names.Split(','))
                {
                    var nm = raw.Trim();
                    if (nm.Length == 0) continue;
                    var d = MkBlock("kmila_vhdl_signal_decl");
                    AddField(d, "NAME", nm);
                    AddField(d, "TYPE", capturedType);
                    if (!string.IsNullOrEmpty(initValue))
                        AddField(d, "INIT", initValue);
                    declsHead = ChainAppend(declsHead, d);
                }
            }
            else if (kind == "type_enum")
            {
                var typeName = m!.Groups["name"].Value.Trim();
                var membersRaw = m.Groups["members"].Value;
                var enumBlock = MkBlock("kmila_vhdl_type_enum");
                AddField(enumBlock, "NAME", typeName);
                Dictionary<string, object?>? memberHead = null;
                foreach (var raw in membersRaw.Split(','))
                {
                    var mn = raw.Trim();
                    if (mn.Length == 0) continue;
                    var mb = MkBlock("kmila_vhdl_enum_member");
                    AddField(mb, "NAME", mn);
                    memberHead = ChainAppend(memberHead, mb);
                }
                if (memberHead != null) AddInput(enumBlock, "MEMBERS", memberHead);
                declsHead = ChainAppend(declsHead, enumBlock);
            }
            else if (kind == "constant")
            {
                var d = MkBlock("kmila_vhdl_constant");
                AddField(d, "NAME",  m!.Groups["name"].Value.Trim());
                AddField(d, "TYPE",  NormaliseType(m.Groups["type"].Value));
                AddField(d, "VALUE", m.Groups["value"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "subtype")
            {
                var d = MkBlock("kmila_vhdl_subtype");
                AddField(d, "NAME",      m!.Groups["name"].Value.Trim());
                AddField(d, "TYPE_EXPR", m.Groups["type"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "alias")
            {
                var d = MkBlock("kmila_vhdl_alias");
                AddField(d, "NAME",   m!.Groups["name"].Value.Trim());
                AddField(d, "TARGET", m.Groups["target"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "type_array")
            {
                var d = MkBlock("kmila_vhdl_type_array");
                AddField(d, "NAME",      m!.Groups["name"].Value.Trim());
                AddField(d, "RANGE",     m.Groups["range"].Value.Trim());
                AddField(d, "ELEM_TYPE", m.Groups["elem"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "type_record")
            {
                var d = MkBlock("kmila_vhdl_type_record");
                AddField(d, "NAME", m!.Groups["name"].Value.Trim());
                var bodyText = m.Groups["body"].Value;
                // Body is `<name> : <type>; <name> : <type>; …` — split
                // on top-level `;`, per-part regex `<name>\s*:\s*<type>`.
                Dictionary<string, object?>? fieldsHead = null;
                var fieldRe = new Regex(
                    @"^\s*(?<n>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<t>[^;]+?)\s*$",
                    RegexOptions.Compiled);
                foreach (var partRaw in bodyText.Split(';'))
                {
                    var part = partRaw.Trim();
                    if (part.Length == 0) continue;
                    var fm = fieldRe.Match(part);
                    if (!fm.Success) continue;
                    var fType = fm.Groups["t"].Value.Trim();
                    foreach (var fRaw in fm.Groups["n"].Value.Split(','))
                    {
                        var fName = fRaw.Trim();
                        if (fName.Length == 0) continue;
                        var f = MkBlock("kmila_vhdl_record_field");
                        AddField(f, "NAME", fName);
                        AddField(f, "TYPE", fType);
                        fieldsHead = ChainAppend(fieldsHead, f);
                    }
                }
                if (fieldsHead != null) AddInput(d, "FIELDS", fieldsHead);
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "variable")
            {
                var names = m!.Groups["names"].Value;
                var capturedType = NormaliseType(m.Groups["type"].Value);
                var initGroup = m.Groups["init"];
                var initValue = initGroup.Success ? initGroup.Value.Trim() : "";
                foreach (var raw in names.Split(','))
                {
                    var nm = raw.Trim();
                    if (nm.Length == 0) continue;
                    var d = MkBlock("kmila_vhdl_variable_decl");
                    AddField(d, "NAME", nm);
                    AddField(d, "TYPE", capturedType);
                    AddField(d, "INIT", initValue);
                    declsHead = ChainAppend(declsHead, d);
                }
            }
            else if (kind == "attribute_spec")
            {
                var d = MkBlock("kmila_vhdl_attribute_spec");
                AddField(d, "NAME",   m!.Groups["name"].Value.Trim());
                AddField(d, "TARGET", m.Groups["target"].Value.Trim());
                AddField(d, "CLASS",  m.Groups["class"].Value.Trim());
                AddField(d, "VALUE",  m.Groups["value"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            else if (kind == "attribute_decl")
            {
                var d = MkBlock("kmila_vhdl_attribute_decl");
                AddField(d, "NAME", m!.Groups["name"].Value.Trim());
                AddField(d, "TYPE", m.Groups["type"].Value.Trim());
                declsHead = ChainAppend(declsHead, d);
            }
            // 2026-07-19 Phase 19 — component / procedure / function
            // declarations. Helper builders handle inner splitting.
            else if (kind == "component_decl")
            {
                declsHead = ChainAppend(declsHead, BuildComponentDeclBlock(m!));
            }
            else if (kind == "procedure_decl")
            {
                declsHead = ChainAppend(declsHead,
                    BuildSubprogramBlock("kmila_vhdl_procedure_decl", m!, declsOriginal));
            }
            else if (kind == "function_decl")
            {
                declsHead = ChainAppend(declsHead,
                    BuildSubprogramBlock("kmila_vhdl_function_decl", m!, declsOriginal));
            }
            else if (kind == "raw_decl")
            {
                var d = MkBlock("kmila_vhdl_raw_decl");
                AddField(d, "TEXT", rawText);
                declsHead = ChainAppend(declsHead, d);
            }
            // 2026-07-19 Phase 9 — comment_decl chain block
            else if (kind == "comment_decl")
            {
                var d = MkBlock("kmila_vhdl_comment_decl");
                AddField(d, "TEXT", rawText);
                declsHead = ChainAppend(declsHead, d);
            }
        }
        if (declsHead != null) AddInput(b, "DECLS", declsHead);

        // Concurrent body — split into processes + raw_concurrent runs.
        var bodyHead = ParseConcurrentBody(body, bodyOriginal);
        if (bodyHead != null) AddInput(b, "BODY", bodyHead);

        return b;
    }

    // 2026-07-19 Phase 4 (F-11): accept optional trailing process label
    // `end process p1;` (LRM §11.3 — recommended for hierarchy readability).
    // 2026-07-19 Phase 17: accept bare `process begin` (no sensitivity list) —
    // common in testbench code that uses `wait` primitives explicitly.
    // Also `begin\b` (not `begin\s+`) so leading whitespace + blanked
    // comments INSIDE the process body are preserved for Phase 9's
    // seq-slot comment detection to see.
    // 2026-07-19 Phase 20 — `<decls>` region captured between the optional
    // header `is` and the mandatory `begin`. VHDL allows a declarative part
    // (variables, constants, subprograms, …) inside a process body — the
    // pre-P20 regex jumped straight from `is` to `begin` and dumped every
    // intervening decl into raw_concurrent. Non-greedy `.*?` matches empty
    // when there are no decls, keeping pre-P20 processes byte-idempotent.
    // Uses `\bis\b` (not `is\s+`) to avoid the Phase 17 `\s+`-gobble hazard.
    private static readonly Regex ProcessRe = new(
        @"\bprocess(?:\s*\(\s*(?<sens>[^)]*?)\s*\))?\s*(?:\bis\b)?(?<decls>.*?)\bbegin\b(?<body>.*?)\bend\s+process\s*(?:\w+\s*)?;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Phase 3 roundtrip follow-up (Session 27+, 2026-07-23): recompute
    /// the decls/body boundary for a matched `process ... end process ;`
    /// span. <see cref="ProcessRe"/>'s non-greedy <c>(?&lt;decls&gt;.*?)</c>
    /// matches the FIRST <c>begin</c> in the span — which, when the
    /// process declares a nested procedure/function, is the subprogram
    /// body's <c>begin</c>, not the process's own. That splits the
    /// declaration in half, sending the header to <c>raw_decl</c> and
    /// spraying the procedure body + closer <c>end procedure;</c> into
    /// <c>&lt;body&gt;</c>, producing raw_seq tiles.
    ///
    /// Walker rule: track a stack of open-subprogram names. Push on
    /// <c>procedure|function NAME</c>. Pop on <c>end procedure</c>,
    /// <c>end function</c>, or <c>end NAME</c> where NAME matches the
    /// top of the stack. Other <c>end X</c> tokens (<c>end if</c>,
    /// <c>end case</c>, <c>end loop</c>, <c>end record</c>,
    /// <c>end block</c>) always live at depth ≥ 1 and are ignored. The
    /// first <c>begin</c> at depth 0 is the process's own begin.
    ///
    /// Returns <c>true</c> on find with <paramref name="pos"/> set to the
    /// scan offset of the depth-0 <c>begin</c> keyword. Returns
    /// <c>false</c> if the walker never finds one (malformed input);
    /// callers fall back to the regex split so behaviour never
    /// hard-regresses vs. pre-fix. Skips quoted string literals via a
    /// simple parity toggle — the walker looks for keyword tokens, not
    /// nested constructs, so char-literal / attribute edge cases are
    /// acceptable defensive gaps.
    /// </summary>
    private static bool TryFindDepth0Begin(string scan, int start, int end, out int pos)
    {
        var stack = new Stack<string>();
        int i = start;
        bool inString = false;
        while (i < end)
        {
            char c = scan[i];
            if (c == '"') { inString = !inString; i++; continue; }
            if (inString) { i++; continue; }
            if (!(char.IsLetter(c) || c == '_')) { i++; continue; }

            int wordStart = i;
            while (i < end && (char.IsLetterOrDigit(scan[i]) || scan[i] == '_')) i++;
            var lw = scan.Substring(wordStart, i - wordStart).ToLowerInvariant();

            if (lw == "procedure" || lw == "function")
            {
                while (i < end && char.IsWhiteSpace(scan[i])) i++;
                if (i < end && (char.IsLetter(scan[i]) || scan[i] == '_'))
                {
                    int nameStart = i;
                    while (i < end && (char.IsLetterOrDigit(scan[i]) || scan[i] == '_')) i++;
                    stack.Push(scan.Substring(nameStart, i - nameStart));
                }
                continue;
            }
            if (lw == "end")
            {
                while (i < end && char.IsWhiteSpace(scan[i])) i++;
                if (i < end && (char.IsLetter(scan[i]) || scan[i] == '_'))
                {
                    int nextStart = i;
                    while (i < end && (char.IsLetterOrDigit(scan[i]) || scan[i] == '_')) i++;
                    var next = scan.Substring(nextStart, i - nextStart);
                    var lnext = next.ToLowerInvariant();
                    if (stack.Count > 0)
                    {
                        if (lnext == "procedure" || lnext == "function")
                            stack.Pop();
                        else if (string.Equals(next, stack.Peek(), StringComparison.OrdinalIgnoreCase))
                            stack.Pop();
                        // else: end if/case/loop/record/block inside body — ignore.
                    }
                }
                continue;
            }
            if (lw == "begin" && stack.Count == 0)
            {
                pos = wordStart;
                return true;
            }
        }
        pos = -1;
        return false;
    }

    /// <summary>
    /// Phase 3 roundtrip follow-up: computes the correct decls/body
    /// slices for a matched <see cref="ProcessRe"/>. Uses
    /// <see cref="TryFindDepth0Begin"/> to locate the process's own
    /// <c>begin</c> even when the process declares nested subprograms;
    /// falls back to the regex-provided split on any walker miss.
    /// Output slices are indexed into <paramref name="scan"/> and mirror
    /// slices into <paramref name="original"/> at the same positions
    /// (scan and original share a common character grid — <c>scan</c>
    /// is <c>original</c> with comments blanked to spaces).
    /// </summary>
    private static void ResolveProcessDeclsBody(
        Match m, string scan, string original,
        out string declsScan, out string declsOriginal,
        out string bodyScan,  out string bodyOriginal)
    {
        int declsStart = m.Groups["decls"].Index;
        int bodyEnd = m.Groups["body"].Index + m.Groups["body"].Length;
        if (TryFindDepth0Begin(scan, declsStart, bodyEnd, out int beginPos))
        {
            int declsLen = beginPos - declsStart;
            int bodyStart = beginPos + "begin".Length;
            int bodyLen = bodyEnd - bodyStart;
            declsScan     = scan.Substring(declsStart, declsLen);
            declsOriginal = original.Substring(declsStart, declsLen);
            bodyScan      = scan.Substring(bodyStart, bodyLen);
            bodyOriginal  = original.Substring(bodyStart, bodyLen);
        }
        else
        {
            declsScan     = m.Groups["decls"].Value;
            declsOriginal = original.Substring(m.Groups["decls"].Index, m.Groups["decls"].Length);
            bodyScan      = m.Groups["body"].Value;
            bodyOriginal  = original.Substring(m.Groups["body"].Index, m.Groups["body"].Length);
        }
    }

    /// <summary>
    /// Concurrent signal assignment — same shape as the sequential one but
    /// lives outside a process. Pattern requires the cursor to be at the
    /// start of the assignment (we anchor with \G in the caller's loop).
    /// </summary>
    /// <remarks>
    /// 2026-07-19 Phase 5: target widened to accept an optional
    /// parenthesized index/slice suffix (`q(i) &lt;= r(i);`) — required
    /// for for-generate bodies to parse cleanly instead of degrading to
    /// raw_concurrent.
    /// </remarks>
    // Pass 18 (2026-07-30): use OperationsRegex.BalancedIndexSuffix so
    // nested-paren LHS like `mem(to_integer(unsigned(addr))) <= din;`
    // no longer degrades to raw_concurrent (was silently dropped by the
    // `[^()]*` guard). See Interpreter/Models/Constansts.cs.
    private static readonly Regex AssignConcurrentRe = new(
        @"\G\s*(?<target>\w+" + OperationsRegex.BalancedIndexSuffix + @")\s*<=\s*(?<value>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private Dictionary<string, object?>? ParseConcurrentBody(string scan, string original)
    {
        Dictionary<string, object?>? head = null;
        int i = 0;
        var rawAcc = new StringBuilder();

        void FlushRaw()
        {
            var text = rawAcc.ToString().Trim();
            rawAcc.Clear();
            if (text.Length == 0) return;
            head = ChainAppend(head, BuildRaw("kmila_vhdl_raw_concurrent", text));
        }

        while (i < scan.Length)
        {
            while (i < scan.Length && char.IsWhiteSpace(scan[i]))
            {
                // 2026-07-19 Phase 9 — comment detection in the concurrent
                // body. See ParseTopLevel for the pattern.
                if (i < original.Length && original[i] == '-'
                    && i + 1 < original.Length && original[i + 1] == '-')
                {
                    FlushRaw();
                    int cmtStart = i + 2;
                    if (cmtStart < original.Length && original[cmtStart] == ' ') cmtStart++;
                    int cmtEnd = cmtStart;
                    while (cmtEnd < original.Length
                           && original[cmtEnd] != '\n' && original[cmtEnd] != '\r') cmtEnd++;
                    var text = original.Substring(cmtStart, cmtEnd - cmtStart).TrimEnd();
                    var c = MkBlock("kmila_vhdl_comment_concurrent");
                    AddField(c, "TEXT", text);
                    head = ChainAppend(head, c);
                    i = cmtEnd;
                    while (i < original.Length
                           && (original[i] == '\n' || original[i] == '\r')) i++;
                    continue;
                }
                rawAcc.Append(original[i]);
                i++;
            }
            if (i >= scan.Length) break;

            var kw = PeekIdent(scan, i);
            if (string.Equals(kw, "process", StringComparison.OrdinalIgnoreCase))
            {
                var m = ProcessRe.Match(scan, i);
                if (m.Success && m.Index == NextNonWhitespace(scan, i))
                {
                    FlushRaw();
                    var p = MkBlock("kmila_vhdl_process");
                    AddField(p, "SENS", m.Groups["sens"].Value.Trim());
                    // 2026-07-19 Phase 20 — declarative part between
                    // `process[(sens)][is]` and `begin`. Empty when the
                    // process has no local decls (pre-P20 shape).
                    // Phase 3 roundtrip follow-up (2026-07-23): the
                    // decls/body split now walks the region with a
                    // subprogram-depth-aware scanner so nested procedure
                    // declarations don't poison the split at their body's
                    // `begin`.
                    ResolveProcessDeclsBody(m, scan, original,
                        out var declsRegion, out var declsOriginal,
                        out var bodyRegion,  out var bodyOriginal);
                    if (!string.IsNullOrWhiteSpace(declsRegion))
                    {
                        var declsHead = ParseDeclarations(declsRegion, declsOriginal);
                        if (declsHead != null) AddInput(p, "DECLS", declsHead);
                    }
                    var pBody = ParseSequentialBody(bodyRegion, bodyOriginal);
                    if (pBody != null) AddInput(p, "BODY", pBody);
                    head = ChainAppend(head, p);
                    i = m.Index + m.Length;
                    continue;
                }
            }


            // Concurrent signal assignment: `target <= expr;`. Recognized
            // outside any process so trailing fan-out lines like
            // `bitN <= qN;` produce real blocks instead of raw_concurrent
            // fallback tiles.
            //
            // F-N conditional-assign (2026-07-06 batch 2): the VALUE
            // captured by AssignConcurrentRe may itself be a conditional
            // `v1 when c1 else v2 when c2 else vN;`. Detect a top-level
            // `when` in the VALUE and route to BuildCondAssign so the
            // arms become first-class blocks instead of raw_expr.
            var am = AssignConcurrentRe.Match(scan, i);
            if (am.Success && am.Index == NextNonWhitespace(scan, i))
            {
                FlushRaw();
                var target = am.Groups["target"].Value;
                var value  = am.Groups["value"].Value;
                var condBlock = TryBuildCondAssign(target, value);
                if (condBlock != null)
                {
                    head = ChainAppend(head, condBlock);
                }
                else
                {
                    var a = MkBlock("kmila_vhdl_assign_concurrent");
                    AddField(a, "TARGET", target);
                    AddInput(a, "VALUE", BuildExpression(value));
                    head = ChainAppend(head, a);
                }
                i = am.Index + am.Length;
                continue;
            }

            // 2026-07-19 Phase 6 — `with <subj> select <target> <= …;`
            // Concurrent selected-signal assignment. Runs before the F-3
            // raw fallback so the whole statement becomes one first-class
            // block instead of a raw_concurrent tile.
            if (string.Equals(kw, "with", StringComparison.OrdinalIgnoreCase))
            {
                int consumedWith = TryParseWithSelect(scan, original, i, out var withBlock);
                if (consumedWith > 0 && withBlock != null)
                {
                    FlushRaw();
                    head = ChainAppend(head, withBlock);
                    i += consumedWith;
                    continue;
                }
            }

            // 2026-07-19 Phase 4 (F-11): first-class labeled concurrent
            // statements — labeled processes (`p1: process (…) …`) and
            // component instantiations (`u1: my_comp port map (…);`).
            // Runs BEFORE the F-3 raw fallback so both forms now become
            // proper blocks. Returns 0 if the shape at `i` isn't a labeled
            // statement, caller then tries raw recovery.
            var consumedLabel = TryParseLabeledConcurrent(scan, original, i, out var labelBlock);
            if (consumedLabel > 0 && labelBlock != null)
            {
                FlushRaw();
                head = ChainAppend(head, labelBlock);
                i += consumedLabel;
                continue;
            }

            // F-3 (2026-06-28) — concurrent-body recovery for known
            // unrecognized constructs: labeled statements (instantiations,
            // named processes, named generates) and bare `for/if …
            // generate` blocks. Consume each as ONE raw_concurrent tile
            // instead of poisoning the rest of the architecture body.
            var consumedUnknown = TryConsumeUnknownConcurrent(scan, original, i, out var rawTile);
            if (consumedUnknown > 0 && rawTile != null)
            {
                FlushRaw();
                head = ChainAppend(head, rawTile);
                i += consumedUnknown;
                continue;
            }

            rawAcc.Append(original[i]);
            i++;
        }

        FlushRaw();
        return head;
    }

    // F-N conditional-assign (2026-07-06 batch 2): parse a value string of
    // the form `v1 when c1 else v2 when c2 else … else vN` into a
    // kmila_vhdl_cond_assign block. Returns null if the value has no
    // top-level `when` (caller falls back to a plain assign_concurrent).
    //
    // Grammar in canonical form:
    //   value ::= expr ( 'when' expr 'else' expr )+
    //
    // The `else` clauses inside the arms are NOT the final else — the
    // final `<value>` at the tail is what the emitter renders after the
    // last `else`. Arms are stored as a chain of kmila_vhdl_cond_arm
    // blocks with VALUE + COND, and the tail expr lives in ELSE_VALUE.
    private static Dictionary<string, object?>? TryBuildCondAssign(string target, string value)
    {
        var whens = FindTopLevelKeywords(value, "when");
        if (whens.Count == 0) return null;
        var elses = FindTopLevelKeywords(value, "else");
        if (elses.Count != whens.Count) return null;
        // Verify interleaving: each `else` must appear AFTER its matching
        // `when`. Zip-scan.
        for (int k = 0; k < whens.Count; k++)
        {
            if (elses[k] <= whens[k]) return null;
            if (k + 1 < whens.Count && whens[k + 1] <= elses[k]) return null;
        }

        var block = MkBlock("kmila_vhdl_cond_assign");
        AddField(block, "TARGET", target);
        Dictionary<string, object?>? armsHead = null;

        // First value: [0, whens[0]).
        int valueStart = 0;
        for (int k = 0; k < whens.Count; k++)
        {
            int vEnd    = whens[k];
            int condEnd = elses[k];
            var vText   = value.Substring(valueStart, vEnd - valueStart).Trim();
            var cText   = value.Substring(vEnd + "when".Length,
                                          condEnd - (vEnd + "when".Length)).Trim();
            var arm = MkBlock("kmila_vhdl_cond_arm");
            AddInput(arm, "VALUE", BuildExpression(vText));
            AddInput(arm, "COND",  BuildExpression(cText));
            armsHead = ChainAppend(armsHead, arm);
            valueStart = condEnd + "else".Length;
        }
        if (armsHead != null) AddInput(block, "ARMS", armsHead);

        var elseText = value.Substring(valueStart).Trim();
        if (elseText.Length > 0)
            AddInput(block, "ELSE_VALUE", BuildExpression(elseText));

        return block;
    }

    // Paren-aware, quote-aware scan for a KEYWORD at top depth. Returns
    // every match position (start of the keyword). Keyword boundary is
    // enforced on both sides so `otherwise` doesn't count as `else`.
    private static List<int> FindTopLevelKeywords(string s, string keyword)
    {
        var hits = new List<int>();
        int depth = 0;
        for (int k = 0; k < s.Length; k++)
        {
            var c = s[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (c == '"')
            {
                int q = s.IndexOf('"', k + 1);
                if (q < 0) break;
                k = q; continue;
            }
            if (c == '\'')
            {
                int q = s.IndexOf('\'', k + 1);
                if (q >= 0) k = q;
                continue;
            }
            if (depth != 0) continue;
            if (k + keyword.Length > s.Length) continue;
            if (string.Compare(s, k, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0)
                continue;
            if (k > 0 && (char.IsLetterOrDigit(s[k - 1]) || s[k - 1] == '_')) continue;
            int after = k + keyword.Length;
            if (after < s.Length && (char.IsLetterOrDigit(s[after]) || s[after] == '_')) continue;
            hits.Add(k);
            k = after - 1; // skip past this keyword; -1 because loop k++ advances
        }
        return hits;
    }

    /* ── Process body: sequential statements ─────────────────────── */

    // 2026-07-19 Phase 5: target widened to accept indexed/sliced LHS
    // (`q(i) <= r(i);` inside process bodies).
    // Pass 18 (2026-07-30): swap `[^()]*` for the shared balanced-paren
    // fragment to accept nested calls in the LHS index.
    private static readonly Regex AssignSeqRe = new(
        @"\G\s*(?<target>\w+" + OperationsRegex.BalancedIndexSuffix + @")\s*<=\s*(?<value>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 2026-07-19 Phase 7 — variable assignment `v := expr;` (distinct
    // from signal assign `<=`). Matches the same target shape.
    // Pass 18 (2026-07-30): shared balanced-paren fragment; fences the
    // process-local `mem(to_integer(unsigned(addr))) := din;` shape
    // planned for pass 19.
    private static readonly Regex VariableAssignSeqRe = new(
        @"\G\s*(?<target>\w+" + OperationsRegex.BalancedIndexSuffix + @")\s*:=\s*(?<value>[^;]+?)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private Dictionary<string, object?>? ParseSequentialBody(string scan, string original)
    {
        Dictionary<string, object?>? head = null;
        int i = 0;
        var rawAcc = new StringBuilder();

        void FlushRaw()
        {
            var text = rawAcc.ToString().Trim();
            rawAcc.Clear();
            if (text.Length == 0) return;
            head = ChainAppend(head, BuildRaw("kmila_vhdl_raw_seq", text));
        }

        while (i < scan.Length)
        {
            while (i < scan.Length && char.IsWhiteSpace(scan[i]))
            {
                // 2026-07-19 Phase 9 — comment detection inside a
                // sequential body (process / if / case / loop body).
                if (i < original.Length && original[i] == '-'
                    && i + 1 < original.Length && original[i + 1] == '-')
                {
                    FlushRaw();
                    int cmtStart = i + 2;
                    if (cmtStart < original.Length && original[cmtStart] == ' ') cmtStart++;
                    int cmtEnd = cmtStart;
                    while (cmtEnd < original.Length
                           && original[cmtEnd] != '\n' && original[cmtEnd] != '\r') cmtEnd++;
                    var text = original.Substring(cmtStart, cmtEnd - cmtStart).TrimEnd();
                    var c = MkBlock("kmila_vhdl_comment_seq");
                    AddField(c, "TEXT", text);
                    head = ChainAppend(head, c);
                    i = cmtEnd;
                    while (i < original.Length
                           && (original[i] == '\n' || original[i] == '\r')) i++;
                    continue;
                }
                rawAcc.Append(original[i]);
                i++;
            }
            if (i >= scan.Length) break;

            var kw = PeekIdent(scan, i);

            // if … end if;
            if (string.Equals(kw, "if", StringComparison.OrdinalIgnoreCase))
            {
                var consumed = TryParseIf(scan, original, i, out var ifBlock);
                if (consumed > 0 && ifBlock != null)
                {
                    FlushRaw();
                    head = ChainAppend(head, ifBlock);
                    i += consumed;
                    continue;
                }
                // Balanced parse failed (malformed source — missing
                // `end if;`, stray tokens, etc.). Rather than tokenizing
                // the broken chain char-by-char into dozens of raw
                // fragments, consume the rest of this sequential body
                // as ONE raw_seq tile so the student sees a single
                // editable chip instead of confetti.
                int kwStart = NextNonWhitespace(scan, i);
                FlushRaw();
                var bodyTail = original.Substring(kwStart).TrimEnd();
                if (bodyTail.Length > 0)
                {
                    head = ChainAppend(head, BuildRaw("kmila_vhdl_raw_seq", bodyTail));
                }
                i = scan.Length;
                continue;
            }

            // signal assignment
            var am = AssignSeqRe.Match(scan, i);
            if (am.Success && am.Index == NextNonWhitespace(scan, i))
            {
                FlushRaw();
                var a = MkBlock("kmila_vhdl_assign_seq");
                AddField(a, "TARGET", am.Groups["target"].Value);
                AddInput(a, "VALUE", BuildExpression(am.Groups["value"].Value));
                head = ChainAppend(head, a);
                i = am.Index + am.Length;
                continue;
            }

            // 2026-07-19 Phase 7 — variable assignment `v := expr;`
            var vam = VariableAssignSeqRe.Match(scan, i);
            if (vam.Success && vam.Index == NextNonWhitespace(scan, i))
            {
                FlushRaw();
                var a = MkBlock("kmila_vhdl_variable_assign");
                AddField(a, "TARGET", vam.Groups["target"].Value);
                AddInput(a, "VALUE", BuildExpression(vam.Groups["value"].Value));
                head = ChainAppend(head, a);
                i = vam.Index + vam.Length;
                continue;
            }

            // F-4 (2026-06-28): case / when. Parses `case <subj> is when …
            // => … [when others => …] end case;` into a structured
            // kmila_vhdl_case block. Falls through to the F-3 raw-recovery
            // path on malformed sources.
            if (string.Equals(kw, "case", StringComparison.OrdinalIgnoreCase))
            {
                int consumedCase = TryParseCase(scan, original, i, out var caseBlock);
                if (consumedCase > 0 && caseBlock != null)
                {
                    FlushRaw();
                    head = ChainAppend(head, caseBlock);
                    i += consumedCase;
                    continue;
                }
            }

            // 2026-07-19 Phase 2 + 3 — first-class sequential primitives.
            // Runs BEFORE the F-3 raw-fallback so wait/exit/next/return/
            // null/report/assert + loops become proper blocks. Returns 0
            // if the keyword doesn't match one of the primitives; caller
            // then tries the raw-fallback.
            var consumedPrim = TryParseSequentialPrimitive(scan, original, i, out var primBlock);
            if (consumedPrim > 0 && primBlock != null)
            {
                FlushRaw();
                head = ChainAppend(head, primBlock);
                i += consumedPrim;
                continue;
            }

            // 2026-07-19 Phase 19 — procedure call as sequential statement
            // (`printmsg("…");`, `check_and_gate('0','0','0');`). Placed
            // AFTER TryParseSequentialPrimitive so bare-keyword shapes
            // (wait/assert/report/…) don't get miscategorised. Precedence
            // rule per handoff §5: kmila_vhdl_procedure_call sits below
            // AssignSeq/VariableAssign (assignment already claimed those
            // lines) and below the primitive parser.
            var consumedPCall = TryParseProcedureCall(scan, original, i, out var pcallBlock);
            if (consumedPCall > 0 && pcallBlock != null)
            {
                FlushRaw();
                head = ChainAppend(head, pcallBlock);
                i += consumedPCall;
                continue;
            }

            // F-3 (2026-06-28): graceful recovery for known unrecognized
            // sequential constructs (case / for / while / loop / wait).
            // Previously these poisoned the rest of the process body —
            // ParseSequentialBody just kept accumulating into rawAcc until
            // i == scan.Length, so a `case … end case;` followed by `q
            // <= d;` produced ONE giant raw_seq instead of raw + assign.
            // Now we consume each unrecognized construct as a single
            // raw_seq tile and resume normal scanning afterwards.
            var consumedUnknown = TryConsumeUnknownSequential(scan, original, i, out var rawTile);
            if (consumedUnknown > 0 && rawTile != null)
            {
                FlushRaw();
                head = ChainAppend(head, rawTile);
                i += consumedUnknown;
                continue;
            }

            rawAcc.Append(original[i]);
            i++;
        }

        FlushRaw();
        return head;
    }

    // F-4 (2026-06-28) — case / when parser. Consumes `case <subj> is …
    // end case;` and builds a kmila_vhdl_case block with an ALTS chain
    // of kmila_vhdl_case_when / kmila_vhdl_case_others alternatives.
    // Returns chars consumed; 0 means malformed → caller falls back to
    // the F-3 raw-recovery path.
    private int TryParseCase(
        string scan, string original, int start,
        out Dictionary<string, object?>? block)
    {
        block = null;
        int p = NextNonWhitespace(scan, start);
        if (!ConsumeKw(scan, ref p, "case")) return 0;
        // Subject expression: up to the next `is` at depth 0.
        int isPos = FindKeywordAtDepth(scan, p, "is", 0);
        if (isPos < 0) return 0;
        string subjectText = scan.Substring(p, isPos - p).Trim();
        if (subjectText.Length == 0) return 0;
        int q = isPos + 2;
        // Find matching `end case`.
        int endIdx = FindEndKeyword(scan, q, "case");
        if (endIdx < 0) return 0;
        int semi = FindCharAtDepth(scan, endIdx, ';', 0);
        if (semi < 0) return 0;

        var caseBlock = MkBlock("kmila_vhdl_case");
        AddInput(caseBlock, "SUBJECT", BuildExpression(subjectText));

        // Locate all top-level `when …` positions inside (q, endIdx-3).
        // endIdx points after `end case` keyword; the alternatives live
        // between `is` (q) and the start of `end case` (endIdx - 4 minus
        // whitespace). Walk through paren-aware.
        Dictionary<string, object?>? altsHead = null;
        var whenPositions = new List<int>();
        int depth = 0;
        for (int k = q; k < endIdx - 4; k++)
        {
            var c = scan[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth != 0) continue;
            // boundary before k
            if (k > 0)
            {
                var prev = scan[k - 1];
                if (char.IsLetterOrDigit(prev) || prev == '_') continue;
            }
            if (k + 4 <= scan.Length &&
                string.Compare(scan, k, "when", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 &&
                (k + 4 == scan.Length ||
                 !char.IsLetterOrDigit(scan[k + 4]) && scan[k + 4] != '_'))
            {
                whenPositions.Add(k);
            }
        }
        // Append a sentinel for "end of alternatives" so each when's body
        // span is just [thisWhen, nextWhen).
        //
        // Round-trip fidelity fix (2026-07-06): the last arm's body span
        // must END at the "end" keyword of "end case", NOT at the "case"
        // token. Otherwise the "end" keyword (plus its preceding
        // whitespace) leaks into the last arm's ParseSequentialBody and
        // becomes a raw_seq chip, which on re-emit prints a stray "end"
        // line. Walk back through whitespace + "end" (3 chars) to find
        // the correct sentinel.
        int endKwEnd = endIdx - 4; // just before "case"
        while (endKwEnd > q && char.IsWhiteSpace(scan[endKwEnd - 1])) endKwEnd--;
        int endKwStart = endKwEnd - 3; // "end" is 3 chars
        whenPositions.Add(endKwStart);

        for (int w = 0; w < whenPositions.Count - 1; w++)
        {
            int wStart = whenPositions[w];
            int wEnd   = whenPositions[w + 1];
            int afterWhen = wStart + 4; // past "when"
            // Choice text: up to `=>` at depth 0.
            int arrow = FindArrowAtDepth(scan, afterWhen, wEnd);
            if (arrow < 0) continue;
            string choice = scan.Substring(afterWhen, arrow - afterWhen).Trim();
            int bodyStart = arrow + 2;
            string bodyScan = scan.Substring(bodyStart, wEnd - bodyStart);
            string bodyOrig = original.Substring(bodyStart, wEnd - bodyStart);
            bool isOthers = string.Equals(choice, "others", StringComparison.OrdinalIgnoreCase);
            var alt = MkBlock(isOthers ? "kmila_vhdl_case_others" : "kmila_vhdl_case_when");
            if (!isOthers) AddField(alt, "CHOICE", choice);
            var altBody = ParseSequentialBody(bodyScan, bodyOrig);
            if (altBody != null) AddInput(alt, "BODY", altBody);
            altsHead = ChainAppend(altsHead, alt);
        }

        if (altsHead != null) AddInput(caseBlock, "ALTS", altsHead);
        block = caseBlock;
        return semi - start + 1;
    }

    // Helper for F-4 — find `=>` at depth 0 inside [from, to).
    private static int FindArrowAtDepth(string s, int from, int to)
    {
        int depth = 0;
        for (int k = from; k < to - 1; k++)
        {
            var c = s[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth != 0) continue;
            if (c == '=' && s[k + 1] == '>') return k;
        }
        return -1;
    }

    // 2026-07-19 Phase 2 + 3 — first-class parser for sequential
    // primitives (wait/exit/next/return/null/report/assert) and loops
    // (for/while/bare). Returns chars consumed (0 if the keyword at
    // `start` doesn't match). Runs in ParseSequentialBody BEFORE the F-3
    // raw fallback so these constructs land as proper blocks.
    private int TryParseSequentialPrimitive(
        string scan, string original, int start,
        out Dictionary<string, object?>? block)
    {
        block = null;
        int p = NextNonWhitespace(scan, start);
        var kw = PeekIdent(scan, p);
        if (kw == null) return 0;
        var kl = kw.ToLowerInvariant();

        // ── Single-statement primitives (semicolon-terminated) ────────
        if (kl is "wait" or "exit" or "next" or "return" or "null"
                or "report" or "assert")
        {
            int semi = FindCharAtDepth(scan, p, ';', 0);
            if (semi < 0) return 0;
            string body = scan.Substring(p, semi - p + 1);   // scan (blanked)
            string orig = original.Substring(p, semi - p + 1); // original (with comments)
            block = ParseSequentialPrimitiveBody(kl, body, orig);
            if (block == null) return 0;
            return semi - start + 1;
        }

        // ── Loops (for/while/bare loop) ───────────────────────────────
        if (kl is "for" or "while" or "loop")
        {
            int q = p + kw.Length;
            int endIdx = FindEndKeyword(scan, q, "loop");
            if (endIdx < 0) return 0;
            int semi = FindCharAtDepth(scan, endIdx, ';', 0);
            if (semi < 0) return 0;

            // Optional trailing loop label after `end loop`.
            // Detected but discarded in Phase 3; F-19 add later if needed.

            if (kl == "for")
            {
                // for <var> in <lo> (to|downto) <hi> loop <body> end loop;
                int inPos = FindKeywordAtDepth(scan, q, "in", 0);
                if (inPos < 0 || inPos >= endIdx) return 0;
                string varName = scan.Substring(q, inPos - q).Trim();
                if (varName.Length == 0) return 0;
                int loopPos = FindKeywordAtDepth(scan, inPos + 2, "loop", 0);
                if (loopPos < 0 || loopPos >= endIdx) return 0;
                string rangeText = scan.Substring(inPos + 2, loopPos - (inPos + 2)).Trim();
                int dirPos = FindKeywordAtDepth(rangeText, 0, "downto", 0);
                string dir = "downto";
                if (dirPos < 0)
                {
                    dirPos = FindKeywordAtDepth(rangeText, 0, "to", 0);
                    dir = "to";
                    if (dirPos < 0) return 0;
                }
                string lo = rangeText.Substring(0, dirPos).Trim();
                string hi = rangeText.Substring(dirPos + dir.Length).Trim();
                int bodyStart = loopPos + 4;
                // endIdx is position just past `end loop`. Walk back:
                // 4 chars for "loop", then whitespace, then 3 chars for "end".
                int endTokenEnd = endIdx - 4;
                while (endTokenEnd > bodyStart && char.IsWhiteSpace(scan[endTokenEnd - 1])) endTokenEnd--;
                int bodyEnd = endTokenEnd - 3;
                while (bodyEnd > bodyStart && char.IsWhiteSpace(scan[bodyEnd - 1])) bodyEnd--;

                var forB = MkBlock("kmila_vhdl_for_loop");
                AddField(forB, "VAR", varName);
                AddField(forB, "DIR", dir);
                AddInput(forB, "LO", BuildExpression(lo));
                AddInput(forB, "HI", BuildExpression(hi));
                var body = ParseSequentialBody(
                    scan.Substring(bodyStart, bodyEnd - bodyStart),
                    original.Substring(bodyStart, bodyEnd - bodyStart));
                if (body != null) AddInput(forB, "BODY", body);
                block = forB;
                return semi - start + 1;
            }
            if (kl == "while")
            {
                int loopPos = FindKeywordAtDepth(scan, q, "loop", 0);
                if (loopPos < 0 || loopPos >= endIdx) return 0;
                string condText = scan.Substring(q, loopPos - q).Trim();
                if (condText.Length == 0) return 0;
                int bodyStart = loopPos + 4;
                int endTokenEnd = endIdx - 4;
                while (endTokenEnd > bodyStart && char.IsWhiteSpace(scan[endTokenEnd - 1])) endTokenEnd--;
                int bodyEnd = endTokenEnd - 3;
                while (bodyEnd > bodyStart && char.IsWhiteSpace(scan[bodyEnd - 1])) bodyEnd--;

                var whileB = MkBlock("kmila_vhdl_while_loop");
                AddInput(whileB, "COND", BuildExpression(condText));
                var body = ParseSequentialBody(
                    scan.Substring(bodyStart, bodyEnd - bodyStart),
                    original.Substring(bodyStart, bodyEnd - bodyStart));
                if (body != null) AddInput(whileB, "BODY", body);
                block = whileB;
                return semi - start + 1;
            }
            // bare `loop … end loop;`
            {
                int bodyStart = p + 4; // past "loop"
                int endTokenEnd = endIdx - 4;
                while (endTokenEnd > bodyStart && char.IsWhiteSpace(scan[endTokenEnd - 1])) endTokenEnd--;
                int bodyEnd = endTokenEnd - 3;
                while (bodyEnd > bodyStart && char.IsWhiteSpace(scan[bodyEnd - 1])) bodyEnd--;

                var loopB = MkBlock("kmila_vhdl_loop");
                var body = ParseSequentialBody(
                    scan.Substring(bodyStart, bodyEnd - bodyStart),
                    original.Substring(bodyStart, bodyEnd - bodyStart));
                if (body != null) AddInput(loopB, "BODY", body);
                block = loopB;
                return semi - start + 1;
            }
        }

        return 0;
    }

    // Build one of the seven single-statement primitive blocks from its
    // scan text (blanked comments) + original text. `kw` is the lowercase
    // leading keyword. Returns null on malformed input; caller falls
    // through to raw fallback.
    private static Dictionary<string, object?>? ParseSequentialPrimitiveBody(
        string kw, string body, string orig)
    {
        // Trim trailing ';' and whitespace from the working slice.
        string t = body.TrimEnd(';', ' ', '\t', '\r', '\n');

        switch (kw)
        {
            case "wait":
                {
                    var b = MkBlock("kmila_vhdl_wait");
                    // "wait" | "wait until <expr>" | "wait for <expr>" | "wait on <expr>"
                    var rest = t.Substring(4).Trim();
                    if (rest.Length == 0)
                    {
                        AddField(b, "MODE", "");
                        AddField(b, "EXPR", "");
                        return b;
                    }
                    var m2 = System.Text.RegularExpressions.Regex.Match(rest,
                        @"^(?<mode>until|for|on)\b\s*(?<expr>.*)$",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m2.Success)
                    {
                        AddField(b, "MODE", m2.Groups["mode"].Value.ToLowerInvariant());
                        AddField(b, "EXPR", m2.Groups["expr"].Value.Trim());
                    }
                    else
                    {
                        AddField(b, "MODE", "");
                        AddField(b, "EXPR", rest);
                    }
                    return b;
                }
            case "exit":
            case "next":
                {
                    var b = MkBlock(kw == "exit" ? "kmila_vhdl_exit" : "kmila_vhdl_next");
                    var rest = t.Substring(kw.Length).Trim();
                    string lbl = "";
                    string cond = "";
                    if (rest.Length > 0)
                    {
                        // Optional loop label followed by optional `when <cond>`.
                        var m2 = System.Text.RegularExpressions.Regex.Match(rest,
                            @"^(?<label>\w+)?\s*(?:when\b\s*(?<cond>.+))?$",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m2.Success)
                        {
                            lbl = m2.Groups["label"].Value;
                            cond = m2.Groups["cond"].Value.Trim();
                        }
                    }
                    AddField(b, "LABEL", lbl);
                    AddField(b, "COND", cond);
                    return b;
                }
            case "return":
                {
                    var b = MkBlock("kmila_vhdl_return");
                    AddField(b, "EXPR", t.Substring(6).Trim());
                    return b;
                }
            case "null":
                return MkBlock("kmila_vhdl_null");
            case "report":
                {
                    var b = MkBlock("kmila_vhdl_report");
                    // "report <msg> [severity <level>]"
                    var rest = t.Substring(6).Trim();
                    var m2 = System.Text.RegularExpressions.Regex.Match(rest,
                        @"^(?<msg>.+?)(?:\s+severity\s+(?<sev>\w+))?\s*$",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m2.Success)
                    {
                        AddField(b, "MSG", m2.Groups["msg"].Value.Trim());
                        AddField(b, "SEVERITY", m2.Groups["sev"].Value.ToLowerInvariant());
                    }
                    else AddField(b, "MSG", rest);
                    return b;
                }
            case "assert":
                {
                    var b = MkBlock("kmila_vhdl_assert");
                    // "assert <cond> [report <msg>] [severity <level>]"
                    var rest = t.Substring(6).Trim();
                    string condPart = rest;
                    string msg = "";
                    string sev = "";
                    int reportIdx = FindKeywordAtDepth(rest, 0, "report", 0);
                    int sevIdx = FindKeywordAtDepth(rest, 0, "severity", 0);
                    if (reportIdx >= 0)
                    {
                        condPart = rest.Substring(0, reportIdx).Trim();
                        int afterReport = reportIdx + 6;
                        int msgEnd = sevIdx >= 0 ? sevIdx : rest.Length;
                        msg = rest.Substring(afterReport, msgEnd - afterReport).Trim();
                    }
                    else if (sevIdx >= 0)
                    {
                        condPart = rest.Substring(0, sevIdx).Trim();
                    }
                    if (sevIdx >= 0)
                    {
                        sev = rest.Substring(sevIdx + 8).Trim().ToLowerInvariant();
                    }
                    AddInput(b, "COND", BuildExpression(condPart));
                    AddField(b, "MSG", msg);
                    AddField(b, "SEVERITY", sev);
                    return b;
                }
        }
        return null;
    }

    // 2026-07-19 Phase 19 — procedure call at scan[start]:
    //   <ident>(<args>);   or   <ident>;
    // Distinct from kmila_vhdl_call which is an expression. Handled
    // BEFORE TryConsumeUnknownSequential so procedure calls become
    // proper blocks instead of raw_seq. Precedence rule: only recognises
    // shapes ending in `;` at depth 0 immediately after the closing `)`
    // (or after the ident when no args); rejects known keywords so it
    // doesn't collide with wait/exit/assert/report/case/if/for/while/etc.
    private static int TryParseProcedureCall(
        string scan, string original, int start,
        out Dictionary<string, object?>? block)
    {
        block = null;
        int p = NextNonWhitespace(scan, start);
        var kw = PeekIdent(scan, p);
        if (kw.Length == 0) return 0;
        var kl = kw.ToLowerInvariant();
        // Guard rail: known sequential-statement heads are handled by
        // TryParseSequentialPrimitive / TryConsumeUnknownSequential /
        // TryParseIf / TryParseCase. Defensive here in case one of
        // those returns 0 without consuming.
        if (kl is "wait" or "exit" or "next" or "return" or "null"
                or "report" or "assert" or "case" or "if" or "for"
                or "while" or "loop" or "elsif" or "else" or "end"
                or "then" or "begin" or "when" or "is") return 0;

        int q = p + kw.Length;
        while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
        string argsText = "";
        int argsIdx = -1;
        if (q < scan.Length && scan[q] == '(')
        {
            argsIdx = q;
            var argsBody = ExtractParenBody(scan, q);
            if (argsBody == null) return 0;
            argsText = argsBody;
            q += argsBody.Length + 2; // past `(` … `)`
        }
        while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
        if (q >= scan.Length || scan[q] != ';') return 0;

        var b = MkBlock("kmila_vhdl_procedure_call");
        AddField(b, "NAME", kw);
        Dictionary<string, object?>? argsHead = null;
        if (argsText.Length > 0)
        {
            foreach (var argRaw in SplitTopLevelCommas(argsText))
            {
                var argText = argRaw.Trim();
                if (argText.Length == 0) continue;
                var argWrapper = MkBlock("kmila_vhdl_call_arg");
                AddInput(argWrapper, "VALUE", BuildExpression(argText));
                argsHead = ChainAppend(argsHead, argWrapper);
            }
        }
        if (argsHead != null) AddInput(b, "ARGS", argsHead);
        block = b;
        return q - start + 1; // include the `;`
    }

    // F-3 (2026-06-28) — known unrecognized sequential constructs. Detect
    // the construct head at scan[start], depth-track to its matching `end
    // <kw>;` terminator, emit the verbatim text as ONE raw_seq block.
    // Returns total characters consumed (0 if not a known head). Keeps
    // ParseSequentialBody recovering instead of poisoning the rest of the
    // body when the catalog doesn't cover a statement.
    private static int TryConsumeUnknownSequential(
        string scan, string original, int start,
        out Dictionary<string, object?>? rawTile)
    {
        rawTile = null;
        int p = NextNonWhitespace(scan, start);
        var kw = PeekIdent(scan, p);
        if (kw == null) return 0;
        string endKw;
        switch (kw.ToLowerInvariant())
        {
            case "case":  endKw = "case"; break;
            case "loop":  endKw = "loop"; break;
            case "for":
            case "while": endKw = "loop"; break;
            case "wait":
            case "exit":
            case "next":
            case "return":
            case "null":
            case "report":
            case "assert":
                // Single-statement constructs — just consume up to next `;`
                // at depth 0.
                int semi = FindCharAtDepth(scan, p, ';', 0);
                if (semi < 0) return 0;
                rawTile = BuildRaw("kmila_vhdl_raw_seq",
                    original.Substring(p, semi - p + 1).Trim());
                return semi - start + 1;
            default: return 0;
        }
        // Depth-track scan for `end <endKw>` at depth 0, then mandatory `;`.
        int q = p + kw.Length;
        // for/while open into a `loop` body — first find that loop keyword
        // to register the depth opener consistently; we just bump depth via
        // FindEndKeyword's nested-loop handling.
        int endPos = FindEndKeyword(scan, q, endKw);
        if (endPos < 0) return 0;
        // Skip past `end endKw` and optional trailing label, up to `;`.
        int afterEnd = endPos;
        int semiAfter = FindCharAtDepth(scan, afterEnd, ';', 0);
        if (semiAfter < 0) return 0;
        rawTile = BuildRaw("kmila_vhdl_raw_seq",
            original.Substring(p, semiAfter - p + 1).Trim());
        return semiAfter - start + 1;
    }

    // 2026-07-19 Phase 4 (F-11) — first-class parser for labeled
    // concurrent statements. Detects two shapes at `start`:
    //   1) <label>: process (…) [is] begin … end process [<label>];
    //   2) <label>: <component> [generic map (…)] port map (…);
    // Returns chars consumed (0 if the shape at `start` isn't a labeled
    // statement or is one of the generate variants that Phase 5 handles).
    private int TryParseLabeledConcurrent(
        string scan, string original, int start,
        out Dictionary<string, object?>? block)
    {
        block = null;
        int p = NextNonWhitespace(scan, start);
        var label = PeekIdent(scan, p);
        if (label == null) return 0;

        int afterLabel = p + label.Length;
        int q = afterLabel;
        while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
        // Must be a single ':' (not `:=`).
        if (q >= scan.Length || scan[q] != ':') return 0;
        if (q + 1 < scan.Length && scan[q + 1] == '=') return 0;
        int afterColon = q + 1;
        while (afterColon < scan.Length && char.IsWhiteSpace(scan[afterColon])) afterColon++;

        var inner = PeekIdent(scan, afterColon);
        if (inner == null) return 0;
        var il = inner.ToLowerInvariant();

        // 2026-07-19 Phase 5 (F-7) — labeled generate statements.
        //   <label>: for … generate … end generate;
        //   <label>: if  … generate … end generate;
        if (il == "for" || il == "if")
        {
            int consumed = TryParseGenerate(scan, original, start, label,
                afterColon, il, out block);
            if (consumed > 0) return consumed;
        }
        // Note: `while … generate` is not valid VHDL. Fall through to
        // raw_concurrent if encountered.
        if (il == "for" || il == "if" || il == "while") return 0;
        // Skip `block` — Phase 6/8 area.
        if (il == "block") return 0;

        // ── (1) Labeled process ────────────────────────────────────────
        if (il == "process")
        {
            // Reuse ProcessRe starting at afterColon.
            var m = ProcessRe.Match(scan, afterColon);
            if (!m.Success || m.Index != afterColon) return 0;

            var proc = MkBlock("kmila_vhdl_process");
            AddField(proc, "LABEL", label);
            AddField(proc, "SENS", m.Groups["sens"].Value.Trim());
            // 2026-07-19 Phase 20 — declarative part inside labeled process.
            // Phase 3 roundtrip follow-up (2026-07-23): subprogram-depth-
            // aware decls/body split — see ResolveProcessDeclsBody.
            ResolveProcessDeclsBody(m, scan, original,
                out var declsRegion, out var declsOriginal,
                out var bodyRegion,  out var bodyOriginal);
            if (!string.IsNullOrWhiteSpace(declsRegion))
            {
                var declsHead = ParseDeclarations(declsRegion, declsOriginal);
                if (declsHead != null) AddInput(proc, "DECLS", declsHead);
            }
            var pBody = ParseSequentialBody(bodyRegion, bodyOriginal);
            if (pBody != null) AddInput(proc, "BODY", pBody);
            block = proc;
            return (m.Index + m.Length) - start;
        }

        // ── (2) Component instantiation ────────────────────────────────
        // `<label>: <component> [generic map (…)] port map (…);`
        // Only accept if we can find `port map` (VHDL's required map).
        int semi = FindCharAtDepth(scan, afterColon, ';', 0);
        if (semi < 0) return 0;
        string body = scan.Substring(afterColon, semi - afterColon);

        // Locate `port map` keyword sequence at depth 0.
        int portMapPos = FindTwoWordKeyword(body, 0, "port", "map");
        if (portMapPos < 0) return 0;

        // Component name is the identifier after `<label>:` — it's `inner`.
        // But VHDL allows `entity work.foo` or `configuration cfg` too.
        // Capture everything up to `generic map` or `port map` as the
        // component reference (verbatim).
        int compStart = afterColon;
        int compEnd = FindTwoWordKeyword(body, 0, "generic", "map");
        if (compEnd < 0) compEnd = portMapPos;
        else compEnd += afterColon; // relative → absolute
        if (compEnd == portMapPos) compEnd = portMapPos;
        else compEnd += 0;

        // Recompute compEnd in absolute coords.
        int genericMapRel = FindTwoWordKeyword(body, 0, "generic", "map");
        int genericMapAbs = genericMapRel < 0 ? -1 : afterColon + genericMapRel;
        int portMapAbs = afterColon + portMapPos;
        int compEndAbs = genericMapAbs >= 0 ? genericMapAbs : portMapAbs;

        string component = scan.Substring(compStart, compEndAbs - compStart).Trim();
        if (component.Length == 0) return 0;

        var inst = MkBlock("kmila_vhdl_component_inst");
        AddField(inst, "LABEL", label);
        AddField(inst, "COMPONENT", component);

        // Parse generic map body if present.
        if (genericMapAbs >= 0)
        {
            int openParen = genericMapAbs + "generic".Length;
            // Skip whitespace + "map"
            while (openParen < scan.Length && char.IsWhiteSpace(scan[openParen])) openParen++;
            openParen += 3; // past "map"
            while (openParen < scan.Length && char.IsWhiteSpace(scan[openParen])) openParen++;
            if (openParen < scan.Length && scan[openParen] == '(')
            {
                string gmBody = ExtractParenBody(scan, openParen)!;
                var gmHead = ParseAssociationList(gmBody);
                if (gmHead != null) AddInput(inst, "GENERIC_MAP", gmHead);
            }
        }

        // Parse port map body.
        {
            int openParen = portMapAbs + "port".Length;
            while (openParen < scan.Length && char.IsWhiteSpace(scan[openParen])) openParen++;
            openParen += 3; // past "map"
            while (openParen < scan.Length && char.IsWhiteSpace(scan[openParen])) openParen++;
            if (openParen < scan.Length && scan[openParen] == '(')
            {
                string pmBody = ExtractParenBody(scan, openParen)!;
                var pmHead = ParseAssociationList(pmBody);
                if (pmHead != null) AddInput(inst, "PORT_MAP", pmHead);
            }
        }

        block = inst;
        return (semi + 1) - start;
    }

    // 2026-07-19 Phase 6 — `with <subj> select <target> <= <v1> when
    // <c1>, <v2> when <c2>, …, <vN> when others;`. Detects the head at
    // `start`, parses the alt chain, returns chars consumed (0 on
    // malformed → caller falls through to raw_concurrent).
    // 2026-07-20 — strip VHDL-93 delay-qualifier prefix + suffix from a
    // signal-assignment RHS. Kmila's simulator ignores delay semantics
    // (no timing simulation), so the block editor can drop these too and
    // preserve semantic correctness. Recognizes:
    //   • leading `transport` or `inertial` keyword
    //   • trailing `after <time-literal>` at depth-0 paren nesting
    // Applied at the WITH-SELECT VALUE + concurrent/sequential assign RHS
    // capture points so BuildExpression sees a clean expression.
    private static readonly Regex DelayLeadRe = new(
        @"^\s*(?:transport|inertial)\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static int TryParseWithSelect(
        string scan, string original, int start,
        out Dictionary<string, object?>? block)
    {
        block = null;
        int p = NextNonWhitespace(scan, start);
        if (!ConsumeKw(scan, ref p, "with")) return 0;
        int selectPos = FindKeywordAtDepth(scan, p, "select", 0);
        if (selectPos < 0) return 0;
        string subjectText = scan.Substring(p, selectPos - p).Trim();
        if (subjectText.Length == 0) return 0;

        int q = selectPos + "select".Length;
        // Target and `<=` follow.
        int leArrow = -1;
        int depth = 0;
        for (int k = q; k < scan.Length - 1; k++)
        {
            var c = scan[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth != 0) continue;
            if (c == '<' && scan[k + 1] == '=') { leArrow = k; break; }
            if (c == ';') return 0; // hit ';' before finding `<=` — malformed
        }
        if (leArrow < 0) return 0;
        string target = scan.Substring(q, leArrow - q).Trim();
        if (target.Length == 0) return 0;

        int semi = FindCharAtDepth(scan, leArrow + 2, ';', 0);
        if (semi < 0) return 0;
        string body = scan.Substring(leArrow + 2, semi - (leArrow + 2)).Trim();

        // Split alternatives on top-level commas.
        var altsHead = (Dictionary<string, object?>?)null;
        foreach (var partRaw in SplitTopLevelCommas(body))
        {
            var part = partRaw.Trim();
            if (part.Length == 0) continue;
            // Each alt is `<value> when <choice>` or `<value> when others`.
            int whenPos = FindKeywordAtDepth(part, 0, "when", 0);
            if (whenPos < 0) return 0;
            var valueText  = part.Substring(0, whenPos).Trim();
            var choiceText = part.Substring(whenPos + "when".Length).Trim();
            bool isOthers  = string.Equals(choiceText, "others",
                StringComparison.OrdinalIgnoreCase);
            var alt = MkBlock(isOthers
                ? "kmila_vhdl_with_others"
                : "kmila_vhdl_with_when");
            // 2026-07-20 — delay qualifiers (`transport … after …`) stay
            // as raw_expr inside the VALUE slot. Attempted to strip them
            // for a cleaner block tree, but P21's --roundtrip parity
            // regressed by 4 cases (005/006/012/014) — GHDL honors delays
            // and produces different VCD timing when they're gone, while
            // Kmila ignores them either way. Semantic preservation wins;
            // 8 raws stay to keep 122/122 --roundtrip parity.
            AddInput(alt, "VALUE", BuildExpression(valueText));
            if (!isOthers) AddField(alt, "CHOICE", choiceText);
            altsHead = ChainAppend(altsHead, alt);
        }

        var ws = MkBlock("kmila_vhdl_with_select");
        AddInput(ws, "SUBJECT", BuildExpression(subjectText));
        AddField(ws, "TARGET", target);
        if (altsHead != null) AddInput(ws, "ALTS", altsHead);
        block = ws;
        return (semi + 1) - start;
    }

    // 2026-07-19 Phase 5 (F-7) — labeled generate parser. Handles:
    //   <label>: for <var> in <lo> (to|downto) <hi> generate … end generate [<label>];
    //   <label>: if <cond> generate … [else generate …] end generate [<label>];
    // `kind` is "for" or "if" — dispatches to the correct block shape.
    // `afterColon` points at the first char after the `:` (whitespace-skipped).
    // Returns chars consumed from `start`; 0 on malformed.
    private int TryParseGenerate(
        string scan, string original,
        int start, string label,
        int afterColon, string kind,
        out Dictionary<string, object?>? block)
    {
        block = null;
        // Find matching `end generate` at depth 0.
        int endIdx = FindEndKeyword(scan, afterColon, "generate");
        if (endIdx < 0) return 0;
        // Optional trailing label after `end generate`.
        int q = endIdx;
        while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
        if (q < scan.Length && (char.IsLetter(scan[q]) || scan[q] == '_'))
        {
            var trailingLabel = PeekIdent(scan, q);
            if (trailingLabel != null) q += trailingLabel.Length;
        }
        int semi = FindCharAtDepth(scan, q, ';', 0);
        if (semi < 0) return 0;

        // Body span: from after the `generate` keyword to the start of `end`.
        // FindEndKeyword returns pos just past "end<ws>generate" — walk back
        // 8 chars (generate) + ws + 3 chars (end).
        int endTokenEnd = endIdx - 8; // just before "generate"
        while (endTokenEnd > afterColon && char.IsWhiteSpace(scan[endTokenEnd - 1])) endTokenEnd--;
        int endTokenStart = endTokenEnd - 3; // "end" is 3 chars
        while (endTokenStart > afterColon && char.IsWhiteSpace(scan[endTokenStart - 1])) endTokenStart--;
        // (endTokenStart is now the start of `end`; walk back one more so we
        // don't include the leading whitespace between body and `end`.)
        int endEndOfBody = endTokenEnd - 3;
        while (endEndOfBody > afterColon && char.IsWhiteSpace(scan[endEndOfBody - 1])) endEndOfBody--;

        if (kind == "for")
        {
            // `for` word starts at afterColon; skip past it.
            int p = afterColon + 3;
            int inPos = FindKeywordAtDepth(scan, p, "in", 0);
            if (inPos < 0 || inPos >= endEndOfBody) return 0;
            string varName = scan.Substring(p, inPos - p).Trim();
            if (varName.Length == 0) return 0;
            // Find `generate` at depth 0 (bounds the range expression).
            int genPos = FindKeywordAtDepth(scan, inPos + 2, "generate", 0);
            if (genPos < 0 || genPos >= endEndOfBody) return 0;
            string rangeText = scan.Substring(inPos + 2, genPos - (inPos + 2)).Trim();
            int dirPos = FindKeywordAtDepth(rangeText, 0, "downto", 0);
            string dir = "downto";
            if (dirPos < 0)
            {
                dirPos = FindKeywordAtDepth(rangeText, 0, "to", 0);
                dir = "to";
                if (dirPos < 0) return 0;
            }
            string lo = rangeText.Substring(0, dirPos).Trim();
            string hi = rangeText.Substring(dirPos + dir.Length).Trim();
            int bodyStart = genPos + "generate".Length;
            int bodyEnd = endEndOfBody;

            var genB = MkBlock("kmila_vhdl_generate_for");
            AddField(genB, "LABEL", label);
            AddField(genB, "VAR", varName);
            AddField(genB, "DIR", dir);
            AddInput(genB, "LO", BuildExpression(lo));
            AddInput(genB, "HI", BuildExpression(hi));
            var bodyBlocks = ParseConcurrentBody(
                scan.Substring(bodyStart, bodyEnd - bodyStart),
                original.Substring(bodyStart, bodyEnd - bodyStart));
            if (bodyBlocks != null) AddInput(genB, "BODY", bodyBlocks);
            block = genB;
            return (semi + 1) - start;
        }

        if (kind == "if")
        {
            // `if <cond> generate <then> [else generate <else>] end generate;`
            int p = afterColon + 2; // past "if"
            int genPos = FindKeywordAtDepth(scan, p, "generate", 0);
            if (genPos < 0 || genPos >= endEndOfBody) return 0;
            string condText = scan.Substring(p, genPos - p).Trim();
            if (condText.Length == 0) return 0;
            int thenStart = genPos + "generate".Length;

            // Look for `else generate` inside the body at depth 0.
            int elseStart = -1;
            int elseGenPos = FindTwoWordKeyword(
                scan.Substring(thenStart, endEndOfBody - thenStart),
                0, "else", "generate");
            if (elseGenPos >= 0)
            {
                int elseAbs = thenStart + elseGenPos;
                elseStart = elseAbs + "else".Length;
                while (elseStart < endEndOfBody && char.IsWhiteSpace(scan[elseStart])) elseStart++;
                elseStart += "generate".Length;
            }
            int thenEnd = elseStart >= 0
                ? FindKeywordAtDepth(scan, thenStart, "else", 0)
                : endEndOfBody;
            if (thenEnd < 0 || thenEnd > endEndOfBody) thenEnd = endEndOfBody;

            var genB = MkBlock("kmila_vhdl_generate_if");
            AddField(genB, "LABEL", label);
            AddInput(genB, "COND", BuildExpression(condText));
            var thenBlocks = ParseConcurrentBody(
                scan.Substring(thenStart, thenEnd - thenStart),
                original.Substring(thenStart, thenEnd - thenStart));
            if (thenBlocks != null) AddInput(genB, "THEN_BODY", thenBlocks);
            if (elseStart >= 0)
            {
                var elseBlocks = ParseConcurrentBody(
                    scan.Substring(elseStart, endEndOfBody - elseStart),
                    original.Substring(elseStart, endEndOfBody - elseStart));
                if (elseBlocks != null) AddInput(genB, "ELSE_BODY", elseBlocks);
            }
            block = genB;
            return (semi + 1) - start;
        }

        return 0;
    }

    // Split a `<f1> => <a1>, <f2> => <a2>, …` body into a chain of
    // kmila_vhdl_port_assoc blocks. Positional (no `=>`) associations
    // get FORMAL="" and ACTUAL = the whole item.
    private static Dictionary<string, object?>? ParseAssociationList(string body)
    {
        Dictionary<string, object?>? head = null;
        var parts = SplitTopLevelCommas(body);
        foreach (var raw in parts)
        {
            var text = raw.Trim();
            if (text.Length == 0) continue;
            var assoc = MkBlock("kmila_vhdl_port_assoc");
            // Locate `=>` at depth 0 inside `text`.
            int arrow = -1;
            int depth = 0;
            for (int k = 0; k < text.Length - 1; k++)
            {
                var c = text[k];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (depth == 0 && c == '=' && text[k + 1] == '>') { arrow = k; break; }
            }
            if (arrow < 0)
            {
                // Positional association — preserve the ordinal position
                // via a blank FORMAL.
                AddField(assoc, "FORMAL", "");
                AddInput(assoc, "ACTUAL", BuildExpression(text));
            }
            else
            {
                var formal = text.Substring(0, arrow).Trim();
                var actual = text.Substring(arrow + 2).Trim();
                AddField(assoc, "FORMAL", formal);
                AddInput(assoc, "ACTUAL", BuildExpression(actual));
            }
            head = ChainAppend(head, assoc);
        }
        return head;
    }

    // 2026-07-19 Phase 10 helper — true when `s` starts with '(' and the
    // matching ')' is the very last character (i.e. the whole string is
    // wrapped in one balanced pair at depth 0).
    private static bool WrapsWholeAtDepthZero(string s)
    {
        if (s.Length < 2 || s[0] != '(' || s[^1] != ')') return false;
        int depth = 0;
        for (int k = 0; k < s.Length; k++)
        {
            if (s[k] == '(') depth++;
            else if (s[k] == ')')
            {
                depth--;
                if (depth == 0 && k != s.Length - 1) return false;
            }
        }
        return depth == 0;
    }

    // 2026-07-19 Phase 10 helper — true when `s` contains a `=>` at
    // depth 0. Used to distinguish `(a => b)` (named aggregate) from
    // `(a + b)` (parenthesized expression).
    private static bool ContainsTopLevelArrow(string s)
    {
        int depth = 0;
        for (int k = 0; k < s.Length - 1; k++)
        {
            var c = s[k];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0 && c == '=' && s[k + 1] == '>') return true;
        }
        return false;
    }

    // Locate a two-word keyword sequence `<w1> <w2>` at depth 0 inside `s`
    // starting at `from`. Returns the index of `<w1>`, or -1. Whitespace
    // between the two words is tolerated but no other tokens.
    private static int FindTwoWordKeyword(string s, int from, string w1, string w2)
    {
        int i = from;
        while (i < s.Length)
        {
            int p = FindKeywordAtDepth(s, i, w1, 0);
            if (p < 0) return -1;
            int q = p + w1.Length;
            while (q < s.Length && char.IsWhiteSpace(s[q])) q++;
            if (q + w2.Length <= s.Length
                && string.Compare(s, q, w2, 0, w2.Length, StringComparison.OrdinalIgnoreCase) == 0
                && (q + w2.Length == s.Length
                    || !char.IsLetterOrDigit(s[q + w2.Length]) && s[q + w2.Length] != '_'))
            {
                return p;
            }
            i = p + w1.Length;
        }
        return -1;
    }

    // F-3 (2026-06-28) — concurrent-body counterpart of
    // TryConsumeUnknownSequential. Recognizes labeled statements
    // (`<ident>: <stmt>` — instantiations, named generates, named
    // processes) and bare `for/if … generate` blocks. Emits a single
    // raw_concurrent tile per construct.
    private static int TryConsumeUnknownConcurrent(
        string scan, string original, int start,
        out Dictionary<string, object?>? rawTile)
    {
        rawTile = null;
        int p = NextNonWhitespace(scan, start);
        if (p >= scan.Length) return 0;

        // Labeled statement: `<ident>: …`
        var label = PeekIdent(scan, p);
        if (label != null)
        {
            int afterLabel = p + label.Length;
            // Skip whitespace, expect ':'
            int q = afterLabel;
            while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
            if (q < scan.Length && scan[q] == ':' && (q + 1 >= scan.Length || scan[q + 1] != '='))
            {
                int afterColon = q + 1;
                while (afterColon < scan.Length && char.IsWhiteSpace(scan[afterColon])) afterColon++;
                var inner = PeekIdent(scan, afterColon);
                string? endKw = null;
                if (inner != null)
                {
                    var il = inner.ToLowerInvariant();
                    if (il == "process") endKw = "process";
                    else if (il == "for" || il == "if" || il == "while") endKw = "generate";
                    else if (il == "block") endKw = "block";
                }
                if (endKw != null)
                {
                    int endIdx = FindEndKeyword(scan, afterColon + inner!.Length, endKw);
                    if (endIdx < 0) return 0;
                    int semi = FindCharAtDepth(scan, endIdx, ';', 0);
                    if (semi < 0) return 0;
                    rawTile = BuildRaw("kmila_vhdl_raw_concurrent",
                        original.Substring(p, semi - p + 1).Trim());
                    return semi - start + 1;
                }
                // Default: treat as instantiation — consume to next ';'.
                int instSemi = FindCharAtDepth(scan, afterColon, ';', 0);
                if (instSemi < 0) return 0;
                rawTile = BuildRaw("kmila_vhdl_raw_concurrent",
                    original.Substring(p, instSemi - p + 1).Trim());
                return instSemi - start + 1;
            }

            // Non-labeled `for/if … generate` block.
            var kl = label.ToLowerInvariant();
            if (kl == "for" || kl == "if" || kl == "while")
            {
                int endIdx = FindEndKeyword(scan, p + label.Length, "generate");
                if (endIdx < 0) return 0;
                int semi = FindCharAtDepth(scan, endIdx, ';', 0);
                if (semi < 0) return 0;
                rawTile = BuildRaw("kmila_vhdl_raw_concurrent",
                    original.Substring(p, semi - p + 1).Trim());
                return semi - start + 1;
            }
        }
        return 0;
    }

    // Helper for F-3 — find the literal sequence `end <kw>` at depth 0
    // anywhere in [from, scan.Length). Returns the index immediately after
    // `end <kw>`, or -1.
    // 2026-07-19 Phase 20 — nested-scope aware. The pre-P20 implementation
    // returned the FIRST `end <endKw>` at paren-depth 0, which broke on
    // nested loops (`for i … for j … end loop; end loop;` returned the
    // inner end). Now tracks keyword nesting: standalone `<endKw>` at
    // depth-0 is a NESTED opener (depth++); `end <endKw>` closes (depth--).
    // The `sawOuter` flag distinguishes callers that pass `from` past the
    // outer's opener (bare-loop, bare-case) from those that don't
    // (`for X in Y loop …` — the outer `loop` keyword still ahead).
    // Backward-compat with pre-P20 callers is preserved when they've
    // already passed the outer's opener: first `end <endKw>` still returns.
    private static int FindEndKeyword(string scan, int from, string endKw)
    {
        int depth = 0;
        int parenDepth = 0;
        bool sawOuter = false;
        for (int k = from; k < scan.Length; k++)
        {
            var c = scan[k];
            if (c == '(') { parenDepth++; continue; }
            if (c == ')') { parenDepth--; continue; }
            if (parenDepth != 0) continue;
            if (k > 0)
            {
                var prev = scan[k - 1];
                if (char.IsLetterOrDigit(prev) || prev == '_') continue;
            }
            // `end <endKw>` — the closer.
            if (k + 3 < scan.Length &&
                string.Compare(scan, k, "end", 0, 3, StringComparison.OrdinalIgnoreCase) == 0 &&
                !char.IsLetterOrDigit(scan[k + 3]) && scan[k + 3] != '_')
            {
                int q = k + 3;
                while (q < scan.Length && char.IsWhiteSpace(scan[q])) q++;
                if (q + endKw.Length <= scan.Length &&
                    string.Compare(scan, q, endKw, 0, endKw.Length, StringComparison.OrdinalIgnoreCase) == 0 &&
                    (q + endKw.Length == scan.Length ||
                     !char.IsLetterOrDigit(scan[q + endKw.Length]) && scan[q + endKw.Length] != '_'))
                {
                    if (!sawOuter) return q + endKw.Length; // outer already passed
                    depth--;
                    if (depth == 0) return q + endKw.Length;
                    k = q + endKw.Length - 1;
                    continue;
                }
            }
            // Standalone `<endKw>` — an opener (outer or nested).
            if (k + endKw.Length <= scan.Length &&
                string.Compare(scan, k, endKw, 0, endKw.Length, StringComparison.OrdinalIgnoreCase) == 0 &&
                (k + endKw.Length == scan.Length ||
                 !char.IsLetterOrDigit(scan[k + endKw.Length]) && scan[k + endKw.Length] != '_'))
            {
                if (!sawOuter) { sawOuter = true; depth = 1; }
                else depth++;
                k = k + endKw.Length - 1;
                continue;
            }
        }
        return -1;
    }

    // Helper for F-3 — find a single char at the given paren depth.
    private static int FindCharAtDepth(string s, int from, char target, int wantedDepth)
    {
        int depth = 0;
        for (int k = from; k < s.Length; k++)
        {
            var c = s[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth == wantedDepth && c == target) return k;
        }
        return -1;
    }

    /// <summary>
    /// Try to consume an <c>if … [elsif …]* [else …] end if;</c> chain
    /// starting at <paramref name="start"/> in <paramref name="src"/>.
    /// Returns the number of characters consumed (0 if no match).
    /// </summary>
    private int TryParseIf(string src, string original, int start, out Dictionary<string, object?>? block)
    {
        block = null;
        // Find the matching "end if;" — this is a depth-tracked search
        // because if-bodies can nest.
        int p = start;
        if (!ConsumeKw(src, ref p, "if")) return 0;

        // Parse: condition up to "then"
        int condStart = p;
        int thenPos = FindKeywordAtDepth(src, p, "then", 0);
        if (thenPos < 0) return 0;
        string condText = src.Substring(condStart, thenPos - condStart).Trim();
        p = thenPos + "then".Length;

        // Collect branches: (condition, scan_body, original_body)
        var branches = new List<(string Cond, string Body, string BodyOriginal)>();
        string? elseBody = null;
        string? elseBodyOriginal = null;
        int branchBodyStart = p;

        int depth = 0;
        int q = p;
        int endIfPos = -1;
        while (q < src.Length)
        {
            // Track nested if/end-if pairs at the same logical depth.
            // PeekIdent skips leading whitespace internally, so anchor all
            // offset math on `kwStart` (the identifier's actual position)
            // rather than `q`, which may still be sitting on indent
            // whitespace. The old version used `q + N` and broke any time
            // the keyword wasn't at column 0 — the elsif condition then
            // captured leading-whitespace + "elsif" + actual cond, which
            // landed in BuildExpression as a raw_expr fallback.
            var kw = PeekIdent(src, q);
            int kwStart = NextNonWhitespace(src, q);
            if (string.Equals(kw, "if", StringComparison.OrdinalIgnoreCase)
                && IsKeywordBoundary(src, kwStart + 2))
            {
                depth++;
                q = kwStart + 2;
                continue;
            }
            if (string.Equals(kw, "end", StringComparison.OrdinalIgnoreCase)
                && IsKeywordBoundary(src, kwStart + 3))
            {
                // "end if" at the right depth closes our scope.
                int probe = kwStart + 3;
                while (probe < src.Length && char.IsWhiteSpace(src[probe])) probe++;
                if (probe + 2 <= src.Length
                    && string.Equals(src.Substring(probe, 2), "if", StringComparison.OrdinalIgnoreCase)
                    && IsKeywordBoundary(src, probe + 2))
                {
                    if (depth == 0)
                    {
                        // Flush the current branch body.
                        //
                        // Round-trip fidelity fix (2026-07-06 batch 2): do NOT
                        // Trim() the two slices independently. `src` has line
                        // comments blanked to spaces, so `src.Trim()` strips
                        // through blanked-comment content while
                        // `original.Trim()` stops at the first `-`. After
                        // independent trims the two strings desynchronize by
                        // the comment prefix length, and every downstream
                        // `original[i]` read in ParseSequentialBody lands on
                        // the wrong character — producing truncated comment
                        // fragments and duplicated statements. Slice at the
                        // same offsets and let ParseSequentialBody skip
                        // leading/trailing whitespace on its own.
                        var thisBody         = src.Substring(branchBodyStart, kwStart - branchBodyStart);
                        var thisBodyOriginal = original.Substring(branchBodyStart, kwStart - branchBodyStart);
                        if (elseBody != null)
                        {
                            elseBody         = thisBody;
                            elseBodyOriginal = thisBodyOriginal;
                        }
                        else
                        {
                            branches.Add((condText, thisBody, thisBodyOriginal));
                        }

                        int semi = src.IndexOf(';', probe + 2);
                        if (semi < 0) return 0;
                        endIfPos = semi + 1;
                        break;
                    }
                    else
                    {
                        depth--;
                        q = probe + 2;
                        continue;
                    }
                }
                q = kwStart + 3;
                continue;
            }
            if (depth == 0
                && string.Equals(kw, "elsif", StringComparison.OrdinalIgnoreCase)
                && IsKeywordBoundary(src, kwStart + 5))
            {
                // See scan/original alignment note above — no .Trim() here.
                var thisBody         = src.Substring(branchBodyStart, kwStart - branchBodyStart);
                var thisBodyOriginal = original.Substring(branchBodyStart, kwStart - branchBodyStart);
                branches.Add((condText, thisBody, thisBodyOriginal));

                int e = kwStart + "elsif".Length;
                int t2 = FindKeywordAtDepth(src, e, "then", 0);
                if (t2 < 0) return 0;
                condText = src.Substring(e, t2 - e).Trim();
                branchBodyStart = t2 + "then".Length;
                q = branchBodyStart;
                continue;
            }
            if (depth == 0
                && string.Equals(kw, "else", StringComparison.OrdinalIgnoreCase)
                && IsKeywordBoundary(src, kwStart + 4)
                && !LooksLikeElsif(src, kwStart))
            {
                // See scan/original alignment note above — no .Trim() here.
                var thisBody         = src.Substring(branchBodyStart, kwStart - branchBodyStart);
                var thisBodyOriginal = original.Substring(branchBodyStart, kwStart - branchBodyStart);
                branches.Add((condText, thisBody, thisBodyOriginal));
                branchBodyStart = kwStart + "else".Length;
                elseBody         = string.Empty;
                elseBodyOriginal = string.Empty;
                q = branchBodyStart;
                continue;
            }

            q++;
        }

        if (endIfPos < 0) return 0;
        if (branches.Count == 0) return 0;

        // Build the if-chain. F-8 (2026-07-05): the outermost is
        // `kmila_vhdl_if`; every elsif branch becomes a `kmila_vhdl_elsif`
        // block in a linear next-chain attached to the outer if's
        // ELSIF_CHAIN slot. This replaces the pre-F-8 encoding where
        // elsif was wrapped as a solitary nested `kmila_vhdl_if` inside
        // ELSE_BODY. The emitter still recognises the old shape as a
        // fallback so pre-F-8 .bvhdl sidecars keep round-tripping.
        var ifB = MkBlock("kmila_vhdl_if");
        AddInput(ifB, "COND", BuildExpression(branches[0].Cond));
        var thenStmts = ParseSequentialBody(branches[0].Body, branches[0].BodyOriginal);
        if (thenStmts != null) AddInput(ifB, "THEN_BODY", thenStmts);

        if (branches.Count > 1)
        {
            Dictionary<string, object?>? chainHead = null;
            for (int idx = 1; idx < branches.Count; idx++)
            {
                var elsifB = MkBlock("kmila_vhdl_elsif");
                AddInput(elsifB, "COND", BuildExpression(branches[idx].Cond));
                var armBody = ParseSequentialBody(branches[idx].Body, branches[idx].BodyOriginal);
                if (armBody != null) AddInput(elsifB, "BODY", armBody);
                chainHead = ChainAppend(chainHead, elsifB);
            }
            if (chainHead != null) AddInput(ifB, "ELSIF_CHAIN", chainHead);
        }

        if (elseBody != null && !string.IsNullOrWhiteSpace(elseBody))
        {
            var elseStmts = ParseSequentialBody(elseBody, elseBodyOriginal ?? elseBody);
            if (elseStmts != null) AddInput(ifB, "ELSE_BODY", elseStmts);
        }

        block = ifB;
        return endIfPos - start;
    }

    private static bool LooksLikeElsif(string src, int q)
    {
        // Disambiguate "else" vs "elsif" — both start with "els".
        // Looking only at position q where we matched "else", check the
        // raw character at q+4: if it's 'i' or 'I' and forms "elsif",
        // this isn't a plain else.
        if (q + 5 > src.Length) return false;
        return string.Equals(src.Substring(q, 5), "elsif", StringComparison.OrdinalIgnoreCase)
            && IsKeywordBoundary(src, q + 5);
    }

    /* ── Expressions ─────────────────────────────────────────────── */

    // 2026-07-19 1:1 audit — accept the full std_ulogic alphabet, not just
    // '0'/'1'. Widens from `[01]` to `[01XZUWLH\-]` (LRM §14.2 std_ulogic).
    // Case-insensitive on match; emit uppercases via the dropdown option
    // list on the JS side.
    private static readonly Regex BitLiteralRe = new(@"^\s*'(?<b>[01XZUWLHxzuwlh\-])'\s*$", RegexOptions.Compiled);
    // 2026-07-19 Phase 20 — accept `<name>'<attr>` attribute-select forms
    // (e.g. `abcd'length`, `sig'range`, `x'left`). Round-trips as a single
    // kmila_vhdl_id with the full text in NAME. VHDL LRM §16.2 predefined
    // attributes are all lowercase identifiers, so `\w+` covers them.
    private static readonly Regex IdentRe      = new(@"^\s*(?<n>[a-zA-Z_][a-zA-Z_0-9]*(?:'[a-zA-Z_][a-zA-Z_0-9]*)?)\s*$", RegexOptions.Compiled);
    // F-9/F-10 completeness (2026-07-05) — plain decimal integer literals.
    // Needed so slice bounds like `reg(6 downto 0)` parse HI/LO as
    // recognized atoms instead of leaking two raw_expr chips (observed on
    // the 8-bit shift register EXAMPLE). Rendered through the id block
    // so no new schema surface; emitter outputs the number verbatim.
    // Underscore separators (e.g. `1_000`) tolerated per VHDL LRM §13.4.
    private static readonly Regex IntLiteralRe = new(@"^\s*(?<n>[0-9][0-9_]*)\s*$", RegexOptions.Compiled);

    // 2026-07-20 — VHDL time literal (`10 ns`, `300 us`, `1.5 ms`).
    // Reuses `kmila_vhdl_id` as a text carrier; emitter renders NAME
    // verbatim so the literal round-trips including its unit suffix.
    private static readonly Regex TimeLiteralRe = new(
        @"^\s*(?<n>[0-9][0-9_]*(?:\.[0-9_]+)?)\s+(?<u>fs|ps|ns|us|ms|sec|min|hr)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Pass 21 (2026-07-30): swap `[^)]+` for BalancedExpression so
    // `rising_edge(clk_bus(0))` (edge on a bit-slice or converter) parses
    // as EDGE_DETECT instead of falling through to raw_expr.
    private static readonly Regex RisingEdgeRe = new(
        @"^\s*rising_edge\s*\(\s*(?<sig>" + OperationsRegex.BalancedExpression + @")\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // F-7 fix (2026-06-28): falling_edge is a one-regex clone — students
    // doing async-reset designs hit this constantly and the raw_expr
    // fallback was opaque pedagogically.
    // Pass 21 (2026-07-30): balanced-paren too.
    private static readonly Regex FallingEdgeRe = new(
        @"^\s*falling_edge\s*\(\s*(?<sig>" + OperationsRegex.BalancedExpression + @")\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NotPrefixRe = new(
        @"^\s*not\b\s*(?<rest>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // F-5 fix (2026-06-28): vector / string literals — bit-string forms
    // (B"…", O"…", X"…") and bare quoted strings ("00", "1010"). Without
    // this, every selector / mux comparison `sel = "00"` degraded to
    // raw_expr on the RHS, hiding a very common student-facing pattern.
    private static readonly Regex VectorLiteralRe = new(
        @"^\s*(?<lit>(?:[bBoOxX])?""[^""]*"")\s*$",
        RegexOptions.Compiled);

    // F-10 fix (2026-07-05): vector slice `sig(hi downto lo)` / `(lo to hi)`
    // and scalar index `sig(i)`. Detects the outermost identifier + a
    // single paren group. Nested parens inside the index expression are
    // handled by BuildExpression recursing on the raw inside.
    private static readonly Regex SliceRe = new(
        @"^\s*(?<sig>[a-zA-Z_][a-zA-Z_0-9]*)\s*\(\s*(?<hi>[^()]+?)\s+(?<dir>downto|to)\s+(?<lo>[^()]+?)\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Pass 21 (2026-07-30): manual balanced-paren scan for slice shapes
    /// with nested-paren hi/lo — <c>sig(to_integer(x) downto 0)</c>,
    /// <c>state(unsigned(n) - 1 downto 0)</c>, etc. Runs after the
    /// fast-path <see cref="SliceRe"/>; the two regexes-cannot-be-one
    /// because we need to split on top-level (depth-0) <c>downto</c>/<c>to</c>
    /// after tracking balanced parens through hi. Returns false if the
    /// shape doesn't parse as a slice — caller falls through to index /
    /// call / raw fallbacks.
    /// </summary>
    private static bool TryExtractSliceWithBalancedParens(string s,
        out string sig, out string hi, out string lo, out string dir)
    {
        sig = string.Empty; hi = string.Empty; lo = string.Empty; dir = string.Empty;
        int i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        int nameStart = i;
        if (i >= s.Length || !(char.IsLetter(s[i]) || s[i] == '_')) return false;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
        int nameEnd = i;
        if (nameEnd == nameStart) return false;
        var name = s.Substring(nameStart, nameEnd - nameStart);
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        if (i >= s.Length || s[i] != '(') return false;
        int openParen = i;
        // Find matching close paren.
        int depth = 0;
        int close = -1;
        for (int j = i; j < s.Length; j++)
        {
            if (s[j] == '(') depth++;
            else if (s[j] == ')')
            {
                depth--;
                if (depth == 0) { close = j; break; }
            }
        }
        if (close < 0) return false;
        // Interior must be the whole tail (after possible whitespace).
        int t = close + 1;
        while (t < s.Length && char.IsWhiteSpace(s[t])) t++;
        if (t != s.Length) return false;
        // Scan interior for top-level `downto` / `to`.
        string interior = s.Substring(openParen + 1, close - openParen - 1);
        int splitStart = -1, splitEnd = -1;
        string dirWord = string.Empty;
        int scanDepth = 0;
        for (int k = 0; k < interior.Length; k++)
        {
            char c = interior[k];
            if (c == '(') scanDepth++;
            else if (c == ')') scanDepth--;
            else if (scanDepth == 0 && (c == 'd' || c == 't' || c == 'D' || c == 'T'))
            {
                // whitespace-bounded keyword.
                bool leftBound = k == 0 || char.IsWhiteSpace(interior[k - 1]);
                if (!leftBound) continue;
                if (k + 6 <= interior.Length &&
                    interior.Substring(k, 6).Equals("downto", StringComparison.OrdinalIgnoreCase) &&
                    (k + 6 == interior.Length || char.IsWhiteSpace(interior[k + 6])))
                {
                    splitStart = k; splitEnd = k + 6; dirWord = "downto"; break;
                }
                if (k + 2 <= interior.Length &&
                    interior.Substring(k, 2).Equals("to", StringComparison.OrdinalIgnoreCase) &&
                    (k + 2 == interior.Length || char.IsWhiteSpace(interior[k + 2])))
                {
                    splitStart = k; splitEnd = k + 2; dirWord = "to"; break;
                }
            }
        }
        if (splitStart < 0) return false;
        var hiRaw = interior.Substring(0, splitStart).Trim();
        var loRaw = interior.Substring(splitEnd).Trim();
        if (hiRaw.Length == 0 || loRaw.Length == 0) return false;
        sig = name; hi = hiRaw; lo = loRaw; dir = dirWord.ToLowerInvariant();
        return true;
    }
    // 2026-07-19 Phase 20 — `idx` excludes commas so multi-arg function
    // calls (`to_unsigned(4, abcd'length)`, `resize(x, 8)`) fall through
    // to CallHeadRe. Pre-P20 the comma-permissive form was catching them
    // as pseudo-indexes and emitting `raw_expr` for the multi-arg idx.
    private static readonly Regex IndexRe = new(
        @"^\s*(?<sig>[a-zA-Z_][a-zA-Z_0-9]*)\s*\(\s*(?<idx>[^(),]+?)\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // F-N call (2026-07-06 batch 2): head-of-expression `name(` — followed
    // by a balanced paren body. Handles the cases IndexRe can't:
    // multi-arg calls like `to_unsigned(x, 8)` and nested-paren calls
    // like `resize(unsigned(a), 5)`. IndexRe still catches single-arg
    // no-paren shapes like `sig(i)` / `unsigned(a)`; the two are
    // structurally different but emit identically for the single-arg
    // no-paren shape, so keeping the split preserves byte-identity on
    // existing fixtures.
    private static readonly Regex CallHeadRe = new(
        @"^\s*(?<name>[a-zA-Z_][a-zA-Z_0-9]*)\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Split a paren-body on top-level commas — commas inside nested
    // parens or quoted literals don't split. Used by the call-argument
    // parser.
    private static List<string> SplitTopLevelCommas(string s)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;
        for (int k = 0; k < s.Length; k++)
        {
            var c = s[k];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '"')
            {
                int q = s.IndexOf('"', k + 1);
                if (q < 0) break;
                k = q; continue;
            }
            else if (c == '\'')
            {
                int q = s.IndexOf('\'', k + 1);
                if (q >= 0) k = q;
                continue;
            }
            else if (c == ',' && depth == 0)
            {
                parts.Add(s.Substring(start, k - start));
                start = k + 1;
            }
        }
        parts.Add(s.Substring(start));
        return parts;
    }

    // F-12 fix (2026-07-05): `(others => '0')` and `(others => "0000")`
    // aggregate — common student idiom for zeroing a register bank.
    private static readonly Regex OthersAggregateRe = new(
        @"^\s*\(\s*others\s*=>\s*(?<val>'[01]'|""[^""]*"")\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // F-9 fix (2026-07-05): arithmetic and relational operators. Grouped
    // by VHDL precedence tier. BuildExpression scans lowest → highest so
    // `a + b < c` splits at `<` first (relational is lower precedence than
    // adding). Rendered via the shared kmila_vhdl_binop block — the OP
    // field carries the operator verbatim and the emitter's binop path
    // renders `LHS op RHS` without needing per-operator knowledge.
    private static readonly string[] RelationalOps = { "/=", "<=", ">=", "<", ">" };
    // F-9 completeness (2026-07-05) — `&` is a VHDL adding_operator per
    // LRM §9.2 (concatenation). Placed before `+`/`-` in the array so
    // `FindTopLevelOperator` scans it first when both appear (rare, but
    // deterministic split points matter for round-trip stability).
    private static readonly string[] AddingOps = { "&", "+", "-" };
    private static readonly string[] MultiplyingOps = { "*", "/", "mod", "rem" };
    // 2026-07-19 Phase 20 — VHDL LRM §9.2 miscellaneous_operator `**`
    // (exponentiation). Highest precedence — split LAST when recursing
    // lowest→highest, so `2 ** N - 1` splits at `-` first, then `**`.
    private static readonly string[] PowerOps = { "**" };

    private static Dictionary<string, object?> BuildExpression(string text)
    {
        var s = text.Trim();
        if (s.Length == 0) return BuildRaw("kmila_vhdl_raw_expr", "");

        // 2026-07-19 Phase 5 — check patterns that REQUIRE outer parens
        // BEFORE the paren-strip below. `(others => '0')` and named
        // aggregates depend on the outer `()` for their syntactic identity;
        // stripping first would break OthersAggregateRe's match.
        var otherAggEarly = OthersAggregateRe.Match(s);
        if (otherAggEarly.Success)
        {
            var b = MkBlock("kmila_vhdl_others");
            AddField(b, "VAL", otherAggEarly.Groups["val"].Value);
            return b;
        }

        // 2026-07-19 Phase 10 — general aggregate `(e1, e2, …)` or
        // `(0 => '1', 2 => '1', others => '0')` or any mixed form. Must
        // run BEFORE the paren-strip since the outer `()` is the syntax.
        // Detection: the trimmed expression is wrapped in a single depth-0
        // paren AND at least one of the following holds:
        //   • a top-level comma exists (positional or named list)
        //   • a top-level `=>` exists (single named element)
        // Guards against consuming a parenthesized sub-expression like
        // `(a + b)` which has neither — that path uses paren-strip.
        if (s.Length >= 2 && s[0] == '(' && s[^1] == ')' && WrapsWholeAtDepthZero(s))
        {
            var body = s.Substring(1, s.Length - 2);
            var parts = SplitTopLevelCommas(body);
            bool multi = parts.Count >= 2;
            bool hasArrow = false;
            foreach (var p in parts)
            {
                if (FindTopLevelKeywords(p, "=>").Count > 0) { hasArrow = true; break; }
                // Also detect the symbolic `=>` (arrow scanner handles both).
                if (ContainsTopLevelArrow(p)) { hasArrow = true; break; }
            }
            if (multi || hasArrow)
            {
                var agg = MkBlock("kmila_vhdl_aggregate");
                Dictionary<string, object?>? elemsHead = null;
                foreach (var partRaw in parts)
                {
                    var part = partRaw.Trim();
                    if (part.Length == 0) continue;
                    var elem = MkBlock("kmila_vhdl_agg_elem");
                    int arrow = -1;
                    int depth = 0;
                    for (int k = 0; k < part.Length - 1; k++)
                    {
                        var c = part[k];
                        if (c == '(') depth++;
                        else if (c == ')') depth--;
                        else if (depth == 0 && c == '=' && part[k + 1] == '>') { arrow = k; break; }
                    }
                    if (arrow < 0)
                    {
                        AddField(elem, "MODE", "positional");
                        AddField(elem, "CHOICE", "");
                        AddInput(elem, "VALUE", BuildExpression(part));
                    }
                    else
                    {
                        var choiceRaw = part.Substring(0, arrow).Trim();
                        var valueRaw  = part.Substring(arrow + 2).Trim();
                        bool isOthers = string.Equals(
                            choiceRaw, "others", StringComparison.OrdinalIgnoreCase);
                        AddField(elem, "MODE", isOthers ? "others" : "indexed");
                        AddField(elem, "CHOICE", isOthers ? "" : choiceRaw);
                        AddInput(elem, "VALUE", BuildExpression(valueRaw));
                    }
                    elemsHead = ChainAppend(elemsHead, elem);
                }
                if (elemsHead != null) AddInput(agg, "ELEMS", elemsHead);
                return agg;
            }
        }

        // 2026-07-19 Phase 5 — strip outer parens that wrap the whole
        // expression at depth 0. Required so `(not input(2))` recurses
        // as `not input(2)` instead of falling to raw_expr. Only strip
        // when the outermost `(...)` covers the entire trimmed string —
        // otherwise `(a) and (b)` would lose its RHS.
        while (s.Length >= 2 && s[0] == '(' && s[^1] == ')')
        {
            int depth = 0;
            bool wrapsAll = true;
            for (int k = 0; k < s.Length; k++)
            {
                if (s[k] == '(') depth++;
                else if (s[k] == ')')
                {
                    depth--;
                    if (depth == 0 && k != s.Length - 1) { wrapsAll = false; break; }
                }
            }
            if (!wrapsAll) break;
            s = s.Substring(1, s.Length - 2).Trim();
            if (s.Length == 0) return BuildRaw("kmila_vhdl_raw_expr", "");
        }

        var bit = BitLiteralRe.Match(s);
        if (bit.Success)
        {
            var b = MkBlock("kmila_vhdl_literal_bit");
            // Normalize to uppercase — VHDL LRM §14.2 std_ulogic values
            // are case-insensitive; the dropdown surface uses uppercase.
            AddField(b, "BIT", bit.Groups["b"].Value.ToUpperInvariant());
            return b;
        }

        var re = RisingEdgeRe.Match(s);
        if (re.Success)
        {
            var b = MkBlock("kmila_vhdl_rising_edge");
            AddInput(b, "SIG", BuildExpression(re.Groups["sig"].Value));
            return b;
        }
        var fe = FallingEdgeRe.Match(s);
        if (fe.Success)
        {
            var b = MkBlock("kmila_vhdl_falling_edge");
            AddInput(b, "SIG", BuildExpression(fe.Groups["sig"].Value));
            return b;
        }
        var vec = VectorLiteralRe.Match(s);
        if (vec.Success)
        {
            var b = MkBlock("kmila_vhdl_literal_vec");
            AddField(b, "TEXT", vec.Groups["lit"].Value);
            return b;
        }

        // F-12 (2026-07-05): `(others => '0')` — check before the binop
        // scan so the inner `=>` isn't misread. Same shape as VectorLiteral
        // + IndexRe, so must run before those atomic checks.
        var otherAgg = OthersAggregateRe.Match(s);
        if (otherAgg.Success)
        {
            var b = MkBlock("kmila_vhdl_others");
            AddField(b, "VAL", otherAgg.Groups["val"].Value);
            return b;
        }

        // F-2 fix (2026-06-28): binary logical operators (and/or/xor + the
        // negated forms). Lower precedence than equality / not, so we scan
        // for them after the eq/not branches below would short-circuit on
        // a simpler shape. The scanner is paren-aware via
        // FindTopLevelBinaryOp so `(a or b) and c` parses correctly.
        var binPos = FindTopLevelBinaryOp(s);
        if (binPos.idx >= 0)
        {
            var lhs = s.Substring(0, binPos.idx);
            var rhs = s.Substring(binPos.idx + binPos.op.Length);
            var b = MkBlock("kmila_vhdl_binop");
            AddField(b, "OP", binPos.op.ToLowerInvariant());
            AddInput(b, "LHS", BuildExpression(lhs));
            AddInput(b, "RHS", BuildExpression(rhs));
            return b;
        }

        // Equality: top-level "=" (not "<=", not "/=").
        var eqPos = FindTopLevelEquality(s);
        if (eqPos >= 0)
        {
            var lhs = s.Substring(0, eqPos);
            var rhs = s.Substring(eqPos + 1);
            var b = MkBlock("kmila_vhdl_eq");
            AddInput(b, "LHS", BuildExpression(lhs));
            AddInput(b, "RHS", BuildExpression(rhs));
            return b;
        }

        // F-9 (2026-07-05): non-equality relational (`/=`, `<`, `>`, `<=`,
        // `>=`). Lower precedence than adding/multiplying, higher than
        // logical + equality. Same block-shape as binop.
        var relPos = FindTopLevelOperator(s, RelationalOps, symbolic: true);
        if (relPos.idx >= 0)
        {
            var lhs = s.Substring(0, relPos.idx);
            var rhs = s.Substring(relPos.idx + relPos.op.Length);
            var b = MkBlock("kmila_vhdl_binop");
            AddField(b, "OP", relPos.op);
            AddInput(b, "LHS", BuildExpression(lhs));
            AddInput(b, "RHS", BuildExpression(rhs));
            return b;
        }

        // F-9 (2026-07-05): adding ops (`+`, `-`). Split at rightmost so
        // `a - b - c` parses as `(a - b) - c` (left-associative).
        var addPos = FindTopLevelOperator(s, AddingOps, symbolic: true);
        if (addPos.idx >= 0)
        {
            var lhs = s.Substring(0, addPos.idx);
            var rhs = s.Substring(addPos.idx + addPos.op.Length);
            var b = MkBlock("kmila_vhdl_binop");
            AddField(b, "OP", addPos.op);
            AddInput(b, "LHS", BuildExpression(lhs));
            AddInput(b, "RHS", BuildExpression(rhs));
            return b;
        }

        // F-9 (2026-07-05): multiplying ops (`*`, `/`, `mod`, `rem`).
        // Higher precedence than adding — split after adding so nested
        // arith trees bind correctly.
        var mulPos = FindTopLevelOperator(s, MultiplyingOps, symbolic: false);
        if (mulPos.idx >= 0)
        {
            var lhs = s.Substring(0, mulPos.idx);
            var rhs = s.Substring(mulPos.idx + mulPos.op.Length);
            var b = MkBlock("kmila_vhdl_binop");
            AddField(b, "OP", mulPos.op);
            AddInput(b, "LHS", BuildExpression(lhs));
            AddInput(b, "RHS", BuildExpression(rhs));
            return b;
        }

        // 2026-07-19 Phase 20: exponentiation `**` (LRM §9.2 miscellaneous_operator).
        // Highest of the arith tiers — split LAST so `2 ** N - 1` binds as
        // `(2 ** N) - 1` (correct VHDL semantics).
        var powPos = FindTopLevelOperator(s, PowerOps, symbolic: true);
        if (powPos.idx >= 0)
        {
            var lhs = s.Substring(0, powPos.idx);
            var rhs = s.Substring(powPos.idx + powPos.op.Length);
            var b = MkBlock("kmila_vhdl_binop");
            AddField(b, "OP", powPos.op);
            AddInput(b, "LHS", BuildExpression(lhs));
            AddInput(b, "RHS", BuildExpression(rhs));
            return b;
        }

        // Unary `not` — keep the negated operand as a sub-expression so a
        // simple identifier like `not q0` doesn't fall through to raw.
        // The emitter renders the negated operand via the kmila_vhdl_not
        // block, defined in kmila-blocks-vhdl.js.
        var negM = NotPrefixRe.Match(s);
        if (negM.Success)
        {
            var b = MkBlock("kmila_vhdl_not");
            AddInput(b, "OPERAND", BuildExpression(negM.Groups["rest"].Value));
            return b;
        }

        // F-10 (2026-07-05): vector slice `sig(3 downto 0)` — recognize
        // before the scalar index case so `(3 downto 0)` isn't misread as
        // an index expression.
        // Pass 21 (2026-07-30): fast-path via SliceRe covers the common
        // (parenless hi/lo) case; the balanced-paren-aware manual scan
        // below catches nested-paren shapes like
        // `sig(to_integer(x) downto 0)`.
        var slice = SliceRe.Match(s);
        if (slice.Success)
        {
            var b = MkBlock("kmila_vhdl_slice");
            AddField(b, "SIG", slice.Groups["sig"].Value);
            AddField(b, "DIR", slice.Groups["dir"].Value.ToLowerInvariant());
            AddInput(b, "HI", BuildExpression(slice.Groups["hi"].Value));
            AddInput(b, "LO", BuildExpression(slice.Groups["lo"].Value));
            return b;
        }
        if (TryExtractSliceWithBalancedParens(s, out var sliceSig, out var sliceHi, out var sliceLo, out var sliceDir))
        {
            var b = MkBlock("kmila_vhdl_slice");
            AddField(b, "SIG", sliceSig);
            AddField(b, "DIR", sliceDir);
            AddInput(b, "HI", BuildExpression(sliceHi));
            AddInput(b, "LO", BuildExpression(sliceLo));
            return b;
        }

        // F-10 (2026-07-05): scalar index `sig(i)`. IdentRe below would
        // otherwise fall through to raw once the atomic identifier match
        // fails on the paren.
        var idx = IndexRe.Match(s);
        if (idx.Success)
        {
            var b = MkBlock("kmila_vhdl_index");
            AddField(b, "SIG", idx.Groups["sig"].Value);
            AddInput(b, "IDX", BuildExpression(idx.Groups["idx"].Value));
            return b;
        }

        // F-N call (2026-07-06 batch 2): `name(a1, a2, …)` with balanced
        // paren body. Runs AFTER IndexRe so a bare single-arg no-paren
        // shape (`sig(i)`, `unsigned(a)`) stays as an index — those
        // round-trip byte-identically today, and reclassifying them as
        // calls would churn every existing fixture. What IndexRe can't
        // do is multi-arg (`to_unsigned(x, 8)`) or nested parens
        // (`resize(unsigned(a), 5)`) — those hit here and produce a
        // kmila_vhdl_call block with an ARGS chain of kmila_vhdl_call_arg
        // wrappers, each holding one expression sub-block.
        var callHead = CallHeadRe.Match(s);
        if (callHead.Success && s.EndsWith(')'))
        {
            int openParen = callHead.Index + callHead.Length - 1; // at '('
            var body = ExtractParenBody(s, openParen);
            if (body != null && callHead.Index + callHead.Length + body.Length + 1 == s.Length)
            {
                var args = SplitTopLevelCommas(body);
                var callBlock = MkBlock("kmila_vhdl_call");
                AddField(callBlock, "NAME", callHead.Groups["name"].Value);
                Dictionary<string, object?>? argsHead = null;
                foreach (var arg in args)
                {
                    var argText = arg.Trim();
                    if (argText.Length == 0) continue;
                    var argBlock = MkBlock("kmila_vhdl_call_arg");
                    AddInput(argBlock, "VALUE", BuildExpression(argText));
                    argsHead = ChainAppend(argsHead, argBlock);
                }
                if (argsHead != null) AddInput(callBlock, "ARGS", argsHead);
                return callBlock;
            }
        }

        var id = IdentRe.Match(s);
        if (id.Success)
        {
            var b = MkBlock("kmila_vhdl_id");
            AddField(b, "NAME", id.Groups["n"].Value);
            return b;
        }

        // F-9/F-10 completeness — bare integer literal. Reuses the id
        // block; emitter outputs NAME verbatim, so `6` round-trips as `6`.
        var intLit = IntLiteralRe.Match(s);
        if (intLit.Success)
        {
            var b = MkBlock("kmila_vhdl_id");
            AddField(b, "NAME", intLit.Groups["n"].Value);
            return b;
        }

        // 2026-07-20 — VHDL time literal `<num> <unit>` (`300 ns`,
        // `1.5 ms`, etc.). Emits verbatim through the id-as-text carrier
        // — Kmila's simulator ignores timing anyway.
        var timeLit = TimeLiteralRe.Match(s);
        if (timeLit.Success)
        {
            var b = MkBlock("kmila_vhdl_id");
            AddField(b, "NAME", timeLit.Groups["n"].Value + " " + timeLit.Groups["u"].Value);
            return b;
        }

        // Anything more elaborate (function calls beyond rising_edge,
        // deep aggregates, …) goes into a raw expression.
        return BuildRaw("kmila_vhdl_raw_expr", s);
    }

    // Generic top-level operator scanner. `symbolic=true` means the
    // operator is a punctuator (no letter-boundary check); `symbolic=false`
    // means the operator is a word (mod, rem, sll…) and requires letter
    // boundaries on both sides so `modulus` isn't misread as `mod ulus`.
    // Returns rightmost match across the ops list (left-associative). The
    // ops list is scanned in the order given; ties broken by rightmost
    // position. Scan skips paren groups and quoted literals.
    private static (int idx, string op) FindTopLevelOperator(string s, string[] ops, bool symbolic)
    {
        int depth = 0;
        int bestIdx = -1;
        string bestOp = "";
        for (int k = 0; k < s.Length; k++)
        {
            var c = s[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (c == '"')
            {
                int q = s.IndexOf('"', k + 1);
                if (q < 0) return (-1, "");
                k = q; continue;
            }
            if (c == '\'')
            {
                // Bit literal '0' / '1' — skip.
                int q = s.IndexOf('\'', k + 1);
                if (q < 0) continue;
                k = q; continue;
            }
            if (depth != 0) continue;
            foreach (var op in ops)
            {
                if (k + op.Length > s.Length) continue;
                if (string.Compare(s, k, op, 0, op.Length, StringComparison.OrdinalIgnoreCase) != 0) continue;
                // Phase 20: universal overlap rejects for `*` — it must
                // never match a position that is part of `**` (LRM §9.2
                // exponentiation). Applies to both symbolic + word passes
                // since MultiplyingOps includes `*` under `symbolic:false`.
                if (op == "*" && k + 1 < s.Length && s[k + 1] == '*') continue;
                if (op == "*" && k > 0 && s[k - 1] == '*') continue;
                if (!symbolic)
                {
                    // Word op — require non-alnum on both sides.
                    if (k > 0 && (char.IsLetterOrDigit(s[k - 1]) || s[k - 1] == '_')) continue;
                    var after = k + op.Length;
                    if (after < s.Length && (char.IsLetterOrDigit(s[after]) || s[after] == '_')) continue;
                }
                else
                {
                    // Symbolic ops need to not overlap with `<=` / `>=` etc.
                    // For `<` / `>` alone, reject if next char is `=`.
                    if (op == "<" && k + 1 < s.Length && s[k + 1] == '=') continue;
                    if (op == ">" && k + 1 < s.Length && s[k + 1] == '=') continue;
                    // Reject `-` right after another operator (unary minus).
                    if (op == "-" && k > 0)
                    {
                        var prev = s[k - 1];
                        // Skip if preceded by an operator character or start.
                        while (k > 0 && char.IsWhiteSpace(prev)) { break; }
                        if (prev == '(' || prev == ',' || "+-*/=<>&|".Contains(prev, StringComparison.Ordinal)) continue;
                    }
                }
                bestIdx = k; bestOp = op;
                // Advance past the matched op to avoid re-matching its tail.
                k += op.Length - 1;
                break;
            }
        }
        return (bestIdx, bestOp);
    }

    private static int FindTopLevelEquality(string s)
    {
        int depth = 0;
        for (int k = 0; k < s.Length; k++)
        {
            var c = s[k];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0 && c == '=')
            {
                // Skip "<=" / ">=" / "/=" / "=>".
                if (k > 0 && (s[k - 1] == '<' || s[k - 1] == '>' || s[k - 1] == '/' || s[k - 1] == ':'))
                    continue;
                if (k + 1 < s.Length && s[k + 1] == '>') continue;
                return k;
            }
        }
        return -1;
    }

    // F-2 fix (2026-06-28): paren-aware scan for the LAST occurrence of a
    // top-level binary boolean operator. "Last" because we want LEFT
    // associativity: `a and b and c` → ((a and b) and c). The candidates
    // are scanned in precedence order — `or` < `xor` < `and` (lower
    // precedence wins, then we split at the rightmost occurrence). Pure
    // string scan, no tokenizer — good enough for the Phase-1 surface
    // (academic-VHDL combinational gates). Returns (-1, "") if no match.
    private static readonly string[] BinaryOpKeywords =
        { "or", "nor", "xor", "xnor", "and", "nand" };

    private static (int idx, string op) FindTopLevelBinaryOp(string s)
    {
        int depth = 0;
        // Walk left-to-right tracking parens + quoted literals; remember
        // the rightmost match per precedence tier. After the scan, return
        // the lowest-precedence (= leftmost in priority list) match found.
        var hits = new int[BinaryOpKeywords.Length];
        for (int i = 0; i < hits.Length; i++) hits[i] = -1;

        for (int k = 0; k < s.Length; k++)
        {
            var c = s[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (c == '"')
            {
                // skip bit-string literal
                int q = s.IndexOf('"', k + 1);
                if (q < 0) return (-1, "");
                k = q;
                continue;
            }
            if (depth != 0) continue;
            // Word boundary at k? Previous char must be non-letter/digit.
            if (k > 0)
            {
                var prev = s[k - 1];
                if (char.IsLetterOrDigit(prev) || prev == '_') continue;
            }
            for (int op = 0; op < BinaryOpKeywords.Length; op++)
            {
                var kw = BinaryOpKeywords[op];
                if (k + kw.Length > s.Length) continue;
                if (string.Compare(s, k, kw, 0, kw.Length, StringComparison.OrdinalIgnoreCase) != 0) continue;
                var after = k + kw.Length;
                if (after < s.Length)
                {
                    var nxt = s[after];
                    if (char.IsLetterOrDigit(nxt) || nxt == '_') continue;
                }
                hits[op] = k; // rightmost wins
            }
        }
        for (int op = 0; op < BinaryOpKeywords.Length; op++)
        {
            if (hits[op] >= 0) return (hits[op], BinaryOpKeywords[op]);
        }
        return (-1, "");
    }

    /* ── Block-tree helpers ──────────────────────────────────────── */

    private static Dictionary<string, object?> MkBlock(string type)
        => new() { ["type"] = type, ["id"] = NewId() };

    private static string NewId()
        => "k" + Guid.NewGuid().ToString("N").Substring(0, 9);

    private static void AddField(Dictionary<string, object?> b, string name, string value)
    {
        if (!b.TryGetValue("fields", out var fObj) || fObj is not Dictionary<string, object?> fields)
        {
            fields = new Dictionary<string, object?>();
            b["fields"] = fields;
        }
        fields[name] = value;
    }

    private static void AddInput(Dictionary<string, object?> b, string slotName, Dictionary<string, object?> child)
    {
        if (!b.TryGetValue("inputs", out var iObj) || iObj is not Dictionary<string, object?> inputs)
        {
            inputs = new Dictionary<string, object?>();
            b["inputs"] = inputs;
        }
        inputs[slotName] = new Dictionary<string, object?> { ["block"] = child };
    }

    private static Dictionary<string, object?> ChainAppend(
        Dictionary<string, object?>? head,
        Dictionary<string, object?> b)
    {
        if (head == null) return b;
        var cur = head;
        while (cur.TryGetValue("next", out var nObj)
               && nObj is Dictionary<string, object?> nDict
               && nDict.TryGetValue("block", out var nBlock)
               && nBlock is Dictionary<string, object?> next)
        {
            cur = next;
        }
        cur["next"] = new Dictionary<string, object?> { ["block"] = b };
        return head;
    }

    private static Dictionary<string, object?> BuildRaw(string type, string text)
    {
        var b = MkBlock(type);
        AddField(b, "TEXT", text);
        return b;
    }

    /// <summary>
    /// Recursively count blocks of any kmila_vhdl_raw_* type in a tree
    /// of block dictionaries. Used by the Editor's switch-to-Blocks
    /// banner so the student is told when their source contained
    /// constructs the catalog couldn't fully represent.
    /// </summary>
    private static int CountRawBlocks(IEnumerable<Dictionary<string, object?>> blocks)
    {
        int total = 0;
        foreach (var b in blocks) total += CountRawBlocks(b);
        return total;
    }

    private static int CountRawBlocks(Dictionary<string, object?> block)
    {
        int total = 0;
        if (block.TryGetValue("type", out var typeObj) && typeObj is string t
            && t.StartsWith("kmila_vhdl_raw_", StringComparison.Ordinal))
        {
            total++;
        }
        if (block.TryGetValue("inputs", out var iObj) && iObj is Dictionary<string, object?> inputs)
        {
            foreach (var entry in inputs.Values)
            {
                if (entry is Dictionary<string, object?> wrap
                    && wrap.TryGetValue("block", out var inner)
                    && inner is Dictionary<string, object?> child)
                {
                    total += CountRawBlocks(child);
                }
            }
        }
        if (block.TryGetValue("next", out var nObj) && nObj is Dictionary<string, object?> nWrap
            && nWrap.TryGetValue("block", out var nInner) && nInner is Dictionary<string, object?> nChild)
        {
            total += CountRawBlocks(nChild);
        }
        return total;
    }

    /* ── Source-scanning helpers ─────────────────────────────────── */

    private static int NextNonWhitespace(string s, int from)
    {
        int k = from;
        while (k < s.Length && char.IsWhiteSpace(s[k])) k++;
        return k;
    }

    private static string PeekIdent(string s, int from)
    {
        int k = NextNonWhitespace(s, from);
        int start = k;
        while (k < s.Length && (char.IsLetterOrDigit(s[k]) || s[k] == '_')) k++;
        return k > start ? s.Substring(start, k - start) : string.Empty;
    }

    private static bool ConsumeKw(string s, ref int pos, string kw)
    {
        int k = NextNonWhitespace(s, pos);
        if (k + kw.Length > s.Length) return false;
        if (!string.Equals(s.Substring(k, kw.Length), kw, StringComparison.OrdinalIgnoreCase)) return false;
        if (!IsKeywordBoundary(s, k + kw.Length)) return false;
        pos = k + kw.Length;
        return true;
    }

    private static bool IsKeywordBoundary(string s, int pos)
    {
        if (pos >= s.Length) return true;
        var c = s[pos];
        return !(char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>
    /// Walk forward from the architecture's begin-position and return the
    /// index of the matching `end …;` while tracking `process … end process`
    /// pairs so a stray `end architecture name;` typed inside a malformed
    /// process body doesn't falsely terminate the architecture. Accepts
    /// any of the syntactically valid forms: `end;`, `end &lt;archname&gt;;`,
    /// `end architecture;`, `end architecture &lt;archname&gt;;`.
    /// On success <c>endLen</c> is the length of the closer
    /// (start of `end` to and including `;`). Returns -1 if no match.
    /// </summary>
    // 2026-07-19 Phase 19 — walk forward from just after the architecture
    // header's `is` keyword, skipping any nested subprogram (`procedure` /
    // `function`) or `component` scopes, and return the position of the
    // outermost `begin` (or -1 if not found).
    //
    // Without this, an inline `procedure … begin … end procedure;` inside
    // DECLS would poison the ArchitectureHeaderRe's non-greedy `\bbegin\b`
    // capture and truncate DECLS at the FIRST nested begin.
    private static int FindArchitectureBegin(string s, int from)
    {
        int i = from;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
            if (s[i] == '"') { int j = SkipString(s, i); if (j < 0) return -1; i = j; continue; }
            int cl = SkipCharLit(s, i);
            if (cl != i) { i = cl; continue; }
            var kw = PeekIdent(s, i);
            if (kw.Length == 0) { i++; continue; }
            var kl = kw.ToLowerInvariant();
            if (kl == "begin") return i;
            if (kl == "procedure" || kl == "function")
            {
                int next = SkipSubprogramScope(s, i, kl);
                if (next < 0) return -1;
                i = next;
                continue;
            }
            if (kl == "component")
            {
                int next = SkipComponentScope(s, i);
                if (next < 0) return -1;
                i = next;
                continue;
            }
            i += kw.Length;
        }
        return -1;
    }

    // Skip `procedure|function <name> … end procedure|function|<name>;`
    // scope. Assumes `s[start]` starts with the keyword. Returns the
    // position just past the terminating `;`, or -1 on malformed input.
    private static int SkipSubprogramScope(string s, int start, string kw)
    {
        int i = start + kw.Length;
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        var name = PeekIdent(s, i);
        if (name.Length == 0) return -1;
        var nameLower = name.ToLowerInvariant();
        i += name.Length;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
            if (s[i] == '"') { int j = SkipString(s, i); if (j < 0) return -1; i = j; continue; }
            int cl = SkipCharLit(s, i);
            if (cl != i) { i = cl; continue; }
            var tok = PeekIdent(s, i);
            if (tok.Length == 0) { i++; continue; }
            if (string.Equals(tok, "end", StringComparison.OrdinalIgnoreCase))
            {
                int j = i + 3;
                while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
                var follow = PeekIdent(s, j);
                var fl = follow.ToLowerInvariant();
                if (fl == kw || fl == nameLower)
                {
                    int semi = s.IndexOf(';', j);
                    if (semi < 0) return -1;
                    return semi + 1;
                }
                // Bare `end;` — also a valid subprogram terminator.
                if (follow.Length == 0 && j < s.Length && s[j] == ';')
                {
                    return j + 1;
                }
                // Some other `end` (end if, end case, end loop, …). Skip.
                i = j + Math.Max(follow.Length, 0);
                if (i == j) i = j + 1;
                continue;
            }
            i += tok.Length;
        }
        return -1;
    }

    // Skip `component <name> … end component [<name>];` scope.
    private static int SkipComponentScope(string s, int start)
    {
        int i = start + 9; // past "component"
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        var name = PeekIdent(s, i);
        if (name.Length == 0) return -1;
        i += name.Length;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
            if (s[i] == '"') { int j = SkipString(s, i); if (j < 0) return -1; i = j; continue; }
            int cl = SkipCharLit(s, i);
            if (cl != i) { i = cl; continue; }
            var tok = PeekIdent(s, i);
            if (tok.Length == 0) { i++; continue; }
            if (string.Equals(tok, "end", StringComparison.OrdinalIgnoreCase))
            {
                int j = i + 3;
                while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
                var follow = PeekIdent(s, j);
                if (string.Equals(follow, "component", StringComparison.OrdinalIgnoreCase))
                {
                    int semi = s.IndexOf(';', j);
                    if (semi < 0) return -1;
                    return semi + 1;
                }
                i = j + Math.Max(follow.Length, 0);
                if (i == j) i = j + 1;
                continue;
            }
            i += tok.Length;
        }
        return -1;
    }

    // 2026-07-19 Phase 19 — skip past a double-quoted VHDL string literal
    // starting at s[from]. Returns the index just past the closing `"`, or
    // -1 on unterminated. VHDL doubles a `"` inside a string to escape it,
    // but the outer walker only cares about balanced quotes.
    private static int SkipString(string s, int from)
    {
        if (from >= s.Length || s[from] != '"') return from;
        int i = from + 1;
        while (i < s.Length)
        {
            if (s[i] == '"')
            {
                if (i + 1 < s.Length && s[i + 1] == '"') { i += 2; continue; }
                return i + 1;
            }
            i++;
        }
        return -1;
    }

    // Character literal `'X'` where X is a single char. Only treat as a
    // char literal when there's a matching `'` two chars later — otherwise
    // (`sig'range`, `s'length`) the `'` is an attribute-select and the
    // walker should NOT skip.
    private static int SkipCharLit(string s, int from)
    {
        if (from + 2 < s.Length && s[from] == '\'' && s[from + 2] == '\''
            && s[from + 1] != '\'')
            return from + 3;
        return from;
    }

    private static int FindArchitectureEnd(string s, int from, string archName, out int endLen)
    {
        endLen = 0;
        int i = from;
        int processDepth = 0;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
            // Phase 19: string / char literals — skip verbatim so keywords
            // inside them (e.g. "End rst process") don't corrupt depth.
            if (s[i] == '"') { int j = SkipString(s, i); if (j < 0) return -1; i = j; continue; }
            int cl = SkipCharLit(s, i);
            if (cl != i) { i = cl; continue; }
            var kw = PeekIdent(s, i);
            if (kw.Length == 0) { i++; continue; }

            int kwStart = NextNonWhitespace(s, i);

            if (string.Equals(kw, "process", StringComparison.OrdinalIgnoreCase))
            {
                processDepth++;
                i = kwStart + kw.Length;
                continue;
            }

            if (string.Equals(kw, "end", StringComparison.OrdinalIgnoreCase))
            {
                // Peek what follows the `end` keyword to classify it.
                int after = kwStart + 3;
                while (after < s.Length && char.IsWhiteSpace(s[after])) after++;
                var follow = PeekIdent(s, after);

                if (string.Equals(follow, "process", StringComparison.OrdinalIgnoreCase))
                {
                    if (processDepth > 0) processDepth--;
                    int semi = s.IndexOf(';', after);
                    if (semi < 0) return -1;
                    i = semi + 1;
                    continue;
                }

                // Only consider `end <foo>;` as the architecture closer
                // when we're at process-depth 0. Below that, it's either
                // a malformed token inside a process body (skip) or some
                // other `end` we don't care about.
                if (processDepth == 0)
                {
                    bool isArchKw = string.Equals(follow, "architecture", StringComparison.OrdinalIgnoreCase);
                    bool isArchName = string.Equals(follow, archName, StringComparison.OrdinalIgnoreCase);
                    bool isBareEnd  = follow.Length == 0 && after < s.Length && s[after] == ';';
                    if (isBareEnd || isArchKw || isArchName)
                    {
                        int semi = s.IndexOf(';', after);
                        if (semi < 0) return -1;
                        endLen = (semi + 1) - kwStart;
                        return kwStart;
                    }
                }

                // Some other `end` (end if, end case, end loop, …) — just
                // advance past it.
                i = after + Math.Max(follow.Length, 0);
                if (i == kwStart) i = kwStart + 3;
                continue;
            }

            // Skip identifier / token.
            i = kwStart + kw.Length;
        }
        return -1;
    }

    /// <summary>
    /// Scan forward looking for the given keyword at <paramref name="startDepth"/>
    /// paren depth. Returns the keyword's index or -1 if not found.
    /// </summary>
    private static int FindKeywordAtDepth(string s, int from, string keyword, int startDepth)
    {
        int depth = startDepth;
        int i = from;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0)
            {
                var kw = PeekIdent(s, i);
                if (string.Equals(kw, keyword, StringComparison.OrdinalIgnoreCase))
                {
                    int kwStart = NextNonWhitespace(s, i);
                    return kwStart;
                }
            }
            i++;
        }
        return -1;
    }
}
