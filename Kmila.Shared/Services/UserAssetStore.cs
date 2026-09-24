using System.Text.Json;
using LibraryCompiler.Repositories;
using LibraryCompiler.Services;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Owns the user's writable assets directory — extra FPGA device JSON files
/// and extra language JSON files that the student (or a third-party contributor)
/// drops in at runtime. Separate from the bundled assets under <c>wwwroot/</c>
/// so app updates can ship new built-in devices/languages without clobbering
/// anything the user has added.
/// </summary>
/// <remarks>
/// Directory layout (under the platform data root — <c>FileSystem.AppDataDirectory</c>
/// on MAUI, <c>ApplicationData/KmilaData</c> on the Web build):
/// <code>
///   UserAssets/
///     Devices/*.json         -- extra FPGA definitions (same schema as the bundled ones)
///     Dictionaries/*.json    -- extra language packs (same schema as en.json / es.json)
/// </code>
/// Designed for a future marketplace: all files are plain JSON with the same
/// shape as the bundled ones, so importing is copy-paste and exporting is
/// "take the file out of this folder."
/// </remarks>
public class UserAssetStore : IDisposable
{
    /// <summary>Name of the optional display-name key inside a language file.</summary>
    public const string LanguageDisplayNameKey = "__LANG_NAME__";

    private readonly string _root;
    private readonly ILogger _logger;

    // External-change watchers. One per asset directory; we don't hand them
    // out to callers — they exist purely to fire OnChange when something
    // outside the in-app Settings flow mutates the directory (cloud sync,
    // Finder, a different tool, …). Debounced via _watcherDebounce so a
    // batch of writes (text editors often save+rename) collapses into one
    // notification.
    private readonly List<FileSystemWatcher> _watchers = new();
    private System.Threading.Timer? _watcherDebounce;
    private const int WatcherDebounceMs = 250;

    public UserAssetStore(ILogger<UserAssetStore>? logger = null)
    {
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        _root = ResolveRoot();
        EnsureDirs();
        StartWatchers();
    }

    /// <summary>Fires when a file is imported or deleted so UIs can refresh.</summary>
    public event Action? OnChange;

    public string UserDevicesDir       => Path.Combine(_root, "UserAssets", "Devices");
    public string UserDictionariesDir  => Path.Combine(_root, "UserAssets", "Dictionaries");
    /// <summary>
    /// User-imported VHDL library packs. Same .vhd / .vhdl files the
    /// student would feed to a real toolchain — file-stem becomes the
    /// library name surfaced in <c>use my_pkg.x.all</c>.
    /// </summary>
    public string UserLibrariesDir     => Path.Combine(_root, "UserAssets", "Libraries");
    /// <summary>Persistent cache for compiled library artifacts, written by <c>FileSystemBlobStore</c>.</summary>
    public string LibraryCacheDir      => Path.Combine(_root, "UserAssets", "LibraryCache");

    private void EnsureDirs()
    {
        try
        {
            Directory.CreateDirectory(UserDevicesDir);
            Directory.CreateDirectory(UserDictionariesDir);
            Directory.CreateDirectory(UserLibrariesDir);
            Directory.CreateDirectory(LibraryCacheDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Failed to create user asset directories under {Root}", _root);
        }
    }

    // ------------------------------------------------------------------
    // FPGA devices
    // ------------------------------------------------------------------

    /// <summary>All user-supplied FPGA JSON file paths (alphabetical, full paths).</summary>
    public List<string> ListUserFpgaPaths()
    {
        try
        {
            if (!Directory.Exists(UserDevicesDir)) return new();
            return Directory.GetFiles(UserDevicesDir, "*.json", SearchOption.TopDirectoryOnly)
                            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Failed to list user FPGA files");
            return new();
        }
    }

    /// <summary>
    /// Imports a JSON stream as a new FPGA device file. Rejects files that
    /// can't be parsed as a minimal FPGA object (needs at least a
    /// <c>model</c> field). Returns the saved file path on success.
    /// </summary>
    public async Task<string?> ImportFpgaAsync(Stream source, string originalFileName)
    {
        var safeName = SanitizeFileName(originalFileName, ".json");
        if (string.IsNullOrEmpty(safeName)) return null;

        var target = Path.Combine(UserDevicesDir, safeName);
        try
        {
            using (var ms = new MemoryStream())
            {
                await source.CopyToAsync(ms);
                ms.Position = 0;

                // Cheap validation: confirm it's JSON with a "model" field.
                try
                {
                    using var doc = JsonDocument.Parse(ms);
                    if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                        !doc.RootElement.TryGetProperty("model", out _))
                    {
                        _logger.LogWarning("[UserAssetStore] Rejected FPGA import: no 'model' field in {Name}", safeName);
                        return null;
                    }
                }
                catch (JsonException jex)
                {
                    _logger.LogWarning(jex, "[UserAssetStore] Rejected FPGA import — invalid JSON: {Name}", safeName);
                    return null;
                }

                ms.Position = 0;
                using var file = File.Create(target);
                await ms.CopyToAsync(file);
            }
            OnChange?.Invoke();
            _logger.LogInformation("[UserAssetStore] Imported FPGA {Path}", target);
            return target;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Import failed for {Name}", safeName);
            return null;
        }
    }

