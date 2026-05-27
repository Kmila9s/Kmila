using Interpreter.Services;
using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Scoped holder for the most recent step-replay timeline. Editor.razor
/// pushes a fresh <see cref="ExecutionTimelineDoc"/> here after every
/// simulation run; <c>StepReplayPanel</c> (the new Replay dock tab)
/// subscribes to <see cref="OnChange"/> and re-renders the
/// <c>ExecutionPlayer</c> with the new doc.
/// </summary>
/// <remarks>
/// Scoped (per circuit) so different projects open in different tabs
/// don't trample each other's replay state. The store is in-memory only
/// — a Refresh of the page wipes it intentionally, since the runtime
/// signal history is also gone.
///
/// Phase B (2026-05-19) added the LIVE branch: while a simulation is
/// running, Editor.razor attaches the active <c>DeltaCycleEngine</c>
/// here so the Replay panel can surface Pause / Resume / Step controls
/// that drive the engine directly. The engine is detached when the run
/// ends, and the post-hoc <see cref="Current"/> doc takes over.
/// </remarks>
public class ReplayState
{
    /// <summary>The active replay document, or null when no simulation has run yet.</summary>
    public ExecutionTimelineDoc? Current { get; private set; }

    /// <summary>Fires whenever <see cref="Current"/> is set (or cleared).</summary>
    public event Action? OnChange;

    /// <summary>
    /// Publish a fresh replay. Pass <c>null</c> to clear (e.g. when the
    /// user switches projects and the previous replay no longer applies).
    /// </summary>
    public void Set(ExecutionTimelineDoc? doc)
    {
        Current = doc;
        OnChange?.Invoke();
    }

    // ─── Phase C: detail-level + capture cache ───────────────────────────

    /// <summary>
    /// Maximum number of frames the next rebuild will emit. Default 200
    /// matches Phase A; UI lets the user drop to 50 (faster scrub on long
    /// runs) or raise to 1000 (more fidelity on short runs). Setting this
    /// re-runs <see cref="ReplayBuilder.Build"/> against the cached
    /// waveform so the Replay tab refreshes immediately.
    /// </summary>
    public int MaxFrames { get; private set; } = 200;

    private WaveformData? _cachedWaveform;
    private string _cachedSource = string.Empty;

    /// <summary>
    /// Stores the just-finished simulation's waveform + source, runs an
    /// initial <see cref="ReplayBuilder.Build"/> at the current
    /// <see cref="MaxFrames"/>, and publishes the doc. The cache lets later
    /// <see cref="SetMaxFrames"/> calls re-derive a new doc at a different
    /// detail level without rerunning the simulation.
    /// </summary>
    public void StoreCaptureAndBuild(WaveformData waveform, string source)
    {
        _cachedWaveform = waveform;
        _cachedSource = source ?? string.Empty;
        Current = ReplayBuilder.Build(_cachedWaveform, _cachedSource, MaxFrames);
        OnChange?.Invoke();
    }

    /// <summary>
    /// Phase C: change the detail level. Triggers a rebuild against the
    /// cached waveform if one is present; otherwise just updates the field
    /// so the next capture honours the new setting.
    /// </summary>
    public void SetMaxFrames(int max)
    {
        if (max <= 0) return;
        MaxFrames = max;
        if (_cachedWaveform != null)
        {
            Current = ReplayBuilder.Build(_cachedWaveform, _cachedSource, MaxFrames);
            OnChange?.Invoke();
        }
    }

    // ─── Phase B: live-mode wiring ───────────────────────────────────────

    /// <summary>
    /// The active simulation engine while a run is in flight, otherwise
    /// null. Set by <see cref="AttachLive"/>; cleared by
    /// <see cref="DetachLive"/>. UI consumers should never poke the engine
    /// directly — go through <see cref="LivePause"/>, <see cref="LiveResume"/>,
    /// <see cref="LiveStep"/> so the no-engine case stays a single null check.
    /// </summary>
    public DeltaCycleEngine? LiveEngine { get; private set; }

    /// <summary>Fires whenever the live-engine slot is set or cleared.</summary>
    public event Action? OnLiveChanged;

    /// <summary>True while a live simulation is attached.</summary>
    public bool IsLiveActive => LiveEngine != null;

