using Debugger;
using Interpreter.Models;
using Interpreter.Services;
using Kmila.Shared.Models;
using Parser.Models;
namespace Kmila.Shared.Services;

/// <summary>
/// Manages the building and simulation of a VHDL program.
/// </summary>
public class ProgramBuilder : IDisposable
{
    /// <summary>
    /// Occurs when the simulation has ended.
    /// </summary>
    public event Action<bool>? OnSimulationEnded;
    /// <summary>
    /// Occurs when the simulation progress has advanced.
    /// </summary>
    public event Action<double>? OnProgressAdvanced;
    /// <summary>
    /// Occurs when build progress is updated (progress 0-1, message).
    /// </summary>
    public event Action<float, string>? OnBuildProgressChanged;
    /// <summary>
    /// Occurs when a build error occurs.
    /// </summary>
    public event Action<string, string?>? OnBuildError;
    /// <summary>
    /// Occurs when build completes successfully.
    /// </summary>
    public event Action<string>? OnBuildCompleted;
    /// <summary>
    /// Occurs when build is cancelled by user.
    /// </summary>
    public event Action<string>? OnBuildCancelled;
    /// <summary>
    /// Occurs when the simulation completes with waveform data.
    /// </summary>
    public event Action<SignalHistory>? OnSimulationCompleted;
    /// <summary>
    /// Gets the parser used to parse the VHDL file.
    /// </summary>
    public Debugger.Repositories.Parser Parser { get; private set; } = default!;
    /// <summary>
    /// Gets whether a build is currently in progress.
    /// </summary>
    public bool IsBuilding { get; private set; } = false;
    /// <summary>
    /// Gets the signal history from the last simulation run.
    /// </summary>
    public SignalHistory? SimulationHistory => _simulationRunner?.Engine?.History;
    /// <summary>
    /// Effective ns-per-tick used by the most recent run. Persisted on the
    /// builder (not the config) so the waveform factory — which is called
    /// from an event handler on a later frame — can label time axes with
    /// the same scale the simulation actually ran at.
    /// Defaults to <see cref="TimeMachine.Models.TimeClock.NS_PER_TICK"/>
    /// (10) so call sites that haven't been updated still get sensible
    /// labels.
    /// </summary>
    public int LastTickResNs { get; private set; } = TimeMachine.Models.TimeClock.NS_PER_TICK;
    // Legacy timer for step-by-step simulation (TimeMachine path)
    private TimeMachine.Repositories.Timer _timeMachine = new();
    // SimulationRunner for fast full-simulation execution
    private SimulationRunner? _simulationRunner;
    // Cancellation token source for cooperative cancellation of async operations
    private readonly CancellationTokenSource _cts = new();
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramBuilder"/> class.
    /// </summary>
    public ProgramBuilder()
    {
        _timeMachine.OnSimulationEnded += OnSimulationEnd;
        _timeMachine.OnProgressAdvance += OnProgressAdvance;

        // Subscribe to ProgressReporter events from Interpreter module
        ProgressReporter.Instance.ProgressChanged += OnInterpreterProgressChanged;
        ProgressReporter.Instance.ErrorOccurred += OnInterpreterError;
        ProgressReporter.Instance.Completed += OnInterpreterCompleted;
        ProgressReporter.Instance.CancellationRequested += OnInterpreterCancellationRequested;
    }

    /// <summary>
    /// Handles progress updates from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterProgressChanged(object? sender, ProgressEventArgs e)
    {
        var (progress, message) = e.Update.ToTuple();
        OnBuildProgressChanged?.Invoke(progress, message);
    }

    /// <summary>
    /// Handles error events from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterError(object? sender, ProgressEventArgs e)
    {
        OnBuildError?.Invoke(e.Update.Message, e.Update.Detail);
    }

    /// <summary>
    /// Handles completion events from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterCompleted(object? sender, ProgressEventArgs e)
    {
        IsBuilding = false;
        OnBuildCompleted?.Invoke(e.Update.Message);
    }

    /// <summary>
    /// Handles cancellation request events from the Interpreter's ProgressReporter.
    /// </summary>
    private void OnInterpreterCancellationRequested(object? sender, ProgressEventArgs e)
    {
        IsBuilding = false;
        OnBuildCancelled?.Invoke(e.Update.Message);
    }

