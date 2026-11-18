using Interpreter.Services;
using Kmila.Frontend;
using Kmila.Shared.Services.Api;

namespace Kmila.CLI.Simulation;

/// <summary>
/// Loads a design to the point where its <see cref="Entity"/> objects are
/// available, without running it. Used by the analysis commands
/// (<c>synth</c>, <c>flow</c>, <c>fit</c>). Stages the sources in a throw-away
/// sandbox, delegates scan → elaborate → parse to the HDL front-end, and
/// captures the engine's Console chatter so command stdout stays clean.
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
    /// Stages the design, loads it through the front-end and returns the
    /// entities. Throws on a parse failure or when no entity with an
    /// architecture is found.
    /// </summary>
    /// <param name="topFile">Primary source file (absolute path).</param>
    /// <param name="extraFiles">Additional sources.</param>
    /// <param name="entry">Preferred top entity name (defaults to the top file stem).</param>
    /// <param name="elaborate">Inline component instances. The schematic view passes
    /// false so the top's own architecture is synthesised with instances kept as
    /// black boxes — matching what the GUI schematic shows.</param>
    public static Loaded Load(string topFile, IReadOnlyList<string> extraFiles, string? entry,
        bool elaborate = true)
    {
        if (!File.Exists(topFile))
            throw new FileNotFoundException($"top file not found: {topFile}");

        var store = new SandboxProjectStore();
        var project = store.Create("cli-load", retain: false);
        var originalOut = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            var staged = SourceStaging.Stage(project, topFile, extraFiles, entry);

            var design = Frontends.ForPath(topFile).LoadDesign(new DesignRequest
            {
                Root = project.Path,
                Files = staged.Files.Select(n => Path.Combine(project.Path, n)).ToList(),
                Entry = staged.Entry,
                EntryIsExplicit = staged.EntryIsExplicit,
                Elaborate = elaborate,
            });
            design.ThrowIfFailed();
            if (design.Entities.Count == 0)
                throw new InvalidOperationException("parser produced no entities (check the source syntax).");
            var top = design.Top
                ?? throw new InvalidOperationException("no entity with an architecture found.");

            return new Loaded { Top = top, All = design.Entities, Console = captured.ToString() };
        }
        finally
        {
            Console.SetOut(originalOut);
            store.Delete(project.Id);
        }
    }
}
