using System.Runtime.ExceptionServices;
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

    public override Task<IReadOnlyList<LibraryCompiler.Repositories.CompiledLibrary>?> PrepareLibrariesAsync(
        LibraryCompiler.Services.LibraryCompiler? compiler, IReadOnlyList<string> sources,
        CancellationToken cancellationToken = default) =>
        VhdlLibraryPreparer.PrepareAsync(compiler, sources, cancellationToken);
}
