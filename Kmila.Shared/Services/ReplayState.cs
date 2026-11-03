using Interpreter.Services;
using Interpreter.Models;
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
    /// <summary>
    /// Shared cursor bus. The schematic post-hoc replay (see the
    /// "Schematic replay playback" region) advances this on every step so the
    /// schematic's wire colouring / value chips and the waveform viewer follow
    /// the replay without any extra wiring in those consumers.
    /// </summary>
    private readonly SignalCursorBus _cursor;

    public ReplayState(SignalCursorBus cursor)
    {
        _cursor = cursor;
    }

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
            // Remember roughly where the schematic replay was so a detail
            // change doesn't yank the student back to t=0.
            double frac = 0;
            if (IsReplayActive)
            {
                int oldCount = _replayRunner.Document.Steps.Count;
                if (oldCount > 1) frac = (double)_replayRunner.CurrentIndex / (oldCount - 1);
            }

            Current = ReplayBuilder.Build(_cachedWaveform, _cachedSource, MaxFrames);
            OnChange?.Invoke();

            // Re-seat the schematic replay runner on the rebuilt doc at the
            // equivalent position; code-side players reload via OnChange above.
            if (IsReplayActive && Current != null)
            {
                _replayRunner.Load(Current);
                _replayRunner.SeekTo((int)Math.Round(frac * Math.Max(0, Current.Steps.Count - 1)));
            }
        }
    }

    /// <summary>"Maximum" detail level for the schematic replay — one frame per
    /// distinct transition time, capped like <see cref="LiveDocFrameCap"/> so a
    /// pathological run can't flood the tape. Finer than the 1000-frame "Fine"
    /// preset the code replay offers.</summary>
    public const int MaxFramesMaximum = 5000;

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
    /// When the user hits Resume after a breakpoint pause, they're saying
    /// "let it run." Respecting subsequent breakpoints on the same run would
    /// trap them in a pause/resume ping-pong they didn't ask for. This flag
    /// suppresses the OnLiveFrameEmitted breakpoint check until the user
    /// explicitly pauses again, starts a new run, or steps. Step + Pause
    /// reset it. Matches the "Resume = run to end" mental model the user
    /// surfaced in the 2026-06-14 feedback pass.
    /// </summary>
    private bool _breakpointsMutedUntilPause;

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

    /// <summary>
    /// Waveform-side time breakpoints (UX-N-014). Each entry is an absolute
    /// time in nanoseconds. When a live frame's <c>maxTimeMoment</c> crosses
    /// a value in this set, <see cref="DeltaCycleEngine.Pause"/> fires. This
    /// is the complement to the line-based <see cref="Breakpoints"/> — lets
    /// students stop the sim at a fixed time without hunting for the right
    /// VHDL line. Persists alongside line breakpoints across runs.
    /// </summary>
    public HashSet<long> TimeBreakpoints { get; } = new();

    /// <summary>Per-run last tick observed by the time-breakpoint check; lets us detect crossings.</summary>
    private ulong _prevTimeBpTick;

    /// <summary>Fired whenever a breakpoint (line or time) is toggled — UI redraws.</summary>
    public event Action? OnBreakpointsChanged;

    /// <summary>Toggles a source-line breakpoint at the given line number.</summary>
    /// <param name="line">1-based source line number.</param>
    public void ToggleBreakpoint(int line)
    {
        if (!Breakpoints.Remove(line)) Breakpoints.Add(line);
        OnBreakpointsChanged?.Invoke();
    }

    /// <summary>Toggle a waveform time breakpoint at <paramref name="ns"/> nanoseconds.</summary>
    public void ToggleTimeBreakpoint(long ns)
    {
        if (!TimeBreakpoints.Remove(ns)) TimeBreakpoints.Add(ns);
        OnBreakpointsChanged?.Invoke();
    }

    private Dictionary<string, List<int>> _liveLineMap = new();
    private Dictionary<string, string> _liveValues = new(StringComparer.OrdinalIgnoreCase);
    private string _liveSource = string.Empty;
    private DateTime _lastUiNudge = DateTime.MinValue;

    /// <summary>
    /// Set of 1-based source lines that carry at least one signal/variable
    /// assignment, i.e. lines the engine can actually pause on when a
    /// breakpoint is set. Rebuilt alongside <see cref="_liveLineMap"/> from
    /// the source whenever <see cref="AttachLive"/> or
    /// <see cref="RefreshLineMap"/> runs. Used by the Editor (#4) to flag
    /// breakpoints dropped on untrackable lines (conditions, declarations,
    /// blank lines) so the user knows they'll never fire.
    /// </summary>
    private HashSet<int> _trackedLines = new();

    /// <summary>
    /// Read-only view of <see cref="_trackedLines"/>.
    /// </summary>
    public IReadOnlyCollection<int> TrackedLines => _trackedLines;

    /// <summary>
    /// Fired whenever the trackable-line set is rebuilt (after
    /// <see cref="RefreshLineMap"/> or <see cref="AttachLive"/>). Lets the
    /// breakpoint list UI re-evaluate which breakpoints are inert without
    /// polling — separate from <see cref="OnBreakpointsChanged"/> so the
    /// Editor's repaint→refresh path doesn't loop.
    /// </summary>
    public event Action? OnLineMapChanged;

    // ─── #3: schematic step-through ──────────────────────────────────────

    private int[] _liveActiveLines = Array.Empty<int>();
    private string[] _liveActiveSignals = Array.Empty<string>();
    private DateTime _lastStepNudge = DateTime.MinValue;

    /// <summary>
    /// Opt-in: when true, every delta-cycle frame records which source lines
    /// and signals changed (see <see cref="LiveActiveLines"/> /
    /// <see cref="LiveActiveSignals"/>) and fires <see cref="OnLiveStepChanged"/>
    /// so the schematic can pulse the executing cell (#3). Off by default —
    /// the SchematicViewer flips it on while its Debug mode is showing over a
    /// live run, and back off when it leaves Debug mode, so the per-frame
    /// bookkeeping only runs when something is actually watching.
    /// </summary>
    public bool TrackActiveLines { get; set; }

    /// <summary>
    /// 1-based source lines whose signals changed in the most recent delta.
    /// The schematic maps these to cells (via <c>Cell.SourceLine</c>) and
    /// pulses them. Only meaningful while <see cref="TrackActiveLines"/> is on
    /// (or a breakpoint set / live accumulation forced the per-frame lineset).
    /// </summary>
    public IReadOnlyList<int> LiveActiveLines => _liveActiveLines;

    /// <summary>Names of the signals that changed in the most recent delta.</summary>
    public IReadOnlyList<string> LiveActiveSignals => _liveActiveSignals;

    /// <summary>
    /// Fired after each delta's active lines/signals are recorded (throttled
    /// to ~10 Hz during a free run, immediate when the engine ends the frame
    /// paused — i.e. on a breakpoint hit or a single Step). Separate from
    /// <see cref="OnLiveChanged"/> because that one fires from the UI thread
    /// before the next frame's data exists.
    /// </summary>
    public event Action? OnLiveStepChanged;

    /// <summary>
    /// Opt-in: when true, <see cref="OnLiveRunFrame"/> fires (throttled ~18 Hz)
    /// on every delta DURING a free run too, not only when paused. The schematic
    /// sets this in Debug so it can animate the live activity wavefront via a
    /// lightweight JS DOM update (no Blazor re-render), which the paused-only
    /// <see cref="OnLiveStepChanged"/> path deliberately avoids. Default false,
    /// so no other consumer's cost changes.
    /// </summary>
    public bool AnimateDuringRun { get; set; }

    /// <summary>
    /// Throttled per-delta nudge during a free run, gated by
    /// <see cref="AnimateDuringRun"/>. Consumers read
    /// <see cref="LiveActiveLines"/> / <see cref="LiveActiveSignals"/> and update
    /// the DOM directly — they must NOT trigger a full component re-render here.
    /// </summary>
    public event Action? OnLiveRunFrame;
    private DateTime _lastRunFrame = DateTime.MinValue;
    private DateTime _lastLiveCursorNudge = DateTime.MinValue;

    /// <summary>
    /// True when the given 1-based line carries a signal/variable assignment
    /// that a breakpoint could pause on. False for conditions, declarations,
    /// comments, blank lines — anything the engine emits no transition for.
    /// Reflects the last source the map was built from (see
    /// <see cref="RefreshLineMap"/>).
    /// </summary>
    public bool IsLineTracked(int line) => _trackedLines.Contains(line);

    /// <summary>
    /// Rebuild the signal→line map (and <see cref="TrackedLines"/>) from the
    /// current editor source WITHOUT a live run. The Editor calls this when
    /// breakpoints change so it can classify each breakpoint as trackable or
    /// not before the user ever hits Run. During a run <see cref="AttachLive"/>
    /// rebuilds it from the saved file content.
    /// </summary>
    public void RefreshLineMap(string source)
    {
        _liveSource = source ?? string.Empty;
        _liveLineMap = BuildLiveLineMap(_liveSource);
        _trackedLines = _liveLineMap.Values.SelectMany(v => v).ToHashSet();
        // #5: condition lines (if / elsif / case-when arms) are now valid
        // breakpoint targets too, so they count as "tracked" for the #4 warning.
        _trackedLines.UnionWith(BuildConditionLines(_liveSource));
        OnLineMapChanged?.Invoke();
    }

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
    public bool LiveReplayDuringRun { get; set; }

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
        {
            LiveEngine.OnFrameEmitted -= OnLiveFrameEmitted;
            LiveEngine.OnConditionEvaluated -= OnLiveConditionEvaluated;
        }

        LiveEngine = engine;
        _liveSource = sourceCode ?? string.Empty;
        _liveValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _prevTimeBpTick = 0; // reset crossing tracker so the first frame after Run can hit a time breakpoint
        _breakpointsMutedUntilPause = false; // every new run starts with bps live
        _liveLineMap = BuildLiveLineMap(_liveSource);
        _trackedLines = _liveLineMap.Values.SelectMany(v => v).ToHashSet();
        _trackedLines.UnionWith(BuildConditionLines(_liveSource));
        OnLineMapChanged?.Invoke();

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
        // Enable per-frame active-line bookkeeping for the lifetime of this
        // engine. Used to be flipped on/off by SchematicViewer mounting,
        // which left the data empty when the user ran a sim without
        // visiting the Diagram tab — the Editor's Monaco line highlight
        // path (added 2026-06-10) needs this populated too. The opposite
        // flip happens in DetachLive.
        TrackActiveLines = true;
        engine.OnFrameEmitted += OnLiveFrameEmitted;
        engine.OnConditionEvaluated += OnLiveConditionEvaluated;
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
        {
            LiveEngine.OnFrameEmitted -= OnLiveFrameEmitted;
            LiveEngine.OnConditionEvaluated -= OnLiveConditionEvaluated;
        }
        LiveEngine = null;
        LiveDoc = null;
        LiveFrameTick = 0;
        _liveActiveLines = Array.Empty<int>();
        _liveActiveSignals = Array.Empty<string>();
        // Pair with the AttachLive flip — once nothing is alive to read
        // active-line data, stop populating it on the next live engine
        // (until something asks for it again).
        TrackActiveLines = false;
        OnLiveChanged?.Invoke();
        OnLiveStepChanged?.Invoke();
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
        bool needLineSet = accumulate || Breakpoints.Count > 0 || TrackActiveLines;

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

        // #3: record the active lines/signals BEFORE firing any consumer
        // events. Previously the breakpoint branch fired OnLiveChanged
        // first and then the TrackActiveLines branch populated the arrays,
        // so any subscriber reading Replay.LiveActiveLines from that
        // OnLiveChanged invocation saw the PREVIOUS frame's data (or
        // empty on the first frame) — schematic + Monaco line highlights
        // both fell out of sync as a result.
        if (TrackActiveLines)
        {
            _liveActiveLines = lineSet != null ? lineSet.ToArray() : Array.Empty<int>();
            _liveActiveSignals = args.ChangedSignals
                .Select(s => s.Name ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToArray();
        }

        // Phase E: breakpoint check — still fires whether we accumulate or
        // not. The lineSet computation above honoured Breakpoints.Count > 0
        // so this branch only runs when we actually have a set to check.
        // Skipped while breakpoints are muted (user pressed Resume); that
        // mute stays in effect until the next explicit Pause, Step, or run.
        if (lineSet != null && Breakpoints.Count > 0 && !_breakpointsMutedUntilPause)
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

        // UX-N-014: waveform-side time breakpoints. Fire when the engine's
        // tick crosses any breakpoint time. Timescale is 10 ns per tick
        // (canonical Kmila scale); convert tick → ns and pause on the first
        // frame that reaches or passes a breakpoint. The previous-tick check
        // avoids re-firing on the same boundary across multiple deltas.
        // Honor the same `_breakpointsMutedUntilPause` flag as line bps.
        if (TimeBreakpoints.Count > 0 && args.Tick != _prevTimeBpTick && !_breakpointsMutedUntilPause)
        {
            long nowNs = (long)args.Tick * 10L;
            long prevNs = (long)_prevTimeBpTick * 10L;
            foreach (var bpNs in TimeBreakpoints)
            {
                if (bpNs > prevNs && bpNs <= nowNs)
                {
                    LiveEngine?.Pause();
                    OnLiveChanged?.Invoke();
                    break;
                }
            }
            _prevTimeBpTick = args.Tick;
        }

        // OnLiveStepChanged nudge only fires when the engine is actually
        // paused (post-Step, post-breakpoint). During free run the
        // highlight set churns too fast to read AND re-rendering the
        // schematic 10× per second starves the UI thread when many cells
        // are present — what the user perceives as "step forward freezes
        // the sim". OnLiveChanged still fires from LiveStep/LivePause/
        // LiveResume so consumers see the data right after a pause
        // boundary through that path.
        if (TrackActiveLines && LiveEngine?.IsPaused == true)
        {
            // Move the shared cursor to the engine's current tick so the
            // schematic's Debug value chips + wire colouring (and the waveform
            // viewer's cursor) track the stepped engine. Without this they stay
            // frozen at wherever the user last scrubbed, so a live step looked
            // like it changed nothing. Only on the paused/step boundary — never
            // per-delta during a free run (would churn the waveform).
            _cursor.SetCursorTick(args.Tick);
            _lastStepNudge = DateTime.UtcNow;
            OnLiveStepChanged?.Invoke();
        }
        // Live-activity animation during a FREE run (opt-in). Throttled to ~18 Hz
        // and delivered through a separate event so the consumer updates the DOM
        // directly (JS) rather than re-rendering the whole schematic per frame.
        else if (AnimateDuringRun && TrackActiveLines)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastRunFrame).TotalMilliseconds >= 55)
            {
                _lastRunFrame = now;
                OnLiveRunFrame?.Invoke();
            }
            // Advance the shared cursor at a gentler rate so the Debug value
            // chips + wire colouring refresh DURING a free run — they read the
            // waveform at the cursor, so without this they freeze at the pre-run
            // position (the user's "values don't update during live play").
            // ~5 Hz keeps the Blazor/SignalR re-render cost bounded; the glow
            // stays on the faster JS path above.
            if ((now - _lastLiveCursorNudge).TotalMilliseconds >= 180)
            {
                _lastLiveCursorNudge = now;
                _cursor.SetCursorTick(args.Tick);
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

    /// <summary>
    /// #5: a non-sequential branch was taken (an <c>if</c>/<c>elsif</c>
    /// condition that went true, or a <c>case</c> arm that matched), carrying
    /// the 1-based source line. Pauses the engine if a breakpoint sits on that
    /// line — the signal-transition path can't catch these because the
    /// condition itself produces no signal change. Also feeds the schematic
    /// step-through so the cell on the condition line pulses.
    /// </summary>
    private void OnLiveConditionEvaluated(int line)
    {
        if (line <= 0) return;

        if (TrackActiveLines)
        {
            _liveActiveLines = new[] { line };
            _liveActiveSignals = Array.Empty<string>();
            bool pausedNow = LiveEngine?.IsPaused ?? false;
            var now = DateTime.UtcNow;
            if (pausedNow || (now - _lastStepNudge).TotalMilliseconds >= 100)
            {
                _lastStepNudge = now;
                OnLiveStepChanged?.Invoke();
            }
        }

        if (Breakpoints.Contains(line))
        {
            LiveEngine?.Pause();
            OnLiveChanged?.Invoke();
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
        var lines = source.Replace("\r\n", "\n", System.StringComparison.Ordinal).Split('\n');
        // Pass 18 (2026-07-30): use OperationsRegex.BalancedIndexSuffix
        // so nested-paren LHS shapes surface in the live-line map.
        var assignRx = new System.Text.RegularExpressions.Regex(
            @"([a-zA-Z_][a-zA-Z0-9_]*)" + OperationsRegex.BalancedIndexSuffix + @"\s*(?:<=|:=)",
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

    /// <summary>
    /// #5: scan for lines that hold a breakpoint-able branch condition — an
    /// <c>if</c> / <c>elsif</c> keyword or a <c>case</c> <c>when</c> arm. These
    /// are the lines the engine's <c>OnConditionEvaluated</c> can pause on, so
    /// they're "tracked" for the #4 untracked-breakpoint warning even though no
    /// signal assignment lives there. Mirrors the parser's per-keyword line
    /// capture closely enough for the gutter hint (exact pausing still comes
    /// from the parser-captured line, not this regex).
    /// </summary>
    private static HashSet<int> BuildConditionLines(string source)
    {
        var set = new HashSet<int>();
        if (string.IsNullOrEmpty(source)) return set;
        var lines = source.Replace("\r\n", "\n", System.StringComparison.Ordinal).Split('\n');
        // Leading-keyword match: `if` / `elsif` / `when` at the start of the
        // (trimmed) line. Excludes `end if` / `end case` since those don't lead
        // with the keyword. Conditional-assignment `when` lines also carry a
        // `<=`, so they're already tracked via the assignment scan regardless.
        var rx = new System.Text.RegularExpressions.Regex(
            @"^\s*(if|elsif|when)\b",
            System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var commentIdx = line.IndexOf("--", StringComparison.Ordinal);
            if (commentIdx >= 0) line = line.Substring(0, commentIdx);
            if (rx.IsMatch(line)) set.Add(i + 1);
        }
        return set;
    }

    /// <summary>Cooperative pause — no-op if no engine is attached.</summary>
    public void LivePause()
    {
        // Re-arm breakpoint enforcement: the user explicitly stopped the run,
        // so the next free run should hit breakpoints again.
        _breakpointsMutedUntilPause = false;
        LiveEngine?.Pause();
        OnLiveChanged?.Invoke();
    }

    /// <summary>Cooperative resume — no-op if no engine is attached.</summary>
    /// <remarks>
    /// Resume means "let the sim run to the end". Breakpoints are muted
    /// until the user pauses again, starts a new run, or steps. Without
    /// this the user got trapped in a pause/resume ping-pong every time
    /// a process touched a bp line — they expected "Resume = continue,
    /// stop nagging" and the old behavior felt broken.
    /// </remarks>
    public void LiveResume()
    {
        _breakpointsMutedUntilPause = true;
        LiveEngine?.Resume();
        OnLiveChanged?.Invoke();
    }

    /// <summary>
    /// Advance by exactly one delta cycle. Implies a prior Pause; the
    /// engine re-pauses itself after one frame fires.
    /// </summary>
    /// <remarks>
    /// <c>StepOneDelta</c> works by temporarily calling <c>Resume</c> on the
    /// engine and subscribing a one-shot <c>OnFrameEmitted</c> handler that
    /// sets <c>_isRunning = false</c> again after a single frame. That means
    /// when <c>StepOneDelta</c> returns, the engine is still resumed (the
    /// frame hasn't fired yet) — so an immediate <c>OnLiveChanged.Invoke()</c>
    /// here would see <c>IsLivePaused == false</c>, disable the Step button
    /// in the UI, and leave it stuck disabled because nothing nudges the UI
    /// again after the engine's internal one-shot handler flips
    /// <c>_isRunning</c> back to false.
    ///
    /// Fix: subscribe a second one-shot handler AFTER calling
    /// <c>StepOneDelta</c>. Subscription order is invocation order, so the
    /// engine's internal one-shot runs first (re-pauses the engine), then
    /// ours runs and fires <c>OnLiveChanged</c> — by which time
    /// <c>IsLivePaused</c> is correctly true and the Step button re-enables.
    /// We skip this dance when the engine wasn't paused to begin with
    /// (StepOneDelta would be a no-op then).
    /// </remarks>
    public void LiveStep()
    {
        if (LiveEngine == null)
        {
            OnLiveChanged?.Invoke();
            return;
        }

        // Step Forward is the surgical control — it deliberately respects
        // breakpoints. Reset the Resume-mute so the user can resume bp
        // enforcement just by stepping once.
        _breakpointsMutedUntilPause = false;
        bool wasPaused = LiveEngine.IsPaused;
        LiveEngine.StepOneDelta();

        if (wasPaused)
        {
            Action<FrameEmittedArgs>? followUp = null;
            followUp = _ =>
            {
                var engine = LiveEngine;
                if (engine != null && followUp != null)
                {
                    engine.OnFrameEmitted -= followUp;
                }
                OnLiveChanged?.Invoke();
            };
            LiveEngine.OnFrameEmitted += followUp;
        }
        else
        {
            // Engine was already running — StepOneDelta is a no-op. Fire
            // OnLiveChanged anyway so subscribers can react to whatever
            // state the user observed.
            OnLiveChanged?.Invoke();
        }
    }

    // ─── Schematic replay playback (post-hoc) ────────────────────────────
    //
    // The live branch above forward-steps a running engine. This branch plays
    // the *captured* timeline (`Current`) back through the SAME consumer events
    // the schematic + editor already listen to — `OnLiveStepChanged` for the
    // active-cell glow and Monaco line highlight, and the shared `_cursor` for
    // wire-value colouring and the waveform cursor — so a finished run can be
    // re-watched forwards AND backwards, scrubbed, sped up, and looped, with no
    // engine attached. Reuses `TimelineRunner`, the exact state machine behind
    // the code-side `ExecutionPlayer`.

    private readonly TimelineRunner _replayRunner = new();
    private bool _replayWired;
    private int[] _replayActiveLines = Array.Empty<int>();
    private string[] _replayActiveSignals = Array.Empty<string>();

    /// <summary>True while the schematic replay is driving the shared cursor
    /// and active sets (no live engine attached). Consumers gate their
    /// executing-cell glow on <see cref="IsLiveActive"/> OR this.</summary>
    public bool IsReplayActive { get; private set; }

    /// <summary>When true, autoplay restarts from the top instead of stopping
    /// at the last step.</summary>
    public bool ReplayLoop { get; set; }

    /// <summary>1-based source lines responsible for the current replay step —
    /// the post-hoc analogue of <see cref="LiveActiveLines"/>.</summary>
    public IReadOnlyList<int> ReplayActiveLines => _replayActiveLines;

    /// <summary>Signal names changed at the current replay step.</summary>
    public IReadOnlyList<string> ReplayActiveSignals => _replayActiveSignals;

    /// <summary>True while the replay tape is auto-advancing.</summary>
    public bool ReplayIsPlaying => _replayRunner.IsPlaying;

    /// <summary>0-based index of the current replay step.</summary>
    public int ReplayIndex => _replayRunner.CurrentIndex;

    /// <summary>Total steps in the loaded replay timeline.</summary>
    public int ReplayCount => _replayRunner.Document.Steps.Count;

    /// <summary>Current playback speed multiplier (0.25…4).</summary>
    public double ReplaySpeed => _replayRunner.Speed;

    /// <summary>Enter replay mode over the captured <see cref="Current"/> doc.
    /// No-op when nothing has been captured yet.</summary>
    public void ReplayEnter()
    {
        if (Current == null || Current.Steps.Count == 0) return;
        if (!_replayWired)
        {
            _replayWired = true;
            _replayRunner.OnTick += OnReplayTick;
            _replayRunner.OnComplete += OnReplayComplete;
        }
        _replayRunner.Load(Current);   // resets to step 0, fires OnTick → publish
        IsReplayActive = true;
        PublishReplayStep();
        OnLiveChanged?.Invoke();        // toolbar swap + editor tab badge
        OnLiveStepChanged?.Invoke();    // schematic glow + line highlight
    }

    /// <summary>Leave replay mode and clear the active sets. Does not touch the
    /// cursor (the student may want to keep inspecting the last position).</summary>
    public void ReplayExit()
    {
        _replayRunner.Pause();
        IsReplayActive = false;
        _replayActiveLines = Array.Empty<int>();
        _replayActiveSignals = Array.Empty<string>();
        OnLiveChanged?.Invoke();
        OnLiveStepChanged?.Invoke();
    }

    public void ReplayPlayPause()
    {
        if (_replayRunner.IsPlaying) _replayRunner.Pause();
        else _replayRunner.Play();
        OnLiveChanged?.Invoke();
    }

    public void ReplayPause() { _replayRunner.Pause(); OnLiveChanged?.Invoke(); }
    public void ReplayStepForward() => _replayRunner.StepForward();
    public void ReplayStepBack() => _replayRunner.StepBack();
    public void ReplayRestart() => _replayRunner.Restart();
    public void ReplaySeekTo(int index) => _replayRunner.SeekTo(index);

    public void SetReplaySpeed(double speed)
    {
        _replayRunner.Speed = Math.Clamp(speed, 0.1, 8.0);
        OnLiveChanged?.Invoke();
    }

    public void SetReplayLoop(bool loop)
    {
        ReplayLoop = loop;
        // If we flipped loop on while parked at the end, kick playback again so
        // the toggle feels immediate.
        if (loop && IsReplayActive && !_replayRunner.IsPlaying
            && _replayRunner.CurrentIndex >= _replayRunner.Document.Steps.Count - 1)
        {
            _replayRunner.Restart();
            _replayRunner.Play();
        }
        OnLiveChanged?.Invoke();
    }

    private void OnReplayTick()
    {
        PublishReplayStep();
        OnLiveStepChanged?.Invoke();
    }

    private void OnReplayComplete()
    {
        if (ReplayLoop)
        {
            _replayRunner.Restart();
            _replayRunner.Play();
        }
        else
        {
            OnLiveChanged?.Invoke(); // play button flips back to ▶
        }
    }

    private void PublishReplayStep()
    {
        var step = _replayRunner.CurrentStep;
        _replayActiveLines = step.HighlightLines?.ToArray() ?? Array.Empty<int>();

        // Active signals = the ones that actually CHANGED at this step, not the
        // whole snapshot — otherwise every wire glows on every frame. Diff the
        // current step's values against the PREVIOUS step in the timeline
        // (deterministic for scrub/seek as well as play), mirroring the live
        // mode's "signals that changed this delta" semantics.
        var steps = _replayRunner.Document.Steps;
        int i = _replayRunner.CurrentIndex;
        if (i > 0 && i < steps.Count && step.Signals != null)
        {
            var prev = steps[i - 1].Signals;
            var changed = new List<string>();
            foreach (var kv in step.Signals)
                if (prev == null || !prev.TryGetValue(kv.Key, out var pv) || pv != kv.Value)
                    changed.Add(kv.Key);
            _replayActiveSignals = changed.ToArray();
        }
        else
        {
            // First frame (or empty): nothing has "changed" yet — show the
            // circuit at rest rather than lighting everything up.
            _replayActiveSignals = Array.Empty<string>();
        }

        // step.T is a time in ns (ReplayBuilder emits distinct transition
        // times); the shared cursor is in ticks. Convert via the captured
        // waveform's timescale so wire-value colouring + the waveform cursor
        // land on the right column.
        int scale = Math.Max(1, _cachedWaveform?.TimescaleNs ?? 10);
        _cursor.SetCursorTick((ulong)Math.Max(0, step.T / scale));
    }
}