    /// <summary>
    /// Requests cancellation of the current build operation.
    /// </summary>
    /// <remarks>
    /// This method signals the Interpreter's ProgressReporter to cancel the current operation.
    /// The cancellation is cooperative and may not be immediate depending on the current phase.
    /// </remarks>
    public void CancelBuild()
    {
        if (!IsBuilding) return;
        ProgressReporter.Instance.RequestCancellation();
    }
    /// <summary>
    /// Builds the program from the specified project file.
    /// </summary>
    /// <param name="file">The project file to build.</param>
    /// <returns>True if the build was successful, otherwise false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the build is cancelled by the user.</exception>
    public bool Build(ProjectFile file)
    {
        IsBuilding = true;

        // Start a cancellable operation in the ProgressReporter
        ProgressReporter.Instance.Reset();
        ProgressReporter.Instance.StartCancellableOperation();

        try
        {
            string filePath = Path.Join(file.Project.BasePath, file.FileName);
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            // Accept both .vhd (IEEE 1076 traditional) and .vhdl (common on Linux
            // tooling). The Parser treats them identically — this is just an
            // input gate.
            if (ext != ".vhdl" && ext != ".vhd")
                throw new Exception($"File not valid: {Path.GetFileName(filePath)}");

            // Check for cancellation before starting parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            Parser = new(filePath);
            Parser.ParseFile();

            // Check for cancellation after parse
            ProgressReporter.Instance.ThrowIfCancellationRequested();

            if (!Parser.Entities.Any())
                throw new Exception("File not valid");

            return true;
        }
        catch (OperationCanceledException)
        {
            IsBuilding = false;
            throw;
        }
        catch
        {
            IsBuilding = false;
            throw;
        }
    }
    /// <summary>
    /// Runs a full simulation using SimulationRunner (bypasses TimeMachine for speed).
    /// </summary>
    /// <param name="portMoments">Port moment configurations from the UI.</param>
    /// <param name="duration">Total simulation duration.</param>
    /// <param name="systemClockMhz">
    /// Effective system clock frequency (MHz) for this run. Resolved by the
    /// caller from the selected FPGA's default and any per-project override.
    /// Defaults to 50 MHz when the caller didn't compute one (kept for
    /// call sites that predate the FPGA-sourced clock).
    /// </param>
    public async Task RunSimulationAsync(
        List<PortMomentConfiguration>? portMoments,
        TimeSpan duration,
        int systemClockMhz = 50,
        int tickResNs = TimeMachine.Models.TimeClock.NS_PER_TICK)
    {
        // Get first entity with architecture
        var entity = Parser.Entities.FirstOrDefault(e => e.Architecture != null);
        if (entity == null)
            throw new InvalidOperationException("No entity with architecture found.");

        // Remember the effective tick resolution for downstream waveform
        // labelling — see LastTickResNs doc comment.
        LastTickResNs = tickResNs > 0 ? tickResNs : TimeMachine.Models.TimeClock.NS_PER_TICK;

        // Build simulation config from UI parameters
        var config = StimulusBuilder.BuildConfig(entity, portMoments, duration,
            tickResNs: LastTickResNs,
            systemClockMhz: systemClockMhz);

        // Surface the config to stdout (and therefore /logs via the Console
        // redirector wired up in Program.cs) so the user can see exactly what
        // the auto-clock + stimulus injector produced. Wrapped in try/catch
        // because null values on a Variable's getters would otherwise crash
        // the entire Run click before the simulator gets a chance.
        try
        {
            Console.WriteLine($"[Sim] Entity={entity?.Name ?? "?"} ports={entity?.Ports?.Count ?? 0} " +
                $"totalTicks={config?.TotalTicks} clocks={config?.Clocks?.Count ?? 0} stimuli={config?.Stimuli?.Count ?? 0}");
            if (config?.Clocks != null)
            {
                foreach (var clk in config.Clocks)
                {
                    if (clk == null) continue;
                    Console.WriteLine($"[Sim.Clock] name={clk.Signal?.Name ?? "?"} " +
                        $"halfPeriodTicks={clk.HalfPeriodTicks} initialHigh={clk.InitialHigh}");
                }
            }
            if (entity?.Ports != null)
            {
                foreach (var p in entity.Ports)
                {
                    if (p == null) continue;
                    Console.WriteLine($"[Sim.Port] name={p.Name ?? "?"} dir={p.PortType} size={p.Size}");
                }
            }
        }
        catch (Exception diagEx)
        {
            Console.WriteLine($"[Sim.Diag] failed: {diagEx.Message}");
        }

        // Dispose previous runner if any
        _simulationRunner?.Dispose();
        _simulationRunner = new SimulationRunner();

        // Subscribe to runner events and forward them. Important: we fire
        // OnSimulationCompleted whenever *any* signal history exists — even
        // on user-stopped runs (completed=false). That way the user sees a
        // partial waveform of whatever ran, instead of a blank Waveforms tab
        // after they hit Stop mid-simulation.
        _simulationRunner.OnProgressChanged += progress => OnProgressAdvanced?.Invoke(progress);
        _simulationRunner.OnSimulationEnded += completed =>
        {
            OnSimulationEnded?.Invoke(completed);
            var history = _simulationRunner?.Engine?.History;
            if (history != null && history.TotalSamples > 0)
            {
                // SimulationRunner now records stimulus + clock transitions
                // live during its tick loop (via engine.History.RecordChanges
                // on the direct-write set), so the post-run synthesis we
                // used to run here is no longer needed.
                OnSimulationCompleted?.Invoke(history);
            }
        };
        _simulationRunner.OnError += msg => OnBuildError?.Invoke("Simulation error", msg);

        // Run on current async context (caller should be on background/UI thread)
        await _simulationRunner.RunAsync(config);
    }

