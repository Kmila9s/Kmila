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
        _devicesPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Raw", "Devices");
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
    /// Gets a list of all FPGAs from the JSON files in the devices directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="FPGA"/> objects.</returns>
    public async Task<List<FPGA?>> GetFPGAsAsync()
    {
        if (!Directory.Exists(_devicesPath))
            return new();
        var fpgasFiles = Directory.GetFiles(_devicesPath);
        var tasks = fpgasFiles.Select(ReadFPGAJsonFileAsync);
        await Task.WhenAll(tasks);
        return tasks.Select(t => t.Result).ToList();
    }
}