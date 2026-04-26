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

        // User-imported FPGA / language assets — a writable directory under
        // FileSystem.AppDataDirectory that survives app updates.
        builder.Services.AddSingleton<UserAssetStore>();

        // JSONFileReader with MAUI's FileSystem for reading bundled FPGA device
        // files. The UserAssetStore injection adds user-imported FPGAs into
        // GetFPGAsAsync, and ReadFPGAJsonFileAsync reads absolute paths
        // directly from disk when they come from that store.
        builder.Services.AddSingleton<JSONFileReader>(provider =>
            new JSONFileReader(
                openPackageFileAsync: path => FileSystem.OpenAppPackageFileAsync(path),
                logger: provider.GetService<ILogger<JSONFileReader>>(),
                userAssets: provider.GetRequiredService<UserAssetStore>()));

        // ProgramBuilder handles compiling/building FPGA programs from user projects
        builder.Services.AddSingleton<ProgramBuilder>();

        // Tracks the currently selected FPGA device across the application
        builder.Services.AddSingleton<SelectedFPGA>();

        // Configuration service for saving/loading simulation configurations
        builder.Services.AddSingleton<ConfigurationService>();

        // Shared UI state for the custom sidebar (collapse / mobile drawer)
        builder.Services.AddSingleton<SidebarState>();

        // Shared state for the Editor bottom dock (active tab + collapse)
        builder.Services.AddSingleton<EditorDockState>();

        // Run-bar state (time/scale/run-state/progress/port moments) + build log
        builder.Services.AddSingleton<SimulationParameters>();
        builder.Services.AddSingleton<BuildLog>();

        // In-app logs (/logs page) — same instance shared between DI and the
        // logger provider so every ILogger<T> write lands in AppLogStore.
        var appLog = new AppLogStore();
        builder.Services.AddSingleton(appLog);
        builder.Logging.AddProvider(new InMemoryLoggerProvider(appLog));

        // Translation service with MAUI's FileSystem. Looks in the user-assets
        // Dictionaries directory first (so imported packs win and imported
        // *new* language codes resolve), then falls back to bundled Resources/
        // Raw/Dictionaries/{lang}.json.
        builder.Services.AddSingleton<Traductor>(provider =>
        {
            var userAssets = provider.GetRequiredService<UserAssetStore>();

            // Discover user languages before the first Traductor call.
            foreach (var userLang in userAssets.ListUserLanguagePaths())
            {
                var code = Path.GetFileNameWithoutExtension(userLang);
                var display = UserAssetStore.ReadLanguageDisplayName(userLang);
                Traductor.AddUserLanguage(display, code);
            }

            return new Traductor(async path =>
            {
                var fileName = Path.GetFileName(path);

                // 1. User-imported dictionaries win over bundled ones.
                var userPath = Path.Combine(userAssets.UserDictionariesDir, fileName);
                if (File.Exists(userPath))
                {
                    return File.OpenRead(userPath);
                }

                try
                {
                    // 2. Primary bundled path: Resources/Raw/Dictionaries/{lang}.json.
                    return await FileSystem.OpenAppPackageFileAsync(path);
                }
                catch (FileNotFoundException)
                {
                    // 3. Alternative: files sometimes live at the root of Raw.
                    try
                    {
                        return await FileSystem.OpenAppPackageFileAsync(fileName);
                    }
                    catch
                    {
                        throw new FileNotFoundException($"Dictionary not found: {path}");
                    }
                }
            });
        });

        // Curriculum catalog — reads Learn/manifest.json + per-lesson Markdown
        // from the app package via FileSystem.OpenAppPackageFileAsync.
        builder.Services.AddSingleton<LessonCatalog>(provider =>
            new LessonCatalog(
                async path =>
                {
                    try
                    {
                        return await FileSystem.OpenAppPackageFileAsync(path);
                    }
                    catch (FileNotFoundException)
                    {
                        throw new FileNotFoundException($"Lesson asset not found: {path}");
                    }
                },
                provider.GetRequiredService<ILoggerFactory>().CreateLogger<LessonCatalog>()));

        // Progress tracker for lessons — scoped so repeated navigations reuse
        // the hydrated cache but a cold start still reads from SQLite.
        builder.Services.AddScoped<LessonProgressStore>();

        // Sidebar MRU — caps the sidebar project list at 10 most-recent.
        builder.Services.AddScoped<RecentProjectsTracker>();

        // Captured-run history: every simulation lands in SQLite so the Runs
        // dock tab can replay and compare past executions.
        builder.Services.AddScoped<SimulationSnapshotStore>();

        // Single-file .kmila project bundle — export/import all state in one JSON blob.
        builder.Services.AddScoped<ProjectBundleService>();

        // Register the Blazor WebView for hosting Razor components inside MAUI
        builder.Services.AddMauiBlazorWebView();

        // BlazorBootstrap provides Bootstrap-based Blazor UI components
        builder.Services.AddBlazorBootstrap();

        // Enable Blazor developer tools and debug logging in DEBUG builds only
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Redirect Console.Out into an ILogger so that Console.WriteLine calls
        // from the referenced Parser / Interpreter / TimeMachine projects land
        // in the in-app /logs viewer at Debug level instead of the (invisible
        // on mobile) platform console.
        var consoleLogger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Console");
        Console.SetOut(new Kmila.Shared.Services.ConsoleToLoggerWriter(
            consoleLogger, LogLevel.Debug));

        return app;
    }
}
