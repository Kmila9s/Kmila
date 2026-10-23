using Interpreter.Services;
using Kmila.CLI.Cli;
using Kmila.CLI.Simulation;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila fit</c> — estimate a design's resource usage and (with
/// <c>--fpga</c>) compare it to the device's capacity. Reuses the engine's
/// <see cref="Resources"/> estimator + the device specs via
/// <see cref="FpgaResolver"/>; the used-vs-available math lives in
/// <see cref="ResourceReport"/>.
/// </summary>
public static class FitCommand
{
    /// <summary>Estimates resources and prints a utilization table.</summary>
    public static Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "quiet", "q", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Fit());
            return Task.FromResult(0);
        }

        if (!SynthCommand.TopFile(p, out var top, out var extras, out var err))
        {
            Console.Error.WriteLine($"kmila fit: {err}");
            return Task.FromResult(64);
        }

        var fpga = p.Get("fpga");
        Kmila.Shared.Models.FPGAResources? device = null;
        string? deviceName = null;
        if (!string.IsNullOrWhiteSpace(fpga))
        {
            if (string.Equals(fpga, "list", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(string.Join(Environment.NewLine, FpgaResolver.KnownModels()));
                return Task.FromResult(0);
            }
            var spec = FpgaResolver.Load(fpga);
            if (spec == null)
            {
                Console.Error.WriteLine($"kmila fit: unknown FPGA '{fpga}'. Use `--fpga list`.");
                return Task.FromResult(1);
            }
            device = spec.Resources;
            deviceName = fpga;
        }

        DesignLoader.Loaded loaded;
        try { loaded = DesignLoader.Load(top, extras, p.Get("entity", "top")); }
        catch (Exception ex) { Console.Error.WriteLine($"kmila fit: {ex.Message}"); return Task.FromResult(1); }

        // Resources.Calculate writes diagnostics to Console; capture them.
        Resources res;
        var originalOut = Console.Out;
        var sink = new StringWriter();
        Console.SetOut(sink);
        try
        {
            // Aggregate the whole design: a structural top (all logic delegated to
            // port-mapped children) would otherwise report only its own I/O. Parse
            // every design file fresh so each child carries a self estimate, and
            // let ComputeHierarchical sum the port-mapped subtree. Flat / single-
            // entity designs fall through to the same self-only numbers.
            var entryName = p.Get("entity", "top");
            if (string.IsNullOrWhiteSpace(entryName))
                entryName = Path.GetFileNameWithoutExtension(top);
            var files = new List<string> { top };
            files.AddRange(extras);
            var byName = Kmila.Shared.Services.HierarchyLoader.ParseDesign(
                files, entryName, out var topEntity, out var specialize);
            var rootEntity = topEntity ?? loaded.Top;
            res = Resources.ComputeHierarchical(rootEntity, byName, specialize);
        }
        catch (Exception ex) { Console.SetOut(originalOut); Console.Error.WriteLine($"kmila fit: {ex.Message}"); return Task.FromResult(1); }
        finally { Console.SetOut(originalOut); }

        var rows = ResourceReport.Build(res, device);
        Console.Out.WriteLine(ResourceReport.Format(rows, loaded.Top.Name, deviceName));

        bool over = ResourceReport.AnyOverBudget(rows);
        if (over && !p.Has("quiet", "q"))
            Console.Error.WriteLine("[fit] design EXCEEDS the selected device budget");
        return Task.FromResult(over ? 1 : 0);
    }
}
