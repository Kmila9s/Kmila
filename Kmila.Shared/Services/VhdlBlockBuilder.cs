using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Reverse direction of <see cref="VhdlBlockEmitter"/>: takes VHDL source
/// and produces a kmila-blocks workspace JSON in the same shape that
/// <c>window.kmilaBlocklySerialize</c> returns. Backlog #16, Phase 0
/// vertical slice.
///
/// Recognition strategy: regex-based text scanning, not a full parser.
/// We match the 7 forward constructs (entity, architecture, port,
/// signal_decl, process, if/elsif/else, assign_seq) plus the 4
/// expression atoms (id, literal_bit, eq, rising_edge). Anything else
/// gets wrapped in a raw-VHDL block at the appropriate slot type so
/// the source survives round-trip — see the four kmila_vhdl_raw_*
/// block defs in kmila-blocks-vhdl.js.
///
/// This deliberately ignores comments and reformats whitespace; a
/// future phase can address true comment preservation by attaching
/// sticky-note metadata to neighboring blocks (Step 6 of #16).
/// </summary>
/// <remarks>
/// The regex approach is brittle on pathological inputs (nested
/// processes with their own if chains, weird formatting, etc.) but
/// degrades gracefully — anything that doesn't match a recognized
/// pattern becomes a raw block holding the original text. Robustness
/// improves as patterns are tightened; correctness is bounded by the
/// round-trip property test (Step 3 follow-up).
/// </remarks>
public class VhdlBlockBuilder
{
    private readonly ILogger _logger;

