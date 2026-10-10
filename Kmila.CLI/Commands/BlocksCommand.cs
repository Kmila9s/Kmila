using Kmila.CLI.Cli;
using Kmila.Shared.Services;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila blocks</c> — convert between VHDL and the block-editor's
/// workspace JSON, in either direction. Reuses <see cref="VhdlBlockBuilder"/>
/// (VHDL → blocks JSON) and <see cref="VhdlBlockEmitter"/> (blocks JSON → VHDL),
/// both plain string-in/string-out.
/// </summary>
public static class BlocksCommand
{
    /// <summary>Runs the chosen conversion direction.</summary>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "quiet", "q", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Blocks());
            return Task.FromResult(0);
        }

        var toJson = p.Get("to-json");
        var toVhdl = p.Get("to-vhdl");
        if (string.IsNullOrWhiteSpace(toJson) && string.IsNullOrWhiteSpace(toVhdl))
        {
            Console.Error.WriteLine("kmila blocks: use --to-json <file.vhdl> or --to-vhdl <file.json>.");
            return Task.FromResult(64);
        }

        var outFile = p.Get("output-file", "o");
        string result;
        try
        {
            if (!string.IsNullOrWhiteSpace(toJson))
            {
                var path = Path.GetFullPath(toJson);
                if (!File.Exists(path)) { Console.Error.WriteLine($"kmila blocks: not found: {path}"); return Task.FromResult(1); }
                result = new VhdlBlockBuilder().Build(File.ReadAllText(path));
            }
            else
            {
                var path = Path.GetFullPath(toVhdl!);
                if (!File.Exists(path)) { Console.Error.WriteLine($"kmila blocks: not found: {path}"); return Task.FromResult(1); }
                var emitter = new VhdlBlockEmitter();
                result = emitter.Emit(File.ReadAllText(path));
                if (emitter.LastDiagnostics.Count > 0 && !p.Has("quiet", "q"))
                    foreach (var d in emitter.LastDiagnostics)
                        Console.Error.WriteLine($"[blocks] {d}");
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"kmila blocks: {ex.Message}"); return Task.FromResult(1); }

        if (string.IsNullOrWhiteSpace(result))
        {
            Console.Error.WriteLine("kmila blocks: conversion produced empty output.");
            return Task.FromResult(1);
        }

        if (string.IsNullOrWhiteSpace(outFile))
        {
            Console.Out.Write(result);
            if (!result.EndsWith('\n')) Console.Out.WriteLine();
        }
        else
        {
            File.WriteAllText(Path.GetFullPath(outFile), result);
            if (!p.Has("quiet", "q")) Console.Error.WriteLine($"[blocks] → {Path.GetFullPath(outFile)}");
        }
        return Task.FromResult(0);
    }
}
