using Kmila.CLI.Cli;
using Kmila.CLI.I18n;
using Kmila.CLI.Simulation;
using Kmila.Shared.Models.Practice;
using Kmila.Shared.Services.Practice;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila practice</c> — list exercises, show one, or auto-grade a
/// candidate solution. Reuses the shipping in-process grader
/// (<see cref="PracticeCatalog"/> + <see cref="PracticeEvaluator"/>) over the
/// bundled <c>Practice/</c> content, so a terminal (or CI) gets the exact same
/// verdict the GUI produces.
/// </summary>
public static class PracticeCommand
{
    /// <summary>Dispatches the practice sub-command (list | show | grade | grade-all).</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "quiet", "q", "verbose", "v", "help", "h", "hidden");
        if (p.Has("help", "h") || args.Length == 0)
        {
            Console.WriteLine(HelpText.Practice());
            return args.Length == 0 ? 64 : 0;
        }

        var s = Strings.Load(p.Get("lang"));
        var lang = s.Lang;
        var sub = p.Positionals.Count > 0 ? p.Positionals[0].ToLowerInvariant() : "list";

        PracticeCatalog catalog;
        try { catalog = new PracticeCatalog(PracticeContent.OpenDelegate()); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila practice: {ex.Message}"); return 1; }

        switch (sub)
        {
            case "list": return await ListAsync(catalog, lang);
            case "show": return await ShowAsync(catalog, p, lang);
            case "grade": return await GradeAsync(catalog, p, lang);
            case "grade-all": return await GradeAllAsync(catalog, p, lang);
            default:
                Console.Error.WriteLine($"kmila practice: unknown sub-command '{sub}'. Use list|show|grade|grade-all.");
                return 64;
        }
    }

    private static async Task<int> ListAsync(PracticeCatalog catalog, string lang)
    {
        var metas = await catalog.ListAsync();
        if (metas.Count == 0) { Console.Error.WriteLine("kmila practice: no exercises found."); return 1; }
        foreach (var m in metas.OrderBy(m => m.Difficulty).ThenBy(m => m.Id))
        {
            var title = PracticeCatalog.Localize(m.Titles, lang, m.Id);
            Console.Out.WriteLine($"{m.Id,-28} {m.Modality,-8} {m.Difficulty,-6} {title}");
        }
        Console.Error.WriteLine($"[practice] {metas.Count} exercise(s)");
        return 0;
    }

