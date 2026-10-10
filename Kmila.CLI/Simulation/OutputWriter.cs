using System.Text.Json;
using Kmila.Shared.Models;
using Kmila.Shared.Services;

namespace Kmila.CLI.Simulation;

/// <summary>Supported <c>--output-format</c> values.</summary>
public enum OutputFormat
{
    /// <summary>Value-Change Dump (default) — open in Surfer/GTKWave/any VCD viewer.</summary>
    Vcd,
    /// <summary>Human-readable signal dump.</summary>
    Txt,
    /// <summary>One column per signal, one row per time step.</summary>
    Csv,
    /// <summary>Run metadata plus the embedded VCD (shape-compatible with the shared SimulateResponse).</summary>
    Json,
    /// <summary>Static waveform image via the app's WaveformExporter.</summary>
    Svg,
}

/// <summary>
/// Renders a <see cref="SimRunResult"/> to a chosen textual format. VCD/TXT/CSV
/// come straight from the engine's <see cref="Interpreter.Services.SignalHistory"/>;
/// JSON wraps the VCD plus run metadata (shape-compatible with the shared
/// <c>SimulateResponse</c>); SVG reuses the app's <see cref="WaveformExporter"/>.
/// </summary>
public static class OutputWriter
{
    /// <summary>Parses a format name (case-insensitive), throwing on an unknown value.</summary>
    public static OutputFormat ParseFormat(string? text) =>
        (text ?? "vcd").Trim().ToLowerInvariant() switch
        {
            "vcd"  => OutputFormat.Vcd,
            "txt" or "text" => OutputFormat.Txt,
            "csv"  => OutputFormat.Csv,
            "json" => OutputFormat.Json,
            "svg"  => OutputFormat.Svg,
            var other => throw new FormatException($"unknown --output-format '{other}' (use vcd|txt|csv|json|svg)"),
        };

    /// <summary>The conventional default file name for a format (used when writing svg without -o).</summary>
    public static string DefaultFileName(OutputFormat fmt) => fmt switch
    {
        OutputFormat.Vcd  => "waveform.vcd",
        OutputFormat.Txt  => "signals.txt",
        OutputFormat.Csv  => "signals.csv",
        OutputFormat.Json => "result.json",
        OutputFormat.Svg  => "waveform.svg",
        _ => "output.txt",
    };

    /// <summary>Renders the result to a string in the requested format.</summary>
    public static string Render(SimRunResult result, OutputFormat fmt)
    {
        switch (fmt)
        {
            case OutputFormat.Vcd:
                return result.History.ExportToVcd(result.Entity, timescaleNs: result.TickResNs);

            case OutputFormat.Txt:
                return result.History.ExportToText();

            case OutputFormat.Csv:
                return result.History.ExportToCsv();

            case OutputFormat.Json:
                return JsonSerializer.Serialize(new
                {
                    entity      = result.Entity,
                    durationMs  = result.DurationMs,
                    totalTicks  = result.TotalTicks,
                    tickResNs   = result.TickResNs,
                    signalCount = result.SignalCount,
                    log         = result.Log,
                    vcd         = result.History.ExportToVcd(result.Entity, timescaleNs: result.TickResNs),
                }, new JsonSerializerOptions { WriteIndented = true });

            case OutputFormat.Svg:
            {
                var data = WaveformData.FromSignalHistory(result.History, result.TickResNs);
                long endNs = (long)result.TotalTicks * result.TickResNs;
                if (endNs <= 0) endNs = 1;
                var title = $"{result.Entity} — 0..{endNs}ns";
                return WaveformExporter.BuildSvg(
                    data, viewStartNs: 0, viewEndNs: endNs,
                    trackWidthPx: 1000, includeClock: true, title: title);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(fmt));
        }
    }
}
