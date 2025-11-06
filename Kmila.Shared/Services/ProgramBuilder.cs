using Debugger;
using Kmila.Shared.Models;
namespace Kmila.Shared.Services;



public class ProgramBuilder : IDisposable
{
    public event Action<bool>? OnSimulationEnded;
    public event Action<double>? OnProgressAdvanced;
    public Debugger.Repositories.Parser Parser { get; private set; } = default!;
    private TimeMachine.Repositories.Timer _timeMachine = new();
    private readonly CancellationTokenSource _cts = new();
    public ProgramBuilder()
    {
        _timeMachine.OnSimulationEnded += OnSimulationEnd;
        _timeMachine.OnProgressAdvance += OnProgressAdvance;
    }
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
    public void RestartTimer(TimeSpan WaitTime) => _timeMachine.Restart(WaitTime);
    public void PauseTimer() => _timeMachine.Pause();
    public void ContinueTimer() => _timeMachine.Continue();
    public void StopTimer() => _timeMachine.Stop();
    private void OnProgressAdvance(double progress)
    {
        OnProgressAdvanced?.Invoke(progress);
    }
    private void OnSimulationEnd(bool ended)
    {
        OnSimulationEnded?.Invoke(ended);
    }
    public void SetSimulationTime(TimeSpan WaitTime)
    {
        _timeMachine.Start(WaitTime);
    }
    public void Dispose()
    {
        _cts.Cancel();
        _cts?.Dispose();
    }
}