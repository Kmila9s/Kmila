using System.Text.RegularExpressions;
using Interpreter.Models;
using Interpreter.Services;
using LibraryCompiler.Repositories;
using LibraryCompiler.Services;
using Parser.Models;

namespace Kmila.Frontend;

/// <summary>
/// Resolves the VHDL <c>library</c>/<c>use</c> clauses of a design through the
/// <see cref="LibraryCompiler.Services.LibraryCompiler"/> and seeds the compiled
/// symbols (functions/procedures into <see cref="FunctionRegistry"/>, types into
/// <see cref="Parser.Services.UserTypeRegistry"/>) so the parser recognises them.
/// </summary>
internal static class VhdlLibraryPreparer
{
    /// <summary><c>use library.package…</c> — captures library (1) and package (2).</summary>
    private static readonly Regex UseClauseRe = new(
        @"(?im)^\s*use\s+([a-z_][a-z0-9_]*)\s*\.\s*([a-z_][a-z0-9_]*)",
        RegexOptions.Compiled);

    /// <summary><c>library X;</c> / <c>library X, Y;</c> — captures the name list (1).</summary>
    private static readonly Regex LibraryDeclRe = new(
        @"(?im)^\s*library\s+([a-z_][a-z0-9_]*(?:\s*,\s*[a-z_][a-z0-9_]*)*)\s*;",
        RegexOptions.Compiled);

    /// <summary>
    /// Compiles every library the sources import. Returns null when no library
    /// needs compiling (or no compiler is available).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A <c>use</c> clause lacks its <c>library</c> declaration, or names a
    /// library the resolver doesn't know. The build halts deliberately: we
    /// can't guess what the missing library would have contributed.
    /// </exception>
    public static async Task<IReadOnlyList<CompiledLibrary>?> PrepareAsync(
        LibraryCompiler.Services.LibraryCompiler? compiler, IEnumerable<string> sources, CancellationToken ct)
    {
        if (compiler is null) return null;

        var requestedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // STD / WORK / IEEE are implicit in VHDL, no declaration needed.
        var declaredViaLibraryClause = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "STD", "WORK", "IEEE" };
        var useClauseLibraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in sources)
        {
            string source;
            try { source = File.ReadAllText(path); }
            catch { continue; }   // unreadable sibling — the parser surfaces it later

            source = VhdlText.StripLineComments(source);

            foreach (Match m in LibraryDeclRe.Matches(source))
                foreach (var raw in m.Groups[1].Value.Split(','))
                {
                    var name = raw.Trim();
                    if (name.Length > 0) declaredViaLibraryClause.Add(name);
                }

            foreach (Match m in UseClauseRe.Matches(source))
            {
                var lib = m.Groups[1].Value;
                var pkg = m.Groups[2].Value;
                useClauseLibraries.Add(lib);

                // STD is intrinsic; WORK is the project itself (parsed directly).
                if (string.Equals(lib, "STD", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(lib, "WORK", StringComparison.OrdinalIgnoreCase)) continue;

                // IEEE resolver entries are keyed by package (std_logic_1164,
                // numeric_std, …); a user plugin's file stem is both its
                // library and its package entry.
                requestedPackages.Add(string.Equals(lib, "IEEE", StringComparison.OrdinalIgnoreCase) ? pkg : lib);
            }
        }

        var missingLibraryDecl = useClauseLibraries
            .Where(l => !declaredViaLibraryClause.Contains(l))
            .OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingLibraryDecl.Count > 0)
        {
            var first = missingLibraryDecl[0];
            throw new InvalidOperationException(
                $"`use {first}.…` is missing its `library {first};` declaration. " +
                "Add the declaration before the `use` clause, or remove the " +
                "`use` clause if it isn't needed.");
        }

        if (requestedPackages.Count == 0) return null;

        var unknown = requestedPackages
            .Where(name => compiler.Resolver.Resolve(name) is null)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknown.Count > 0)
        {
            var first = unknown[0];
            throw new InvalidOperationException(
                $"Library '{first}' not found. Import it under " +
                "Settings → Libraries before building, or remove the " +
                $"corresponding `use {first}.…` clause.");
        }

        var compiled = await compiler
            .CompileAsync(requestedPackages, BuildExecutionMode.Linear, ct)
            .ConfigureAwait(false);

        // Re-seed the type registry from this build's libraries only, so a
        // previous project's type names don't leak into "did you mean" hints.
        global::Parser.Services.UserTypeRegistry.Instance.Clear();

        foreach (var lib in compiled)
        foreach (var sym in lib.Functions)
        {
            switch (sym.Kind)
            {
                case LibrarySymbolKind.Function:
                case LibrarySymbolKind.Procedure:
                    FunctionRegistry.Instance.Register(new FunctionDefinition
                    {
                        Name        = sym.Name,
                        PackageName = lib.LibraryName,
                        IsPure      = sym.Kind == LibrarySymbolKind.Function
                    });
                    break;
                case LibrarySymbolKind.Type:
                    global::Parser.Services.UserTypeRegistry.Instance.Register(sym.Name);
                    break;
            }
        }
        return compiled;
    }
}
