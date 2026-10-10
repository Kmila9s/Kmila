using Kmila.CLI.Cli;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila completion bash|zsh|fish</c> — prints a shell completion
/// script for the command set, so users can tab-complete sub-commands. Install,
/// e.g., with <c>kmila completion zsh &gt; ~/.kmila-completion.zsh</c> and source it.
/// </summary>
public static class CompletionCommand
{
    private const string Commands =
        "sim ui check diff practice synth flow fit blocks serve completion version help";

    /// <summary>Emits the requested shell's completion script.</summary>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine("kmila completion bash|zsh|fish — print a shell completion script");
            return Task.FromResult(0);
        }

        var shell = (p.Positionals.Count > 0 ? p.Positionals[0] : p.Get("shell") ?? "").ToLowerInvariant();
        switch (shell)
        {
            case "bash": Console.Out.WriteLine(Bash()); return Task.FromResult(0);
            case "zsh": Console.Out.WriteLine(Zsh()); return Task.FromResult(0);
            case "fish": Console.Out.WriteLine(Fish()); return Task.FromResult(0);
            default:
                Console.Error.WriteLine("kmila completion: specify a shell (bash|zsh|fish).");
                return Task.FromResult(64);
        }
    }

    private static string Bash() => $$"""
        # kmila bash completion — source this file (e.g. from ~/.bashrc)
        _kmila() {
          local cur="${COMP_WORDS[COMP_CWORD]}"
          if [ "$COMP_CWORD" -eq 1 ]; then
            COMPREPLY=( $(compgen -W "{{Commands}}" -- "$cur") )
          else
            COMPREPLY=( $(compgen -f -- "$cur") )
          fi
        }
        complete -F _kmila kmila
        """;

    private static string Zsh() => $$"""
        # kmila zsh completion — source this file (e.g. from ~/.zshrc)
        _kmila() {
          if (( CURRENT == 2 )); then
            compadd {{Commands}}
          else
            _files
          fi
        }
        compdef _kmila kmila
        """;

    private static string Fish() => $$"""
        # kmila fish completion — save to ~/.config/fish/completions/kmila.fish
        complete -c kmila -f
        for c in {{Commands}}
          complete -c kmila -n "__fish_use_subcommand" -a $c
        end
        """;
}