    /// <summary>
    /// True when a live engine is attached AND currently paused (the user
    /// pressed Pause). Lets the UI swap the Pause button for a Resume
    /// button without polling.
    /// </summary>
    public bool IsLivePaused => LiveEngine?.IsPaused ?? false;

    /// <summary>
    /// The growing per-frame timeline built while the simulation runs. Each
    /// engine delta appends one <see cref="TimelineStep"/>. Null until
    /// <see cref="AttachLive"/> is called with a source string; the post-hoc
    /// <see cref="Current"/> doc is what the UI falls back to when this is
    /// absent.
    /// </summary>
    public ExecutionTimelineDoc? LiveDoc { get; private set; }

    /// <summary>
    /// Monotonically-increasing tick counter incremented every time a new
    /// frame is appended to <see cref="LiveDoc"/>. Exposed so the player
    /// component can observe doc growth without comparing list counts.
    /// </summary>
    public int LiveFrameTick { get; private set; }

    /// <summary>
    /// Optional gutter-breakpoint set: source-line numbers (1-based) that
    /// cause the engine to <see cref="DeltaCycleEngine.Pause"/> when ANY
    /// signal mapped to that line transitions in a delta. Mutated by
    /// <see cref="ToggleBreakpoint"/> and consulted from the live-frame
    /// handler. Stays in a separate slot from the post-hoc replay state so
    /// Phase E (breakpoints) survives across runs without being wiped by
    /// <see cref="DetachLive"/>.
    /// </summary>
    public HashSet<int> Breakpoints { get; } = new();

    /// <summary>Fired whenever a breakpoint is toggled — UI redraws gutter glyphs.</summary>
    public event Action? OnBreakpointsChanged;

    public void ToggleBreakpoint(int line)
    {
        if (!Breakpoints.Remove(line)) Breakpoints.Add(line);
        OnBreakpointsChanged?.Invoke();
    }

    private Dictionary<string, List<int>> _liveLineMap = new();
    private Dictionary<string, string> _liveValues = new(StringComparer.OrdinalIgnoreCase);
    private string _liveSource = string.Empty;
    private DateTime _lastUiNudge = DateTime.MinValue;

    /// <summary>
    /// Opt-in: when true, every delta-cycle frame is appended to
    /// <see cref="LiveDoc"/> so the user can scrub frame-by-frame while a sim
    /// is still running. When false (default), the live engine is still
    /// attached — Pause / Resume / Step controls and breakpoints keep
    /// working — but the heavy per-frame snapshot accumulation is skipped
    /// entirely. Settings page lets the user flip it; we read the persisted
    /// value on app boot (see MainLayout).
    /// </summary>
    /// <remarks>
    /// Even when on, the live tape is capped at
    /// <see cref="LiveDocFrameCap"/> entries with FIFO eviction so a runaway
    /// simulation can't OOM the browser tab.
    /// </remarks>
    public bool LiveReplayDuringRun { get; set; } = false;

    /// <summary>
    /// Hard cap on the number of <see cref="TimelineStep"/> entries kept in
    /// <see cref="LiveDoc"/> while accumulating. 5000 frames covers any
    /// reasonable pedagogical inspection (more than the eye can absorb
    /// scrolling); older frames roll off the front when the cap is hit.
    /// Not user-tunable — the on/off toggle is the user-facing knob.
    /// </summary>
    public const int LiveDocFrameCap = 5000;