    /// <summary>Deletes a user FPGA file by base name (ignores directory component).</summary>
    public bool DeleteFpga(string fileName)
    {
        try
        {
            var name = Path.GetFileName(fileName);
            var path = Path.Combine(UserDevicesDir, name);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            OnChange?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Delete FPGA failed: {Name}", fileName);
            return false;
        }
    }

    // ------------------------------------------------------------------
    // Languages
    // ------------------------------------------------------------------

    /// <summary>
    /// User-supplied language pack file paths. File name (minus .json) is the
    /// language code.
    /// </summary>
    public List<string> ListUserLanguagePaths()
    {
        try
        {
            if (!Directory.Exists(UserDictionariesDir)) return new();
            return Directory.GetFiles(UserDictionariesDir, "*.json", SearchOption.TopDirectoryOnly)
                            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Failed to list user language files");
            return new();
        }
    }

    /// <summary>Imports a language JSON. File name becomes the language code.</summary>
    public async Task<string?> ImportLanguageAsync(Stream source, string originalFileName)
    {
        var safeName = SanitizeFileName(originalFileName, ".json");
        if (string.IsNullOrEmpty(safeName)) return null;

        var target = Path.Combine(UserDictionariesDir, safeName);
        try
        {
            using var ms = new MemoryStream();
            await source.CopyToAsync(ms);
            ms.Position = 0;
            try
            {
                using var doc = JsonDocument.Parse(ms);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("[UserAssetStore] Rejected language import — root is not an object: {Name}", safeName);
                    return null;
                }
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex, "[UserAssetStore] Rejected language import — invalid JSON: {Name}", safeName);
                return null;
            }

            ms.Position = 0;
            using var file = File.Create(target);
            await ms.CopyToAsync(file);

