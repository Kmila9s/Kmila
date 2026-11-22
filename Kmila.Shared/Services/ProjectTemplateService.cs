using System.Text.Json;
using System.Text.Json.Serialization;
using Kmila.Frontend;
using Kmila.Shared.Models;
using Kmila.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// One entry in the gallery: id + display keys + default settings + one file
/// set per HDL language.
/// </summary>
public sealed record ProjectTemplate(
    string  Id,
    string  Icon,
    string  NameKey,
    string  DescriptionKey,
    string? DefaultFpgaModel,
    int     DefaultClockMhz,
    HdlVariants<ProjectTemplateVariant> Variants)
{
    /// <summary>True when the template's (preferred) file set forms a library to register.</summary>
    public bool AutoImportAsLibrary => Variants.Preferred?.Payload.IsLibrary == true;
}

/// <summary>One HDL language's file set of a template.</summary>
/// <param name="Files">Files to create, relative to the template root ('/'-separated, no leading slash).</param>
/// <param name="LibraryFiles">Paths (subset of <paramref name="Files"/>), in
/// compile order, that form the library registered when the template
/// auto-imports as a library (declaration first, then body). Empty for a
/// non-library template.</param>
public sealed record ProjectTemplateVariant(
    IReadOnlyList<ProjectTemplateFile> Files,
    IReadOnlyList<string> LibraryFiles)
{
    /// <summary>True when this file set forms a library.</summary>
    public bool IsLibrary => LibraryFiles.Count > 0;
}

/// <summary>One file in a template's tree. Path is relative to the template root and may include directory components.</summary>
public sealed record ProjectTemplateFile(string Path);

/// <summary>
/// Loads the template gallery + creates projects from a chosen template.
/// Mirrors the <see cref="LessonCatalog"/> stream-delegate pattern so a single
/// implementation works on the Blazor Web host (file system) and MAUI Blazor
/// Hybrid (FileSystem.OpenAppPackageFileAsync).
/// </summary>
/// <remarks>
/// The manifest + per-template metadata are loaded once and cached. File
/// contents are not cached (small N, and the user creates each file once).
/// </remarks>
public sealed class ProjectTemplateService
{
    private readonly Func<string, Task<Stream>> _open;
    private readonly Projects _projects;
    private readonly ProjectFiles _projectFiles;
    private readonly JSONFileReader _jsonReader;
    private readonly SelectedFPGA _selectedFpga;
    private readonly SimulationParameters _sim;
    private readonly UserAssetStore _userAssets;
    private readonly ILogger _logger;

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private IReadOnlyList<ProjectTemplate>? _cached;

