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
                // F-1 fix (2026-06-28): use the regex-captured type, not a
                // hard-coded `std_logic`. PortLineRe already accepts the
                // optional `(N downto 0)` suffix, so vector bus widths and
                // unsigned/signed/integer types now round-trip intact.
                AddField(portB, "TYPE", NormaliseType(m.Groups["type"].Value));
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
        // F-1 fix (2026-06-28): widen <type> to accept `std_logic_vector(N
        // downto 0)`, `unsigned(7 downto 0)`, etc. — same suffix shape as
        // PortLineRe. Without this, signals silently downgraded to scalar
        // `std_logic` and the round-trip emitted a structurally wrong
        // architecture.
        @"\bsignal\s+(?<names>\w+(?:\s*,\s*\w+)*)\s*:\s*(?<type>\w+(?:\s*\(\s*[^)]+\s*\))?)\s*(?::=\s*[^;]+?)?\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Normalize a captured VHDL type by collapsing internal whitespace and
    // lowercasing the base keyword. Keeps `std_logic_vector(7 downto 0)`
    // recognisable while tolerating sloppy input like `STD_LOGIC_VECTOR( 7
    // DOWNTO 0 )`.
    private static string NormaliseType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "std_logic";
        var t = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
        // Lowercase the leading identifier only; keep numeric ranges verbatim
        var paren = t.IndexOf('(');
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
        Dictionary<string, object?>? declsHead = null;
        foreach (Match m in SignalDeclRe.Matches(decls))
        {
            var names = m.Groups["names"].Value;
            var capturedType = NormaliseType(m.Groups["type"].Value);
            foreach (var raw in names.Split(','))
            {
                var nm = raw.Trim();
                if (nm.Length == 0) continue;
                var d = MkBlock("kmila_vhdl_signal_decl");
                AddField(d, "NAME", nm);
                AddField(d, "TYPE", capturedType);
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
        whenPositions.Add(endIdx - 4); // start of "end case"

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

    // F-3 (2026-06-28) — known unrecognized sequential constructs. Detect
    // the construct head at scan[start], depth-track to its matching `end
    // <kw>;` terminator, emit the verbatim text as ONE raw_seq block.
    // Returns total characters consumed (0 if not a known head). Keeps
    // ParseSequentialBody recovering instead of poisoning the rest of the
    // body when the catalog doesn't cover a statement.
    private int TryConsumeUnknownSequential(
        string scan, string original, int start,
        out Dictionary<string, object?>? rawTile)
    {
        rawTile = null;
        int p = NextNonWhitespace(scan, start);
        var kw = PeekIdent(scan, p);
        if (kw == null) return 0;
        string endKw;
        bool requiresLoop = false;
        switch (kw.ToLowerInvariant())
        {
            case "case":  endKw = "case"; break;
            case "loop":  endKw = "loop"; break;
            case "for":
            case "while": endKw = "loop"; requiresLoop = true; break;
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

    // F-3 (2026-06-28) — concurrent-body counterpart of
    // TryConsumeUnknownSequential. Recognizes labeled statements
    // (`<ident>: <stmt>` — instantiations, named generates, named
    // processes) and bare `for/if … generate` blocks. Emits a single
    // raw_concurrent tile per construct.
    private int TryConsumeUnknownConcurrent(
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
    private static int FindEndKeyword(string scan, int from, string endKw)
    {
        int depth = 0;
        for (int k = from; k < scan.Length; k++)
        {
            var c = scan[k];
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth != 0) continue;
            if (k > 0)
            {
                var prev = scan[k - 1];
                if (char.IsLetterOrDigit(prev) || prev == '_') continue;
            }
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
                    return q + endKw.Length;
                }
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
    // F-7 fix (2026-06-28): falling_edge is a one-regex clone — students
    // doing async-reset designs hit this constantly and the raw_expr
    // fallback was opaque pedagogically.
    private static readonly Regex FallingEdgeRe = new(
        @"^\s*falling_edge\s*\(\s*(?<sig>[^)]+)\s*\)\s*$",
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
