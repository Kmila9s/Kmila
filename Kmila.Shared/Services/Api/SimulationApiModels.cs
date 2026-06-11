using System.Text.Json.Serialization;

namespace Kmila.Shared.Services.Api;

// DTOs for the headless simulation API consumed by SimulatorBench and any
// other external automation. POCOs only — no ASP.NET dependency so this
// file compiles on every Kmila.Shared target framework.

public sealed class CreateProjectRequest
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("retain")] public bool Retain { get; set; }
}

public sealed class CreateProjectResponse
{
    [JsonPropertyName("projectId")] public string ProjectId { get; set; } = string.Empty;
    [JsonPropertyName("sandboxPath")] public string SandboxPath { get; set; } = string.Empty;
}

public sealed class WriteFileRequest
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

public sealed class WriteFileResponse
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("bytes")] public long Bytes { get; set; }
}

public sealed class SimulateRequest
{
    [JsonPropertyName("projectId")] public string ProjectId { get; set; } = string.Empty;
    [JsonPropertyName("entryPoint")] public string? EntryPoint { get; set; }
    [JsonPropertyName("fpga")] public string? Fpga { get; set; }
    [JsonPropertyName("timeScale")] public string TimeScale { get; set; } = "NS";
    [JsonPropertyName("duration")] public int Duration { get; set; } = 1000;
    [JsonPropertyName("systemClockMhz")] public int SystemClockMhz { get; set; } = 50;
    [JsonPropertyName("tickResNs")] public int TickResNs { get; set; } = 10;
    [JsonPropertyName("portMoments")] public List<ApiPortMoment> PortMoments { get; set; } = new();
    [JsonPropertyName("files")] public List<WriteFileRequest>? Files { get; set; }
}

public sealed class ApiPortMoment
{
    [JsonPropertyName("port")] public string Port { get; set; } = string.Empty;
    [JsonPropertyName("t")] public int Time { get; set; }
    [JsonPropertyName("scale")] public string Scale { get; set; } = "NS";
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
}

public sealed class SimulateResponse
{
    [JsonPropertyName("runId")] public string RunId { get; set; } = string.Empty;
    [JsonPropertyName("entity")] public string Entity { get; set; } = string.Empty;
    [JsonPropertyName("durationMs")] public long DurationMs { get; set; }
    [JsonPropertyName("totalTicks")] public ulong TotalTicks { get; set; }
    [JsonPropertyName("tickResNs")] public int TickResNs { get; set; }
    [JsonPropertyName("signalCount")] public int SignalCount { get; set; }
    [JsonPropertyName("vcd")] public string Vcd { get; set; } = string.Empty;
    [JsonPropertyName("log")] public List<string> Log { get; set; } = new();
}

public sealed class ApiError
{
    [JsonPropertyName("error")] public string Error { get; set; } = string.Empty;
    [JsonPropertyName("detail")] public string? Detail { get; set; }
}