    /// <summary>
    /// Restarts the simulation timer with the specified wait time.
    /// </summary>
    /// <param name="WaitTime">The wait time for the simulation.</param>
    public void RestartTimer(TimeSpan WaitTime) => _timeMachine.Restart(WaitTime);
    /// <summary>
    /// Pauses the simulation (SimulationRunner or TimeMachine).
    /// </summary>
    public void PauseTimer()
    {
        if (_simulationRunner?.IsRunning == true)
            _simulationRunner.Pause();
        else
            _timeMachine.Pause();
    }
    /// <summary>
    /// Resumes the simulation (SimulationRunner or TimeMachine).
    /// </summary>
    public void ContinueTimer()
    {
        if (_simulationRunner?.IsPaused == true)
            _simulationRunner.Resume();
        else
            _timeMachine.Continue();
    }
    /// <summary>
    /// Stops the simulation (SimulationRunner or TimeMachine).
    /// </summary>
    public void StopTimer()
    {
        if (_simulationRunner?.IsRunning == true)
            _simulationRunner.Stop();
        else
            _timeMachine.Stop();
    }
    /// <summary>
    /// Forwards simulation progress from the TimeMachine to subscribers.
    /// </summary>
    /// <param name="progress">The simulation progress value (0.0 to 1.0).</param>
    private void OnProgressAdvance(double progress)
    {
        OnProgressAdvanced?.Invoke(progress);
    }
    /// <summary>
    /// Forwards simulation end event from the TimeMachine to subscribers.
    /// </summary>
    /// <param name="ended">True if the simulation completed, false if it was interrupted.</param>
    private void OnSimulationEnd(bool ended)
    {
        OnSimulationEnded?.Invoke(ended);
    }
    /// <summary>
    /// Sets the simulation time (legacy TimeMachine path).
    /// </summary>
    /// <param name="WaitTime">The wait time for the simulation.</param>
    public void SetSimulationTime(TimeSpan WaitTime)
    {
        _timeMachine.Start(WaitTime);
    }
    /// <summary>
    /// Releases the resources used by the <see cref="ProgramBuilder"/> class.
    /// Unsubscribes from all events and disposes managed resources.
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from TimeMachine events to prevent memory leaks
        _timeMachine.OnSimulationEnded -= OnSimulationEnd;
        _timeMachine.OnProgressAdvance -= OnProgressAdvance;

        // Unsubscribe from ProgressReporter events to prevent memory leaks
        ProgressReporter.Instance.ProgressChanged -= OnInterpreterProgressChanged;
        ProgressReporter.Instance.ErrorOccurred -= OnInterpreterError;
        ProgressReporter.Instance.Completed -= OnInterpreterCompleted;
        ProgressReporter.Instance.CancellationRequested -= OnInterpreterCancellationRequested;

        _simulationRunner?.Dispose();
        _cts.Cancel();
        _cts?.Dispose();
    }
}