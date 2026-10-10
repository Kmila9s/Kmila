using Kmila.CLI.Cli;
using Kmila.CLI.Simulation;
using Kmila.Shared.Services;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila flow</c> — emit a PlantUML control-flow diagram for a
/// design. Reuses <see cref="PlantUmlFlowEmitter"/> (which introspects the
/// parsed entities). Output is PlantUML <b>text</b>; if a <c>plantuml</c> binary
/// is on PATH and the output name ends in <c>.svg/.png</c>, it is rendered.
/// </summary>
public static class FlowCommand
{
    /// <summary>Emits the flow diagram to a file or stdout.</summary>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "quiet", "q", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Flow());
            return Task.FromResult(0);
        }

        if (!SynthCommand.TopFile(p, out var top, out var extras, out var err))
        {
            Console.Error.WriteLine($"kmila flow: {err}");
            return Task.FromResult(64);
        }

        DesignLoader.Loaded loaded;
        try { loaded = DesignLoader.Load(top, extras, p.Get("entity", "top")); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila flow: {ex.Message}"); return Task.FromResult(1); }

        string puml;
        try { puml = PlantUmlFlowEmitter.Emit(loaded.All); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila flow: {ex.Message}"); return Task.FromResult(1); }

        if (string.IsNullOrWhiteSpace(puml))
        {
            Console.Error.WriteLine("kmila flow: the emitter produced no diagram for this design.");
            return Task.FromResult(1);
        }

        var outFile = p.Get("output-file", "o");
        if (string.IsNullOrWhiteSpace(outFile))
        {
            Console.Out.Write(puml);
            if (!puml.EndsWith('\n')) Console.Out.WriteLine();
            return Task.FromResult(0);
        }

        outFile = Path.GetFullPath(outFile);
        var ext = Path.GetExtension(outFile).ToLowerInvariant();
        bool render = ext is ".svg" or ".png";

        // Write the .puml next to the requested output (or as the output itself).
        var pumlPath = render ? Path.ChangeExtension(outFile, ".puml") : outFile;
        File.WriteAllText(pumlPath, puml);

        if (render)
        {
            if (!TryRenderPlantUml(pumlPath, ext, outFile, out var why))
            {
                Console.Error.WriteLine($"[flow] wrote {pumlPath} (could not render {ext}: {why})");
                return Task.FromResult(0);
            }
            if (!p.Has("quiet", "q")) Console.Error.WriteLine($"[flow] entity={loaded.Top.Name} → {outFile}");
        }
        else if (!p.Has("quiet", "q"))
        {
            Console.Error.WriteLine($"[flow] entity={loaded.Top.Name} → {pumlPath}");
        }
        return Task.FromResult(0);
    }

    /// <summary>Best-effort render via a local <c>plantuml</c> CLI, if present.</summary>
    private static bool TryRenderPlantUml(string pumlPath, string ext, string outFile, out string why)
    {
        why = "";
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("plantuml",
                $"-t{ext.TrimStart('.')} \"{pumlPath}\"")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) { why = "could not start plantuml"; return false; }
            proc.WaitForExit(30_000);
            // plantuml writes alongside the .puml with the same stem.
            var produced = Path.ChangeExtension(pumlPath, ext);
            if (File.Exists(produced) && !string.Equals(produced, outFile, StringComparison.Ordinal))
                File.Move(produced, outFile, overwrite: true);
            return File.Exists(outFile);
        }
        catch (Exception ex)
        {
            why = ex.Message.Contains("No such file", StringComparison.OrdinalIgnoreCase) ||
                  ex is System.ComponentModel.Win32Exception
                ? "plantuml not installed"
                : ex.Message;
            return false;
        }
    }
}
