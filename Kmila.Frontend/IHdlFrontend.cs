using System.Runtime.ExceptionServices;
using Interpreter.Services;
using Parser.Models;

namespace Kmila.Frontend;

/// <summary>
/// Turns HDL source files into the engine's executable design model.
/// One implementation per <see cref="HdlLanguage"/>.
/// </summary>
public interface IHdlFrontend
{
    /// <summary>The language this front-end handles.</summary>
    HdlLanguage Language { get; }

    /// <summary>
    /// Full design load: reset engine state, scan every source, optionally
    /// elaborate the hierarchy, parse, and pick the top entity.
    /// Never throws for design errors — inspect <see cref="DesignResult.Error"/>.
    /// </summary>
    DesignResult LoadDesign(DesignRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Names of the library units (VHDL packages) declared in
    /// <paramref name="source"/>, in source order and without duplicates.
    /// Bodies are not declarations. Empty for languages without library units.
    /// </summary>
    IReadOnlyList<string> DeclaredLibraryUnits(string source);

    /// <summary>
    /// The text of library unit <paramref name="name"/> inside
    /// <paramref name="source"/> (declaration through its closing statement),
    /// or null when the unit is not found or its end cannot be located.
    /// </summary>
    string? LibraryUnitText(string source, string name);

    /// <summary>Lists this language's user sources under <paramref name="root"/>.</summary>
    IReadOnlyList<string> DiscoverSources(string root, bool recursive = true);

    /// <summary>
    /// Registers declarations from <paramref name="files"/> (entities,
    /// package subtypes, declared names) into the engine registries so
    /// cross-file references resolve. Best-effort per file.
    /// </summary>
    void ScanSources(IEnumerable<string> files, Action<string>? log = null);

    /// <summary>
    /// Parses one source text. Does not reset or scan the registries (the
    /// engine's synthesis still records what it parses, e.g. port-map edges).
    /// </summary>
    ParsedUnit ParseSource(string source, CancellationToken cancellationToken = default);

    /// <summary>Reads and parses one file; an I/O failure is reported as the unit's error.</summary>
    ParsedUnit ParseFile(string path, CancellationToken cancellationToken = default);

    /// <summary>The entry entity when it has an architecture, else the first entity that has one
    /// (identifiers compared with this language's case rule).</summary>
    Interpreter.Services.Entity? PickTop(IEnumerable<Interpreter.Services.Entity> entities, string? entry);

    /// <summary>
    /// Flattens the hierarchy rooted at unit <paramref name="entry"/> of the design
    /// in <paramref name="files"/> (analysis order) into one source text, or null
    /// when the top's own file can be parsed as is (or the language has no elaborator).
    /// </summary>
    /// <exception cref="Parser.Models.ParserException">A named unit or binding cannot be resolved.</exception>
    string? ElaborateSource(IReadOnlyList<string> files, string entry);

    /// <summary>
    /// The entity a top unit name designates in <paramref name="files"/> — the
    /// entity itself, or the one a configuration configures; null when the
    /// design has no such unit.
    /// </summary>
    string? ResolveEntityName(IReadOnlyList<string> files, string entry);

    /// <summary>
    /// Compiles and seeds the libraries <paramref name="sources"/> import so the
    /// parser recognises their symbols. Library symbols survive
    /// <see cref="EngineState.Reset"/>, so call this before <see cref="LoadDesign"/>.
    /// Returns the compiled libraries, or null when nothing needed compiling
    /// (languages without libraries always return null).
    /// </summary>
    /// <exception cref="InvalidOperationException">A referenced library is unknown or undeclared.</exception>
    Task<IReadOnlyList<LibraryCompiler.Repositories.CompiledLibrary>?> PrepareLibrariesAsync(
        LibraryCompiler.Services.LibraryCompiler? compiler, IReadOnlyList<string> sources,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets engine state, scans all <paramref name="files"/> so cross-file
    /// references resolve, then parses each file on its own (e.g. for linting).
    /// </summary>
    IReadOnlyList<(string File, ParsedUnit Unit)> AnalyzeEach(
        IReadOnlyList<string> files, CancellationToken cancellationToken = default);
}

/// <summary>What to load.</summary>
public sealed record DesignRequest
{
    /// <summary>Project directory. Sources are discovered here unless <see cref="Files"/> is given.</summary>
    public string? Root { get; init; }

    /// <summary>Explicit source list (overrides discovery). Order is significant.</summary>
    public IReadOnlyList<string>? Files { get; init; }

    /// <summary>Top unit (entity, or a configuration of one). Also selects the parse target by file stem.</summary>
    public string? Entry { get; init; }

    /// <summary>
    /// True when the caller named <see cref="Entry"/> explicitly (an <c>--entity</c>
    /// option, an API <c>entryPoint</c>): a unit that does not exist is then an
    /// error, as for GHDL. False when it was inferred (e.g. from the top file's
    /// name): the first entity with an architecture is used, with a warning.
    /// </summary>
    public bool EntryIsExplicit { get; init; } = true;

    /// <summary>Explicit file to parse; otherwise the file named after <see cref="Entry"/>, else the first source.</summary>
    public string? ParseTarget { get; init; }

    /// <summary>Inline component instances into one flat design (requires <see cref="Entry"/>).</summary>
    public bool Elaborate { get; init; }

    /// <summary>Recurse into sub-directories when discovering sources.</summary>
    public bool Recursive { get; init; } = true;
}

/// <summary>Result of parsing one source text.</summary>
public sealed class ParsedUnit
{
    internal ParsedUnit(string source, List<Entity> entities, List<Diagnostic> diagnostics, ExceptionDispatchInfo? error)
    {
        Source = source; Entities = entities; Diagnostics = diagnostics; _error = error;
        // An elaborated text locates its errors in the files the user wrote (G82).
        LineMap = Interpreter.Services.ElaborationLineMap.Parse(source);
        if (LineMap is null) return;
        if (error?.SourceException is Parser.Models.ParserException parseError)
            _error = ExceptionDispatchInfo.Capture(LineMap.Relocate(parseError));
        for (int i = 0; i < diagnostics.Count; i++) diagnostics[i] = LineMap.Relocate(diagnostics[i]);
    }

    /// <summary>Where the lines of an elaborated text come from; null for a source as written.</summary>
    public Interpreter.Services.ElaborationLineMap? LineMap { get; }

    private readonly ExceptionDispatchInfo? _error;

    /// <summary>The text that was parsed.</summary>
    public string Source { get; }

    /// <summary>Every entity the parse produced (possibly partial when <see cref="Error"/> is set).</summary>
    public List<Entity> Entities { get; }

    /// <summary>Lint and parse diagnostics; populated even when the parse failed. Mutable so hosts can append.</summary>
    public List<Diagnostic> Diagnostics { get; }

    /// <summary>The parse failure, or null on success.</summary>
    public Exception? Error => _error?.SourceException;

    /// <summary>Rethrows the original parse exception (stack and type preserved) when the parse failed.</summary>
    public void ThrowIfFailed() => _error?.Throw();
}

/// <summary>Result of a full design load.</summary>
public sealed class DesignResult
{
    internal DesignResult(HdlLanguage language, IReadOnlyList<string> sources, string? sourcePath,
                          ParsedUnit? unit, Entity? top, bool elaborated, List<string> log, Exception? setupError)
    {
        Language = language; Sources = sources; SourcePath = sourcePath; Unit = unit;
        Top = top; Elaborated = elaborated; Log = log; _setupError = setupError;
        Diagnostics = unit?.Diagnostics ?? [];
    }

    private readonly Exception? _setupError;

    public HdlLanguage Language { get; }

    /// <summary>Every user source that was scanned.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>The file chosen as parse target (before elaboration).</summary>
    public string? SourcePath { get; }

    /// <summary>The parsed unit (flattened source when <see cref="Elaborated"/>).</summary>
    public ParsedUnit? Unit { get; }

    /// <summary>All parsed entities.</summary>
    public IReadOnlyList<Entity> Entities => Unit?.Entities ?? [];

    /// <summary>The top entity: <see cref="DesignRequest.Entry"/> with an architecture, else the first with one.</summary>
    public Entity? Top { get; }

    /// <summary>True when the parsed text is the elaborated (flattened) design.</summary>
    public bool Elaborated { get; }

    /// <summary>Scan/elaboration notes (<c>[scan]</c>, <c>[subscan]</c>, <c>[elab]</c>).</summary>
    public List<string> Log { get; }

    /// <summary>Diagnostics of the parsed unit. Mutable so hosts can append build errors.</summary>
    public List<Diagnostic> Diagnostics { get; }

    /// <summary>The text that was parsed (elaborated text when <see cref="Elaborated"/>).</summary>
    public string SourceText => Unit?.Source ?? string.Empty;

    /// <summary>The first failure: no source found, or the parse exception.</summary>
    public Exception? Error => _setupError ?? Unit?.Error;

    /// <summary>Rethrows <see cref="Error"/> unchanged.</summary>
    public void ThrowIfFailed()
    {
        if (_setupError != null) ExceptionDispatchInfo.Throw(_setupError);
        Unit?.ThrowIfFailed();
    }
}