            OnChange?.Invoke();
            _logger.LogInformation("[UserAssetStore] Imported language {Path}", target);
            return target;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Language import failed for {Name}", safeName);
            return null;
        }
    }

    /// <summary>Deletes a user language pack by base name.</summary>
    public bool DeleteLanguage(string fileName)
    {
        try
        {
            var name = Path.GetFileName(fileName);
            var path = Path.Combine(UserDictionariesDir, name);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            OnChange?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Delete language failed: {Name}", fileName);
            return false;
        }
    }

    /// <summary>
    /// Reads the optional <c>__LANG_NAME__</c> entry from a user language file
    /// so the picker can show "Klingon" instead of "klg". Falls back to the
    /// uppercased file-stem.
    /// </summary>
    public static string ReadLanguageDisplayName(string path)
    {
        var fallback = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty(LanguageDisplayNameKey, out var name) &&
                name.ValueKind == JsonValueKind.String)
            {
                var s = name.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s!;
            }
        }
        catch { /* best-effort */ }
        return fallback;
    }

    // ------------------------------------------------------------------
    // VHDL libraries (plugins)
    // ------------------------------------------------------------------

    /// <summary>
    /// Absolute paths of every <c>.vhd</c> / <c>.vhdl</c> file dropped
    /// under <see cref="UserLibrariesDir"/>. Sub-directories are NOT
    /// recursed: one library per top-level file is the v1 convention.
    /// </summary>
    public List<string> ListUserLibraryPaths()
    {
        try
        {
            if (!Directory.Exists(UserLibrariesDir)) return new();
            return Directory.EnumerateFiles(UserLibrariesDir, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(p =>
                                p.EndsWith(".vhd",  StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith(".vhdl", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Failed to list user library files");
            return new();
        }
    }

    /// <summary>
    /// Adapts <see cref="ListUserLibraryPaths"/> into the LibraryCompiler's
    /// input shape. One physical <c>.vhd</c> file can declare multiple
    /// <c>package X is … end;</c> blocks; each becomes its own resolvable
    /// library. We encode the owning file plus the package selector into
    /// <see cref="LibrarySource.SourceKey"/> as <c>{abs-path}#package={X}</c>;
    /// the host's <c>OpenLibrarySource</c> callback parses the fragment and
    /// hands back just the bytes inside that package block so each compiled
    /// artifact has a tight, content-hashable byte range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a file contains no <c>package</c> declaration, we fall back to
    /// the v0 convention (one library per file, library name = file stem,
    /// SourceKey = absolute path with no fragment). Authors who treat each
    /// file as one package don't need to do anything different.
    /// </para>
    /// <para>
    /// Dependencies between user libraries are not declared here — the
    /// orchestrator's resolver tolerates dangling deps and the user can
    /// always split declarations across files when ordering matters.
    /// </para>
    /// </remarks>
    public IReadOnlyList<LibrarySource> ListUserLibrarySources()
    {
        var paths = ListUserLibraryPaths();
        var sources = new List<LibrarySource>();
        foreach (var p in paths)
        {
            string text;
            try { text = File.ReadAllText(p); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserAssetStore] Failed to read user library {Path}", p);
                continue;
            }

            var packageNames = ExtractPackageNames(text).ToList();
            if (packageNames.Count == 0)
            {
                // No `package X is` declaration found — treat the entire
                // file as a single anonymous library named after its stem.
                var stem = Path.GetFileNameWithoutExtension(p);
                if (!string.IsNullOrWhiteSpace(stem))
                {
                    sources.Add(new LibrarySource(
                        LibraryName: stem,
                        Vendor:      "WORK",
                        SourceKey:   p,
                        DependsOn:   Array.Empty<string>()));
                }
                continue;
            }

            foreach (var pkg in packageNames)
            {
                sources.Add(new LibrarySource(
                    LibraryName: pkg,
                    Vendor:      "WORK",
                    SourceKey:   $"{p}#package={pkg}",
                    DependsOn:   Array.Empty<string>()));
            }
        }
        return sources;
    }

    /// <summary>
    /// Imports a <c>.vhd</c> / <c>.vhdl</c> stream as a new user library
    /// pack. Validates that the file declares at least one <c>entity</c>,
    /// <c>package</c>, or top-level <c>function</c> / <c>procedure</c> so a
    /// random text file won't be silently accepted. Returns the saved
    /// path on success.
    /// </summary>
    public async Task<string?> ImportLibraryAsync(Stream source, string originalFileName)
    {
        var safeName = SanitizeLibraryFileName(originalFileName);
        if (string.IsNullOrEmpty(safeName)) return null;

        var target = Path.Combine(UserLibrariesDir, safeName);
        try
        {
            using var ms = new MemoryStream();
            await source.CopyToAsync(ms);
            ms.Position = 0;

            // Cheap validation — sniff for any VHDL declaration. We don't
            // run a full parser here; that's the LibraryCompiler's job at
            // build time.
            var text = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    text,
                    @"(?im)\b(entity|package|function|procedure)\s+[a-z_]"))
            {
                _logger.LogWarning("[UserAssetStore] Rejected library import — no VHDL declarations: {Name}", safeName);
                return null;
            }

            ms.Position = 0;
            using var file = File.Create(target);
            await ms.CopyToAsync(file);

            OnChange?.Invoke();
            _logger.LogInformation("[UserAssetStore] Imported library {Path}", target);
            return target;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Library import failed for {Name}", safeName);
            return null;
        }
    }

    /// <summary>Deletes a user library file by base name (ignores directory component).</summary>
    public bool DeleteLibrary(string fileName)
    {
        try
        {
            var name = Path.GetFileName(fileName);
            var path = Path.Combine(UserLibrariesDir, name);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            OnChange?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] Delete library failed: {Name}", fileName);
            return false;
        }
    }

    /// <summary>
    /// Returns the package names declared in <paramref name="filePath"/> for
    /// the Settings UI's library inspector. Empty when the file declares
    /// no packages (file-stem fallback applies at compile time).
    /// </summary>
    public static IReadOnlyList<string> ListPackagesInFile(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            return ExtractPackageNames(text).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Wipes everything under <see cref="LibraryCacheDir"/>. Pairs with
    /// <c>BuildOrchestrator.Reset()</c> to give Settings → "Clear cache"
    /// a clean two-step: drop the on-disk artifacts, then drop the
    /// in-memory snapshot. Returns the number of files removed.
    /// </summary>
    public int ClearLibraryCache()
    {
        if (!Directory.Exists(LibraryCacheDir)) return 0;
        int removed = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(LibraryCacheDir, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(f); removed++; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[UserAssetStore] ClearLibraryCache: failed to delete {File}", f);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAssetStore] ClearLibraryCache enumeration failed");
        }
        return removed;
    }

    /// <summary>
    /// Reconciles <paramref name="resolver"/>'s user-vendor entries against
    /// the current contents of <see cref="UserLibrariesDir"/>. Adds any new
    /// packages, removes any whose backing file is gone, leaves IEEE
    /// builtins untouched. Hosts wire this into
    /// <see cref="OnChange"/> so the resolver stays in sync after Settings →
    /// Libraries Import / Delete (and after external file changes that
    /// trigger the same event).
    /// </summary>
    public void SyncResolver(LibraryResolver resolver)
    {
        if (resolver is null) return;
        var fresh      = ListUserLibrarySources();
        var freshNames = new HashSet<string>(
            fresh.Select(s => s.LibraryName),
            StringComparer.OrdinalIgnoreCase);

        // 1. Drop user-vendor entries (Vendor != "IEEE") whose package
        //    is no longer present on disk. Iterating over the snapshot
        //    `All` returns a copy, so it's safe to mutate inside.
        foreach (var existing in resolver.All)
        {
            if (string.Equals(existing.Vendor, "IEEE", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!freshNames.Contains(existing.LibraryName))
                resolver.Unregister(existing.LibraryName);
        }

        // 2. Add or refresh anything currently on disk. Register is
        //    add-or-replace, so re-imports with new content also pick
        //    up the latest SourceKey (e.g. updated #package= fragment).
        foreach (var s in fresh) resolver.Register(s);
    }

    private static string? SanitizeLibraryFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var name = Path.GetFileName(input);
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

        // Accept .vhd or .vhdl; default to .vhd if the import had no
        // recognised extension so the file lands somewhere predictable.
        var lower = name.ToLowerInvariant();
        if (!lower.EndsWith(".vhd") && !lower.EndsWith(".vhdl")) name += ".vhd";
        return name;
    }

    private static readonly System.Text.RegularExpressions.Regex PackageDeclRe =
        new(@"(?im)^\s*package\s+([a-z_][a-z0-9_]*)\s+is\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Returns every <c>package X is</c> name declared in <paramref name="vhdlSource"/>.
    /// Skips <c>package body</c> declarations — those don't introduce a
    /// new library, they fill in an already-declared one.
    /// </summary>
    private static IEnumerable<string> ExtractPackageNames(string vhdlSource)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Text.RegularExpressions.Match m in PackageDeclRe.Matches(vhdlSource))
        {
            var name = m.Groups[1].Value;
            // Suppress duplicates and the legacy `package body X is`
            // pattern: we only care about the declaration that introduces
            // the package's symbols.
            if (string.Equals(name, "body", StringComparison.OrdinalIgnoreCase)) continue;
            if (seen.Add(name)) yield return name;
        }
    }

    /// <summary>
    /// Reads the bytes belonging to <paramref name="packageName"/> inside the file
    /// <paramref name="filePath"/>. When the file contains a single package or no
    /// package declarations, returns the full file. When several packages
    /// share the file, returns the substring from <c>package X is</c> through
    /// the matching <c>end</c>. The trim is conservative — better to ship
    /// a few extra lines than miss a function declaration.
    /// </summary>
    public static byte[]? ReadPackageBytes(string filePath, string? packageName)
    {
        if (!File.Exists(filePath)) return null;
        byte[] all;
        try { all = File.ReadAllBytes(filePath); }
        catch { return null; }

        if (string.IsNullOrEmpty(packageName)) return all;

        var text = System.Text.Encoding.UTF8.GetString(all);
        // `package <name> is` opens the block; the matching close is
        // `end [<name>]?;` — VHDL allows the trailing identifier or just
        // `end;`. Two regexes wired with a positional anchor keep the
        // body extraction simple.
        var startRe = new System.Text.RegularExpressions.Regex(
            $@"(?im)^\s*package\s+{System.Text.RegularExpressions.Regex.Escape(packageName)}\s+is\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);
        var startMatch = startRe.Match(text);
        if (!startMatch.Success) return all; // fall back to whole file

        var afterStart = text.Substring(startMatch.Index);
        // Match `end` followed by optional package name and a semicolon.
        var endRe = new System.Text.RegularExpressions.Regex(
            $@"(?im)^\s*end(?:\s+{System.Text.RegularExpressions.Regex.Escape(packageName)})?\s*;",
            System.Text.RegularExpressions.RegexOptions.Compiled);
        var endMatch = endRe.Match(afterStart);
        if (!endMatch.Success) return all; // mis-shaped — better to overshoot than miss symbols

        var slice = afterStart.Substring(0, endMatch.Index + endMatch.Length);
        return System.Text.Encoding.UTF8.GetBytes(slice);
    }

    /// <summary>
    /// Splits a <c>SourceKey</c> of the form <c>{abs-path}#package={X}</c>
    /// into its parts. Returns the raw path and a null package name when
    /// no fragment is present (single-package files).
    /// </summary>
    public static (string Path, string? Package) ParseLibrarySourceKey(string sourceKey)
    {
        if (string.IsNullOrEmpty(sourceKey)) return (sourceKey, null);
        var idx = sourceKey.IndexOf('#');
        if (idx < 0) return (sourceKey, null);

        var path = sourceKey.Substring(0, idx);
        var frag = sourceKey.Substring(idx + 1);
        const string prefix = "package=";
        if (frag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return (path, frag.Substring(prefix.Length));
        return (path, null);
    }

    // ------------------------------------------------------------------
    // Templates (downloaded as starter JSON)
    // ------------------------------------------------------------------

    public static string BuildFpgaTemplate() => @"{
  ""model"":         ""YOUR-MODEL-ID"",
  ""family"":        ""Your family (e.g. Cyclone IV E)"",
  ""manufacturer"":  ""Your vendor (Intel / Xilinx / Lattice / …)"",
  ""resources"": {
    ""logic_units"":          0,
    ""logic_units_type"":     ""Logic Elements (LEs) or LUT4/LUT6"",
    ""registers_flip_flops"": 0,
    ""memory_blocks"":        0,
    ""memory_block_type"":    ""M9K / BRAM36 / EBR / …"",
    ""memory_total_kbits"":   0,
    ""dsp_slices"":           0,
    ""dsp_slice_type"":       ""18x18 / DSP48E1 / DSP48E2 / …""
  },
  ""clocking"": {
    ""pll_count"":           0,
    ""dcm_mmcm_count"":      0,
    ""max_pll_output_mhz"":  0
  },
  ""power"": {
    ""core_voltage_vccint_v"": 1.2
  },
  ""io"": {
    ""max_user_io"":            0,
    ""io_banks_count"":         0,
    ""supported_io_standards"": null
  },
  ""package_options"": [
    ""Package-A"",
    ""Package-B""
  ]
}
";

    public static string BuildLanguageTemplate() => @"{
  ""__LANG_NAME__"": ""Your language name (e.g. Klingon)"",

  ""APP_TITLE"": ""Kmila-9s"",

  ""NAV_PROJECTS"":       ""Projects"",
  ""NAV_CREATE_PROJECT"": ""New project"",
  ""NAV_LEARN"":          ""Learn"",
  ""NAV_SETTINGS"":       ""Settings"",
  ""NAV_LOGS"":           ""Logs"",

  ""HOME_TITLE"":         ""Learn VHDL without hardware"",
  ""HOME_SUBTITLE"":      ""Short tagline for the landing page"",
  ""HOME_GO_TO_PROJECTS"": ""Open projects"",

  ""COMMON_ERROR"":   ""Error"",
  ""COMMON_MESSAGE"": ""Notice"",
  ""COMMON_CANCEL"":  ""Cancel"",
  ""COMMON_SAVE"":    ""Save""
}
";

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string? SanitizeFileName(string input, string requiredExtension)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var name = Path.GetFileName(input);
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        if (!name.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase)) name += requiredExtension;
        return name;
    }

    /// <summary>
    /// Wires a <see cref="FileSystemWatcher"/> per user-asset folder so
    /// external mutations (cloud sync drop, Finder copy, scripted import)
    /// reach the resolver / picker without an app restart. All events fan
    /// into a single debounced <see cref="OnChange"/> fire so a batch of
    /// writes doesn't blow up subscribers.
    /// </summary>
    private void StartWatchers()
    {
        // Blazor WebAssembly has no OS filesystem-change notifications;
        // FileSystemWatcher throws PlatformNotSupportedException per folder.
        // Skip quietly there (assets just don't auto-refresh) instead of
        // logging three warnings on every boot. Other platforms unchanged.
        if (OperatingSystem.IsBrowser()) return;

        foreach (var dir in new[] { UserDevicesDir, UserDictionariesDir, UserLibrariesDir })
        {
            try
            {
                var w = new FileSystemWatcher(dir)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                                   NotifyFilters.Size     | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents   = true
                };
                w.Created += (_, _) => DebouncedFireChange();
                w.Deleted += (_, _) => DebouncedFireChange();
                w.Renamed += (_, _) => DebouncedFireChange();
                w.Changed += (_, _) => DebouncedFireChange();
                _watchers.Add(w);
            }
            catch (Exception ex)
            {
                // FileSystemWatcher fails on some sandboxed targets (older
                // Android, browser-targeting builds). Falling back to "no
                // external watch" matches the v1 behaviour exactly — Settings
                // imports still fire OnChange directly.
                _logger.LogWarning(ex, "[UserAssetStore] Failed to start FileSystemWatcher on {Dir}", dir);
            }
        }
    }

    private void DebouncedFireChange()
    {
        _watcherDebounce?.Dispose();
        _watcherDebounce = new System.Threading.Timer(_ =>
        {
            try { OnChange?.Invoke(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserAssetStore] OnChange subscriber threw");
            }
        }, null, WatcherDebounceMs, System.Threading.Timeout.Infinite);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var w in _watchers)
        {
            try { w.Dispose(); }
            catch { /* swallow — disposing already-broken watchers shouldn't fail teardown */ }
        }
        _watchers.Clear();
        _watcherDebounce?.Dispose();
        _watcherDebounce = null;
    }

    private static string ResolveRoot()
    {
#if WINDOWS || MACCATALYST || ANDROID || IOS
        return Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
#else
        // Blazor WebAssembly: no OS filesystem. Everything Kmila persists lives
        // under /kmila in the in-memory (MEMFS) filesystem, which BrowserFsSync
        // hydrates from / flushes to OPFS so it survives reloads. Runtime-gated
        // so the server (Kmila.Web) path is byte-for-byte unchanged.
        if (OperatingSystem.IsBrowser()) return "/kmila";
        return Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "KmilaData");
#endif
    }
}