    /// <summary>Creates the service with its file-open delegate and collaborating stores.</summary>
    /// <param name="openFileAsync">Delegate that opens a bundled resource stream by path.</param>
    /// <param name="projects">Project persistence service.</param>
    /// <param name="projectFiles">Per-project file service.</param>
    /// <param name="jsonReader">Reader for JSON manifest/metadata resources.</param>
    /// <param name="selectedFpga">Currently selected FPGA target.</param>
    /// <param name="sim">Simulation parameters.</param>
    /// <param name="userAssets">Store for user-registered library assets.</param>
    /// <param name="logger">Optional logger; a null logger is used when omitted.</param>
    public ProjectTemplateService(
        Func<string, Task<Stream>> openFileAsync,
        Projects projects,
        ProjectFiles projectFiles,
        JSONFileReader jsonReader,
        SelectedFPGA selectedFpga,
        SimulationParameters sim,
        UserAssetStore userAssets,
        ILogger<ProjectTemplateService>? logger = null)
    {
        _open = openFileAsync;
        _projects = projects;
        _projectFiles = projectFiles;
        _jsonReader = jsonReader;
        _selectedFpga = selectedFpga;
        _sim = sim;
        _userAssets = userAssets;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Returns the gallery in manifest-declared order. First call loads + caches;
    /// subsequent calls are free. Returns an empty list when the manifest can't
    /// be read so the UI renders an empty state instead of crashing.
    /// </summary>
    public async Task<IReadOnlyList<ProjectTemplate>> ListAsync()
    {
        if (_cached != null) return _cached;
        await _loadGate.WaitAsync();
        try
        {
            if (_cached != null) return _cached;
            _cached = await LoadAsync();
            return _cached;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task<IReadOnlyList<ProjectTemplate>> LoadAsync()
    {
        Manifest? manifest;
        try
        {
            using var stream = await _open("Templates/manifest.json");
            manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProjectTemplate] Failed to load manifest.json");
            return Array.Empty<ProjectTemplate>();
        }

        if (manifest?.Templates is null || manifest.Templates.Count == 0)
        {
            _logger.LogWarning("[ProjectTemplate] manifest.json empty / malformed");
            return Array.Empty<ProjectTemplate>();
        }

        var list = new List<ProjectTemplate>();
        foreach (var id in manifest.Templates)
        {
            try
            {
                using var stream = await _open($"Templates/{id}/template.json");
                var dto = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOpts);
                if (dto is null || !ContentSchema.Accepts(dto.Version, $"Templates/{id}/template.json", _logger)) continue;
                list.Add(new ProjectTemplate(
                    Id:                  dto.Id ?? id,
                    Icon:                dto.Icon ?? "FileEarmark",
                    NameKey:             dto.NameKey ?? id.ToUpperInvariant(),
                    DescriptionKey:      dto.DescriptionKey ?? string.Empty,
                    DefaultFpgaModel:    dto.DefaultFpgaModel,
                    DefaultClockMhz:     dto.DefaultClockMhz ?? 50,
                    Variants:            HdlVariants<ProjectTemplateVariant>.FromIds(
                                            dto.Variants?.ToDictionary(kv => kv.Key, kv => ToVariant(kv.Value)))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProjectTemplate] Failed to load template '{Id}'", id);
            }
        }
        _logger.LogInformation("[ProjectTemplate] Loaded {Count} templates", list.Count);
        return list;
    }

    /// <summary>
    /// Creates a project on disk + in the DB from <paramref name="template"/>.
    /// </summary>
    /// <param name="template">The project template to instantiate.</param>
    /// <param name="projectName">Name for the new project; must be non-empty.</param>
    /// <param name="description">Optional description stored with the project.</param>
    /// <param name="languageCode">Resolved against the template's available
    /// language folders; falls back to English when missing.</param>
    /// <param name="language">HDL language of the file set to create; the
    /// template's preferred variant when null.</param>
    /// <param name="registerAsLibrary">When the template's
    /// <see cref="ProjectTemplate.AutoImportAsLibrary"/> is true and this is
    /// also true, the variant's library files are concatenated and registered
    /// with <see cref="UserAssetStore"/> so other projects can `use` them.</param>
    /// <returns>The created <see cref="Project"/>, or null on failure (look at
    /// <see cref="Projects.LastError"/> for the reason).</returns>
    public async Task<Project?> CreateProjectAsync(
        ProjectTemplate template,
        string projectName,
        string? description,
        string? languageCode = null,
        bool registerAsLibrary = false,
        HdlLanguage? language = null)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            _logger.LogWarning("[ProjectTemplate] CreateProjectAsync: empty projectName");
            return null;
        }

        var picked = language is null ? template.Variants.Preferred
            : template.Variants.For(language) is { } requested ? (language, requested) : null;
        if (picked is not { } choice)
        {
            _logger.LogWarning("[ProjectTemplate] Template '{Id}' has no {Lang} variant",
                template.Id, language?.DisplayName ?? "supported");
            return null;
        }
        var (hdl, variant) = choice;

        // Compute the platform base path (matches ProjectsView.CreateSampleAsync).
        string basePath;
#if WINDOWS || MACCATALYST || ANDROID || IOS
        basePath = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
#else
        // Blazor WebAssembly persists under /kmila in MEMFS (see UserAssetStore).
        basePath = OperatingSystem.IsBrowser()
            ? "/kmila"
            : Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "KmilaData");
#endif

        var ident  = SanitizeIdentifier(projectName);
        var folder = SanitizeFolder(projectName);
        if (string.IsNullOrEmpty(folder)) folder = $"Project_{DateTime.UtcNow.Ticks}";

        var project = new Project
        {
            Name        = projectName.Trim(),
            Description = description ?? string.Empty,
            BasePath    = Path.Combine(basePath, "Projects", folder)
        };

