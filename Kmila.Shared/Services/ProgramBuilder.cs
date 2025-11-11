using Debugger;
using Kmila.Shared.Models;
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
    /// Gets the parser used to parse the VHDL file.
    /// </summary>
    public Debugger.Repositories.Parser Parser { get; private set; } = default!;
    private TimeMachine.Repositories.Timer _timeMachine = new();
    private readonly CancellationTokenSource _cts = new();
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramBuilder"/> class.
    /// </summary>
    public ProgramBuilder()
    {
        _timeMachine.OnSimulationEnded += OnSimulationEnd;
        _timeMachine.OnProgressAdvance += OnProgressAdvance;
    }
    /// <summary>
    /// Builds the program from the specified project file.
    /// </summary>
    /// <param name="file">The project file to build.</param>
    /// <returns>True if the build was successful, otherwise false.</returns>
    public bool Build(ProjectFile file)
    {
        string filePath = Path.Join(file.Project.BasePath, file.FileName);
        string ext = Path.GetExtension(filePath);
        if (ext.ToLowerInvariant() != ".vhdl") throw new Exception($"File not valid: {Path.GetFileName(filePath)}");
        Parser = new(filePath);
        Parser.ParseFile();
        if (!Parser.Entities.Any())
            throw new Exception("File not valid");
        return true;
    }
    /// <summary>
    /// Restarts the simulation timer with the specified wait time.
    /// </summary>
    /// <param name="WaitTime">The wait time for the simulation.</param>
    public void RestartTimer(TimeSpan WaitTime) => _timeMachine.Restart(WaitTime);
    /// <summary>
    /// Pauses the simulation timer.
    /// </summary>
    public void PauseTimer() => _timeMachine.Pause();
    /// <summary>
    /// Continues the simulation timer.
    /// </summary>
    public void ContinueTimer() => _timeMachine.Continue();
    /// <summary>
    /// Stops the simulation timer.
    /// </summary>
    public void StopTimer() => _timeMachine.Stop();
    private void OnProgressAdvance(double progress)
    {
        OnProgressAdvanced?.Invoke(progress);
    }
    private void OnSimulationEnd(bool ended)
    {
        OnSimulationEnded?.Invoke(ended);
    }
    /// <summary>
    /// Sets the simulation time.
    /// </summary>
    /// <param name="WaitTime">The wait time for the simulation.</param>
    public void SetSimulationTime(TimeSpan WaitTime)
    {
        _timeMachine.Start(WaitTime);
    }
    /// <summary>
    /// Releases the resources used by the <see cref="ProgramBuilder"/> class.
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        _cts?.Dispose();
    }
}