using System.Text.RegularExpressions;
using Kmila.Shared.Models;
using Interpreter.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Converts a finished simulation's <see cref="WaveformData"/> into an
/// <see cref="ExecutionTimelineDoc"/> the existing
/// <c>ExecutionPlayer</c> component can step through. Phase A of the
/// live-simulation playback work — see
/// <c>memory/project_editor_live_simulation_playback.md</c>.
/// </summary>
/// <remarks>
/// Phase B (2026-05-19) added Monaco line highlights. Rather than instrument
/// the Interpreter AST end-to-end (canonical Phase B, multi-day refactor),
/// the highlighter regex-scans the user's source code for assignment lines
/// (<c>signal &lt;= …</c> / <c>variable := …</c>) and builds a
/// <c>signalName → line-numbers</c> map. Per frame, the lines of every
/// signal that transitioned exactly at that time are unioned into
/// <see cref="TimelineStep.HighlightLines"/>. This over-highlights
/// conditionally-assigned signals (all branches light up, not just the one
/// that fired) but covers the pedagogical 80% without touching Parser /
/// Operation / SignalHistory.
///
/// Pure function over its inputs; <c>static</c> so no DI registration is
/// needed. Safe to call on any thread.
/// </remarks>
public static class ReplayBuilder
{
    /// <summary>
    /// Build a player-ready timeline from <paramref name="waveform"/>.
    /// Returns <c>null</c> when there's no data worth replaying (zero
    /// signals or zero transitions); callers should render an empty state
    /// in that case.
    /// </summary>
    /// <param name="waveform">Captured simulation output. Already in the
    /// transition-list form the SVG viewer uses.</param>
    /// <param name="sourceCode">The user's main VHDL source — what the
    /// player's read-only Monaco displays. Phase B scans this for assignment
    /// lines to populate Monaco highlights; pass the same text the user
    /// authored, untrimmed, with newlines intact.</param>
    /// <param name="maxFrames">Hard cap on emitted frames so long runs
    /// stay scrub-snappy. The tape stays linear; we just downsample to
    /// at most this many distinct times. Default 200 — well under what
    /// Monaco / the signal table re-render comfortably handle.</param>
    public static ExecutionTimelineDoc? Build(
        WaveformData? waveform,
        string sourceCode,
        int maxFrames = 200)
    {
        if (waveform == null || waveform.Signals.Count == 0) return null;
        var signals = waveform.Signals;

        // Union of all distinct transition times across every signal +
        // an implicit t=0 frame so the player opens on a clean state.
        var timeSet = new SortedSet<long> { 0 };
        foreach (var s in signals)
            foreach (var t in s.Transitions)
                timeSet.Add(t.TimeNs);
        var times = timeSet.ToList();
        if (times.Count == 0) return null;

        // Downsample to maxFrames if the run is wide. Keep first + last
        // points and stride through the middle so the tape's start/end
        // stay anchored to the actual sim boundary.
        if (times.Count > maxFrames)
        {
            var picked = new List<long>(maxFrames);
            picked.Add(times[0]);
            double step = (double)(times.Count - 1) / (maxFrames - 1);
            for (int i = 1; i < maxFrames - 1; i++)
                picked.Add(times[(int)Math.Round(i * step)]);
            picked.Add(times[^1]);
            // Dedupe (Math.Round can collide on dense inputs)
            times = picked.Distinct().OrderBy(t => t).ToList();
        }

        // Phase B: pre-compute the per-time set of signals that transitioned
        // exactly at that timestamp. O(transitions) once, then O(1) per frame.
        // Phase B+ Deep AST: ALSO keep the source line of the winning
        // transition (last delta at that tick) so each frame can read it
        // directly instead of unioning every assignment line for the signal.
        var transitionsAt = new Dictionary<long, HashSet<string>>();
        var transitionLineAt = new Dictionary<(long t, string name), int>();
        foreach (var s in signals)
        {
            foreach (var tr in s.Transitions)
            {
                if (!transitionsAt.TryGetValue(tr.TimeNs, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    transitionsAt[tr.TimeNs] = set;
                }
                set.Add(s.Name);
                if (tr.SourceLine > 0)
                    transitionLineAt[(tr.TimeNs, s.Name)] = tr.SourceLine;
            }
        }

        // Phase B: regex-scan the source for assignment lines per signal.
        // Used as a fallback when the engine didn't tag a transition with a
        // real source line (synthetic ops, legacy paths).
        var signalNames = new HashSet<string>(
            signals.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var lineMap = BuildSignalLineMap(sourceCode, signalNames);

        // For each signal, precompute a step-function evaluator that returns
        // the signal's value at any time by scanning forward through its
        // transition list. This keeps the per-frame cost linear in
        // (signals × log(transitions)) instead of (signals × transitions).
        var evaluators = signals.ToDictionary(
            s => s.Name,
            s => new SignalEvaluator(s.Transitions, s.BitWidth));

        var doc = new ExecutionTimelineDoc
        {
            Code = sourceCode ?? string.Empty,
            Language = "vhdl",
            SignalOrder = signals.Select(s => s.Name).ToList(),
            // Phase D: per-signal port-type metadata drives the filter / group
            // UI in the side table. Hand-authored Concepts lessons leave this
            // empty; the player hides the filter UI when both this is empty
            // AND the signal count is small.
            SignalMeta = signals.ToDictionary(
                s => s.Name,
                s => string.IsNullOrEmpty(s.PortType) ? "INTERNAL" : s.PortType,
                StringComparer.OrdinalIgnoreCase),
            Steps = new List<TimelineStep>(times.Count)
        };

        foreach (var t in times)
        {
            var snap = new Dictionary<string, string>(signals.Count);
            foreach (var s in signals)
            {
                snap[s.Name] = evaluators[s.Name].ValueAt(t);
            }

            // Phase B+ Deep AST: which lines were responsible for THIS frame?
            // Prefer the AST-tagged transition.SourceLine (one line, the
            // exact firing branch); fall back to the regex map (all
            // assignment lines for the signal) only when the engine didn't
            // know — synthetic ops, ConditionalAssignment branches, etc.
            var highlights = new SortedSet<int>();
            if (transitionsAt.TryGetValue(t, out var changedHere))
            {
                foreach (var name in changedHere)
                {
                    if (transitionLineAt.TryGetValue((t, name), out var astLine) && astLine > 0)
                    {
                        highlights.Add(astLine);
                    }
                    else if (lineMap.TryGetValue(name, out var lines))
                    {
                        foreach (var ln in lines) highlights.Add(ln);
                    }
                }
            }

            doc.Steps.Add(new TimelineStep
            {
                T = (int)t,
                HighlightLines = highlights.ToList(),
                Signals = snap,
                Note = new Dictionary<string, string>
                {
                    ["en"] = $"t = {t} ns",
                    ["es"] = $"t = {t} ns"
                }
            });
        }
        return doc;
    }

    // ─── Source-line mapping ─────────────────────────────────────────────

    /// <summary>
    /// Scans <paramref name="sourceCode"/> for lines that assign one of the
    /// signals in <paramref name="signalNames"/> and returns a map of
    /// <c>signalName → 1-based line numbers</c>. The scan is heuristic but
    /// tuned for the VHDL idioms students actually write:
    /// <list type="bullet">
    ///   <item>Plain concurrent: <c>y &lt;= a and b;</c></item>
    ///   <item>Sliced LHS: <c>q(3 downto 0) &lt;= "1010";</c></item>
    ///   <item>Case branches: <c>when S0 =&gt; out &lt;= '0';</c>
    ///   (the scanner strips a leading <c>when X =&gt;</c> before matching).</item>
    ///   <item>Variable assignments: <c>v := value;</c></item>
    /// </list>
    /// Comparison <c>&lt;=</c> inside <c>if</c> / <c>elsif</c> / <c>while</c>
    /// conditions is rejected by looking at the prefix before the operator.
    /// </summary>
    private static Dictionary<string, List<int>> BuildSignalLineMap(
        string? sourceCode, HashSet<string> signalNames)
    {
        var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(sourceCode) || signalNames.Count == 0) return map;

        var lines = sourceCode.Replace("\r\n", "\n", System.StringComparison.Ordinal).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var name in ExtractAssignedNames(lines[i]))
            {
                if (!signalNames.Contains(name)) continue;
                if (!map.TryGetValue(name, out var list))
                {
                    list = new List<int>();
                    map[name] = list;
                }
                list.Add(i + 1); // 1-based — Monaco line numbers start at 1
            }
        }
        return map;
    }

    // Pass 18 (2026-07-30): use the shared balanced-paren fragment so
    // `mem(to_integer(unsigned(addr))) <= din;` picks up the LHS
    // identifier `mem` instead of matching nothing and leaving the line
    // out of the line-highlight map.
    private static readonly Regex _assignRx = new(
        @"([a-zA-Z_][a-zA-Z0-9_]*)" + OperationsRegex.BalancedIndexSuffix + @"\s*(?:<=|:=)",
        RegexOptions.Compiled);

    private static readonly Regex _conditionRx = new(
        @"\b(if|elsif|while|until|assert|return)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Pulls every LHS identifier from a single source line, handling case-
    /// branch and when/else continuations along the way. Yields nothing if
    /// the line looks like a condition (<c>if</c>, <c>elsif</c>, <c>while</c>
    /// before the <c>&lt;=</c>) so VHDL comparisons don't bleed into the
    /// highlight map.
    /// </summary>
    private static IEnumerable<string> ExtractAssignedNames(string rawLine)
    {
        var line = rawLine;
        // Strip line comments — anything after "--" is irrelevant for matching.
        var commentIdx = line.IndexOf("--", StringComparison.Ordinal);
        if (commentIdx >= 0) line = line.Substring(0, commentIdx);

        // Drop the leading "when X =>" arm of a case branch so the actual
        // assignment after the arrow can match. Same trick for the
        // when/else priority chain inside a concurrent assignment.
        var arrowIdx = line.IndexOf("=>", StringComparison.Ordinal);
        if (arrowIdx >= 0) line = line.Substring(arrowIdx + 2);

        foreach (Match m in _assignRx.Matches(line))
        {
            var prefix = line.Substring(0, m.Index);
            // Reject if any condition keyword sits before the operator —
            // means we matched the inside of an `if x <= 0 then …` test.
            if (_conditionRx.IsMatch(prefix)) continue;
            yield return m.Groups[1].Value;
        }
    }

    /// <summary>
    /// Cached forward-scan over a signal's transition list. Caller advances
    /// time monotonically; <see cref="ValueAt"/> returns the value held by
    /// the signal at the requested <c>t</c> (the most recent transition
    /// with <c>TimeNs &lt;= t</c>). Non-monotonic queries still work but
    /// pay an O(N) restart cost — fine for the small frame counts the
    /// player consumes.
    /// </summary>
    private sealed class SignalEvaluator
    {
        private readonly List<WaveformTransition> _transitions;
        private readonly int _bitWidth;
        private int _cursor;

        public SignalEvaluator(List<WaveformTransition> transitions, int bitWidth)
        {
            _transitions = transitions;
            _bitWidth = bitWidth;
        }

        public string ValueAt(long t)
        {
            // If t went backwards, rewind. Cheap when most queries are
            // monotonic (the common case here).
            if (_cursor > 0 && _transitions[_cursor - 1].TimeNs > t)
                _cursor = 0;
            while (_cursor < _transitions.Count && _transitions[_cursor].TimeNs <= t)
                _cursor++;
            if (_cursor == 0) return _bitWidth > 1 ? "0…" : "U";

            var tr = _transitions[_cursor - 1];
            if (!string.IsNullOrEmpty(tr.Label)) return tr.Label!;
            if (tr.Value == null) return "U";
            // std_logic: render 0 or 1 directly. Wider buses get a decimal
            // form when no Label was provided (rare).
            if (_bitWidth == 1)
                return tr.Value == 1.0 ? "1" : tr.Value == 0.0 ? "0" : tr.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
            return tr.Value.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
