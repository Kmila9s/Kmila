using Debugger;
using Kmila.Shared.Models;
namespace Kmila.Shared.Services;



public class ProgramBuilder : IDisposable
{
    public Debugger.Repositories.Parser Parser { get; private set; } = default!;
    private TimeMachine.Repositories.Timer _timeMachine = default!;
    private readonly CancellationTokenSource _cts = new();
    public ProgramBuilder()
    {
        TimeMachine.Repositories.Timer.OnSimulationEnded += OnSimulationEnded;
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
    public void OnSimulationEnded(bool ended)
    {
        if (ended)
        {
            
        }
    }
    public void SetSimulationTime(TimeSpan WaitTime)
    {
        _timeMachine = new();
        _timeMachine.Start(WaitTime);
    }
    public void Dispose()
    {
        _cts.Cancel();
        _cts?.Dispose();
    }
}