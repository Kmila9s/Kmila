using Kmila.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Singleton (MAUI) / Scoped-per-circuit (Web) bridge from
/// <see cref="ProgramBuilder"/> engine events into
/// <see cref="SimulationParameters"/> + downstream side-effects
/// (snapshot capture, in-app toast, OS notification).
///
/// Lives for the entire app session / circuit so progress + completion
/// events keep flowing regardless of whether the Editor is mounted.
/// Before this service existed, the relay subscriptions lived on the
/// Editor component, so navigating away mid-run silently broke the
/// pipeline: the engine kept ticking inside the singleton Builder, but
/// the sidebar / projects-card "background sim" surfaces froze, the
/// run's snapshot was never captured, and the user was left without
/// any notification of the result.
///
/// Editor still subscribes to <see cref="ProgramBuilder.OnSimulationCompleted"/>
/// for its own waveform-merge + replay-doc work; Coordinator's
/// snapshot capture is independent (it uses
/// <see cref="WaveformData.FromSignalHistory"/> on the raw history).
/// </summary>
public class SimulationCoordinator : IDisposable
{
    private readonly ProgramBuilder _builder;
    private readonly SimulationParameters _sim;
    private readonly SimulationSnapshotStore _snapshots;
    private readonly KmilaToastService _toast;
    private readonly ISimEndNotifier _pushNotifier;
    private readonly SelectedFPGA _fpga;
    private readonly Kmila.Shared.Repositories.Projects _projects;
    private readonly Traductor _traductor;
    private readonly ILogger<SimulationCoordinator> _log;
    private bool _disposed;

    /// <summary>
    /// Captured in <see cref="OnSimulationEnded"/> so the data-handler that
    /// fires immediately afterwards can still resolve the project — we can't
    /// read <c>Sim.RunningProjectId</c> there because <see cref="OnSimulationEnded"/>
    /// has already called <c>EndRun()</c> by that point.
    /// </summary>
    private int _lastEndedProjectId;

    public SimulationCoordinator(
        ProgramBuilder builder,
        SimulationParameters sim,
        SimulationSnapshotStore snapshots,
        KmilaToastService toast,
        ISimEndNotifier pushNotifier,
        SelectedFPGA fpga,
        Kmila.Shared.Repositories.Projects projects,
        Traductor traductor,
        ILogger<SimulationCoordinator> log)
    {
        _builder = builder;
        _sim = sim;
        _snapshots = snapshots;
        _toast = toast;
        _pushNotifier = pushNotifier;
        _fpga = fpga;
        _projects = projects;
        _traductor = traductor;
        _log = log;

        _builder.OnProgressAdvanced  += OnProgressAdvanced;
        _builder.OnSimulationEnded   += OnSimulationEnded;
        _builder.OnSimulationCompleted += OnSimulationCompleted;
    }

    /// <summary>Engine progress tick → mirror into Sim so every subscriber re-renders.</summary>
    private void OnProgressAdvanced(double progress)
    {
        _sim.SetProgress(progress);
    }

    /// <summary>
    /// Engine has finished (normally or via Stop). Stamp the terminal
    /// state on Sim so the sidebar / card drop their "running" surface
    /// even when the Editor isn't mounted to react. Fires BEFORE
    /// <see cref="OnSimulationCompleted"/>, so data-dependent work
    /// (snapshot, toast, push) lives there instead.
    /// </summary>
    private void OnSimulationEnded(bool ended)
    {
        _lastEndedProjectId = _sim.RunningProjectId ?? 0;
        _sim.SetState(SimState.Complete);
        _sim.SetProgress(ended ? 100 : 0);
        _sim.EndRun();
    }

    /// <summary>
    /// Engine produced a SignalHistory at the end of the run. Build the
    /// waveform, stamp the recovery cache, capture a snapshot to SQLite,
    /// and fire the in-app toast + OS push notification. Runs regardless
    /// of whether the Editor is mounted — that's the whole point.
    /// </summary>
    private async void OnSimulationCompleted(Interpreter.Services.SignalHistory history)
    {
        try
        {
            int projectId = _lastEndedProjectId;
            if (projectId <= 0) return;

            var wave = WaveformData.FromSignalHistory(history, _builder.LastTickResNs);
            _sim.StampCompleted(projectId, wave);

            // Persist the snapshot so the Runs tab + the 5-min recovery
            // window can both find the run later. Fire-and-forget — capture
            // failures log but never bubble up to the user.
            if (wave.Signals.Count > 0)
            {
                try
                {
                    await _snapshots.CaptureAsync(
                        projectId: projectId,
                        waveform : wave,
                        time     : _sim.Time,
                        scale    : _sim.Scale.ToString(),
                        fpgaModel: _fpga?.FPGA?.Model);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "[SimulationCoordinator] snapshot capture failed");
                }
            }

            // Resolve project name for the toast / notification. If lookup
            // fails (project deleted mid-run, db locked), fall back to a
            // generic title — better to fire a slightly-less-informative
            // notification than to skip the user's feedback entirely.
            string projectName = "Kmila";
            try
            {
                var proj = await _projects.GetByAsync(projectId);
                if (!string.IsNullOrWhiteSpace(proj?.Name)) projectName = proj!.Name!;
            }
            catch { /* fall through with generic title */ }

            int signals = wave.Signals.Count;
            string bodyTemplate = _traductor.T("TOAST_SIM_COMPLETE_BODY_FMT");
            string body = string.Format(bodyTemplate, signals);

            // In-app toast — fires whether or not the user is on the editor.
            // KmilaToastService surfaces it in the top-center stack. Uses the
            // shared ToastMessages.Message builder so HelpText (timestamp)
            // matches every other toast site in the app.
            try
            {
                _toast.Notify(Kmila.Shared.Models.ToastMessages.Message(
                    projectName, body, BlazorBootstrap.ToastType.Success));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "[SimulationCoordinator] toast notify failed");
            }

            // OS-level push notification. Web → browser Notification API.
            // MAUI → native iOS / Android. Implementations request permission
            // lazily on first call.
            try
            {
                await _pushNotifier.NotifySimEndAsync(projectName, body);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "[SimulationCoordinator] push notify failed");
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[SimulationCoordinator] OnSimulationCompleted failed");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _builder.OnProgressAdvanced    -= OnProgressAdvanced;
        _builder.OnSimulationEnded     -= OnSimulationEnded;
        _builder.OnSimulationCompleted -= OnSimulationCompleted;
    }
}
