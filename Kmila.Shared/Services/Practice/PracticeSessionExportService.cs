using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Kmila.Shared.Models.Practice;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Practice;

/// <summary>
/// Builds a downloadable summary of one Practice exercise session — every
/// attempt the user made, the per-test results, the code per submission,
/// and the unified diff between consecutive attempts. The shape is plain
/// text (Markdown report) or a ZIP bundle that adds a structured JSON
/// dump and per-attempt .vhd / .diff sidecars. No natural-language
/// narration is generated; everything is a direct projection of what
/// already lives in the SQLite store.
/// </summary>
public sealed class PracticeSessionExportService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    private readonly PracticeAttemptStore _attempts;
    private readonly ILogger<PracticeSessionExportService> _logger;

    /// <summary>Creates the export service with the attempt store and an optional logger.</summary>
    /// <param name="attempts">Store providing practice attempt data to export.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public PracticeSessionExportService(
        PracticeAttemptStore attempts,
        ILogger<PracticeSessionExportService>? logger = null)
    {
        _attempts = attempts;
        _logger = (ILogger<PracticeSessionExportService>?)logger
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PracticeSessionExportService>.Instance;
    }

    /// <summary>
    /// Returns the ready-to-download payload, or <c>null</c> when the user has
    /// no attempt history for this exercise yet (the UI button is disabled
    /// in that case, but the service double-checks).
    /// </summary>
    public async Task<PracticeSessionExportPayload?> BuildAsync(
        PracticeExercise exercise,
        string lang,
        PracticeSessionExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        var attemptsDesc = await _attempts.GetAttemptsAsync(exercise.Id, limit: PracticeAttemptStore.MaxAttemptsPerExercise);
        if (attemptsDesc.Count == 0) return null;

        // Order oldest-first so diffs read like a session timeline.
        var attempts = attemptsDesc.OrderBy(a => a.SubmittedAt).ThenBy(a => a.Id).ToList();

        var perResults = new Dictionary<int, List<PracticeAttemptTestResult>>(attempts.Count);
        foreach (var a in attempts)
        {
            perResults[a.Id] = await _attempts.GetTestResultsAsync(a.Id);
        }

        var progress = await _attempts.GetProgressAsync(exercise.Id);
        var hintReveals = await _attempts.GetHintRevealsAsync(exercise.Id);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var safeId = SanitizeFileName(exercise.Id);
        var baseName = $"practice-{safeId}-session-{stamp}";

        var markdown = BuildMarkdown(exercise, lang, attempts, perResults, progress, hintReveals, request);

        if (request.Format == PracticeSessionExportFormat.Markdown)
        {
            return new PracticeSessionExportPayload(
                FileName: $"{baseName}.md",
                MimeType: "text/markdown",
                TextContent: markdown,
                BinaryContent: null);
        }

        var bytes = BuildZip(exercise, lang, attempts, perResults, progress, hintReveals, markdown, request);
        return new PracticeSessionExportPayload(
            FileName: $"{baseName}.zip",
            MimeType: "application/zip",
            TextContent: null,
            BinaryContent: bytes);
    }

    // ─── Markdown ──────────────────────────────────────────────────────

    private static string BuildMarkdown(
        PracticeExercise ex,
        string lang,
        List<PracticeAttempt> attempts,
        Dictionary<int, List<PracticeAttemptTestResult>> perResults,
        PracticeProgress? progress,
        List<PracticeHintReveal> hintReveals,
        PracticeSessionExportRequest req)
    {
        var sb = new StringBuilder();
        var title = PickLang(ex.Titles, lang) ?? ex.Id;

        sb.Append("# Práctica · ").Append(title).Append(" — Reporte de sesión\n\n");
        sb.Append("- **Ejercicio:** `").Append(ex.Id).Append("` · ")
          .Append(ex.Modality).Append(" · ").Append(ex.Difficulty).Append('\n');
        sb.Append("- **Idioma del reporte:** ").Append(lang).Append('\n');
        sb.Append("- **Exportado:** ").Append(DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("- **Total de intentos:** ").Append(attempts.Count).Append('\n');

        if (progress is not null)
        {
            sb.Append("- **Estado:** ").Append(progress.Status).Append('\n');
            if (progress.FirstSolvedAt is { } first)
            {
                sb.Append("- **Primer pase:** ").Append(first.ToString("u", CultureInfo.InvariantCulture)).Append('\n');
            }
            if (progress.BestSolveTimeMs > 0)
            {
                sb.Append("- **Mejor tiempo total:** ").Append(progress.BestSolveTimeMs).Append(" ms\n");
            }
            if (progress.MaxHintLevelUsed > 0)
            {
                sb.Append("- **Pistas usadas:** hasta el nivel ").Append(progress.MaxHintLevelUsed).Append(" de 3\n");
            }
        }

        sb.Append('\n');

        // Hint reveals — records that the student required help.
        if (hintReveals.Count > 0)
        {
            sb.Append("## Pistas reveladas\n\n");
            sb.Append("| Nivel | Revelada | Intentos fallidos | Forzada |\n");
            sb.Append("| :---: | --- | ---: | :---: |\n");
            foreach (var h in hintReveals.OrderBy(h => h.Level))
            {
                sb.Append("| ").Append(h.Level).Append(" | ")
                  .Append(h.RevealedAt.ToString("u", CultureInfo.InvariantCulture)).Append(" | ")
                  .Append(h.FailedAttemptsAtReveal).Append(" | ")
                  .Append(h.Forced ? "✓" : "·").Append(" |\n");
            }
            sb.Append('\n');
        }

        // Statement
        var statement = PickLang(ex.Statements, lang);
        if (!string.IsNullOrWhiteSpace(statement))
        {
            sb.Append("## Enunciado\n\n").Append(statement).Append("\n\n");
        }

        // Test cases overview
        sb.Append("## Casos de prueba\n\n");
        sb.Append("| ID | Visible | Descripción | Duración | Clk MHz |\n");
        sb.Append("| --- | :---: | --- | ---: | ---: |\n");
        foreach (var tc in ex.TestCases)
        {
            if (!req.IncludeHiddenTests && !tc.Visible) continue;
            var desc = PickLang(tc.Descriptions, lang) ?? "";
            sb.Append("| `").Append(tc.Id).Append("` | ")
              .Append(tc.Visible ? "✓" : "·").Append(" | ")
              .Append(MdEscape(desc)).Append(" | ")
              .Append(tc.Duration.TotalNanoseconds.ToString("0", CultureInfo.InvariantCulture)).Append(" ns | ")
              .Append(tc.SystemClockMhz).Append(" |\n");
        }
        if (!req.IncludeHiddenTests)
        {
            int hidden = ex.TestCases.Count(t => !t.Visible);
            if (hidden > 0)
                sb.Append("\n_").Append(hidden).Append(" prueba(s) oculta(s) omitida(s)._\n");
        }
        sb.Append('\n');

        // Match policy
        sb.Append("## Política de comparación\n\n");
        sb.Append("- **Tipo:** ").Append(ex.MatchKind).Append('\n');
        sb.Append("- **Puertos a comparar:** ");
        if (ex.OutputPortsToCompare.Count == 0) sb.Append("_(todos)_");
        else sb.Append(string.Join(", ", ex.OutputPortsToCompare.Select(p => "`" + p + "`")));
        sb.Append('\n');
        if (ex.MatchKind == PracticeMatchKind.ValueAtTimes && ex.SampleTimesNs.Count > 0)
        {
            sb.Append("- **Tiempos de muestreo (ns):** ")
              .Append(string.Join(", ", ex.SampleTimesNs))
              .Append('\n');
        }
        sb.Append('\n');

        // Per-attempt sections
        for (int i = 0; i < attempts.Count; i++)
        {
            var a = attempts[i];
            sb.Append("## Intento ").Append(i + 1).Append('/').Append(attempts.Count)
              .Append(" — ").Append(a.Verdict).Append("\n\n");

            sb.Append("- **Enviado:** ").Append(a.SubmittedAt.ToString("u", CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("- **Build:** ").Append(a.BuildDurationMs).Append(" ms · ");
            sb.Append("**Total:** ").Append(a.TotalDurationMs).Append(" ms");
            if (!string.IsNullOrEmpty(a.EngineVersion))
                sb.Append(" · **Engine:** `").Append(a.EngineVersion).Append('`');
            sb.Append('\n');

            if (!string.IsNullOrEmpty(a.BuildErrorMessage))
            {
                sb.Append("- **Build error:** ").Append(MdEscape(a.BuildErrorMessage)).Append('\n');
            }
            if (!string.IsNullOrEmpty(a.BuildErrorDetail))
            {
                sb.Append("\n```\n").Append(Truncate(a.BuildErrorDetail!, 2048)).Append("\n```\n");
            }

            sb.Append('\n');

            // Test results
            var results = perResults.TryGetValue(a.Id, out var rs) ? rs : new List<PracticeAttemptTestResult>();
            var testIndex = ex.TestCases.ToDictionary(t => t.Id, t => t);
            var visibleResults = req.IncludeHiddenTests
                ? results
                : results.Where(r => !testIndex.TryGetValue(r.TestCaseId, out var t) || t.Visible).ToList();

            if (visibleResults.Count > 0)
            {
                sb.Append("### Resultados por test\n\n");
                sb.Append("| Test | Resultado | Clase | Señal mismatch | t (ns) | Esperado | Obtenido | # mismatches | Sim ms |\n");
                sb.Append("| --- | :---: | --- | --- | ---: | --- | --- | ---: | ---: |\n");
                foreach (var r in visibleResults)
                {
                    sb.Append("| `").Append(r.TestCaseId).Append("` | ")
                      .Append(r.Passed ? "✅" : "❌").Append(" | ")
                      .Append(MdEscape(r.ErrorClass ?? "")).Append(" | ")
                      .Append(MdEscape(r.FirstMismatchSignal ?? "—")).Append(" | ")
                      .Append(r.FirstMismatchTimeNs?.ToString(CultureInfo.InvariantCulture) ?? "—").Append(" | ")
                      .Append(MdInline(r.ExpectedAtMismatch)).Append(" | ")
                      .Append(MdInline(r.ActualAtMismatch)).Append(" | ")
                      .Append(r.MismatchCount).Append(" | ")
                      .Append(r.SimulationDurationMs).Append(" |\n");
                }
                sb.Append('\n');
            }

            // Code
            if (req.IncludeFullCode)
            {
                sb.Append("### Código (VHDL)\n\n```vhdl\n").Append(a.CodeSnapshot).Append("\n```\n\n");
            }

            // Diff vs previous
            if (req.IncludeDiffs && i > 0)
            {
                var prev = attempts[i - 1];
                var diff = LineDiff.UnifiedDiff(
                    prev.CodeSnapshot, a.CodeSnapshot,
                    $"intento-{i:D3}.vhd", $"intento-{i + 1:D3}.vhd");
                if (diff.Length == 0)
                {
                    sb.Append("### Diff vs intento ").Append(i).Append("\n\n_(sin cambios — código idéntico al intento anterior)_\n\n");
                }
                else
                {
                    sb.Append("### Diff vs intento ").Append(i).Append("\n\n```diff\n").Append(diff).Append("```\n\n");
                }
            }
        }

        sb.Append("---\n_Generado por Kmila · Practice export_\n");
        return sb.ToString();
    }

    // ─── ZIP ───────────────────────────────────────────────────────────

    private static byte[] BuildZip(
        PracticeExercise ex,
        string lang,
        List<PracticeAttempt> attempts,
        Dictionary<int, List<PracticeAttemptTestResult>> perResults,
        PracticeProgress? progress,
        List<PracticeHintReveal> hintReveals,
        string markdownReport,
        PracticeSessionExportRequest req)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteText(zip, "report.md", markdownReport);

            // session.json — flat, machine-readable dump.
            var dump = new
            {
                schemaVersion = 1,
                exportedAtUtc = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture),
                lang,
                exercise = new
                {
                    id = ex.Id,
                    modality = ex.Modality.ToString(),
                    difficulty = ex.Difficulty.ToString(),
                    titles = ex.Titles,
                    summaries = ex.Summaries,
                    statements = ex.Statements,
                    matchKind = ex.MatchKind.ToString(),
                    outputPortsToCompare = ex.OutputPortsToCompare,
                    sampleTimesNs = ex.SampleTimesNs,
                    testCases = ex.TestCases.Select(tc => new
                    {
                        id = tc.Id,
                        visible = tc.Visible,
                        descriptions = tc.Descriptions,
                        durationNs = tc.Duration.TotalNanoseconds,
                        systemClockMhz = tc.SystemClockMhz,
                    }).ToList(),
                },
                progress = progress is null ? null : new
                {
                    status = progress.Status.ToString(),
                    firstSolvedAt = progress.FirstSolvedAt?.ToString("u", CultureInfo.InvariantCulture),
                    attemptCount = progress.AttemptCount,
                    bestSolveTimeMs = progress.BestSolveTimeMs,
                    lastAttemptAt = progress.LastAttemptAt.ToString("u", CultureInfo.InvariantCulture),
                    maxHintLevelUsed = progress.MaxHintLevelUsed,
                },
                hintReveals = hintReveals.OrderBy(h => h.Level).Select(h => new
                {
                    level = h.Level,
                    revealedAtUtc = h.RevealedAt.ToString("u", CultureInfo.InvariantCulture),
                    failedAttemptsAtReveal = h.FailedAttemptsAtReveal,
                    forced = h.Forced,
                }).ToList(),
                attempts = attempts.Select((a, i) => new
                {
                    ordinal = i + 1,
                    submittedAtUtc = a.SubmittedAt.ToString("u", CultureInfo.InvariantCulture),
                    verdict = a.Verdict.ToString(),
                    buildDurationMs = a.BuildDurationMs,
                    totalDurationMs = a.TotalDurationMs,
                    buildErrorMessage = a.BuildErrorMessage,
                    buildErrorDetail = a.BuildErrorDetail,
                    engineVersion = a.EngineVersion,
                    testResults = (perResults.TryGetValue(a.Id, out var rs) ? rs : new())
                        .Where(r =>
                        {
                            if (req.IncludeHiddenTests) return true;
                            var tc = ex.TestCases.FirstOrDefault(t => t.Id == r.TestCaseId);
                            return tc is null || tc.Visible;
                        })
                        .Select(r => new
                        {
                            testCaseId = r.TestCaseId,
                            passed = r.Passed,
                            errorClass = r.ErrorClass,
                            firstMismatchSignal = r.FirstMismatchSignal,
                            firstMismatchTimeNs = r.FirstMismatchTimeNs,
                            expectedAtMismatch = r.ExpectedAtMismatch,
                            actualAtMismatch = r.ActualAtMismatch,
                            mismatchCount = r.MismatchCount,
                            simulationDurationMs = r.SimulationDurationMs,
                        }).ToList(),
                }).ToList(),
            };
            WriteText(zip, "session.json", JsonSerializer.Serialize(dump, JsonOpts));

            // Per-attempt source files.
            for (int i = 0; i < attempts.Count; i++)
            {
                var a = attempts[i];
                var ord = (i + 1).ToString("D3", CultureInfo.InvariantCulture);
                var verd = a.Verdict.ToString().ToLowerInvariant();
                var ts = a.SubmittedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                WriteText(zip, $"attempts/attempt-{ord}-{ts}-{verd}.vhd", a.CodeSnapshot);
            }

            // Per-pair diffs.
            if (req.IncludeDiffs)
            {
                for (int i = 1; i < attempts.Count; i++)
                {
                    var prev = attempts[i - 1];
                    var curr = attempts[i];
                    var diff = LineDiff.UnifiedDiff(
                        prev.CodeSnapshot, curr.CodeSnapshot,
                        $"attempt-{i:D3}.vhd", $"attempt-{i + 1:D3}.vhd");
                    if (diff.Length == 0) diff = "(no changes — code identical to previous attempt)\n";
                    WriteText(zip, $"diffs/attempt-{i + 1:D3}-vs-{i:D3}.diff", diff);
                }
            }
        }
        return ms.ToArray();
    }

    private static void WriteText(ZipArchive zip, string entryName, string text)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var s = entry.Open();
        using var w = new StreamWriter(s, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        w.Write(text);
    }

    // ─── helpers ───────────────────────────────────────────────────────

    private static string? PickLang(Dictionary<string, string> dict, string lang)
    {
        if (dict.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
        if (dict.TryGetValue("en", out var en) && !string.IsNullOrEmpty(en)) return en;
        if (dict.TryGetValue("es", out var es) && !string.IsNullOrEmpty(es)) return es;
        return dict.Values.FirstOrDefault();
    }

    private static string MdEscape(string s)
        => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|", System.StringComparison.Ordinal).Replace("\n", " ", System.StringComparison.Ordinal).Replace("\r", "", System.StringComparison.Ordinal);

    private static string MdInline(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "—";
        return "`" + s.Replace("`", "ʼ", System.StringComparison.Ordinal).Replace("|", "\\|", System.StringComparison.Ordinal).Replace("\n", " ", System.StringComparison.Ordinal).Replace("\r", "", System.StringComparison.Ordinal) + "`";
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "\n…[truncated]";

    /// <summary>Strips characters that are unsafe across MAUI/Web download targets.</summary>
    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "session";
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) >= 0 || c == ' ' || c == '/' || c == '\\') sb.Append('-');
            else sb.Append(c);
        }
        var clean = sb.ToString().Trim('-');
        return clean.Length == 0 ? "session" : clean;
    }
}