    /// <summary>
    /// Wire the engine into this scope. The Editor calls this right after
    /// constructing the SimulationRunner for the upcoming run. Replaces any
    /// previous engine and seeds a fresh live-timeline doc so the Replay
    /// panel can render frames as they arrive.
    /// </summary>
    /// <param name="engine">The running engine — subscribed to
    /// <c>OnFrameEmitted</c> until <see cref="DetachLive"/>.</param>
    /// <param name="sourceCode">User's VHDL source. Used to build the
    /// signal→line map so each live frame can carry highlight lines.</param>
    public void AttachLive(DeltaCycleEngine engine, string sourceCode = "")
    {
        // Stop subscribing to whatever was attached before — switching
        // engines mid-session is rare but should leave no dangling handlers.
        if (LiveEngine != null)
            LiveEngine.OnFrameEmitted -= OnLiveFrameEmitted;

        LiveEngine = engine;
        _liveSource = sourceCode ?? string.Empty;
        _liveValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _liveLineMap = BuildLiveLineMap(_liveSource);

        // Only build the live tape doc when the opt-in setting is on. When
        // off, the OnFrameEmitted handler still attaches — breakpoints + the
        // pause/resume/step controls all rely on the live engine — but the
        // per-frame snapshot allocation that dominates memory is skipped
        // (see OnLiveFrameEmitted).
        if (LiveReplayDuringRun)
        {
            LiveDoc = new ExecutionTimelineDoc
            {
                Code = _liveSource,
                Language = "vhdl",
                SignalOrder = new List<string>(),
                Steps = new List<TimelineStep>()
            };
        }
        else
        {
            LiveDoc = null;
        }
        LiveFrameTick = 0;
        engine.OnFrameEmitted += OnLiveFrameEmitted;
        OnLiveChanged?.Invoke();
    }

    /// <summary>
    /// Drop the engine reference. Called from the Editor's OnSimulationEnded
    /// handler so the UI's Pause/Resume buttons disappear and the post-hoc
    /// replay surface takes over. Detaches the frame handler and clears the
    /// live doc.
    /// </summary>
    public void DetachLive()
    {
        if (LiveEngine != null)
            LiveEngine.OnFrameEmitted -= OnLiveFrameEmitted;
        LiveEngine = null;
        LiveDoc = null;
        LiveFrameTick = 0;
        OnLiveChanged?.Invoke();
    }

