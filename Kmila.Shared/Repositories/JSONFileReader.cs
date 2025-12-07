using System.Text.Json;
using Kmila.Shared.Models;
#if LINUX || ASPNETCORE
using Microsoft.AspNetCore.Hosting;
#endif

namespace Kmila.Shared.Repositories;
/// <summary>
/// A repository for reading FPGA data from JSON files.
/// </summary>
public class JSONFileReader
{
    private readonly string _devicesPath;
#if LINUX || ASPNETCORE
    private readonly IWebHostEnvironment _env;
    /// <summary>
    /// Initializes a new instance of the <see cref="JSONFileReader"/> class for web environments.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    public JSONFileReader(IWebHostEnvironment env)
    {
        _env = env;
        _devicesPath = Path.Combine(_env.WebRootPath, "Devices");
    }
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="JSONFileReader"/> class for local environments.
    /// </summary>
    public JSONFileReader()
    {
        // MAUI or other local case
        _devicesPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "Devices");
    }
#endif
    /// <summary>
    /// Reads an FPGA JSON file and deserializes it to an <see cref="FPGA"/> object.
    /// </summary>
    /// <param name="JsonFile">The path to the JSON file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized <see cref="FPGA"/> object, or null if an error occurred.</returns>
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
    /// Only returns files with .json extension, sorted alphabetically.
    /// Searches recursively through all subdirectories.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of full file paths to FPGA JSON files.</returns>
    public async Task<List<string>> GetFPGAsAsync()
    {
        try
        {
            if (!Directory.Exists(_devicesPath))
                return new List<string>();

            // Get all JSON files from the devices directory and subdirectories
            var fpgaFiles = Directory.GetFiles(_devicesPath, "*.json", SearchOption.AllDirectories)
                .OrderBy(f => Path.GetFileNameWithoutExtension(f))
                .ToList();

            return await Task.FromResult(fpgaFiles);
        }
        catch (Exception ex)
        {
            // Log the exception if needed (add logging service if available)
            Console.WriteLine($"Error reading FPGA files: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets a dictionary of all FPGAs with their model names as keys and file paths as values.
    /// Useful for displaying FPGAs in dropdowns with friendly names.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary mapping FPGA model names to file paths.</returns>
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
                    // Use the FPGA model name from the JSON as the key
                    fpgaDictionary[fpga.Model] = file;
                }
                else
                {
                    // Fallback to filename if model is not available
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    fpgaDictionary[fileName] = file;
                }
            }

            return fpgaDictionary.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating FPGA dictionary: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }
}