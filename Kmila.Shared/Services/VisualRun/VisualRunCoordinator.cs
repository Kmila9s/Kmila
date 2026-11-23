using Interpreter.Models;
using Interpreter.Services;
using Kmila.Shared.Services;
using Parser.Models;
using static Parser.Models.Constants;
using Entity = Interpreter.Services.Entity;
using Architecture = Interpreter.Services.Architecture;

namespace Kmila.Shared.Services.VisualRun;

// Phase 0 seam. Drives the existing DeltaCycleEngine one tick at a time so
// the visual layer can inject live input writes between ticks. Reuses the
// engine — no parallel sim motor.
//
// Loop shape per tick:
//   1) Drain InputStateMap → write to bound input ports. Variable.Value =
//      newValue triggers OnValueChange → SignalAttributeHandler activates
//      the event flag automatically; the engine reads those flags at the
//      top of RunTimeStepAsync and propagates the change.
//   2) engine.AdvanceTime(tick); await engine.RunTimeStepAsync();
//   3) Snapshot output ports + push into FrameRing; raise OnFrame.
//   4) Speed-throttle (delay or yield).
//
// Bounded HeadlessSimulator + ReplayState live-attach paths are untouched.

/// <summary>
/// Drives the existing <c>DeltaCycleEngine</c> one tick at a time so the visual
/// layer can inject live input writes between ticks, throttle to a watchable
/// speed, and snapshot output ports into a history ring for scrubbing.
/// </summary>
public sealed class VisualRunCoordinator : IDisposable
{
    private readonly Entity _entity;
    private readonly Architecture _arch;
    private readonly HashSet<Variable> _allSignals;
    private readonly Dictionary<string, Variable> _portsByName;
    // Phase 1: ports the user is driving directly via Switch/Button widgets.
    // The auto-clock loop must NOT toggle these — otherwise it'd race with
    // the user's input. Empty for the Phase 0 trivial passthrough path.
    private readonly HashSet<string> _userDrivenPorts;
    private readonly Traductor? _traductor;
    private List<Variable> _clockPorts = new();
    private DeltaCycleEngine? _engine;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    // Half-period (in coordinator ticks) for any auto-clocked input port.
    // 8 ticks at Normal speed (16 ms/tick) gives a ~4 Hz clock — slow
    // enough to watch a 4-bit counter visibly tick through 16 states in
    // ~4 s. Tunable in Phase 2 (per-design clock rate UI).
    private const int CLOCK_HALF_PERIOD_TICKS = 8;

    /// <summary>Port-name → driven-value map written by input widgets (Button/Switch/Stepper) and drained into the design at the top of each tick.</summary>
    public InputStateMap Inputs { get; } = new();
    /// <summary>Bounded ring buffer of past tick snapshots — backs the scrub slider and history readback.</summary>
    public FrameRing Frames { get; }

    // Phase 3++ (2026-06-26): button-bounce chatter. Per-port FIFO of
    // values written to the port one-per-tick, OVERRIDING what
    // InputStateMap holds. Once a port's queue empties, the steady
    // intent from InputStateMap reasserts. Lock-guarded because
    // EnqueueBounce is called from UI threads while the loop drains
    // from Task.Run.
    private readonly Dictionary<string, Queue<string>> _bounceQueues = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _bounceGate = new();

    // Ring 5 (2026-07-14): write-side fan-in. When a widget binding carries
    // a slice spec ("cols_in[0]", "bus[7:4]"), the widget's driven value
    // lands here — keyed by (port name, slice). At drain time each port's
    // contributions are composed by SliceHelper.Compose into the full
    // packed vector and pushed to Inputs, then the existing drain path
    // writes it to the port variable.
    private readonly Dictionary<string, Dictionary<BitSlice, string>> _slicedInputs
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _slicedGate = new();
    /// <summary>Playback speed preset that determines the per-tick delay.</summary>
    public VisualSpeed Speed { get; set; } = VisualSpeed.Normal;
    // Phase 2: when non-null, overrides Speed's preset delay. Lets the UI's
    // numeric step-rate field hold an exact value (e.g. 5 ms/tick) without
    // having to invent a new VisualSpeed variant per option. null means
    // "use Speed.TickDelayMs()".
    /// <summary>Exact per-tick delay in milliseconds; overrides <see cref="Speed"/> when non-null.</summary>
    public int? CustomTickDelayMs { get; set; }
    /// <summary>True while the run loop task is active.</summary>
    public bool IsRunning => _loopTask is { IsCompleted: false };
    /// <summary>True when the loop is paused (running but not advancing ticks).</summary>
    public bool IsPaused { get; private set; }
    /// <summary>The most recently completed tick number.</summary>
    public ulong CurrentTick { get; private set; }

