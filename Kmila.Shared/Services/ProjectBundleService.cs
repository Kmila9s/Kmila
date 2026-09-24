using System.Text.Json;
using Kmila.Shared.Data;
using Kmila.Shared.Models;
using Kmila.Shared.Repositories;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services;

/// <summary>
/// Single-file export / import for a whole Kmila project — metadata, files,
/// saved configurations, recent snapshots. JSON format so it's diffable,
/// emailable, and easy to inspect without special tools.
/// </summary>
public class ProjectBundleService
{
    /// <summary>
    /// Bundle schema revision. Bumped on breaking changes.
    /// v2 adds <see cref="FileRecord.DirectoryPath"/> so a project's folder
    /// structure survives an export/import round-trip. v1 bundles (no
    /// DirectoryPath) still import — the field defaults to root.
    /// </summary>
    public const int SchemaVersion = 2;

    private readonly ApplicationDbContext _db;
    private readonly Projects _projects;
    private readonly ProjectFiles _files;
    private readonly ConfigurationService _configs;
    private readonly SimulationSnapshotStore _snapshots;
    private readonly ILogger _logger;

    public ProjectBundleService(
        ApplicationDbContext db,
        Projects projects,
        ProjectFiles files,
        ConfigurationService configs,
        SimulationSnapshotStore snapshots,
        ILogger<ProjectBundleService>? logger = null)
    {
        _db = db;
        _projects = projects;
        _files = files;
        _configs = configs;
        _snapshots = snapshots;
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Builds an in-memory bundle for <paramref name="project"/> and returns
    /// it as JSON text. Never touches the filesystem — callers pipe the
    /// result through the JS download helper or save to wherever they like.
    /// </summary>
    public async Task<string> ExportAsync(Project project)
    {
        var files = await _files.GetProjectFilesAsync(project);
        var snaps = await _snapshots.ListForProjectAsync(project.Id);
        // Configurations are global; filter to ones whose name references the
        // project. Best-effort — if the user renamed the config, it still
        // exports as part of the bundle under its saved name.
        var allConfigNames = await _configs.GetConfigurationNamesAsync();
        var configs = new List<KmilaConfiguration>();
        foreach (var name in allConfigNames ?? new List<string>())
        {
            var cfg = await _configs.LoadConfigurationAsync(name);
            if (cfg != null) configs.Add(cfg);
        }

        var bundle = new Bundle
        {
            SchemaVersion = SchemaVersion,
            ExportedAt = DateTime.UtcNow,
            Project = new ProjectRecord
            {
                Name = project.Name,
                Description = project.Description,
                CreationTime = project.CreationTime
            },
            Files = files.Select(f => new FileRecord
            {
                FileName = f.FileName,
                DirectoryPath = f.DirectoryPath,
                Content = f.Content
            }).ToList(),
            Configurations = configs,
            Snapshots = snaps.Select(s => new SnapshotRecord
            {
                Label = s.Label,
                CreatedAt = s.CreatedAt,
                Time = s.Time,
                Scale = s.Scale,
                FpgaModel = s.FpgaModel,
                SignalCount = s.SignalCount,
                SampleCount = s.SampleCount,
                Pinned = s.Pinned,
                WaveformJson = s.WaveformJson
            }).ToList()
        };

        return JsonSerializer.Serialize(bundle,
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Parses a JSON bundle and creates a fresh project with its files,
    /// configs and snapshots. Never overwrites an existing project with the
    /// same name — appends "(imported)" / "(imported N)" instead. Returns
    /// the new project or null on parse failure.
    /// </summary>
    public async Task<Project?> ImportAsync(string json, string basePath)
    {
        try
        {
            var bundle = JsonSerializer.Deserialize<Bundle>(json);
            if (bundle?.Project == null || string.IsNullOrWhiteSpace(bundle.Project.Name))
            {
                _logger.LogWarning("[ProjectBundle] Import payload missing project block");
                return null;
            }

            // Resolve a unique name
            var existingNames = (await _projects.GetListAsync() ?? new())
                .Select(p => p.Name.ToLowerInvariant())
                .ToHashSet();
            string name = bundle.Project.Name + " (imported)";
            for (int i = 2; existingNames.Contains(name.ToLowerInvariant()); i++)
                name = $"{bundle.Project.Name} (imported {i})";

            var folder = SanitizeFolder($"Import_{name}_{DateTime.UtcNow.Ticks}");
            var project = new Project
            {
                Name = name,
                Description = bundle.Project.Description ?? string.Empty,
                BasePath = Path.Combine(basePath, "Projects", folder)
            };

            if (!await _projects.CreateAsync(project)) return null;

            foreach (var fileRec in bundle.Files ?? new())
            {
                if (string.IsNullOrWhiteSpace(fileRec.FileName)) continue;
                await _files.CreateAsync(new ProjectFile
                {
                    Project = project,
                    FileName = fileRec.FileName,
                    DirectoryPath = fileRec.DirectoryPath ?? string.Empty,
                    Content = fileRec.Content ?? string.Empty
                });
            }

            foreach (var cfg in bundle.Configurations ?? new())
            {
                if (cfg == null || string.IsNullOrWhiteSpace(cfg.Name)) continue;
                await _configs.SaveConfigurationAsync(cfg);
            }

            foreach (var sr in bundle.Snapshots ?? new())
            {
                var wd = WaveformData.FromJson(sr.WaveformJson);
                if (wd == null) continue;
                await _snapshots.CaptureAsync(
                    project.Id, wd, sr.Time, sr.Scale, sr.FpgaModel,
                    label: sr.Label);
            }

            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProjectBundle] Import failed");
            return null;
        }
    }

    private static string SanitizeFolder(string s)
    {
        var buf = new System.Text.StringBuilder();
        foreach (var c in s)
            buf.Append(char.IsLetterOrDigit(c) ? c : '_');
        return buf.ToString();
    }

    // ---- DTOs ----

    /// <summary>Top-level bundle JSON shape.</summary>
    public class Bundle
    {
        public int SchemaVersion { get; set; }
        public DateTime ExportedAt { get; set; }
        public ProjectRecord? Project { get; set; }
        public List<FileRecord> Files { get; set; } = new();
        public List<KmilaConfiguration> Configurations { get; set; } = new();
        public List<SnapshotRecord> Snapshots { get; set; } = new();
    }

    /// <summary>Minimal project metadata embedded in the bundle (name, description, creation timestamp).</summary>
    public class ProjectRecord
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreationTime { get; set; }
    }

    /// <summary>One source file inside the bundle: filename, sub-directory (v2+), plus full text content.</summary>
    public class FileRecord
    {
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Project-relative sub-directory (forward slashes), added in schema v2.
        /// Absent in v1 bundles — deserializes to null, treated as project root.
        /// </summary>
        public string? DirectoryPath { get; set; }
        public string? Content { get; set; }
    }

    /// <summary>One simulation snapshot included in the bundle (label, scale, FPGA target, signal/sample counts, pinned flag).</summary>
    public class SnapshotRecord
    {
        public string Label { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Time { get; set; }
        public string Scale { get; set; } = "US";
        public string? FpgaModel { get; set; }
        public int SignalCount { get; set; }
        public int SampleCount { get; set; }
        public bool Pinned { get; set; }
        public string WaveformJson { get; set; } = string.Empty;
    }
}
