using Kmila.CLI.Cli;
using DParser = Debugger.Repositories.Parser;
using Sev = Parser.Models.DiagnosticSeverity;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila check</c> — parse + lint a VHDL file and report
/// diagnostics without running a simulation. A fast "does it compile?" gate for
/// CI and editors. Reuses the parser's own diagnostics stream
/// (<see cref="DParser.Diagnostics"/>, populated by its source-level lint pass).
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
        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"kmila check: file not found: {file}");
                errors++;
                continue;
            }

            var diags = new List<Parser.Models.Diagnostic>();
            // The parser runs synthesis, which writes chatter to Console.Out;
            // capture it so `check` output stays clean (diagnostics go to stderr).
            var originalOut = Console.Out;
            var sink = new StringWriter();
            Console.SetOut(sink);
            try
            {
                var parser = new DParser(file);
                parser.ParseFile();
                diags.AddRange(parser.Diagnostics);
                if (parser.Entities.Count == 0)
                    diags.Add(new Parser.Models.Diagnostic
                    {
                        Severity = Sev.Warning,
                        Line = 1,
                        Code = "NO-ENTITY",
                        Message = "no entity was parsed from this file",
                    });
            }
            catch (Exception ex)
            {
                // A hard parse failure. The diagnostics list is still mirrored
                // for the lint pass; add the fatal error on top.
                try { diags.AddRange(new DParser(file).Diagnostics); } catch { }
                diags.Add(new Parser.Models.Diagnostic
                {
                    Severity = Sev.Error,
                    Line = 1,
                    Code = "PARSE-ERROR",
                    Message = ex.Message,
                });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

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