    // Phase 2 tick breakpoints. Set membership check happens at the top of
    // each tick — cheap, fire-once semantics: when CurrentTick equals an
    // entry the coordinator pauses, raises OnBreakpointHit, and won't pause
    // again on the same tick until the user advances. The set itself is
    // user-owned and can be mutated mid-run.
    /// <summary>Tick numbers at which the coordinator should auto-pause. User-owned; safe to mutate mid-run.</summary>
    public HashSet<ulong> TickBreakpoints { get; } = new();
    /// <summary>Raised once on each breakpoint hit, carrying the tick number that triggered the pause.</summary>
    public event Action<ulong>? OnBreakpointHit;
    private ulong _lastBreakTick = ulong.MaxValue; // sentinel so tick=0 can still break

    /// <summary>Fires once per completed tick. Carries the tick number; subscribers read <see cref="LatestPortValue"/> / <see cref="LatestSlicedValue"/> or scan <see cref="Frames"/>.</summary>
    public event Action<ulong>? OnFrame;
    /// <summary>Raised when the loop catches an exception. The coordinator stops itself after raising this.</summary>
    public event Action<string>? OnError;
    /// <summary>Raised after the loop task has fully completed (canceled or errored). Safe place to flip UI back to idle.</summary>
    public event Action? OnStopped;

    /// <summary>Creates a coordinator that will drive the given entity's architecture.</summary>
    /// <param name="entity">Entity to simulate; must have an architecture.</param>
    /// <param name="ringCapacity">Capacity of the frame history ring buffer.</param>
    /// <param name="userDrivenPorts">Ports driven directly by the user; the auto-clock loop leaves these alone.</param>
    /// <param name="traductor">Optional translator used to localize error messages.</param>
    public VisualRunCoordinator(Entity entity, int ringCapacity = 2000, IEnumerable<string>? userDrivenPorts = null, Traductor? traductor = null)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        _traductor = traductor;
        _arch = entity.Architecture
            ?? throw new InvalidOperationException(LocalizeErr("VISUAL_ERR_NO_ARCHITECTURE",
                "Entity '{0}' has no architecture.", entity.Name));
        _allSignals = new HashSet<Variable>();
        foreach (var p in entity.Ports) _allSignals.Add(p);
        foreach (var s in _arch.Signals) _allSignals.Add(s);
        _portsByName = entity.Ports.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _userDrivenPorts = new HashSet<string>(
            userDrivenPorts ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        Frames = new FrameRing(ringCapacity);
    }

    /// <summary>The entity's ports.</summary>
    public IReadOnlyCollection<Variable> Ports => _entity.Ports;

    /// <summary>Returns the current value of the named port, or an empty string if not found.</summary>
    /// <param name="portName">Name of the port to read.</param>
    /// <returns>The port's current value as a string, or empty if unknown.</returns>
    public string LatestPortValue(string portName)
    {
        if (_portsByName.TryGetValue(portName, out var v))
            return v.Value?.ToString() ?? "";
        return "";
    }

    /// <summary>
    /// Phase 3++ slice-aware read. Accepts a binding string that may carry
    /// a trailing slice spec like "count[3]" or "bus[7:0]" — returns the
    /// matching substring of the port's value. Bare names behave the same
    /// as <see cref="LatestPortValue"/>. Returns empty string when the
    /// port isn't found or the slice is out-of-range.
    /// </summary>
    public string LatestSlicedValue(string binding)
    {
        var (name, slice) = SliceHelper.Parse(binding);
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var full = LatestPortValue(name);
        // Pass the port so declared to/downto + offset ranges resolve correctly.
        _portsByName.TryGetValue(name, out var port);
        return SliceHelper.Extract(full, slice, port);
    }

