using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Interpreter.Services;

namespace Kmila.Frontend;

/// <summary>VHDL front-end over the existing engine (Debugger parser + Interpreter synthesis).</summary>
public sealed class VhdlFrontend : HdlFrontendBase
{
    public override HdlLanguage Language => HdlLanguages.Vhdl;

    protected override void ScanCore(IEnumerable<string> files, Action<string>? log)
    {
        foreach (var f in files)
        {
            var name = Path.GetFileName(f);
            try { EntityRegistry.Instance.ScanFile(f); }
            catch (Exception ex) { log?.Invoke($"[scan] {name}: {ex.Message}"); }
            try { PackageSubtypeScanner.ScanFile(f); }
            catch (Exception ex) { log?.Invoke($"[subscan] {name}: {ex.Message}"); }
            try { PackageRegistry.Instance.RegisterDeclaredNamesFromSource(File.ReadAllText(f)); }
            catch { /* best-effort: names only feed the undeclared-identifier check */ }
        }
    }

    protected override ParsedUnit ParseCore(string source, IReadOnlyList<string> companions, CancellationToken cancellationToken)
    {
        var parser = Debugger.Repositories.Parser.FromSource(source);
        ExceptionDispatchInfo? error = null;
        try
        {
            // G21: units of other files this text needs — the entity of an
            // architecture declared elsewhere is parsed first and seeded, and the
            // selected architecture of an entity declared here without one is
            // parsed against it afterwards (line numbers stay this text's own).
            var (entities, architectures) = companions.Count > 0
                ? SourceElaborator.CompanionUnits(companions, source)
                : ([], []);
            foreach (var text in entities) parser.Entities.AddRange(ParseUnit(text, [], cancellationToken));
            parser.ParseFile(cancellationToken);
            foreach (var text in architectures) ParseUnit(text, parser.Entities, cancellationToken);
        }
        catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
        return new ParsedUnit(source, parser.Entities, parser.Diagnostics, error);
    }

    /// <summary>Parses a companion unit's text with <paramref name="seed"/> entities visible; returns its entities.</summary>
    private static List<Entity> ParseUnit(string text, List<Entity> seed, CancellationToken cancellationToken)
    {
        var unit = Debugger.Repositories.Parser.FromSource(text);
        unit.Entities.AddRange(seed);
        unit.ParseFile(cancellationToken);
        return unit.Entities;
    }

    public override string? ElaborateSource(IReadOnlyList<string> files, string entry) =>
        SourceElaborator.ElaborateOrNull(files, entry);

    public override string? ResolveEntityName(IReadOnlyList<string> files, string entry) =>
        SourceElaborator.ResolveEntityName(files, entry);

    // GHDL's wording for `ghdl -e <unit>`.
    protected override string MissingTopMessage(string entry) => $"cannot find entity or configuration \"{entry}\"";
    protected override string TopWithoutBodyMessage(string name) => $"entity \"{name}\" has no architecture";

    private static readonly Regex PackageDeclRe =
        new(@"(?im)^\s*package\s+([a-z_][a-z0-9_]*)\s+is\b", RegexOptions.Compiled);

    /// <summary>Every <c>package X is</c> name; <c>package body X is</c> is skipped.</summary>
    public override IReadOnlyList<string> DeclaredLibraryUnits(string source) =>
        PackageDeclRe.Matches(source ?? string.Empty)
            .Select(m => m.Groups[1].Value)
            .Where(n => !string.Equals(n, "body", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// From <c>package &lt;name&gt; is</c> through its closing <c>end [&lt;name&gt;];</c>
    /// (VHDL allows the trailing identifier or a bare <c>end;</c>).
    /// </summary>
    public override string? LibraryUnitText(string source, string name)
    {
        var escaped = Regex.Escape(name);
        var start = Regex.Match(source, $@"(?im)^\s*package\s+{escaped}\s+is\b");
        if (!start.Success) return null;
        var afterStart = source.Substring(start.Index);
        var end = Regex.Match(afterStart, $@"(?im)^\s*end(?:\s+{escaped})?\s*;");
        return end.Success ? afterStart.Substring(0, end.Index + end.Length) : null;
    }

    public override Task<IReadOnlyList<LibraryCompiler.Repositories.CompiledLibrary>?> PrepareLibrariesAsync(
        LibraryCompiler.Services.LibraryCompiler? compiler, IReadOnlyList<string> sources,
        CancellationToken cancellationToken = default) =>
        VhdlLibraryPreparer.PrepareAsync(compiler, sources, cancellationToken);
}
