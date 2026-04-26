using System.Text.Json;
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
public class UserAssetStore
{
    /// <summary>Name of the optional display-name key inside a language file.</summary>
    public const string LanguageDisplayNameKey = "__LANG_NAME__";

    private readonly string _root;
    private readonly ILogger _logger;

    public UserAssetStore(ILogger<UserAssetStore>? logger = null)
    {
        _logger = (ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        _root = ResolveRoot();
        EnsureDirs();
    }

    /// <summary>Fires when a file is imported or deleted so UIs can refresh.</summary>
    public event Action? OnChange;

    public string UserDevicesDir       => Path.Combine(_root, "UserAssets", "Devices");
    public string UserDictionariesDir  => Path.Combine(_root, "UserAssets", "Dictionaries");

    private void EnsureDirs()
    {
        try
        {
            Directory.CreateDirectory(UserDevicesDir);
            Directory.CreateDirectory(UserDictionariesDir);
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

    private static string ResolveRoot()
    {
#if WINDOWS || MACCATALYST || ANDROID || IOS
        return Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
#else
        return Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "KmilaData");
#endif
    }
}
