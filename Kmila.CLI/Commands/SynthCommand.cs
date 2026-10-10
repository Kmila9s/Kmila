using Interpreter.Synthesis;
using Interpreter.Synthesis.Layout;
using Kmila.CLI.Cli;
using Kmila.CLI.Simulation;
using Kmila.Shared.Models;
using Kmila.Shared.Services;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila synth</c> — synthesize a design to a gate-level netlist
/// and export it as an SVG schematic. Reuses the engine's synthesis + layout +
/// the app's <see cref="SchematicExporter"/>, so the terminal produces the same
/// diagram the GUI does (great for thesis figures).
/// </summary>
public static class SynthCommand
{
    /// <summary>Synthesizes the design and writes an SVG (or prints it to stdout).</summary>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "quiet", "q", "mono", "no-title", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Synth());
            return Task.FromResult(0);
        }

        if (!TopFile(p, out var top, out var extras, out var err))
        {
            Console.Error.WriteLine(err);
            return Task.FromResult(64);
        }

        DesignLoader.Loaded loaded;
        try { loaded = DesignLoader.Load(top, extras, p.Get("entity", "top")); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila synth: {ex.Message}"); return Task.FromResult(1); }

        SchematicRenderMode mode = (p.Get("mode") ?? "grouped").Trim().ToLowerInvariant() switch
        {
            "raw" => SchematicRenderMode.Raw,
            "grouped" => SchematicRenderMode.Grouped,
            "rtl" => SchematicRenderMode.Rtl,
            "nodes" => SchematicRenderMode.Nodes,
            var o => throw_or_default(o),
        };

        var scope = (p.Get("scope") ?? "full").Trim().ToLowerInvariant() switch
        {
            "full" => SchematicExportScope.Full,
            "inputs" => SchematicExportScope.Inputs,
            "outputs" => SchematicExportScope.Outputs,
            _ => SchematicExportScope.Full,
        };

        string svg;
        int cells, nets;
        try
        {
            var netlist = Synthesizer.Synthesize(loaded.Top);
            cells = netlist.Cells.Count; nets = netlist.Nets.Count;
            var layout = LayeredLayout.Layout(netlist);
            var filtered = SelectionFilter.Full(layout);
            var req = new SchematicExportRequest
            {
                Mode = mode,
                Scope = scope,
                ColorMode = p.Has("mono") ? SchematicExportColorMode.Monochrome : SchematicExportColorMode.Color,
                IncludeTitle = !p.Has("no-title"),
            };
            svg = SchematicExporter.BuildSvg(filtered, req, title: loaded.Top.Name);
        }
        catch (Exception ex) { Console.Error.WriteLine($"kmila synth: {ex.Message}"); return Task.FromResult(1); }

        var outFile = p.Get("output-file", "o");
        if (string.IsNullOrWhiteSpace(outFile))
        {
            Console.Out.Write(svg);
            if (!svg.EndsWith('\n')) Console.Out.WriteLine();
        }
        else
        {
            outFile = Path.GetFullPath(outFile);
            File.WriteAllText(outFile, svg);
            if (!p.Has("quiet", "q"))
                Console.Error.WriteLine($"[synth] entity={loaded.Top.Name} cells={cells} nets={nets} → {outFile}");
        }
        return Task.FromResult(0);
    }

    private static SchematicRenderMode throw_or_default(string o)
        => throw new FormatException($"unknown --mode '{o}' (use raw|grouped|rtl|nodes)");

    /// <summary>Resolves the top VHDL file + extra sources from args, shared by the analysis commands.</summary>
    internal static bool TopFile(ArgParser p, out string top, out List<string> extras, out string error)
    {
        top = ""; extras = new(); error = "";
        var t = p.Get("top-file", "main-file", "t");
        if (string.IsNullOrWhiteSpace(t) && p.Positionals.Count > 0) t = p.Positionals[0];
        if (string.IsNullOrWhiteSpace(t))
        {
            error = "give a VHDL file (positional or --top-file).";
            return false;
        }
        var topPath = Path.GetFullPath(t);
        top = topPath;
        if (!File.Exists(topPath)) { error = $"top file not found: {topPath}"; return false; }

        extras = p.GetAll("file")
                  .Concat(p.Positionals.Skip(p.Has("top-file", "main-file", "t") ? 0 : 1))
                  .Select(Path.GetFullPath)
                  .Where(f => !string.Equals(f, topPath, StringComparison.Ordinal))
                  .Distinct().ToList();
        return true;
    }
}
