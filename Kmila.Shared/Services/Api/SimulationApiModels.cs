using System.Text.Json.Serialization;

namespace Kmila.Shared.Services.Api;

// DTOs for the headless simulation API consumed by SimulatorBench and any
// other external automation. POCOs only — no ASP.NET dependency so this
// file compiles on every Kmila.Shared target framework.

/// <summary>Request body for creating a new sandbox project.</summary>
public sealed class CreateProjectRequest
{
    /// <summary>Optional display name for the project.</summary>
    [JsonPropertyName("name")] public string? Name { get; set; }
    /// <summary>When true, the sandbox is kept on disk after the run instead of being cleaned up.</summary>
    [JsonPropertyName("retain")] public bool Retain { get; set; }
}

/// <summary>Response returned after a project is created.</summary>
public sealed class CreateProjectResponse
{
    /// <summary>Identifier of the newly created project.</summary>
    [JsonPropertyName("projectId")] public string ProjectId { get; set; } = string.Empty;
    /// <summary>Filesystem path of the project's sandbox directory.</summary>
    [JsonPropertyName("sandboxPath")] public string SandboxPath { get; set; } = string.Empty;
}

/// <summary>Request body for writing a single file into a project.</summary>
public sealed class WriteFileRequest
{
    /// <summary>File name to write.</summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    /// <summary>Text content to store in the file.</summary>
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

/// <summary>Response returned after a file is written.</summary>
public sealed class WriteFileResponse
{
    /// <summary>Name of the file that was written.</summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    /// <summary>Path where the file was written.</summary>
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    /// <summary>Number of bytes written.</summary>
    [JsonPropertyName("bytes")] public long Bytes { get; set; }
}

/// <summary>Request body for running a simulation.</summary>
public sealed class SimulateRequest
{
    /// <summary>Identifier of the project to simulate.</summary>
    [JsonPropertyName("projectId")] public string ProjectId { get; set; } = string.Empty;
    /// <summary>Optional top-level entity to simulate; inferred when omitted.</summary>
    [JsonPropertyName("entryPoint")] public string? EntryPoint { get; set; }
    /// <summary>Optional HDL language id (e.g. "vhdl"); inferred from the source extensions when omitted.</summary>
    [JsonPropertyName("language")] public string? Language { get; set; }
    /// <summary>Optional target FPGA model.</summary>
    [JsonPropertyName("fpga")] public string? Fpga { get; set; }
    /// <summary>Time scale unit for the run (defaults to nanoseconds).</summary>
    [JsonPropertyName("timeScale")] public string TimeScale { get; set; } = "NS";
    /// <summary>Total simulated duration in <see cref="TimeScale"/> units.</summary>
    [JsonPropertyName("duration")] public int Duration { get; set; } = 1000;
    /// <summary>System clock frequency in megahertz.</summary>
    [JsonPropertyName("systemClockMhz")] public int SystemClockMhz { get; set; } = 50;
    /// <summary>Tick resolution in nanoseconds.</summary>
    [JsonPropertyName("tickResNs")] public int TickResNs { get; set; } = 10;
    /// <summary>Scheduled input stimuli applied to ports over time.</summary>
    [JsonPropertyName("portMoments")] public List<ApiPortMoment> PortMoments { get; set; } = new();
    /// <summary>Optional inline files to write into the project before simulating.</summary>
    [JsonPropertyName("files")] public List<WriteFileRequest>? Files { get; set; }
    /// <summary>
    /// Initial values of objects declared without <c>:=</c>: <c>"lrm"</c> (default, like GHDL: <c>'U'</c>,
    /// <c>T'left</c>) or <c>"didactic"</c> (start at 0, clock proposed by name).
    /// </summary>
    [JsonPropertyName("initialValues")] public string? InitialValues { get; set; }
    /// <summary>
    /// Clocks driven on input ports. When omitted: none under <c>"lrm"</c>; the clock-named port
    /// (<c>clk</c>, <c>clock</c>…) at <see cref="SystemClockMhz"/> under <c>"didactic"</c>.
    /// </summary>
    [JsonPropertyName("clocks")] public List<ApiClock>? Clocks { get; set; }
    /// <summary>Initial values of input ports, set before time 0 (no event at t=0).</summary>
    [JsonPropertyName("portInitials")] public List<ApiPortInitial>? PortInitials { get; set; }
}

/// <summary>A clock driven on an input port.</summary>
public sealed class ApiClock
{
    /// <summary>Input port to drive.</summary>
    [JsonPropertyName("port")] public string Port { get; set; } = string.Empty;
    /// <summary>Period in nanoseconds; 0 or omitted uses <see cref="SimulateRequest.SystemClockMhz"/>.</summary>
    [JsonPropertyName("periodNs")] public double PeriodNs { get; set; }
    /// <summary>True when the clock starts at '1' (default '0').</summary>
    [JsonPropertyName("initialHigh")] public bool InitialHigh { get; set; }
}

/// <summary>The initial value of an input port.</summary>
public sealed class ApiPortInitial
{
    /// <summary>Port (or part, e.g. <c>v(0)</c>).</summary>
    [JsonPropertyName("port")] public string Port { get; set; } = string.Empty;
    /// <summary>Value (<c>0</c>, <c>1</c>, <c>U</c>, a bit string…).</summary>
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
}

/// <summary>A single scheduled input value applied to a port at a given time.</summary>
public sealed class ApiPortMoment
{
    /// <summary>Name of the port to drive.</summary>
    [JsonPropertyName("port")] public string Port { get; set; } = string.Empty;
    /// <summary>Time at which the value is applied, in <see cref="Scale"/> units.</summary>
    [JsonPropertyName("t")] public int Time { get; set; }
    /// <summary>Time scale unit for <see cref="Time"/> (defaults to nanoseconds).</summary>
    [JsonPropertyName("scale")] public string Scale { get; set; } = "NS";
    /// <summary>Value to apply to the port.</summary>
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
}

/// <summary>Response returned after a simulation completes.</summary>
public sealed class SimulateResponse
{
    /// <summary>Identifier of this simulation run.</summary>
    [JsonPropertyName("runId")] public string RunId { get; set; } = string.Empty;
    /// <summary>Name of the entity that was simulated.</summary>
    [JsonPropertyName("entity")] public string Entity { get; set; } = string.Empty;
    /// <summary>Wall-clock duration of the run in milliseconds.</summary>
    [JsonPropertyName("durationMs")] public long DurationMs { get; set; }
    /// <summary>Total number of simulated ticks.</summary>
    [JsonPropertyName("totalTicks")] public ulong TotalTicks { get; set; }
    /// <summary>Tick resolution in nanoseconds.</summary>
    [JsonPropertyName("tickResNs")] public int TickResNs { get; set; }
    /// <summary>Number of signals captured.</summary>
    [JsonPropertyName("signalCount")] public int SignalCount { get; set; }
    /// <summary>VCD (Value Change Dump) waveform output.</summary>
    [JsonPropertyName("vcd")] public string Vcd { get; set; } = string.Empty;
    /// <summary>Log lines emitted during the run.</summary>
    [JsonPropertyName("log")] public List<string> Log { get; set; } = new();
    /// <summary>
    /// Engine error that aborted the run, or null when it completed. The VCD
    /// then holds the partial waveform recorded before the failure.
    /// </summary>
    [JsonPropertyName("error")] public string? Error { get; set; }
}

/// <summary>Error payload returned when an API request fails.</summary>
public sealed class ApiError
{
    /// <summary>Short error code or message.</summary>
    [JsonPropertyName("error")] public string Error { get; set; } = string.Empty;
    /// <summary>Optional detailed error description.</summary>
    [JsonPropertyName("detail")] public string? Detail { get; set; }
}
