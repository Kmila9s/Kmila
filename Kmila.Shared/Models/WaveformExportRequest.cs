namespace Kmila.Shared.Models;

/// <summary>
/// File format for waveform exports. Kept narrow on purpose — extending to
/// PDF later means adding an enum case + a renderer in
/// <see cref="Kmila.Shared.Services.WaveformExporter"/>, no UI churn.
/// </summary>
public enum WaveformExportFormat
{
    Svg,
    Png
}

/// <summary>
/// Concrete export request bubbled by <c>ExportWaveformModal</c> to the
/// caller (currently <c>WaveformViewer</c>). All times are nanoseconds so
/// the renderer doesn't have to know which scale the user typed in.
/// </summary>
/// <param name="Format">Output format.</param>
/// <param name="PngScale">DPI scale for PNG raster (e.g. 2 = retina). Ignored
/// when <paramref name="Format"/> is SVG.</param>
/// <param name="FromNs">Inclusive start of the export range, ns.</param>
/// <param name="ToNs">Exclusive end of the export range, ns.</param>
/// <param name="PageSpanNs">When non-null, split the range into consecutive
/// pages of this length so a long export stays readable. Each page becomes a
/// separate file with a sequence number suffix.</param>
public record WaveformExportRequest(
    WaveformExportFormat Format,
    int PngScale,
    long FromNs,
    long ToNs,
    long? PageSpanNs);