    private static async Task<int> ShowAsync(PracticeCatalog catalog, ArgParser p, string lang)
    {
        var id = ExerciseId(p);
        if (id == null) { Console.Error.WriteLine("kmila practice show: give an exercise id. Try `kmila practice list`."); return 64; }

        var ex = await catalog.GetAsync(id);
        if (ex == null) { Console.Error.WriteLine($"kmila practice: unknown exercise '{id}'."); return 1; }

        Console.Out.WriteLine($"# {PracticeCatalog.Localize(ex.Titles, lang, ex.Id)}");
        Console.Out.WriteLine($"id:         {ex.Id}");
        Console.Out.WriteLine($"modality:   {ex.Modality}");
        Console.Out.WriteLine($"difficulty: {ex.Difficulty}");
        Console.Out.WriteLine($"ports:      {string.Join(", ", ex.OutputPortsToCompare)}");
        Console.Out.WriteLine($"tests:      {ex.TestCases.Count}");
        var statement = PracticeCatalog.Localize(ex.Statements, lang, "");
        if (!string.IsNullOrWhiteSpace(statement))
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine(statement.Trim());
        }
        return 0;
    }

    private static async Task<int> GradeAsync(PracticeCatalog catalog, ArgParser p, string lang)
    {
        var id = ExerciseId(p);
        if (id == null) { Console.Error.WriteLine("kmila practice grade: give an exercise id."); return 64; }

        var solPath = p.Get("solution", "file", "f");
        // Allow a second positional as the solution path: `grade <id> <file>`.
        if (string.IsNullOrWhiteSpace(solPath) && p.Positionals.Count > 1) solPath = p.Positionals[1];
        if (string.IsNullOrWhiteSpace(solPath))
        {
            Console.Error.WriteLine("kmila practice grade: give --solution <file.vhdl>.");
            return 64;
        }
        solPath = Path.GetFullPath(solPath);
        if (!File.Exists(solPath)) { Console.Error.WriteLine($"kmila practice: solution not found: {solPath}"); return 1; }

        var ex = await catalog.GetAsync(id);
        if (ex == null) { Console.Error.WriteLine($"kmila practice: unknown exercise '{id}'."); return 1; }

        bool verbose = p.Has("verbose", "v");
        var verdict = await GradeOneAsync(ex, File.ReadAllText(solPath), verbose);
        PrintVerdict(verdict, PracticeCatalog.Localize(ex.Titles, lang, ex.Id), verbose);
        return verdict.Kind == PracticeVerdictKind.Solved ? 0 : 1;
    }

    private static async Task<int> GradeAllAsync(PracticeCatalog catalog, ArgParser p, string lang)
    {
        var id = p.Get("exercise", "id");
        var dir = p.Positionals.Count > 1 ? p.Positionals[1] : p.Get("dir");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(dir))
        {
            Console.Error.WriteLine("kmila practice grade-all: usage: grade-all <dir> --exercise <id>");
            return 64;
        }
        dir = Path.GetFullPath(dir);
        if (!Directory.Exists(dir)) { Console.Error.WriteLine($"kmila practice: not a directory: {dir}"); return 1; }

        var ex = await catalog.GetAsync(id);
        if (ex == null) { Console.Error.WriteLine($"kmila practice: unknown exercise '{id}'."); return 1; }

        var files = Directory.EnumerateFiles(dir)
            .Where(Kmila.Frontend.HdlLanguages.IsHdlSource)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0) { Console.Error.WriteLine($"kmila practice: no {Kmila.Frontend.HdlLanguages.AcceptList()} files in directory."); return 1; }

        bool verbose = p.Has("verbose", "v");
        int solved = 0;
        foreach (var f in files)
        {
            var verdict = await GradeOneAsync(ex, File.ReadAllText(f), verbose);
            if (verdict.Kind == PracticeVerdictKind.Solved) solved++;
            int pass = verdict.TestResults.Count(r => r.Passed);
            Console.Out.WriteLine($"{Path.GetFileName(f),-32} {verdict.Kind,-12} {pass}/{verdict.TestResults.Count}");
        }
        Console.Error.WriteLine($"[practice] {solved}/{files.Count} solved for '{id}'");
        return solved == files.Count ? 0 : 1;
    }

    /// <summary>Grades one candidate, capturing engine console chatter so stdout stays clean.</summary>
    private static async Task<PracticeAttemptVerdict> GradeOneAsync(PracticeExercise ex, string code, bool verbose)
    {
        var originalOut = Console.Out;
        var captured = new StringWriter();
        if (!verbose) Console.SetOut(captured);
        try
        {
            var evaluator = new PracticeEvaluator();
            return await evaluator.GradeAsync(ex, code);
        }
        finally
        {
            if (!verbose) Console.SetOut(originalOut);
        }
    }

    private static void PrintVerdict(PracticeAttemptVerdict v, string title, bool verbose)
    {
        int pass = v.TestResults.Count(r => r.Passed);
        Console.Out.WriteLine($"{v.Kind}: {title}  ({pass}/{v.TestResults.Count} tests, {v.TotalDurationMs}ms)");
        if (v.Kind == PracticeVerdictKind.BuildError && !string.IsNullOrWhiteSpace(v.BuildErrorMessage))
            Console.Error.WriteLine($"  build error: {v.BuildErrorMessage}");
        foreach (var r in v.TestResults.Where(r => !r.Passed))
        {
            var where = r.FirstMismatchSignal != null
                ? $" @ {r.FirstMismatchSignal}={r.ActualAtMismatch} (expected {r.ExpectedAtMismatch}) t={r.FirstMismatchTimeNs}ns"
                : "";
            Console.Error.WriteLine($"  test {r.TestCaseId}: {r.ErrorClass ?? "fail"}{where}");
        }
    }

    private static string? ExerciseId(ArgParser p)
    {
        var id = p.Get("exercise", "id");
        if (!string.IsNullOrWhiteSpace(id)) return id;
        // Second positional after the sub-command: `show <id>` / `grade <id>`.
        return p.Positionals.Count > 1 ? p.Positionals[1] : null;
    }
}
