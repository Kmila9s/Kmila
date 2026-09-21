using Kmila.Shared.Models;
using Parser.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Coarse-grained state of the simulation pipeline. Drives the Run-bar state
/// machine and which tab the dock auto-focuses on transitions.
/// </summary>
public enum SimState
{
    /// <summary>No build has been attempted, or the design has drifted since the last build.</summary>
    Idle,
    /// <summary>Parser is running right now.</summary>
    Building,
    /// <summary>Last build succeeded — parsed AST + resource estimate available.</summary>
    BuiltOk,
    /// <summary>Last build failed — see <see cref="BuildLog"/> for details.</summary>
    BuildError,
    /// <summary>Simulation is actively running, <see cref="SimulationParameters.Progress"/> advances.</summary>
    Running,
    /// <summary>Simulation is paused; user can resume or stop.</summary>
    Paused,
    /// <summary>Simulation finished successfully — waveform populated, Run button becomes "run again".</summary>
    Complete
}

/// <summary>
/// Which simulation engine owns the currently-running "background" sim. Signal-only
/// runs (ProgramBuilder + SimulationCoordinator) keep the delta-cycle engine ticking
/// after the user leaves the Editor by design; Visual runs (VisualRunCoordinator) do
/// not, and their pause/stop plumbing routes through <see cref="VisualRun.VisualRunRegistry"/>.
/// </summary>
public enum SimMode
{
    Signal,
    Visual
}

/// <summary>
/// Shared state for the Run bar, Ports panel, and Output panel on the Editor
/// page. Replaces the assorted local fields that used to live inside the old
/// SimulationParametersModal.
/// </summary>
/// <remarks>
/// All mutators fire <see cref="OnChange"/> (and, for log-adjacent things,
/// <see cref="BuildLog.OnChange"/> via the parent Editor.razor). Components
/// subscribe once, invoke StateHasChanged on change, and render off the
/// properties here. No polling.
/// </remarks>
public class SimulationParameters
{
    /// <summary>Simulation duration — just the numeric magnitude. Scaled by <see cref="Scale"/>.</summary>
    public int Time { get; private set; } = 1000;

    /// <summary>Time unit for <see cref="Time"/> (NS / US / MS / S / M / H).</summary>
    public TimeScale Scale { get; private set; } = TimeScale.US;

    /// <summary>
    /// System clock frequency in MHz for the current sim. Null means "use
    /// whatever the selected FPGA reports as its default" — resolved at run
    /// time by <see cref="StimulusBuilder"/>. When the user explicitly sets
    /// a value here (Run-bar clock field), it wins and a warning chip lights
    /// up because custom clocks can produce very long sims or inaccuracies.
    /// </summary>
    public int? ClockOverrideMhz { get; private set; }

    /// <summary>True when <see cref="ClockOverrideMhz"/> diverges from the FPGA default.</summary>
    public bool ClockIsCustom { get; private set; }

    /// <summary>
    /// Run mode chosen in the Run bar. <c>true</c> = Fast: the event-driven
    /// <c>FastSimulationRunner</c> (skips idle ticks) with live visuals off — a
    /// progress bar during the run, the full waveform built once at the end.
    /// <c>false</c> = Visual (default): the reference <c>SimulationRunner</c> with
    /// the live-updating waveform + step/breakpoint replay. Both produce the same
    /// final waveform; Fast is for seconds-scale runs where the live redraw is the
    /// bottleneck. Session state — see <see cref="SetFastMode"/>.
    /// </summary>
    public bool FastMode { get; private set; }

    /// <summary>Coarse state of the pipeline; drives Run-bar button rendering.</summary>
    public SimState State { get; private set; } = SimState.Idle;

    /// <summary>0..100 — only meaningful while <see cref="State"/> is Running.</summary>
    public double Progress { get; private set; } = 0;

