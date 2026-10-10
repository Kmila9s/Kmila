using Kmila.CLI.Cli;
using Kmila.CLI.Tui;

namespace Kmila.CLI.Commands;

/// <summary>Implements <c>kmila ui</c> — the full-screen ANSI terminal UI.</summary>
public static class UiCommand
{
    /// <summary>Parses options and runs the terminal UI.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "ascii", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Ui());
            return 0;
        }

        await Task.CompletedTask;

        var topFile = p.Get("top-file", "t");
        var openVcd = p.Get("open");
        var dir = p.Get("dir") ?? (topFile != null ? Path.GetDirectoryName(Path.GetFullPath(topFile)) : null)
                               ?? Directory.GetCurrentDirectory();
        var lang = p.Get("lang");
        bool ascii = p.Has("ascii");

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("kmila ui: a real interactive terminal is required (stdin/stdout are redirected).");
            return 1;
        }

        var app = new TuiApp(dir, topFile, openVcd, lang, ascii);
        return app.Run();
    }
}
