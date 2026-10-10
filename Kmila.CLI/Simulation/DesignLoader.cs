using Interpreter.Services;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Parses + elaborates a VHDL design to the point where its
/// <see cref="Entity"/> objects are available — the shared front half of the
/// simulation pipeline, without running. Used by the analysis commands
/// (<c>synth</c>, <c>flow</c>, <c>fit</c>) that need the parsed entity but no
/// waveform. Mirrors the scan→elaborate→parse steps in
/// <see cref="KmilaSimulationService"/> and captures the engine's Console
/// chatter so command stdout stays clean.
/// </summary>
public static class DesignLoader
{
    /// <summary>The parsed entities plus the one chosen as the top.</summary>
    public sealed class Loaded
    {
        /// <summary>The entity selected as the top (matches <c>entry</c> when given, else the first with an architecture).</summary>
        public required Entity Top { get; init; }
        /// <summary>Every entity parsed from the (elaborated) source.</summary>
        public required IReadOnlyList<Entity> All { get; init; }
        /// <summary>Captured engine Console output during parse/elaborate.</summary>
        public string Console { get; init; } = string.Empty;
    }

    /// <summary>
    /// Stages the design into a temp project, scans + elaborates + parses it,
    /// and returns the entities. Throws on a parse failure or when no entity
    /// with an architecture is found.
    /// </summary>
    /// <param name="topFile">Primary VHDL file (absolute path).</param>
    /// <param name="extraFiles">Additional sources.</param>
    /// <param name="entry">Preferred top entity name (defaults to the top file stem).</param>
    public static Loaded Load(string topFile, IReadOnlyList<string> extraFiles, string? entry)
    {
        if (!File.Exists(topFile))
            throw new FileNotFoundException($"top file not found: {topFile}");

        var dir = Path.Combine(Path.GetTempPath(), "kmila-cli-load", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var originalOut = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            void Stage(string src)
            {
                if (!File.Exists(src)) throw new FileNotFoundException($"source file not found: {src}");
                File.Copy(src, Path.Combine(dir, Path.GetFileName(src)), overwrite: true);
            }
            Stage(topFile);
            foreach (var f in extraFiles) Stage(f);

            var topName = Path.GetFileName(topFile);
            var entryName = string.IsNullOrWhiteSpace(entry)
                ? Path.GetFileNameWithoutExtension(topName)
                : entry!;

            // Reset process-global registries, then scan every source.
            EntityRegistry.Instance.Clear();
            PackageRegistry.Instance.Clear();
            FunctionRegistry.Instance.Clear();
            global::Parser.Services.ParseProfile.Reset();

            string? vhdlPath = null;
            foreach (var f in Directory.EnumerateFiles(dir))
            {
                if (!(f.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase) ||
                      f.EndsWith(".vhd", StringComparison.OrdinalIgnoreCase)))
                    continue;
                vhdlPath ??= f;
                if (string.Equals(Path.GetFileNameWithoutExtension(f), entryName, StringComparison.OrdinalIgnoreCase))
                    vhdlPath = f;
                try { EntityRegistry.Instance.ScanFile(f); } catch { }
                try { PackageSubtypeScanner.ScanFile(f); } catch { }
            }
            if (vhdlPath == null) throw new InvalidOperationException("no .vhdl/.vhd source found.");

            // Elaborate (inline components) when possible.
            try
            {
                var flat = SourceElaborator.ElaborateOrNull(dir, entryName);
                if (flat != null)
                {
                    var elaborated = Path.Combine(dir, $"_elaborated_{entryName}.vhd");
                    File.WriteAllText(elaborated, flat);
                    vhdlPath = elaborated;
                }
            }
            catch { /* elaboration is best-effort */ }

            var parser = new Debugger.Repositories.Parser(vhdlPath);
            parser.ParseFile();
            if (parser.Entities.Count == 0)
                throw new InvalidOperationException("parser produced no entities (check the VHDL syntax).");

            var top = parser.Entities
                          .FirstOrDefault(e => string.Equals(e.Name, entryName, StringComparison.OrdinalIgnoreCase) && e.Architecture != null)
                      ?? parser.Entities.FirstOrDefault(e => e.Architecture != null);
            if (top == null)
                throw new InvalidOperationException("no entity with an architecture found.");

            return new Loaded { Top = top, All = parser.Entities, Console = captured.ToString() };
        }
        finally
        {
            Console.SetOut(originalOut);
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