    /// <summary>
    /// Estimated wall-clock seconds remaining for the running sim, or <c>null</c>
    /// when not yet estimable (the run just started, &lt;1% done) or not running.
    /// Derived from the observed progress rate in <see cref="SetProgress"/> —
    /// no upfront model, so it self-calibrates within the first moments of a run.
    /// In Fast mode the estimate is rougher because progress jumps over idle
    /// ticks, but it still converges as the run proceeds.
    /// </summary>
    public double? RemainingSeconds { get; private set; }

    /// <summary>Active wall-clock seconds since the run began (excludes paused time), or null when idle.</summary>
    public double? ElapsedSeconds
    {
        get
        {
            if (_runStartUtc is not { } start || State is not (SimState.Running or SimState.Paused))
                return null;
            var paused = _pausedTotal + (_pauseStartUtc is { } ps ? DateTime.UtcNow - ps : TimeSpan.Zero);
            return Math.Max(0, (DateTime.UtcNow - start - paused).TotalSeconds);
        }
    }

    // Run-timing for the ETA. Paused time is accumulated so it doesn't inflate
    // the rate estimate (see SetState / SetProgress).
    private DateTime? _runStartUtc;
    private DateTime? _pauseStartUtc;
    private TimeSpan _pausedTotal;

    /// <summary>
    /// Port-moment configurations keyed by port name. Populated by
    /// PortMomentInput callbacks in the Ports tab; consumed by the Run button.
    /// </summary>
    public Dictionary<string, PortMomentConfiguration> PortMoments { get; } = new();

    /// <summary>
    /// Name of the currently loaded saved configuration, or null if the user
    /// hasn't loaded/saved one yet. Drives the Configurations pill label.
    /// </summary>
    public string? LoadedName { get; private set; }

    /// <summary>
    /// True when the live state has drifted from the last saved/loaded config
    /// (or from "no config" if nothing has been saved). Drives the dirty dot
    /// on the Configurations pill.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// One-shot "apply this saved configuration once the Editor finishes
    /// loading the project" slot. Set by <c>ProjectsView.CreateSampleAsync</c>
    /// before navigating, consumed + cleared at the tail of
    /// <c>Editor.LoadProjectAsync</c>. Null otherwise.
    /// </summary>
    /// <remarks>
    /// Needed because <see cref="ClearPortMoments"/> runs during project load
    /// (we want project-A stimulus gone when jumping to project-B), so we
    /// can't pre-populate port moments before navigating — they'd be wiped.
    /// </remarks>
    public string? PendingConfigurationName { get; set; }

    /// <summary>
    /// Alternate one-shot slot that carries the config object directly —
    /// skipping the save → name-normalize → DB-round-trip used by
    /// <see cref="PendingConfigurationName"/>. Preferred when the caller
    /// already has the <see cref="KmilaConfiguration"/> in hand (e.g. the
    /// Learn lessons, which build it from the manifest rather than reading
    /// it back from SQLite — avoids subtle key-encoding mismatches for
    /// names with Unicode separators like "·").
    /// </summary>
    public KmilaConfiguration? PendingConfigurationPayload { get; set; }

    /// <summary>
    /// Project ID that owns the currently-running (or most-recently-run)
    /// simulation, or null when nothing's running. Set by the Editor page at
    /// run start / cleared at run end. The sidebar uses this to render a
    /// "running" pulse next to the project card; a returning user can see
    /// at a glance that their long sim is still going.
    /// </summary>
    public int? RunningProjectId { get; private set; }

    /// <summary>
    /// Which engine owns the current run — <see cref="SimMode.Signal"/> for the
    /// ProgramBuilder/DeltaCycleEngine path (background-persistent), <see cref="SimMode.Visual"/>
    /// for VisualRunCoordinator (paused when its tab is hidden, stopped on project change).
    /// The sidebar / project-card badges use this to render a mode chip and to route
    /// external Pause/Resume/Stop through the correct engine.
    /// </summary>
    public SimMode RunningMode { get; private set; } = SimMode.Signal;

