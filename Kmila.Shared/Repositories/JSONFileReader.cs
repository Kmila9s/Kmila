using Microsoft.Extensions.Logging;
using System.Text.Json;
using Kmila.Shared.Models;
using Kmila.Shared.Services;
#if LINUX || ASPNETCORE
using Microsoft.AspNetCore.Hosting;
#endif

namespace Kmila.Shared.Repositories;

/// <summary>
/// A repository for reading FPGA data from JSON files.
/// Supports both ASP.NET Core (web) and MAUI (mobile/desktop) environments.
/// </summary>
/// <remarks>
/// For web environments, this class reads from the file system using IWebHostEnvironment.
/// For MAUI environments, pass a Func that wraps FileSystem.OpenAppPackageFileAsync.
/// </remarks>
public class JSONFileReader
{
    /// <summary>
    /// Logger (web + MAUI). Optional at construction time — falls back to a
    /// no-op so the DI factories that don't pass one still work.
    /// </summary>
    private readonly Microsoft.Extensions.Logging.ILogger _logger =
        Microsoft.Extensions.Logging.Abstractions.NullLogger<JSONFileReader>.Instance;

    /// <summary>
    /// Optional backing store for user-imported FPGA JSON files. When set,
    /// <see cref="GetFPGAsAsync"/> merges the user-supplied paths on top of
    /// the bundled ones, and <see cref="ReadFPGAJsonFileAsync"/> reads
    /// directly from disk whenever the caller hands over an absolute path.
    /// </summary>
    private readonly UserAssetStore? _userAssets;

#if LINUX || ASPNETCORE
    private readonly string _devicesPath;
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the <see cref="JSONFileReader"/> class for web environments.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    /// <param name="logger">Optional logger.</param>
    public JSONFileReader(IWebHostEnvironment env,
                          Microsoft.Extensions.Logging.ILogger<JSONFileReader>? logger = null,
                          UserAssetStore? userAssets = null)
    {
        _env = env;
        if (logger != null) _logger = logger;
        _userAssets = userAssets;
        // First try the RCL static files path
        _devicesPath = Path.Combine(_env.WebRootPath ?? "", "_content", "Kmila.Shared", "Devices");
        if (!Directory.Exists(_devicesPath))
        {
            // Fall back to direct wwwroot path (development)
            _devicesPath = Path.Combine(_env.ContentRootPath, "..", "Kmila.Shared", "wwwroot", "Devices");
        }
        _logger.LogInformation($"[JSONFileReader] WebRootPath: {_env.WebRootPath}");
        _logger.LogInformation($"[JSONFileReader] ContentRootPath: {_env.ContentRootPath}");
        _logger.LogInformation($"[JSONFileReader] DevicesPath: {_devicesPath}");
        _logger.LogInformation($"[JSONFileReader] DevicesPath exists: {Directory.Exists(_devicesPath)}");
    }

