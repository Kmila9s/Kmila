namespace Kmila.Shared.Models;
public class SelectedFPGA
{
    public FPGA FPGA { get; set; } = default!;
    public string Path { get; set; } = string.Empty;
    public event Action? OnFPGAChange;
    public void SetFPGA(FPGA fpga, string path)
    {
        FPGA = fpga;
        Path = path;
        OnFPGAChange?.Invoke();
    }
}