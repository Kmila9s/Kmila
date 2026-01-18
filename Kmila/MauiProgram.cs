using Microsoft.Extensions.Logging;
using Kmila.Shared.Services;
using Kmila.Shared.Data;
using Kmila.Shared.Repositories;
using Kmila.Shared.Models;
using Kmila.Services;

namespace Kmila;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add device-specific services used by the Kmila.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddSingleton<ApplicationDbContext>(provider =>
        {
            string filePath = "Kmila.db";
			string target = Path.Combine(FileSystem.AppDataDirectory, filePath);
			Task.Run(async () => {
				if(!File.Exists(target)){
					try{
						using Stream source = await FileSystem.OpenAppPackageFileAsync(filePath);
						using FileStream destination = File.Create(target);
						await source.CopyToAsync(destination);
					}catch(Exception ex){
						Console.WriteLine(ex.Message);
						File.Create(target).Dispose();
					}
				}
			}).Wait();
			return new(target);
        });
        builder.Services.AddSingleton<Projects>();
        builder.Services.AddSingleton<ProjectFiles>();

        // JSONFileReader with MAUI's FileSystem for reading bundled FPGA device files
        builder.Services.AddSingleton<JSONFileReader>(provider =>
            new JSONFileReader(path => FileSystem.OpenAppPackageFileAsync(path)));

        builder.Services.AddSingleton<ProgramBuilder>();

        builder.Services.AddSingleton<SelectedFPGA>();

        // Configuration service for saving/loading simulation configurations
        builder.Services.AddSingleton<ConfigurationService>();

        // Translation service with MAUI's FileSystem for reading bundled dictionary files
        // The path should be "Dictionaries/{lang}.json" which maps to Resources/Raw/Dictionaries/
        builder.Services.AddSingleton<Traductor>(provider =>
            new Traductor(async path =>
            {
                try
                {
                    // Primary path: Resources/Raw/Dictionaries/{lang}.json (accessed as Dictionaries/{lang}.json)
                    var stream = await FileSystem.OpenAppPackageFileAsync(path);
                    Console.WriteLine($"[Traductor] Successfully loaded dictionary from: {path}");
                    return stream;
                }
                catch (FileNotFoundException)
                {
                    // Try alternative path without Dictionaries prefix (in case files are at root of Raw)
                    var fileName = Path.GetFileName(path);
                    try
                    {
                        var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                        Console.WriteLine($"[Traductor] Loaded dictionary from fallback path: {fileName}");
                        return stream;
                    }
                    catch
                    {
                        Console.WriteLine($"[Traductor] Dictionary file not found at: {path} or {fileName}");
                        throw new FileNotFoundException($"Dictionary not found: {path}");
                    }
                }
            }));

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddBlazorBootstrap();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
