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

    /// <summary>
    /// Parses one text; <paramref name="companions"/> are the project's other
    /// sources, from which design units the text needs but does not hold are taken
    /// (an architecture's entity, an entity's architecture). Called with the
    /// engine-state gate held.
    /// </summary>
    protected abstract ParsedUnit ParseCore(string source, IReadOnlyList<string> companions, CancellationToken cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Text-only (no registry access). Languages without an elaborator keep this default.</remarks>
    public virtual string? ElaborateSource(IReadOnlyList<string> files, string entry) => null;

    /// <inheritdoc/>
    public virtual string? ResolveEntityName(IReadOnlyList<string> files, string entry) => entry;

    /// <summary>The diagnostic for a top unit the design does not have (language wording).</summary>
    protected virtual string MissingTopMessage(string entry) => $"cannot find top unit \"{entry}\"";

    /// <summary>The diagnostic for a top unit declared without a body (language wording).</summary>
    protected virtual string TopWithoutBodyMessage(string name) => $"top unit \"{name}\" has no body";

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
        return ParseCore(source, [], cancellationToken);
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
        return SourceOrder.Sort(root, Directory.EnumerateFiles(root, "*.*", option)
                        .Where(f => Language.Owns(f) && !HdlLanguages.IsGeneratedArtifact(f)));
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
        if (request.Elaborate && !string.IsNullOrEmpty(request.Entry))
        {
            try
            {
                text = ElaborateSource(sources, request.Entry!);
                if (text != null)
                {
                    elaborated = true;
                    log.Add($"[elab] inlined components for {request.Entry}");
                }
            }
            catch (Parser.Models.ParserException ex)
            {
                // An unresolvable unit or binding is the design's error, as in GHDL.
                return new DesignResult(Language, sources, target, null, null, false, log, ex);
            }
            catch (Exception ex)
            {
                log.Add($"[elab] elaboration failed, falling back to original source: {ex.Message}");
            }
        }

        var unit = ParseCore(text ?? File.ReadAllText(target), elaborated ? [] : sources, cancellationToken);
        string? topName = string.IsNullOrEmpty(request.Entry) ? null : ResolveEntityName(sources, request.Entry!) ?? request.Entry;
        var top = PickTop(unit.Entities, topName);
        var entryError = CheckEntry(request, topName, top, unit, log);
        return new DesignResult(Language, sources, target, unit, top, elaborated, log, entryError);
    }

    /// <summary>
    /// G23: a top unit that the design does not have (or that has no
    /// architecture) is an error when the entry was named explicitly — GHDL:
    /// <c>cannot find entity or configuration</c> — and a warning when it was
    /// inferred, in which case the first entity with an architecture is used.
    /// </summary>
    private Exception? CheckEntry(DesignRequest request, string? topName, Entity? top, ParsedUnit unit, List<string> log)
    {
        if (topName == null || unit.Error != null || (top != null && top.Name.Equals(topName, StringComparison.OrdinalIgnoreCase)))
            return null;
        bool declared = unit.Entities.Any(e => e.Name.Equals(topName, StringComparison.OrdinalIgnoreCase));
        string message = declared ? TopWithoutBodyMessage(topName) : MissingTopMessage(request.Entry!);
        if (request.EntryIsExplicit) return new InvalidOperationException(message);
        string fallback = top == null ? "" : $"; using \"{top.Name}\"";
        log.Add($"[top] {message}{fallback}");
        unit.Diagnostics.Add(new Parser.Models.Diagnostic
        {
            Severity = Parser.Models.DiagnosticSeverity.Warning,
            Code = declared ? "ENTRY-NO-BODY" : "ENTRY-NOT-FOUND",
            Message = message + fallback,
            Line = 1,
        });
        return null;
    }

    public IReadOnlyList<(string File, ParsedUnit Unit)> AnalyzeEach(
        IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        using var _ = EngineState.Enter(cancellationToken);
        EngineState.Reset();
        var existing = files.Where(File.Exists).ToList();
        ScanCore(existing, null);
        return existing.Select(f => (f, ParseCore(File.ReadAllText(f), existing, cancellationToken))).ToList();
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
