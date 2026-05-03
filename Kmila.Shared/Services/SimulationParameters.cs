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

    /// <summary>Coarse state of the pipeline; drives Run-bar button rendering.</summary>
    public SimState State { get; private set; } = SimState.Idle;

    /// <summary>0..100 — only meaningful while <see cref="State"/> is Running.</summary>
    public double Progress { get; private set; } = 0;

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
    /// Starts a run "session" — records which project owns the sim and
    /// fires <see cref="OnChange"/> so reactive UI (sidebar pulse, Run-bar
    /// state machine) picks it up.
    /// </summary>
    public void BeginRun(int projectId)
    {
        RunningProjectId = projectId;
        OnChange?.Invoke();
    }

    /// <summary>Ends the current run session (success or failure).</summary>
    public void EndRun()
    {
        if (RunningProjectId == null) return;
        RunningProjectId = null;
        OnChange?.Invoke();
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

    /// <summary>Transitions the run state machine. Does not mark dirty.</summary>
    /// <param name="state">New simulation state (Idle/Building/Running/Paused/Done/Error).</param>
    public void SetState(SimState state)
    {
        if (State == state) return;
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
        OnChange?.Invoke();
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