        if (!await _projects.CreateAsync(project))
        {
            _logger.LogWarning("[ProjectTemplate] Projects.CreateAsync failed: {Err}", _projects.LastError);
            return null;
        }

        var writtenContents = new Dictionary<string, string>();
        foreach (var (rel, content) in await RenderFilesAsync(template, variant, project.Name, languageCode))
        {
            // Split path into directory + filename.
            var lastSlash = rel.LastIndexOf('/');
            var dir       = lastSlash >= 0 ? rel.Substring(0, lastSlash) : string.Empty;
            var fileName  = lastSlash >= 0 ? rel.Substring(lastSlash + 1) : rel;

            var ok = await _projectFiles.CreateAsync(new ProjectFile
            {
                Project       = project,
                FileName      = fileName,
                DirectoryPath = dir,
                Content       = content
            });
            if (!ok)
            {
                _logger.LogWarning("[ProjectTemplate] Failed to write {Rel}: {Err}",
                    rel, _projectFiles.LastError);
            }
            else
            {
                writtenContents[rel] = content;
            }
        }

        // Apply default FPGA when the template specifies one (library
        // template leaves this null — no top-level entity to drive).
        if (!string.IsNullOrEmpty(template.DefaultFpgaModel))
        {
            try
            {
                var dict = await _jsonReader.GetFPGAsDictionaryAsync();
                if (dict.TryGetValue(template.DefaultFpgaModel!, out var path))
                {
                    var fpga = await _jsonReader.ReadFPGAJsonFileAsync(path);
                    if (fpga != null)
                    {
                        _selectedFpga.SetFPGA(fpga, path);
                    }
                }
                else
                {
                    _logger.LogWarning("[ProjectTemplate] Default FPGA '{Model}' not in dictionary",
                        template.DefaultFpgaModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ProjectTemplate] Could not load default FPGA");
            }

            try
            {
                _sim.SetClockOverride(null, template.DefaultClockMhz);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ProjectTemplate] SetClockOverride failed");
            }
        }

        // Library auto-import: concatenate the variant's library files and hand to
        // UserAssetStore. ImportLibraryAsync fires OnChange → SyncResolver,
        // so the new library is visible to LibraryCompiler on the next build
        // without an app restart.
        if (variant.IsLibrary && registerAsLibrary)
        {
            await RegisterAsLibraryAsync(hdl, variant, writtenContents, ident);
        }

