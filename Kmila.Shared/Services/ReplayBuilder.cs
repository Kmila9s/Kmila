using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Converts a finished simulation's <see cref="WaveformData"/> into an
/// <see cref="ExecutionTimelineDoc"/> the existing
/// <c>ExecutionPlayer</c> component can step through. Phase A of the
/// live-simulation playback work — see
/// <c>memory/project_editor_live_simulation_playback.md</c>.
/// </summary>
/// <remarks>
/// Strategy in this phase: walk the union of transition times across every
/// signal, build one frame per distinct time. Line-highlight is left empty
/// for now — the AST→source mapping needed to colour the currently-executing
/// process body is Phase B. Even without highlights, the player gives the
/// user a tape transport over the run + the live signal table, which is the
/// 80% of the value.
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
    /// player's read-only Monaco displays. Trim and accept whatever the
    /// editor handed back; we don't parse it again here.</param>
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
            Steps = new List<TimelineStep>(times.Count)
        };

        foreach (var t in times)
        {
            var snap = new Dictionary<string, string>(signals.Count);
            foreach (var s in signals)
            {
                snap[s.Name] = evaluators[s.Name].ValueAt(t);
            }
            doc.Steps.Add(new TimelineStep
            {
                T = (int)t,
                HighlightLines = new List<int>(),
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
                return tr.Value == 1.0 ? "1" : tr.Value == 0.0 ? "0" : tr.Value.ToString() ?? "?";
            return tr.Value.Value.ToString("0");
        }
    }
}
