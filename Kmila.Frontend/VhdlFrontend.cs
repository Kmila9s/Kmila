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

    protected override ParsedUnit ParseCore(string source, CancellationToken cancellationToken)
    {
        var parser = Debugger.Repositories.Parser.FromSource(source);
        ExceptionDispatchInfo? error = null;
        try { parser.ParseFile(cancellationToken); }
        catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
        return new ParsedUnit(source, parser.Entities, parser.Diagnostics, error);
    }

    public override string? ElaborateSource(string root, string entry) =>
        SourceElaborator.ElaborateOrNull(root, entry);

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