    /// <summary>
    /// Reads an FPGA JSON file and deserializes it to an <see cref="FPGA"/> object.
    /// </summary>
    /// <param name="JsonFile">The path to the JSON file.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task<FPGA?> ReadFPGAJsonFileAsync(string JsonFile)
    {
        try
        {
            string json = await File.ReadAllTextAsync(JsonFile);
            return JsonSerializer.Deserialize<FPGA>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a list of all FPGA JSON file paths from the devices directory.
    /// </summary>
    /// <returns>A list of full file paths to FPGA JSON files.</returns>
    public async Task<List<string>> GetFPGAsAsync()
    {
        try
        {
            _logger.LogInformation($"[JSONFileReader] GetFPGAsAsync - Checking path: {_devicesPath}");
            if (!Directory.Exists(_devicesPath))
            {
                _logger.LogInformation($"[JSONFileReader] GetFPGAsAsync - Directory does not exist!");
                return new List<string>();
            }

            var fpgaFiles = Directory.GetFiles(_devicesPath, "*.json", SearchOption.AllDirectories)
                .OrderBy(f => Path.GetFileNameWithoutExtension(f))
                .ToList();

            // Merge in user-imported FPGA files on top of the bundled ones.
            if (_userAssets != null)
            {
                var userFiles = _userAssets.ListUserFpgaPaths();
                if (userFiles.Count > 0)
                {
                    fpgaFiles.AddRange(userFiles);
                    _logger.LogInformation("[JSONFileReader] Added {Count} user-imported FPGA files", userFiles.Count);
                }
            }

            _logger.LogInformation($"[JSONFileReader] GetFPGAsAsync - Found {fpgaFiles.Count} files");
            return await Task.FromResult(fpgaFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[JSONFileReader] Error reading FPGA files: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets a dictionary of all FPGAs with their model names as keys and file paths as values.
    /// </summary>
    /// <returns>A dictionary mapping FPGA model names to file paths.</returns>
    public async Task<Dictionary<string, string>> GetFPGAsDictionaryAsync()
    {
        try
        {
            var files = await GetFPGAsAsync();
            var fpgaDictionary = new Dictionary<string, string>();

            foreach (var file in files)
            {
                var fpga = await ReadFPGAJsonFileAsync(file);
                if (fpga != null && !string.IsNullOrEmpty(fpga.Model))
                {
                    fpgaDictionary[fpga.Model] = file;
                }
                else
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    fpgaDictionary[fileName] = file;
                }
            }

            return fpgaDictionary.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating FPGA dictionary: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }
#else
    // Known FPGA device files bundled with the app
    private static readonly string[] _knownDevices = new[]
    {
        // Intel/Altera Cyclone IV E
        "EP4CE6", "EP4CE10", "EP4CE15", "EP4CE22", "EP4CE30", "EP4CE40", "EP4CE55", "EP4CE75", "EP4CE115",
        // Lattice iCE40
        "iCE40HX1K", "iCE40HX4K", "iCE40HX8K", "iCE40UP3K", "iCE40UP5K",
        // Lattice ECP5
        "LFE5U-12", "LFE5U-25", "LFE5U-45", "LFE5U-85",
        // Xilinx Spartan-6
        "XC6SLX4", "XC6SLX9", "XC6SLX16", "XC6SLX25", "XC6SLX45", "XC6SLX75", "XC6SLX100", "XC6SLX150",
        // Xilinx Artix-7
        "XC7A12T", "XC7A15T", "XC7A25T", "XC7A35T", "XC7A50T", "XC7A75T", "XC7A100T", "XC7A200T"
    };

    private readonly Func<string, Task<Stream>>? _openPackageFileAsync;

    /// <summary>
    /// Initializes a new instance for MAUI environments.
    /// </summary>
    /// <param name="openPackageFileAsync">
    /// A function to open app package files. In MAUI, pass:
    /// path => FileSystem.OpenAppPackageFileAsync(path)
    /// </param>
    public JSONFileReader(Func<string, Task<Stream>>? openPackageFileAsync = null,
                          Microsoft.Extensions.Logging.ILogger<JSONFileReader>? logger = null,
                          UserAssetStore? userAssets = null)
    {
        _openPackageFileAsync = openPackageFileAsync;
        if (logger != null) _logger = logger;
        _userAssets = userAssets;
    }

    /// <summary>
    /// Reads an FPGA JSON file and deserializes it to an <see cref="FPGA"/> object.
    /// </summary>
    /// <param name="deviceNameOrPath">The device name (e.g., "EP4CE10") or full path.</param>
    /// <returns>The deserialized FPGA object, or null if an error occurred.</returns>
    public async Task<FPGA?> ReadFPGAJsonFileAsync(string deviceNameOrPath)
    {
        try
        {
            // If the caller handed us an absolute path (user-imported FPGA
            // under UserAssets/Devices, or any path-shaped string), skip the
            // app-package lookup and read straight from disk.
            if (Path.IsPathRooted(deviceNameOrPath) && File.Exists(deviceNameOrPath))
            {
                string directJson = await File.ReadAllTextAsync(deviceNameOrPath);
                return JsonSerializer.Deserialize<FPGA>(directJson);
            }

            string deviceName = Path.GetFileNameWithoutExtension(deviceNameOrPath);
            string resourcePath = $"Devices/{deviceName}.json";

            if (_openPackageFileAsync != null)
            {
                using Stream stream = await _openPackageFileAsync(resourcePath);
                using StreamReader reader = new(stream);
                string json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<FPGA>(json);
            }
            else
            {
                // Fallback to file system (for desktop without MAUI packaging)
                string basePath = AppContext.BaseDirectory;
                string filePath = Path.Combine(basePath, resourcePath);
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    return JsonSerializer.Deserialize<FPGA>(json);
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading FPGA file '{deviceNameOrPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a list of all known FPGA device names plus any user-imported
    /// FPGA files (as absolute paths — <see cref="ReadFPGAJsonFileAsync"/>
    /// handles both shapes).
    /// </summary>
    public Task<List<string>> GetFPGAsAsync()
    {
        var all = _knownDevices.OrderBy(d => d).Cast<string>().ToList();
        if (_userAssets != null)
        {
            var userFiles = _userAssets.ListUserFpgaPaths();
            if (userFiles.Count > 0)
            {
                all.AddRange(userFiles);
                _logger.LogInformation("[JSONFileReader] Added {Count} user-imported FPGA files", userFiles.Count);
            }
        }
        return Task.FromResult(all);
    }

    /// <summary>
    /// Gets a dictionary of all FPGAs with their model names as keys.
    /// </summary>
    /// <returns>A dictionary mapping FPGA model names to device identifiers.</returns>
    public async Task<Dictionary<string, string>> GetFPGAsDictionaryAsync()
    {
        try
        {
            var fpgaDictionary = new Dictionary<string, string>();

            // Use the merged list so user-imported FPGAs appear in the picker.
            var entries = await GetFPGAsAsync();
            foreach (var entry in entries)
            {
                var fpga = await ReadFPGAJsonFileAsync(entry);
                if (fpga != null && !string.IsNullOrEmpty(fpga.Model))
                {
                    fpgaDictionary[fpga.Model] = entry;
                }
                else
                {
                    var key = Path.GetFileNameWithoutExtension(entry);
                    fpgaDictionary[key] = entry;
                }
            }

            return fpgaDictionary.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating FPGA dictionary: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }
#endif
}
