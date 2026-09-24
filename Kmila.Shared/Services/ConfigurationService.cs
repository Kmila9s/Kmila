using Kmila.Shared.Data;
using Kmila.Shared.Models;

namespace Kmila.Shared.Services;

/// <summary>
/// Service for managing Kmila simulation configurations (.k-config files).
/// Provides functionality to save, load, export, and import configurations.
/// </summary>
public class ConfigurationService
{
    // Database context for persisting configurations as key-value settings
    private readonly ApplicationDbContext _dbContext;
    // File extension used for exported configuration files
    private const string CONFIG_EXTENSION = ".k-config";
    // Storage key for tracking the most recently used configuration
    private const string LAST_CONFIG_KEY = "KMILA_LAST_CONFIG";

    /// <summary>
    /// Event raised when configurations are updated.
    /// </summary>
    public event Action? OnConfigurationsChanged;

    /// <summary>
    /// Gets the last error message from an operation.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the ConfigurationService.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public ConfigurationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new configuration from the current simulation state.
    /// </summary>
    /// <param name="name">The name for the configuration.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="fpga">The selected FPGA.</param>
    /// <param name="fpgaPath">The FPGA file path.</param>
    /// <param name="timeValue">The simulation time value.</param>
    /// <param name="timeScale">The time scale.</param>
    /// <param name="portMoments">The port moment configurations.</param>
    /// <returns>A new KmilaConfiguration instance.</returns>
    public KmilaConfiguration CreateConfiguration(
        string name,
        string? description,
        FPGA? fpga,
        string fpgaPath,
        int timeValue,
        string timeScale,
        List<PortMomentConfiguration>? portMoments = null)
    {
        var config = new KmilaConfiguration
        {
            Name = name,
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Time = new TimeConfiguration
            {
                Value = timeValue,
                Scale = timeScale
            },
            PortMoments = portMoments ?? new List<PortMomentConfiguration>()
        };

        if (fpga != null)
        {
            config.FPGA = new FPGAConfiguration
            {
                Model = fpga.Model ?? string.Empty,
                Family = fpga.Family ?? string.Empty,
                Manufacturer = fpga.Manufacturer ?? string.Empty,
                Path = fpgaPath
            };
        }

        return config;
    }

    /// <summary>
    /// Saves a configuration to storage.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> SaveConfigurationAsync(KmilaConfiguration config)
    {
        try
        {
            config.ModifiedAt = DateTime.UtcNow;
            var json = config.ToJson();
            var key = $"CONFIG_{config.Name.Replace(" ", "_")}";
            await _dbContext.SetSettingAsync(key, json);
            await _dbContext.SetSettingAsync(LAST_CONFIG_KEY, key);
            OnConfigurationsChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Loads a configuration by name.
    /// </summary>
    /// <param name="name">The configuration name.</param>
    /// <returns>The loaded configuration, or null if not found.</returns>
    public async Task<KmilaConfiguration?> LoadConfigurationAsync(string name)
    {
        try
        {
            var key = $"CONFIG_{name.Replace(" ", "_")}";
            var json = await _dbContext.GetSettingAsync(key);
            if (string.IsNullOrEmpty(json))
            {
                LastError = "Configuration not found";
                return null;
            }
            return KmilaConfiguration.FromJson(json);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Gets the last used configuration.
    /// </summary>
    /// <returns>The last configuration, or null if none exists.</returns>
    public async Task<KmilaConfiguration?> GetLastConfigurationAsync()
    {
        try
        {
            var lastKey = await _dbContext.GetSettingAsync(LAST_CONFIG_KEY);
            if (string.IsNullOrEmpty(lastKey))
                return null;

            var json = await _dbContext.GetSettingAsync(lastKey);
            if (string.IsNullOrEmpty(json))
                return null;

            return KmilaConfiguration.FromJson(json);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Lists all saved configuration names.
    /// </summary>
    /// <returns>List of configuration names.</returns>
    public async Task<List<string>> GetConfigurationNamesAsync()
    {
        try
        {
            var settings = await _dbContext.Settings.ToListAsync();
            return settings
                .Where(s => s.Key.StartsWith("CONFIG_"))
                .Select(s => s.Key.Replace("CONFIG_", "").Replace("_", " "))
                .OrderBy(n => n)
                .ToList();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return new List<string>();
        }
    }

    /// <summary>
    /// Deletes a configuration by name.
    /// </summary>
    /// <param name="name">The configuration name to delete.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteConfigurationAsync(string name)
    {
        try
        {
            var key = $"CONFIG_{name.Replace(" ", "_")}";
            var setting = await _dbContext.Settings.Where(s => s.Key == key).FirstOrDefaultAsync();
            if (setting != null)
            {
                await _dbContext.DeleteAsync(setting);
                OnConfigurationsChanged?.Invoke();
                return true;
            }
            LastError = "Configuration not found";
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Exports a configuration to a .k-config file content string.
    /// </summary>
    /// <param name="config">The configuration to export.</param>
    /// <returns>The JSON content for the .k-config file.</returns>
    public string ExportConfiguration(KmilaConfiguration config)
    {
        return config.ToJson();
    }

    /// <summary>
    /// Imports a configuration from a .k-config file content string.
    /// </summary>
    /// <param name="content">The JSON content from a .k-config file.</param>
    /// <returns>The imported configuration, or null if parsing fails.</returns>
    public KmilaConfiguration? ImportConfiguration(string content)
    {
        var config = KmilaConfiguration.FromJson(content);
        if (config == null)
        {
            LastError = "Invalid configuration file format";
        }
        return config;
    }

    /// <summary>
    /// Gets the file extension for configuration files.
    /// </summary>
    public static string FileExtension => CONFIG_EXTENSION;
}