    /// <summary>
    /// Per-delta handler: snapshots changed signals into the running value
    /// map, appends a new <see cref="TimelineStep"/> with the union of their
    /// assignment lines, and (throttled) raises <see cref="OnChange"/> so
    /// the Replay panel re-renders.
    ///
    /// Throttling: the simulator can fire thousands of frames per wall-clock
    /// second; gating the UI nudge to ~10 Hz keeps Blazor's renderer from
    /// drowning. Frames keep accumulating in the doc between nudges — the
    /// snapshot the user sees at pause is still complete.
    ///
    /// Breakpoints (Phase E): if any changed signal's mapped line is in
    /// <see cref="Breakpoints"/>, the engine is paused immediately. The
    /// user can then resume or step.
    /// </summary>
    private void OnLiveFrameEmitted(FrameEmittedArgs args)
    {
        // Cheap path when the user hasn't opted into live accumulation:
        // resolve only what breakpoints need (the union of changed-signal
        // source lines) and skip the per-frame snapshot dictionary that
        // dominates memory on long sims. Pause/Resume/Step still work —
        // they read LiveEngine directly, not the doc.
        bool accumulate = LiveReplayDuringRun && LiveDoc != null;

        // We still need the lineSet to fire breakpoints even when not
        // accumulating, but skip it altogether when there are no
        // breakpoints AND we're not accumulating — that's the truly
        // zero-allocation fast path on a long opt-out run.
        bool needLineSet = accumulate || Breakpoints.Count > 0;

        SortedSet<int>? lineSet = needLineSet ? new SortedSet<int>() : null;

        foreach (var sig in args.ChangedSignals)
        {
            var name = sig.Name ?? string.Empty;
            if (string.IsNullOrEmpty(name)) continue;

            // _liveValues is only consumed by the accumulation snapshot;
            // skip updating it when nobody will read it.
            if (accumulate)
                _liveValues[name] = sig.Value?.ToString() ?? "U";

            if (lineSet != null)
            {
                if (args.SourceLines.TryGetValue(sig, out var astLine) && astLine > 0)
                {
                    lineSet.Add(astLine);
                }
                else if (_liveLineMap.TryGetValue(name, out var lines))
                {
                    foreach (var ln in lines) lineSet.Add(ln);
                }
            }
        }

        if (accumulate)
        {
            // Compute a stable signal-order + port-type metadata. Existing
            // positions are preserved so the side table doesn't reshuffle.
            foreach (var sig in args.ChangedSignals)
            {
                var name = sig.Name ?? string.Empty;
                if (string.IsNullOrEmpty(name)) continue;
                if (!LiveDoc!.SignalOrder.Contains(name))
                    LiveDoc.SignalOrder.Add(name);
                if (!LiveDoc.SignalMeta.ContainsKey(name))
                {
                    LiveDoc.SignalMeta[name] = sig.IsPort
                        ? sig.PortType.ToString()
                        : "INTERNAL";
                }
            }

            var t = (int)(args.Tick * 1000) + args.Delta;
            var snapshot = new Dictionary<string, string>(_liveValues);

            LiveDoc!.Steps.Add(new TimelineStep
            {
                T = t,
                HighlightLines = lineSet!.ToList(),
                Signals = snapshot,
                Note = new Dictionary<string, string>
                {
                    ["en"] = $"t = {t} ns",
                    ["es"] = $"t = {t} ns"
                }
            });
            LiveFrameTick++;

            // Hard FIFO cap so an extreme run still terminates with bounded
            // memory. The user-facing on/off toggle warns about the cost;
            // this is the safety net that catches the cases where they
            // forget. 5000 frames ≫ what anyone can usefully scrub.
            if (LiveDoc.Steps.Count > LiveDocFrameCap)
            {
                int excess = LiveDoc.Steps.Count - LiveDocFrameCap;
                LiveDoc.Steps.RemoveRange(0, excess);
            }
        }

        // Phase E: breakpoint check — still fires whether we accumulate or
        // not. The lineSet computation above honoured Breakpoints.Count > 0
        // so this branch only runs when we actually have a set to check.
        if (lineSet != null && Breakpoints.Count > 0)
        {
            foreach (var ln in lineSet)
            {
                if (Breakpoints.Contains(ln))
                {
                    LiveEngine?.Pause();
                    OnLiveChanged?.Invoke();
                    break;
                }
            }
        }

        // Throttle the OnChange nudge to ~10 Hz. Only relevant when
        // accumulating — there's nothing for the panel to re-render
        // otherwise (the post-hoc replay doc is what shows after the run).
        if (accumulate)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUiNudge).TotalMilliseconds >= 100)
            {
                _lastUiNudge = now;
                OnChange?.Invoke();
            }
        }
    }

    private static Dictionary<string, List<int>> BuildLiveLineMap(string source)
    {
        // Mirror ReplayBuilder's regex scanner without taking a hard dep on
        // it — duplicating ~10 lines is cheaper than reshaping ReplayBuilder
        // to expose this helper publicly. Falls back to an empty map when
        // the source is blank (line-highlight column just stays empty).
        var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(source)) return map;
        var lines = source.Replace("\r\n", "\n").Split('\n');
        var assignRx = new System.Text.RegularExpressions.Regex(
            @"([a-zA-Z_][a-zA-Z0-9_]*)\s*(?:\([^)]*\))?\s*(?:<=|:=)",
            System.Text.RegularExpressions.RegexOptions.Compiled);
        var conditionRx = new System.Text.RegularExpressions.Regex(
            @"\b(if|elsif|while|until|assert|return)\b",
            System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var commentIdx = line.IndexOf("--", StringComparison.Ordinal);
            if (commentIdx >= 0) line = line.Substring(0, commentIdx);
            var arrowIdx = line.IndexOf("=>", StringComparison.Ordinal);
            if (arrowIdx >= 0) line = line.Substring(arrowIdx + 2);
            foreach (System.Text.RegularExpressions.Match m in assignRx.Matches(line))
            {
                var prefix = line.Substring(0, m.Index);
                if (conditionRx.IsMatch(prefix)) continue;
                var name = m.Groups[1].Value;
                if (!map.TryGetValue(name, out var list))
                {
                    list = new List<int>();
                    map[name] = list;
                }
                list.Add(i + 1);
            }
        }
        return map;
    }

    /// <summary>Cooperative pause — no-op if no engine is attached.</summary>
    public void LivePause()
    {
        LiveEngine?.Pause();
        OnLiveChanged?.Invoke();
    }

    /// <summary>Cooperative resume — no-op if no engine is attached.</summary>
    public void LiveResume()
    {
        LiveEngine?.Resume();
        OnLiveChanged?.Invoke();
    }

    /// <summary>
    /// Advance by exactly one delta cycle. Implies a prior Pause; the
    /// engine re-pauses itself after one frame fires.
    /// </summary>
    public void LiveStep()
    {
        LiveEngine?.StepOneDelta();
        OnLiveChanged?.Invoke();
    }
}
