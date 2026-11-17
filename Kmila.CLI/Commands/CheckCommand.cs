using Kmila.CLI.Cli;
using Sev = Parser.Models.DiagnosticSeverity;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila check</c> — parse + lint HDL files and report
/// diagnostics without running a simulation. A fast "does it compile?" gate for
/// CI and editors. Reuses the front-end's per-file diagnostics (lint pass +
/// parse errors).
/// </summary>
public static class CheckCommand
{
    /// <summary>Parses the file(s), prints diagnostics, and returns an exit code.</summary>
    /// <returns>0 when there are no errors, 1 when any error is found, 64 on a usage error.</returns>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "quiet", "q", "help", "h", "warnings-as-errors");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Check());
            return Task.FromResult(0);
        }

        // Accept the file as --top-file/-t or as a bare positional, plus extra --file.
        var files = new List<string>();
        var top = p.Get("top-file", "main-file", "t");
        if (!string.IsNullOrWhiteSpace(top)) files.Add(top);
        files.AddRange(p.GetAll("file"));
        files.AddRange(p.Positionals);
        files = files.Select(Path.GetFullPath).Distinct().ToList();

        if (files.Count == 0)
        {
            Console.Error.WriteLine("kmila check: give a VHDL file (positional or --top-file). Try `kmila check --help`.");
            return Task.FromResult(64);
        }

        bool quiet = p.Has("quiet", "q");
        bool warnAsError = p.Has("warnings-as-errors");

        int errors = 0, warnings = 0, infos = 0;

        // The front-end scans every input first (so cross-file entities and
        // packages resolve — single-file input still only sees its own
        // packages, like GHDL analysing one file) and then parses each file on
        // its own. Synthesis writes chatter to Console.Out; capture it so the
        // diagnostics on stderr stay clean.
        IReadOnlyList<(string File, Kmila.Frontend.ParsedUnit Unit)> analyzed;
        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try { analyzed = Kmila.Frontend.Frontends.ForSources(files).AnalyzeEach(files.Where(File.Exists).ToList()); }
        catch (NotSupportedException ex)
        {
            Console.Error.WriteLine($"kmila check: {ex.Message}");
            return Task.FromResult(1);
        }
        finally { Console.SetOut(originalOut); }

        var unitByFile = analyzed.ToDictionary(a => a.File, a => a.Unit);
        foreach (var file in files)
        {
            if (!unitByFile.TryGetValue(file, out var unit))
            {
                Console.Error.WriteLine($"kmila check: file not found: {file}");
                errors++;
                continue;
            }
            // Lint diagnostics are kept even when the parse failed.
            var diags = new List<Parser.Models.Diagnostic>(unit.Diagnostics);
            if (unit.Error != null)
                diags.Add(Parser.Models.Diagnostic.FromAnyException(unit.Error, unit.Source, "PARSE-ERROR"));
            else if (unit.Entities.Count == 0)
                diags.Add(new Parser.Models.Diagnostic
                {
                    Severity = Sev.Warning,
                    Line = 1,
                    Code = "NO-ENTITY",
                    Message = "no entity was parsed from this file",
                });

            var name = Path.GetFileName(file);
            foreach (var d in diags.OrderBy(d => d.Line).ThenBy(d => d.StartColumn))
            {
                switch (d.Severity)
                {
                    case Sev.Error: errors++; break;
                    case Sev.Warning: warnings++; break;
                    default: infos++; break;
                }
                if (!quiet)
                {
                    var sev = d.Severity switch
                    {
                        Sev.Error => "error",
                        Sev.Warning => "warning",
                        _ => "info",
                    };
                    var code = string.IsNullOrEmpty(d.Code) ? "" : $" [{d.Code}]";
                    Console.Error.WriteLine($"{name}:{d.Line}:{d.StartColumn}: {sev}{code}: {d.Message}");
                }
            }
        }

        if (!quiet)
            Console.Error.WriteLine($"[check] {errors} error(s), {warnings} warning(s), {infos} info across {files.Count} file(s)");

        bool failed = errors > 0 || (warnAsError && warnings > 0);
        return Task.FromResult(failed ? 1 : 0);
    }
}