    /// <summary>
    /// Widget-facing convenience: writes a value for a binding string. When
    /// the binding contains a slice spec (e.g. "cols_in[0]" or "bus[7:4]"),
    /// dispatches to <see cref="SetSlicedInput"/>; otherwise falls through
    /// to <see cref="InputStateMap.Set"/>. Widgets can call this
    /// unconditionally without having to know whether the user chose a
    /// whole-port or sliced binding.
    /// </summary>
    public void WriteInput(string binding, string value)
    {
        if (string.IsNullOrEmpty(binding)) return;
        if (binding.Contains('[', StringComparison.Ordinal))
            SetSlicedInput(binding, value);
        else
            Inputs.Set(binding, value);
    }

    /// <summary>
    /// Write-side fan-in (Ring 5): store a per-slice contribution for a
    /// packed input port. Called via <see cref="WriteInput"/> when a binding
    /// string like "cols_in[0]" is resolved. Contributions are re-composed
    /// each drain via <see cref="SliceHelper.Compose"/> — disjoint slices
    /// from different widgets pack correctly; undriven bits default to '0'.
    /// </summary>
    public void SetSlicedInput(string binding, string value)
    {
        var (portName, slice) = SliceHelper.Parse(binding);
        if (string.IsNullOrEmpty(portName) || slice is null) return;
        if (!_portsByName.ContainsKey(portName)) return;
        lock (_slicedGate)
        {
            if (!_slicedInputs.TryGetValue(portName, out var perSlice))
            {
                perSlice = new Dictionary<BitSlice, string>();
                _slicedInputs[portName] = perSlice;
            }
            perSlice[slice.Value] = value ?? string.Empty;
        }
    }