        return project;
    }

    private async Task<string?> ReadTemplateFileWithFallbackAsync(
        string templateId, string lang, string relPath)
    {
        var langPath = $"Templates/{templateId}/{lang}/files/{relPath}";
        var enPath   = $"Templates/{templateId}/en/files/{relPath}";

        var content = await TryReadAsync(langPath);
        if (content == null && !string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase))
        {
            content = await TryReadAsync(enPath);
        }
        return content;
    }

    private async Task<string?> TryReadAsync(string path)
    {
        try
        {
            using var stream = await _open(path);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }

    private async Task RegisterAsLibraryAsync(
        HdlLanguage language,
        ProjectTemplateVariant variant,
        IReadOnlyDictionary<string, string> writtenContents,
        string ident)
    {
        // The library template ships declaration and body as separate files
        // for readability, but UserAssetStore expects one file that declares
        // the library — and the body must compile alongside. Concatenate in
        // the variant's declared order.
        var parts = variant.LibraryFiles
            .Select(writtenContents.GetValueOrDefault)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();
        if (parts.Count == 0)
        {
            _logger.LogWarning("[ProjectTemplate] Library auto-import: no library files written — skipping");
            return;
        }
        var combined = string.Join(Environment.NewLine + Environment.NewLine, parts);

        // Use the library unit's declared name rather than the project's
        // sanitized identifier — it is the canonical name downstream `use`
        // clauses reference.
        var packageName = Frontends.For(language).DeclaredLibraryUnits(combined).FirstOrDefault() ?? ident;
        var importFileName = packageName + language.PrimaryExtension;

        try
        {
            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(combined));
            var saved = await _userAssets.ImportLibraryAsync(ms, importFileName);
            if (saved is null)
            {
                _logger.LogWarning("[ProjectTemplate] UserAssetStore rejected library {Name}", importFileName);
            }
            else
            {
                _logger.LogInformation("[ProjectTemplate] Registered library {Path}", saved);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProjectTemplate] Library auto-import failed");
        }
    }

    // ---- helpers ----

    /// <summary>Maps a variant DTO, normalising every path to '/'-separated without a leading slash.</summary>
    private static ProjectTemplateVariant ToVariant(TemplateVariantDto dto) => new(
        Files: (dto.Files ?? new())
                   .Where(f => !string.IsNullOrWhiteSpace(f.Path))
                   .Select(f => new ProjectTemplateFile(NormalizePath(f.Path!)))
                   .ToList(),
        LibraryFiles: (dto.LibraryFiles ?? new()).Select(NormalizePath).ToList());

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    /// <summary>
    /// Lowercases + collapses non-identifier characters into underscores so
    /// the result is a valid VHDL identifier. First char is forced to a
    /// letter when the input started with a digit ("8bit" → "x_8bit").
    /// </summary>
    public static string SanitizeIdentifier(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "kmila_project";
        var buf = new System.Text.StringBuilder();
        foreach (var ch in raw.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                buf.Append(ch);
            else
                buf.Append('_');
        }
        var s = buf.ToString().Trim('_');
        if (string.IsNullOrEmpty(s)) return "kmila_project";
        if (char.IsDigit(s[0])) s = "x_" + s;
        return s;
    }

    private static string SanitizeFolder(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw.Trim()
                   .Replace(" ", string.Empty, System.StringComparison.Ordinal)
                   .Replace("·", string.Empty, System.StringComparison.Ordinal)
                   .Replace("/", "_", System.StringComparison.Ordinal)
                   .Replace("\\", "_", System.StringComparison.Ordinal)
                   .Replace("-", "_", System.StringComparison.Ordinal);
        return s;
    }

    /// <summary>
    /// The files a project created from <paramref name="variant"/> gets, rendered:
    /// read from the <paramref name="languageCode"/> folder (English when null, empty or missing) with
    /// <c>{{PROJECT_NAME}}</c>, <c>{{PROJECT_IDENT}}</c> and <c>{{PACKAGE_NAME}}</c>
    /// substituted. Missing files are logged and skipped.
    /// </summary>
    public async Task<IReadOnlyList<(string Path, string Content)>> RenderFilesAsync(
        ProjectTemplate template, ProjectTemplateVariant variant, string projectName, string? languageCode = null)
    {
        var lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.ToLowerInvariant();
        var ident = SanitizeIdentifier(projectName);
        var tokens = new Dictionary<string, string>
        {
            { "PROJECT_NAME", projectName.Trim() },
            { "PROJECT_IDENT", ident },
            { "PACKAGE_NAME", ident },
        };
        var files = new List<(string, string)>();
        foreach (var tf in variant.Files)
        {
            var content = await ReadTemplateFileWithFallbackAsync(template.Id, lang, tf.Path);
            if (content is null)
            {
                _logger.LogWarning("[ProjectTemplate] Missing template file: {Id}/{Lang}/files/{Rel}",
                    template.Id, lang, tf.Path);
                continue;
            }
            files.Add((tf.Path, ApplyTokens(content, tokens)));
        }
        return files;
    }

    private static string ApplyTokens(string content, IReadOnlyDictionary<string, string> tokens)
    {
        var s = content;
        foreach (var kv in tokens)
        {
            s = s.Replace("{{" + kv.Key + "}}", kv.Value, System.StringComparison.Ordinal);
        }
        return s;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ---- DTOs (JSON shapes) ----

    private sealed class Manifest
    {
        public List<string>? Templates { get; set; }
    }

    private sealed class TemplateDto
    {
        public string? Id { get; set; }
        public string? Icon { get; set; }
        public string? NameKey { get; set; }
        public string? DescriptionKey { get; set; }
        public string? DefaultFpgaModel { get; set; }
        public int?    DefaultClockMhz { get; set; }
        public int     Version { get; set; }
        /// <summary>File set per HDL language id (schema v2).</summary>
        public Dictionary<string, TemplateVariantDto>? Variants { get; set; }
    }

    private sealed class TemplateVariantDto
    {
        public List<TemplateFileDto>? Files { get; set; }
        public List<string>? LibraryFiles { get; set; }
    }

    private sealed class TemplateFileDto
    {
        public string? Path { get; set; }
    }
}
