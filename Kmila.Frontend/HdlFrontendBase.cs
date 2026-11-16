using System.Runtime.ExceptionServices;
using Interpreter.Services;

namespace Kmila.Frontend;

/// <summary>
/// Language-neutral design-loading workflow. A language only supplies how to
/// scan declarations, parse one text, and (optionally) elaborate a hierarchy;
/// engine-state locking and reset, source discovery, target selection and top
/// picking are shared and enforced here.
/// </summary>
public abstract class HdlFrontendBase : IHdlFrontend
{
    public abstract HdlLanguage Language { get; }

    /// <summary>Registers declarations of <paramref name="files"/>. Called with the engine-state gate held.</summary>
    protected abstract void ScanCore(IEnumerable<string> files, Action<string>? log);

    /// <summary>Parses one text. Called with the engine-state gate held.</summary>
    protected abstract ParsedUnit ParseCore(string source, CancellationToken cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Text-only (no registry access). Languages without an elaborator keep this default.</remarks>
    public virtual string? ElaborateSource(string root, string entry) => null;

    /// <inheritdoc />
    public virtual IReadOnlyList<string> DeclaredLibraryUnits(string source) => [];

    /// <inheritdoc />
    public virtual string? LibraryUnitText(string source, string name) => null;

    public virtual Task<IReadOnlyList<LibraryCompiler.Repositories.CompiledLibrary>?> PrepareLibrariesAsync(
        LibraryCompiler.Services.LibraryCompiler? compiler, IReadOnlyList<string> sources,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LibraryCompiler.Repositories.CompiledLibrary>?>(null);

    public void ScanSources(IEnumerable<string> files, Action<string>? log = null)
    {
        using var _ = EngineState.Enter();
        ScanCore(files, log);
    }

    public ParsedUnit ParseSource(string source, CancellationToken cancellationToken = default)
    {
        using var _ = EngineState.Enter(cancellationToken);
        return ParseCore(source, cancellationToken);
    }

    public ParsedUnit ParseFile(string path, CancellationToken cancellationToken = default)
    {
        string text;
        try { text = File.ReadAllText(path); }
        catch (Exception ex) { return new ParsedUnit(string.Empty, [], [], ExceptionDispatchInfo.Capture(ex)); }
        return ParseSource(text, cancellationToken);
    }

    public IReadOnlyList<string> DiscoverSources(string root, bool recursive = true)
    {
        if (!Directory.Exists(root)) return [];
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(root, "*.*", option)
                        .Where(f => Language.Owns(f) && !HdlLanguages.IsGeneratedArtifact(f))
                        .ToList();
    }

    public DesignResult LoadDesign(DesignRequest request, CancellationToken cancellationToken = default)
    {
        var log = new List<string>();
        using var _ = EngineState.Enter(cancellationToken);
        EngineState.Reset();

        var sources = request.Files?.Where(f => Language.Owns(f) && File.Exists(f)).ToList()
                      ?? (request.Root != null ? DiscoverSources(request.Root, request.Recursive) : []);

        ScanCore(sources, log.Add);

        var target = request.ParseTarget
                     ?? sources.FirstOrDefault(f => !string.IsNullOrEmpty(request.Entry) &&
                            string.Equals(Path.GetFileNameWithoutExtension(f), request.Entry, IdentifierComparison))
                     ?? sources.FirstOrDefault();
        if (target == null)
            return new DesignResult(Language, sources, null, null, null, false, log,
                new InvalidOperationException($"No {Language.DisplayName} source ({string.Join("/", Language.Extensions)}) found."));

        string? text = null;
        bool elaborated = false;
        if (request.Elaborate && request.Root != null && !string.IsNullOrEmpty(request.Entry))
        {
            try
            {
                text = ElaborateSource(request.Root, request.Entry!);
                if (text != null)
                {
                    elaborated = true;
                    log.Add($"[elab] inlined components for {request.Entry}");
                }
            }
            catch (Exception ex)
            {
                log.Add($"[elab] elaboration failed, falling back to original source: {ex.Message}");
            }
        }

        var unit = ParseCore(text ?? File.ReadAllText(target), cancellationToken);
        return new DesignResult(Language, sources, target, unit, PickTop(unit.Entities, request.Entry),
                                elaborated, log, null);
    }

    public IReadOnlyList<(string File, ParsedUnit Unit)> AnalyzeEach(
        IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        using var _ = EngineState.Enter(cancellationToken);
        EngineState.Reset();
        var existing = files.Where(File.Exists).ToList();
        ScanCore(existing, null);
        return existing.Select(f => (f, ParseCore(File.ReadAllText(f), cancellationToken))).ToList();
    }

    public Entity? PickTop(IEnumerable<Entity> entities, string? entry) =>
        (!string.IsNullOrEmpty(entry)
            ? entities.FirstOrDefault(e => string.Equals(e.Name, entry, IdentifierComparison) && e.Architecture != null)
            : null)
        ?? entities.FirstOrDefault(e => e.Architecture != null);

    /// <summary>How identifiers (entity names, entry points) compare in this language.</summary>
    protected StringComparison IdentifierComparison =>
        Language.CaseSensitiveIdentifiers ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}