    /// <summary>Starts the run loop on a background task. No-op if already running.</summary>
    /// <returns>A completed task; the loop runs in the background.</returns>
    public Task StartAsync()
    {
        if (IsRunning) return Task.CompletedTask;
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Phase 3++ button bounce: enqueue a chatter pattern that overrides
    /// the steady-state input on the given port for one tick per queued
    /// value. After the queue drains, <see cref="InputStateMap"/>'s
    /// current value takes over again. Re-enqueueing replaces the
    /// previous queue (so a press during release-chatter cleanly truncates
    /// the release noise — what a real button does when you re-press it).
    /// </summary>
    public void EnqueueBounce(string portName, IReadOnlyList<string> chatter)
    {
        if (string.IsNullOrEmpty(portName) || chatter is null || chatter.Count == 0) return;
        lock (_bounceGate)
        {
            var q = new Queue<string>(chatter.Count);
            foreach (var v in chatter) q.Enqueue(v);
            _bounceQueues[portName] = q;
        }
    }

    /// <summary>Pauses the loop; ticks stop advancing until <see cref="Resume"/> is called.</summary>
    public void Pause() => IsPaused = true;
    /// <summary>Resumes a paused loop.</summary>
    public void Resume()
    {
        // Clearing _lastBreakTick lets the engine break on the same tick
        // again if the user manually re-pauses then resumes — but the
        // common path is "Resume past the breakpoint and continue", which
        // needs the next tick to NOT immediately re-break. We keep the
        // marker; only a fresh Run (Start) re-arms it.
        IsPaused = false;
    }

    /// <summary>Requests cancellation of the run loop.</summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    // Await the loop task's completion. Used by Visual.razor's StopRun so
    // the post-click render correctly observes IsRunning=false instead of
    // catching the coordinator mid-cancellation.
    /// <summary>Awaits completion of the run loop task after a <see cref="Stop"/>.</summary>
    /// <returns>The loop task, or a completed task if the loop never started.</returns>
    public Task WaitForStopAsync() => _loopTask ?? Task.CompletedTask;

    private async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            InitializeSignals();
            _engine = new DeltaCycleEngine();
            _engine.Initialize(_allSignals, _arch.Tasks);

            // Phase 1 auto-clock: any IN port whose name reads as a clock
            // (clk / clock / aclk / pclk / clk_*) gets a square wave at
            // CLOCK_HALF_PERIOD_TICKS — UNLESS the user has wired a
            // switch/button to it, in which case they drive it manually.
            _clockPorts = _entity.Ports
                .Where(p => (p.PortType == PortType.IN || p.PortType == PortType.INOUT)
                    && ClockPortPolicy.IsClockPortName(p.Name)
                    && !_userDrivenPorts.Contains(p.Name))
                .ToList();

            ulong tick = 0;
            CurrentTick = 0;

            while (!ct.IsCancellationRequested)
            {
                if (IsPaused)
                {
                    await Task.Delay(20, ct).ConfigureAwait(false);
                    continue;
                }

                // Phase 2 tick-breakpoint check. Pause BEFORE advancing the
                // engine so the user sees the canvas state AT the break tick
                // (otherwise the break would render the *next* tick). The
                // _lastBreakTick guard prevents re-pausing on the same tick
                // after Resume.
                if (TickBreakpoints.Count > 0 && tick != _lastBreakTick
                    && TickBreakpoints.Contains(tick))
                {
                    _lastBreakTick = tick;
                    IsPaused = true;
                    OnBreakpointHit?.Invoke(tick);
                    continue;
                }

                // Auto-toggle clocks first; user-driven inputs override
                // anything that lands on a port the user is also driving
                // (shouldn't happen given _userDrivenPorts filter, but
                // ordering is deliberate: user input is the last word).
                if (tick > 0 && (long)tick % CLOCK_HALF_PERIOD_TICKS == 0)
                {
                    foreach (var clk in _clockPorts)
                    {
                        var cur = clk.Value?.ToString();
                        clk.Value = cur == "1" ? "0" : "1";
                    }
                }
                DrainInputsToPorts();

                _engine.AdvanceTime(tick);
                await _engine.RunTimeStepAsync().ConfigureAwait(false);

                SnapshotFrame(tick);
                CurrentTick = tick;
                OnFrame?.Invoke(tick);

                tick++;

                var speed = Speed;
                // Phase 2: CustomTickDelayMs overrides the preset. 0 means
                // "yield only" (Free-like); negative is treated as 0.
                int? custom = CustomTickDelayMs;
                if (custom.HasValue)
                {
                    int d = custom.Value < 0 ? 0 : custom.Value;
                    if (d > 0) await Task.Delay(d, ct).ConfigureAwait(false);
                    else if ((tick & 0xFF) == 0) await Task.Yield();
                }
                else if (speed == VisualSpeed.Free)
                {
                    if ((tick & 0xFF) == 0) await Task.Yield();
                }
                else
                {
                    var delay = speed.TickDelayMs();
                    if (delay > 0) await Task.Delay(delay, ct).ConfigureAwait(false);
                    else await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException) { /* expected on Stop */ }
        catch (Exception ex)
        {
            OnError?.Invoke(LocalizeErr("VISUAL_ERR_RUNTIME", "Runtime error: {0}", (LineMap?.Locate(ex) ?? ex).Message));
        }
        finally
        {
            _engine?.Dispose();
            _engine = null;
            OnStopped?.Invoke();
        }
    }

    /// <summary>Where the lines of an elaborated design come from, so a run-time error names the user's file and line (G82).</summary>
    public Interpreter.Services.ElaborationLineMap? LineMap { get; set; }

    /// <summary>Initial values of objects declared without <c>:=</c> (the app setting; LRM by default).</summary>
    public InitialValuePolicy InitialValues { get; set; } = InitialValuePolicy.Lrm;

    // Inputs are driven by the widgets (a released switch drives '0'); every
    // other object starts under the initial-value policy, like a timed run.
    private void InitializeSignals()
    {
        var widgetDriven = new HashSet<Variable>(_entity.Ports.Where(p => p.PortType is PortType.IN or PortType.INOUT));
        InitialValueSeeder.Apply(InitialValues, InitialValueSeeder.ObjectsOf(_entity), widgetDriven);
        foreach (var port in widgetDriven) port.Value = port.Size > 1 ? new string('0', port.Size) : "0";

        // Seed InputStateMap with current values for every input/inout port
        // so widgets that never write get a deterministic baseline.
        foreach (var port in widgetDriven)
            if (string.IsNullOrEmpty(Inputs.Get(port.Name)))
                Inputs.Set(port.Name, port.Value.ToString()!);
    }

    private void DrainInputsToPorts()
    {
        // Ring 5: compose sliced contributions into a packed port value
        // BEFORE reading the InputStateMap snapshot, so the composed value
        // flows through the same drain path (bounce + normalize + port
        // write). Only touches ports that have at least one slice
        // contribution — a whole-port `Inputs.Set` still wins for ports
        // with no sliced writers.
        lock (_slicedGate)
        {
            foreach (var (portName, perSlice) in _slicedInputs)
            {
                if (perSlice.Count == 0) continue;
                if (!_portsByName.TryGetValue(portName, out var port)) continue;
                int width = port.Size > 0 ? port.Size : 1;
                // Pass the port so declared to/downto + offset ranges compose correctly.
                string composed = SliceHelper.Compose(width, perSlice, port);
                Inputs.Set(portName, composed);
            }
        }

        // Pull the next chatter value for every port that has a non-empty
        // bounce queue. A port with chatter ignores InputStateMap this
        // tick; once the queue drains the steady-state intent reasserts.
        Dictionary<string, string>? bounceOverlay = null;
        lock (_bounceGate)
        {
            foreach (var kv in _bounceQueues)
            {
                if (kv.Value.Count == 0) continue;
                bounceOverlay ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bounceOverlay[kv.Key] = kv.Value.Dequeue();
            }
        }

        var snap = Inputs.Snapshot();
        foreach (var (portName, raw) in snap)
        {
            if (!_portsByName.TryGetValue(portName, out var port)) continue;
            if (port.PortType != PortType.IN && port.PortType != PortType.INOUT) continue;
            string source = raw;
            if (bounceOverlay != null && bounceOverlay.TryGetValue(portName, out var bounced))
            {
                source = bounced;
            }
            string current = port.Value?.ToString() ?? "";
            string normalized = NormalizeForPort(port, source);
            if (current != normalized)
            {
                // Variable.Value setter strips quotes + fires OnValueChange,
                // which flips SignalAttributeHandler._activeFlags[port.Name]
                // = true. The engine picks that up at the top of the next
                // RunTimeStepAsync and notifies sensitive processes.
                port.Value = normalized;
            }
        }

        // Bounce on ports the user hasn't otherwise driven (no entry in
        // InputStateMap): write them directly. Happens when a button is
        // bound to a port that has no other writer.
        if (bounceOverlay != null)
        {
            foreach (var (portName, bounced) in bounceOverlay)
            {
                if (snap.Any(kv => string.Equals(kv.Key, portName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!_portsByName.TryGetValue(portName, out var port)) continue;
                if (port.PortType != PortType.IN && port.PortType != PortType.INOUT) continue;
                string current = port.Value?.ToString() ?? "";
                string normalized = NormalizeForPort(port, bounced);
                if (current != normalized) port.Value = normalized;
            }
        }
    }

    private static string NormalizeForPort(Variable port, string raw)
    {
        if (port.Type == DATATYPES.STD_LOGIC)
        {
            // Tolerate '1', "1", 1 — collapse to single char.
            string stripped = raw.Replace("'", "", System.StringComparison.Ordinal).Replace("\"", "", System.StringComparison.Ordinal);
            if (stripped.Length == 0) return "0";
            return stripped[^1].ToString();
        }
        if (port.Type == DATATYPES.STD_LOGIC_VECTOR && port.Size > 0)
        {
            string stripped = raw.Replace("'", "", System.StringComparison.Ordinal).Replace("\"", "", System.StringComparison.Ordinal);
            if (stripped.Length == port.Size) return stripped;
            if (stripped.Length > port.Size) return stripped[^port.Size..];
            return stripped.PadLeft(port.Size, '0');
        }
        return raw;
    }

    private void SnapshotFrame(ulong tick)
    {
        var dict = new Dictionary<string, string>(_entity.Ports.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in _entity.Ports)
            dict[p.Name] = p.Value?.ToString() ?? "";
        Frames.Push(new VisualFrame(tick, dict));
    }

    /// <summary>Stops the loop, waits briefly for it to finish, and releases engine resources.</summary>
    public void Dispose()
    {
        try { Stop(); } catch { }
        try { _loopTask?.Wait(500); } catch { }
        _cts?.Dispose();
        _engine?.Dispose();
    }

    // Localizes an error message when a Traductor instance was injected by
    // the caller (VisualPanel). Falls back to the English template so tests
    // and non-UI callers that instantiate the coordinator without DI still
    // get a sensible message.
    private string LocalizeErr(string key, string fallbackFormat, params object[] args)
    {
        var template = _traductor?.T(key);
        if (string.IsNullOrEmpty(template)) template = fallbackFormat;
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args);
    }
}
