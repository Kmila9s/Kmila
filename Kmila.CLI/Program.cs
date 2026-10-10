namespace Kmila.CLI;

/// <summary>
/// Entry point of the Kmila terminal head. Dispatches the first positional
/// argument to a sub-command: <c>sim</c> (non-interactive simulation) or
/// <c>ui</c> (full-screen ANSI terminal UI), plus <c>help</c> / <c>version</c>.
/// </summary>
public static class Program
{
    /// <summary>Program version, surfaced by <c>kmila version</c> and the TUI title.</summary>
    public const string Version = "0.1.0";

    /// <summary>
    /// Parses the sub-command and routes to its handler. Unknown commands and
    /// <c>help</c>/<c>--help</c>/no-args print usage.
    /// </summary>
    /// <param name="args">Raw process arguments.</param>
    /// <returns>Process exit code (0 ok, 1 runtime failure, 64 usage error).</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("kmila: no command given. Try `kmila help`.");
            return 64;
        }

        var command = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "sim"                      => await Commands.SimCommand.RunAsync(rest),
                "ui"                       => await Commands.UiCommand.RunAsync(rest),
                "check"                    => await Commands.CheckCommand.RunAsync(rest),
                "diff"                     => await Commands.DiffCommand.RunAsync(rest),
                "practice"                 => await Commands.PracticeCommand.RunAsync(rest),
                "synth"                    => await Commands.SynthCommand.RunAsync(rest),
                "flow"                     => await Commands.FlowCommand.RunAsync(rest),
                "fit"                      => await Commands.FitCommand.RunAsync(rest),
                "blocks"                   => await Commands.BlocksCommand.RunAsync(rest),
                "serve"                    => await Commands.ServeCommand.RunAsync(rest),
                "completion"               => await Commands.CompletionCommand.RunAsync(rest),
                "version" or "--version"   => PrintVersion(),
                "help" or "--help" or "-h" => PrintHelp(),
                _                          => Unknown(command),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"kmila: {ex.Message}");
            return 1;
        }
    }

    private static int PrintVersion()
    {
        Console.WriteLine($"kmila {Version}");
        return 0;
    }

    private static int PrintHelp()
    {
        Console.WriteLine(Cli.HelpText.Root());
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"kmila: unknown command '{command}'. Try `kmila help`.");
        return 64;
    }
}
