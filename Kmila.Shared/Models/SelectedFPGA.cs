namespace Kmila.Shared.Models;

/// <summary>
/// Manages the currently selected FPGA device state across the application.
/// This class acts as a singleton state container that notifies subscribers
/// when the FPGA selection changes.
/// </summary>
/// <remarks>
/// This class is registered as a singleton service in dependency injection
/// and is used to share FPGA selection state between components like TopBar
/// (where selection occurs) and SimulationParametersModal (where validation occurs).
/// </remarks>
public class SelectedFPGA
{
    /// <summary>
    /// Gets or sets the currently selected FPGA device specification.
    /// </summary>
    /// <remarks>
    /// This property holds the complete FPGA specification including resources,
    /// clocking, power, and I/O information. It is used for resource validation
    /// and display throughout the application.
    /// </remarks>
    public FPGA FPGA { get; set; } = default!;

    /// <summary>
    /// Gets or sets the file path to the JSON file containing the FPGA specification.
    /// </summary>
    /// <remarks>
    /// The path is relative to the wwwroot/Devices folder and is used to identify
    /// the source of the FPGA specification for debugging and display purposes.
    /// </remarks>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Event raised when the FPGA selection changes.
    /// </summary>
    /// <remarks>
    /// Components can subscribe to this event to react to FPGA selection changes,
    /// such as revalidating resource usage or updating the display.
    /// </remarks>
    public event Action? OnFPGAChange;

    /// <summary>
    /// Sets the selected FPGA and notifies all subscribers of the change.
    /// </summary>
    /// <param name="fpga">The FPGA device specification to select.</param>
    /// <param name="path">The file path to the FPGA specification JSON file.</param>
    /// <remarks>
    /// This method updates both the FPGA and Path properties and then invokes
    /// the OnFPGAChange event to notify all subscribed components.
    /// </remarks>
    public void SetFPGA(FPGA fpga, string path)
    {
        FPGA = fpga;
        Path = path;
        OnFPGAChange?.Invoke();
    }
}