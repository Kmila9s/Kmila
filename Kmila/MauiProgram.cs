using Microsoft.Extensions.Logging;
using Kmila.Shared.Services;
using Kmila.Shared.Data;
using Kmila.Shared.Repositories;
using Kmila.Shared.Models;
using Kmila.Services;

namespace Kmila;

/// <summary>
/// Static class responsible for building and configuring the MAUI Blazor Hybrid application.
/// Registers all services, fonts, and platform-specific dependencies.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the <see cref="MauiApp"/> instance with all required services,
    /// including database access, FPGA device support, translations, and Blazor components.
    /// </summary>
    /// <returns>A fully configured <see cref="MauiApp"/> ready to run.</returns>
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

        // Register the SQLite database context as a singleton.
        // On first launch, copies the bundled database from the app package to the
        // writable AppDataDirectory. If the bundled file is missing, creates an empty database.
        builder.Services.AddSingleton<ApplicationDbContext>(provider =>
        {
            string filePath = "Kmila.db";
			string target = Path.Combine(FileSystem.AppDataDirectory, filePath);
			Task.Run(async () => {
				// Only copy if the database does not already exist in the app data directory
				if(!File.Exists(target)){
					try{
						// Copy the bundled database from Resources/Raw to writable storage
						using Stream source = await FileSystem.OpenAppPackageFileAsync(filePath);
						using FileStream destination = File.Create(target);
						await source.CopyToAsync(destination);
					}catch(Exception ex){
						// If the bundled file is missing, create an empty database file
						Console.WriteLine(ex.Message);
						File.Create(target).Dispose();
					}
				}
			}).Wait();
			return new(target);
        });
        // Repository services for managing user projects and their associated files
        builder.Services.AddSingleton<Projects>();
        builder.Services.AddSingleton<ProjectFiles>();

        // JSONFileReader with MAUI's FileSystem for reading bundled FPGA device files
        builder.Services.AddSingleton<JSONFileReader>(provider =>
            new JSONFileReader(path => FileSystem.OpenAppPackageFileAsync(path)));

        // ProgramBuilder handles compiling/building FPGA programs from user projects
        builder.Services.AddSingleton<ProgramBuilder>();

        // Tracks the currently selected FPGA device across the application
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

        // Register the Blazor WebView for hosting Razor components inside MAUI
        builder.Services.AddMauiBlazorWebView();

        // BlazorBootstrap provides Bootstrap-based Blazor UI components
        builder.Services.AddBlazorBootstrap();

        // Enable Blazor developer tools and debug logging in DEBUG builds only
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