    /// <summary>
    /// Starts a run "session" — records which project owns the sim and
    /// fires <see cref="OnChange"/> so reactive UI (sidebar pulse, Run-bar
    /// state machine) picks it up.
    /// </summary>
    public void BeginRun(int projectId, SimMode mode = SimMode.Signal)
    {
        RunningProjectId = projectId;
        RunningMode = mode;
        // Start the ETA clock fresh.
        _runStartUtc = DateTime.UtcNow;
        _pauseStartUtc = null;
        _pausedTotal = TimeSpan.Zero;
        RemainingSeconds = null;
        OnChange?.Invoke();
    }

    /// <summary>Ends the current run session (success or failure).</summary>
    public void EndRun()
    {
        if (RunningProjectId == null) return;
        RunningProjectId = null;
        RunningMode = SimMode.Signal;
        RemainingSeconds = null;
        _runStartUtc = null;
        _pauseStartUtc = null;
        OnChange?.Invoke();
    }

    /// <summary>
    /// "Most recent finished run" cache so a user who navigated away from
    /// the Editor while the sim was running (or right as it ended) can
    /// click back into the project and still see the result. After the
    /// 5-minute window (<see cref="LastRunRecoveryWindow"/>) the cache is
    /// dropped on the next <see cref="TryGetRecentRun"/> read so it isn't
    /// pinned forever on a long-idle session.
    /// </summary>
    public int? LastCompletedProjectId { get; private set; }
    /// <summary>Timestamp (UTC) of the most recent finished run.</summary>
    public DateTime? LastCompletedAt { get; private set; }
    /// <summary>Snapshot of the most recent finished waveform — Editor restores from this on revisit.</summary>
    public WaveformData? LastWaveform { get; private set; }