    /// <summary>
    /// Count of raw-VHDL fallback blocks produced by the most recent
    /// <see cref="Build"/> call. Editor uses this to surface a "X
    /// constructs couldn't be fully represented" warning when the
    /// student switches from Code to Blocks. Reset on every Build.
    /// </summary>
    public int LastRawCount { get; private set; }

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
            var scan = BlankLineComments(vhdlSource);
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
            return JsonSerializer.Serialize(doc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VhdlBlockBuilder] Build failed; returning empty workspace");
            return EmptyWorkspace;
        }
    }

    private const string EmptyWorkspace = """{"blocks":{"blocks":[]}}""";

    /* ── Top-level scan ───────────────────────────────────────────── */

    private static readonly Regex EntityRe = new(
        @"\bentity\s+(?<name>\w+)\s+is\s+(?<body>.*?)\bend\s+(?:entity\s*(?:\w+\s*)?|\w+\s*);",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Header only — body & matching `end` are located by a process-aware
    // scan below so a stray `end architecture …;` inside a malformed process
    // body doesn't truncate the architecture prematurely.
    private static readonly Regex ArchitectureHeaderRe = new(
        @"\barchitecture\s+(?<name>\w+)\s+of\s+(?<entity>\w+)\s+is\s+(?<decls>.*?)\bbegin\b",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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
                rawAcc.Append(original[i]);
                i++;
            }
            if (i >= scan.Length) break;

            // Library/use clauses: silently consume through the next ';'
            // (not represented as blocks in Phase 0).
            var keyword = PeekIdent(scan, i);
            if (string.Equals(keyword, "library", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyword, "use", StringComparison.OrdinalIgnoreCase))
            {
                int semi = scan.IndexOf(';', i);
                if (semi < 0) { i = scan.Length; break; }
                rawAcc.Clear();
                i = semi + 1;
                continue;
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
                    // Header matched — now locate the architecture's
                    // closing `end …;` by scanning forward through the
                    // body while tracking `process … end process` depth.
                    // A stray `end architecture name;` mistakenly typed
                    // inside the process body (which the previous regex
                    // would greedily snap to) is ignored, keeping the
                    // whole process body intact as raw_seq text instead
                    // of orphaning everything after it.
                    int bodyStart = m.Index + m.Length;
                    var archName  = m.Groups["name"].Value;
                    int endIdx    = FindArchitectureEnd(scan, bodyStart, archName, out int endLen);
                    if (endIdx >= 0)
                    {
                        FlushRaw();
                        int declsStart = m.Groups["decls"].Index;
                        int declsLen   = m.Groups["decls"].Length;
                        int bodyLen    = endIdx - bodyStart;
                        var bodyScan   = scan.Substring(bodyStart, bodyLen);
                        var bodyOrig   = original.Substring(bodyStart, bodyLen);
                        result.Add(BuildArchitecture(
                            archName,
                            m.Groups["entity"].Value,
                            m.Groups["decls"].Value,
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
        @"\bport\s*\((?<body>.*?)\)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private Dictionary<string, object?> BuildEntity(string name, string body)
    {
        var b = MkBlock("kmila_vhdl_entity");
        AddField(b, "NAME", name);

        var portMatch = PortBlockRe.Match(body);
        if (portMatch.Success)
        {
            var ports = ParsePortList(portMatch.Groups["body"].Value);
            if (ports != null) AddInput(b, "PORTS", ports);
        }
        return b;
    }

    private static readonly Regex PortLineRe = new(
        @"\s*(?<name>\w+)\s*:\s*(?<dir>in|out|inout)\b\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\))?)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private Dictionary<string, object?>? ParsePortList(string body)
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
                var portB = MkBlock("kmila_vhdl_port");
                AddField(portB, "NAME", m.Groups["name"].Value);
                AddField(portB, "DIR",  m.Groups["dir"].Value.ToLowerInvariant());
                AddField(portB, "TYPE", "std_logic"); // Phase 0 vertical slice
                head = ChainAppend(head, portB);
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
    // split into per-name signal_decl blocks below. The initializer is
    // recognized so the regex matches, but Phase 0's signal_decl block
    // has no init field — the `:= …` value is dropped (graceful loss,
    // not a parse failure).
    private static readonly Regex SignalDeclRe = new(
        @"\bsignal\s+(?<names>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<type>\w+)\s*(?::=\s*[^;]+?)?\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        Dictionary<string, object?>? declsHead = null;
        foreach (Match m in SignalDeclRe.Matches(decls))
        {
            var names = m.Groups["names"].Value;
            foreach (var raw in names.Split(','))
            {
                var nm = raw.Trim();
                if (nm.Length == 0) continue;
                var d = MkBlock("kmila_vhdl_signal_decl");
                AddField(d, "NAME", nm);
                AddField(d, "TYPE", "std_logic");
                declsHead = ChainAppend(declsHead, d);
            }
        }
        if (declsHead != null) AddInput(b, "DECLS", declsHead);

        // Concurrent body — split into processes + raw_concurrent runs.
        var bodyHead = ParseConcurrentBody(body, bodyOriginal);
        if (bodyHead != null) AddInput(b, "BODY", bodyHead);

        return b;
    }

    private static readonly Regex ProcessRe = new(
        @"\bprocess\s*\(\s*(?<sens>[^)]*?)\s*\)\s*(?:is\s+)?begin\s+(?<body>.*?)\bend\s+process\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Concurrent signal assignment — same shape as the sequential one but
    /// lives outside a process. Pattern requires the cursor to be at the
    /// start of the assignment (we anchor with \G in the caller's loop).
    /// </summary>
    private static readonly Regex AssignConcurrentRe = new(
        @"\G\s*(?<target>\w+)\s*<=\s*(?<value>[^;]+?)\s*;",
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
                    int innerStart = m.Groups["body"].Index;
                    int innerLen   = m.Groups["body"].Length;
                    var pBody = ParseSequentialBody(
                        m.Groups["body"].Value,
                        original.Substring(innerStart, innerLen));
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
            var am = AssignConcurrentRe.Match(scan, i);
            if (am.Success && am.Index == NextNonWhitespace(scan, i))
            {
                FlushRaw();
                var a = MkBlock("kmila_vhdl_assign_concurrent");
                AddField(a, "TARGET", am.Groups["target"].Value);
                AddInput(a, "VALUE", BuildExpression(am.Groups["value"].Value));
                head = ChainAppend(head, a);
                i = am.Index + am.Length;
                continue;
            }

            rawAcc.Append(original[i]);
            i++;
        }

        FlushRaw();
        return head;
    }

    /* ── Process body: sequential statements ─────────────────────── */

    private static readonly Regex AssignSeqRe = new(
        @"\G\s*(?<target>\w+)\s*<=\s*(?<value>[^;]+?)\s*;",
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

            rawAcc.Append(original[i]);
            i++;
        }

        FlushRaw();
        return head;
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
                        var thisBody         = src.Substring(branchBodyStart, kwStart - branchBodyStart).Trim();
                        var thisBodyOriginal = original.Substring(branchBodyStart, kwStart - branchBodyStart).Trim();
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
                var thisBody         = src.Substring(branchBodyStart, kwStart - branchBodyStart).Trim();
                var thisBodyOriginal = original.Substring(branchBodyStart, kwStart - branchBodyStart).Trim();
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
                var thisBody         = src.Substring(branchBodyStart, kwStart - branchBodyStart).Trim();
                var thisBodyOriginal = original.Substring(branchBodyStart, kwStart - branchBodyStart).Trim();
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

        // Build the if-chain. The outermost is `kmila_vhdl_if`; elsif
        // branches become nested ifs inside ELSE_BODY (the emitter's
        // EmitIfChain flattens this pattern back into proper VHDL).
        Dictionary<string, object?> Build(int idx)
        {
            var ifB = MkBlock("kmila_vhdl_if");
            AddInput(ifB, "COND", BuildExpression(branches[idx].Cond));
            var thenStmts = ParseSequentialBody(branches[idx].Body, branches[idx].BodyOriginal);
            if (thenStmts != null) AddInput(ifB, "THEN_BODY", thenStmts);
            if (idx + 1 < branches.Count)
            {
                AddInput(ifB, "ELSE_BODY", Build(idx + 1));
            }
            else if (elseBody != null && !string.IsNullOrWhiteSpace(elseBody))
            {
                var elseStmts = ParseSequentialBody(elseBody, elseBodyOriginal ?? elseBody);
                if (elseStmts != null) AddInput(ifB, "ELSE_BODY", elseStmts);
            }
            return ifB;
        }

        block = Build(0);
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

    private static readonly Regex BitLiteralRe = new(@"^\s*'(?<b>[01])'\s*$", RegexOptions.Compiled);
    private static readonly Regex IdentRe      = new(@"^\s*(?<n>[a-zA-Z_][a-zA-Z_0-9]*)\s*$", RegexOptions.Compiled);
    private static readonly Regex RisingEdgeRe = new(
        @"^\s*rising_edge\s*\(\s*(?<sig>[^)]+)\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NotPrefixRe = new(
        @"^\s*not\b\s*(?<rest>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private Dictionary<string, object?> BuildExpression(string text)
    {
        var s = text.Trim();
        if (s.Length == 0) return BuildRaw("kmila_vhdl_raw_expr", "");

        var bit = BitLiteralRe.Match(s);
        if (bit.Success)
        {
            var b = MkBlock("kmila_vhdl_literal_bit");
            AddField(b, "BIT", bit.Groups["b"].Value);
            return b;
        }

        var re = RisingEdgeRe.Match(s);
        if (re.Success)
        {
            var b = MkBlock("kmila_vhdl_rising_edge");
            AddInput(b, "SIG", BuildExpression(re.Groups["sig"].Value));
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

        var id = IdentRe.Match(s);
        if (id.Success)
        {
            var b = MkBlock("kmila_vhdl_id");
            AddField(b, "NAME", id.Groups["n"].Value);
            return b;
        }

        // Anything more elaborate (arithmetic, function calls beyond
        // rising_edge, vector slices, …) goes into a raw expression.
        return BuildRaw("kmila_vhdl_raw_expr", s);
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

    /// <summary>
    /// Replace each <c>--</c>…end-of-line comment in <paramref name="source"/>
    /// with spaces, preserving the source length so byte offsets stay
    /// 1:1 with the untouched original. Used so regex / keyword peeks
    /// don't accidentally match VHDL keywords hiding inside a comment
    /// body, while the parallel original string can still feed raw-VHDL
    /// blocks with the comments intact.
    /// </summary>
    private static string BlankLineComments(string source)
    {
        var chars = source.ToCharArray();
        int i = 0;
        while (i < chars.Length)
        {
            // Find a "--" start on this line.
            if (i + 1 < chars.Length && chars[i] == '-' && chars[i + 1] == '-')
            {
                while (i < chars.Length && chars[i] != '\n')
                {
                    chars[i] = ' ';
                    i++;
                }
            }
            else
            {
                i++;
            }
        }
        return new string(chars);
    }

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
    /// any of the syntactically valid forms: `end;`, `end <archname>;`,
    /// `end architecture;`, `end architecture <archname>;`.
    /// On success <paramref name="endLen"/> is the length of the closer
    /// (start of `end` to and including `;`). Returns -1 if no match.
    /// </summary>
    private static int FindArchitectureEnd(string s, int from, string archName, out int endLen)
    {
        endLen = 0;
        int i = from;
        int processDepth = 0;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i])) { i++; continue; }
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