    /// <summary>How long after a run ends the recovery cache stays warm.</summary>
    public static TimeSpan LastRunRecoveryWindow { get; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Stamps the recovery cache. Called by the SimulationCoordinator on
    /// <see cref="ProgramBuilder.OnSimulationCompleted"/> so the cache is
    /// populated regardless of whether the Editor was mounted at the time.
    /// </summary>
    public void StampCompleted(int projectId, WaveformData? wave)
    {
        LastCompletedProjectId = projectId;
        LastCompletedAt        = DateTime.UtcNow;
        LastWaveform           = wave;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Reads the recovery cache. Returns <c>false</c> (and lazily clears
    /// the cache) when the cached run isn't for <paramref name="projectId"/>,
    /// when there's no cached run, or when the entry has aged past
    /// <see cref="LastRunRecoveryWindow"/>. The lazy clear keeps memory
    /// flat on long idle sessions without needing a separate timer.
    /// </summary>
    public bool TryGetRecentRun(int projectId, out WaveformData? wave)
    {
        wave = null;
        if (LastCompletedProjectId == null || LastCompletedAt == null) return false;
        if (DateTime.UtcNow - LastCompletedAt.Value > LastRunRecoveryWindow)
        {
            LastCompletedProjectId = null;
            LastCompletedAt        = null;
            LastWaveform           = null;
            OnChange?.Invoke();
            return false;
        }
        if (LastCompletedProjectId != projectId) return false;
        wave = LastWaveform;
        return wave != null;
    }

    public event Action? OnChange;

    /// <summary>
    /// Called by setters when the user mutates something worth tracking as a
    /// config change. Flipping an already-dirty flag is a no-op (no redundant
    /// OnChange), which avoids render storms while the user types a number.
    /// </summary>
    private void MarkDirty()
    {
        if (IsDirty) return;
        IsDirty = true;
    }

    /// <summary>
    /// Marks the parameter set as freshly loaded from a saved configuration.
    /// Stamps the configuration name and clears <see cref="IsDirty"/>.
    /// </summary>
    /// <param name="name">Name of the configuration being loaded.</param>
    public void MarkLoaded(string name)
    {
        LoadedName = name;
        IsDirty = false;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Marks the parameter set as just saved under the given name. Clears
    /// <see cref="IsDirty"/> so the "unsaved changes" indicator drops.
    /// </summary>
    /// <param name="name">Name under which the configuration was saved.</param>
    public void MarkSaved(string name)
    {
        LoadedName = name;
        IsDirty = false;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Clears the loaded-configuration link (so the next save creates a new
    /// entry) and resets the dirty flag. No-op when neither is set.
    /// </summary>
    public void ClearLoaded()
    {
        if (LoadedName == null && !IsDirty) return;
        LoadedName = null;
        IsDirty = false;
        OnChange?.Invoke();
    }

    /// <summary>Updates the simulation duration in <see cref="Scale"/> units. Marks dirty.</summary>
    /// <param name="time">New duration value; comparison is by exact equality.</param>
    public void SetTime(int time)
    {
        if (Time == time) return;
        Time = time;
        MarkDirty();
        OnChange?.Invoke();
    }

    /// <summary>Updates the simulation timescale (ns / µs / ms / …). Marks dirty.</summary>
    /// <param name="scale">New time-unit scale.</param>
    public void SetScale(TimeScale scale)
    {
        if (Scale == scale) return;
        Scale = scale;
        MarkDirty();
        OnChange?.Invoke();
    }

    /// <summary>
    /// Sets the per-project clock override (MHz). Pass <c>null</c> or
    /// the FPGA default to clear the override and return to "use FPGA
    /// default" mode. Any value other than <paramref name="fpgaDefault"/>
    /// trips <see cref="ClockIsCustom"/>.
    /// </summary>
    public void SetClockOverride(int? mhz, int fpgaDefault)
    {
        int? clamped = mhz.HasValue && mhz.Value > 0 ? mhz : (int?)null;
        bool same = clamped == ClockOverrideMhz;
        ClockOverrideMhz = clamped;
        ClockIsCustom = clamped.HasValue && clamped.Value != fpgaDefault;
        if (!same)
        {
            MarkDirty();
            OnChange?.Invoke();
        }
    }

    /// <summary>Effective clock in MHz for a sim run — override wins over <paramref name="fpgaDefault"/>.</summary>
    public int GetEffectiveClockMhz(int fpgaDefault)
        => (ClockOverrideMhz.HasValue && ClockOverrideMhz.Value > 0)
            ? ClockOverrideMhz.Value
            : Math.Max(1, fpgaDefault);

    /// <summary>Sets the Fast/Visual run mode. Not a config-dirtying change.</summary>
    /// <param name="fast"><c>true</c> for Fast (headless + full waveform at end), <c>false</c> for Visual.</param>
    public void SetFastMode(bool fast)
    {
        if (FastMode == fast) return;
        FastMode = fast;
        OnChange?.Invoke();
    }

    /// <summary>Transitions the run state machine. Does not mark dirty.</summary>
    /// <param name="state">New simulation state (Idle/Building/Running/Paused/Done/Error).</param>
    public void SetState(SimState state)
    {
        if (State == state) return;
        // Pause accounting for the ETA: freeze the clock while paused so the
        // pause gap doesn't deflate the observed progress rate on resume.
        if (state == SimState.Paused && State == SimState.Running)
            _pauseStartUtc = DateTime.UtcNow;
        else if (state == SimState.Running && State == SimState.Paused && _pauseStartUtc is { } ps)
        {
            _pausedTotal += DateTime.UtcNow - ps;
            _pauseStartUtc = null;
        }
        State = state;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Updates the [0..100] progress percentage. Clamped server-side so progress
    /// reporters can pass raw fractions or percents without checking bounds.
    /// </summary>
    /// <param name="progress">Progress percent in [0, 100].</param>
    public void SetProgress(double progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
        UpdateEta();
        OnChange?.Invoke();
    }

    /// <summary>
    /// Re-estimates <see cref="RemainingSeconds"/> from the observed rate:
    /// remaining = activeElapsed × (100 − p) / p. Uses the whole-run average
    /// (not an instantaneous rate) so it stays stable and self-corrects; needs
    /// a little signal first (≥1% done, ≥0.3 s in) to avoid wild early numbers.
    /// </summary>
    private void UpdateEta()
    {
        if (_runStartUtc is not { } start || State != SimState.Running)
            return; // keep prior value; Begin/End reset it
        if (Progress >= 100) { RemainingSeconds = 0; return; }

        double activeElapsed = (DateTime.UtcNow - start - _pausedTotal).TotalSeconds;
        if (Progress < 1.0 || activeElapsed < 0.3)
        {
            RemainingSeconds = null; // not estimable yet — UI shows "calculating…"
            return;
        }
        double ratePerSec = Progress / activeElapsed;
        RemainingSeconds = ratePerSec > 0 ? (100.0 - Progress) / ratePerSec : (double?)null;
    }

    /// <summary>
    /// Inserts or updates the stimulus configuration for a port. Empty
    /// <c>PortName</c> is ignored. Marks the parameter set dirty.
    /// </summary>
    /// <param name="config">Stimulus configuration; <c>PortName</c> is the dictionary key.</param>
    public void UpsertPortMoment(PortMomentConfiguration config)
    {
        if (string.IsNullOrEmpty(config.PortName)) return;
        PortMoments[config.PortName] = config;
        MarkDirty();
        OnChange?.Invoke();
    }

    /// <summary>Removes every port stimulus and marks dirty. No-op when already empty.</summary>
    public void ClearPortMoments()
    {
        if (PortMoments.Count == 0) return;
        PortMoments.Clear();
        MarkDirty();
        OnChange?.Invoke();
    }

    /// <summary>
    /// Replaces all port moments in one shot — used when loading a saved
    /// configuration so we don't fire OnChange per-port.
    /// </summary>
    public void ReplacePortMoments(IEnumerable<PortMomentConfiguration>? configs)
    {
        PortMoments.Clear();
        if (configs != null)
        {
            foreach (var c in configs)
            {
                if (!string.IsNullOrEmpty(c.PortName))
                    PortMoments[c.PortName] = c;
            }
        }
        OnChange?.Invoke();
    }

    /// <summary>
    /// Returns the port moments as a list for the simulation — filtered in two
    /// dimensions:
    ///   1. per-entry: drop rows the user hasn't given a value to yet (empty
    ///      Value). Those exist in the edit buffer so Add / Remove rendering
    ///      doesn't flicker, but they're meaningless at run time.
    ///   2. per-port: drop ports whose list became empty after step 1.
    /// Returns <c>null</c> when nothing is left, matching the nullability
    /// expected by <c>ProgramBuilder.RunSimulationAsync</c>.
    /// </summary>
    public List<PortMomentConfiguration>? GetPortMomentsForRun()
    {
        var forRun = PortMoments.Values
            .Select(c => new PortMomentConfiguration
            {
                PortName = c.PortName,
                Values = c.Values?.Where(v => !string.IsNullOrEmpty(v.Value)).ToList()
                         ?? new List<PortMomentValue>()
            })
            .Where(c => c.Values.Count > 0)
            .ToList();
        return forRun.Count > 0 ? forRun : null;
    }

    /// <summary>
    /// Converts <see cref="Time"/> and <see cref="Scale"/> to an absolute TimeSpan
    /// the ProgramBuilder understands.
    /// </summary>
    public TimeSpan GetDuration() => Scale switch
    {
        TimeScale.NS => TimeSpan.FromTicks(Time),
        TimeScale.US => TimeSpan.FromMicroseconds(Time),
        TimeScale.MS => TimeSpan.FromMilliseconds(Time),
        TimeScale.S  => TimeSpan.FromSeconds(Time),
        TimeScale.M  => TimeSpan.FromMinutes(Time),
        TimeScale.H  => TimeSpan.FromHours(Time),
        _            => TimeSpan.FromMilliseconds(Time)
    };
}
