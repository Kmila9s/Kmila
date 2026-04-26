using Kmila.Web.Components;
using Kmila.Shared.Services;
using Kmila.Web.Services;
using Kmila.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Kmila.Shared.Data;
using Kmila.Shared.Models;

namespace Kmila;

/// <summary>
/// Entry point class for the Kmila Blazor Web application.
/// Configures services, middleware, and the HTTP request pipeline.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the Blazor Web application.
    /// Configures all required services (database, translations, FPGA tools)
    /// and sets up the ASP.NET Core middleware pipeline.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Add device-specific services used by the Kmila.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        /*builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "kmila.db");
            options.UseSqlite($"Data Source={dbPath}");
        });*/
        // Register the SQLite database context using a scoped lifetime.
        //
        // We anchor the DB in the user's ApplicationData folder (KmilaData/)
        // rather than Directory.GetCurrentDirectory() so launches from
        // different shells / IDE roots always open the *same* DB. The old
        // "cwd-relative" path meant starting `dotnet run` from a different
        // directory effectively handed the app a fresh empty database and
        // every project the user had saved appeared to have vanished.
        //
        // Paths:
        //   macOS   →  ~/Library/Application Support/KmilaData/kmila.db
        //   Linux   →  ~/.config/KmilaData/kmila.db        (XDG default)
        //   Windows →  %APPDATA%\KmilaData\kmila.db
        builder.Services.AddScoped<ApplicationDbContext>(sp =>
        {
            var root = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "KmilaData");
            Directory.CreateDirectory(root);
            var dbPath = Path.Combine(root, "kmila.db");
            return new(dbPath);
        });

        // Repository services for managing user projects and their associated files
        builder.Services.AddScoped<Projects>();
        builder.Services.AddScoped<ProjectFiles>();

        // User-imported FPGA / language assets — a writable companion to the
        // bundled wwwroot/Devices and wwwroot/Dictionaries folders.
        builder.Services.AddSingleton<UserAssetStore>();

        // JSONFileReader needs IWebHostEnvironment to find the Devices folder.
        // Merge in the user asset store so imported FPGAs show up in the picker.
        builder.Services.AddSingleton<JSONFileReader>(provider =>
            new JSONFileReader(
                provider.GetRequiredService<IWebHostEnvironment>(),
                logger: provider.GetService<ILogger<JSONFileReader>>(),
                userAssets: provider.GetRequiredService<UserAssetStore>()));

        // ProgramBuilder handles compiling/building FPGA programs from user projects
        builder.Services.AddSingleton<ProgramBuilder>();

        // Tracks the currently selected FPGA device across the application
        builder.Services.AddSingleton<SelectedFPGA>();

        // Configuration service for saving/loading simulation configurations
        builder.Services.AddScoped<ConfigurationService>();

        // Shared UI state for the custom sidebar (collapse / mobile drawer)
        builder.Services.AddScoped<SidebarState>();

        // Shared state for the Editor bottom dock (active tab + collapse)
        builder.Services.AddScoped<EditorDockState>();

        // Run-bar state (time/scale/run-state/progress/port moments) + build log
        builder.Services.AddScoped<SimulationParameters>();
        builder.Services.AddScoped<BuildLog>();

        // In-app logs (/logs page). Construct the store eagerly so we can hand
        // the same instance to the logger provider and to the DI container —
        // .NET's logging pipeline doesn't hand out IServiceProvider at the
        // moment providers are registered.
        var appLog = new AppLogStore();
        builder.Services.AddSingleton(appLog);
        builder.Logging.AddProvider(new InMemoryLoggerProvider(appLog));

        // Translation service for Web - reads from wwwroot/Dictionaries and,
        // before falling back to the bundled copies, from the user's
        // UserAssets/Dictionaries directory so imported language packs win.
        builder.Services.AddSingleton<Traductor>(provider =>
        {
            var env        = provider.GetRequiredService<IWebHostEnvironment>();
            var userAssets = provider.GetRequiredService<UserAssetStore>();
            var logger     = provider.GetRequiredService<ILoggerFactory>()
                                     .CreateLogger("Kmila.Shared.Services.Traductor");

            // Register every user-supplied language pack so the picker lists
            // them before the first Traductor instance is used.
            foreach (var userLang in userAssets.ListUserLanguagePaths())
            {
                var code = Path.GetFileNameWithoutExtension(userLang);
                var display = UserAssetStore.ReadLanguageDisplayName(userLang);
                Traductor.AddUserLanguage(display, code);
            }

            return new Traductor(async path =>
            {
                // path comes as "Dictionaries/{lang}.json"
                var pathsToTry = new List<string>();

                // Highest priority: user-imported language pack (only the
                // filename matters — Dictionaries/ prefix is stripped).
                var fileName = Path.GetFileName(path);
                pathsToTry.Add(Path.Combine(userAssets.UserDictionariesDir, fileName));

                // Development: Direct path to Kmila.Shared project (most reliable during development)
                pathsToTry.Add(Path.Combine(env.ContentRootPath, "..", "Kmila.Shared", "wwwroot", path));

                // Alternative: Relative to current directory
                pathsToTry.Add(Path.Combine(Directory.GetCurrentDirectory(), "..", "Kmila.Shared", "wwwroot", path));

                // Production: RCL static files at _content/Kmila.Shared/ (only if wwwroot exists)
                if (!string.IsNullOrEmpty(env.WebRootPath) && Directory.Exists(env.WebRootPath))
                {
                    pathsToTry.Add(Path.Combine(env.WebRootPath, "_content", "Kmila.Shared", path));
                    pathsToTry.Add(Path.Combine(env.WebRootPath, path));
                }

                logger.LogDebug("[Traductor-Web] Looking for: {Path}", path);
                logger.LogDebug("[Traductor-Web] ContentRootPath: {Root}", env.ContentRootPath);
                logger.LogDebug("[Traductor-Web] WebRootPath: {WebRoot}", env.WebRootPath ?? "(null)");

                foreach (var fullPath in pathsToTry)
                {
                    try
                    {
                        var normalizedPath = Path.GetFullPath(fullPath);
                        logger.LogDebug("[Traductor-Web] Checking: {Path}", normalizedPath);
                        if (File.Exists(normalizedPath))
                        {
                            logger.LogInformation("[Traductor-Web] Found dictionary at: {Path}", normalizedPath);
                            return File.OpenRead(normalizedPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[Traductor-Web] Error checking path {Path}", fullPath);
                    }
                }

                var triedPaths = string.Join("\n  - ", pathsToTry.Select(p => {
                    try { return Path.GetFullPath(p); }
                    catch { return p; }
                }));
                throw new FileNotFoundException($"Dictionary file not found. Tried:\n  - {triedPaths}");
            });
        });

        // Learn / curriculum catalog — reuses the same "try a handful of paths"
        // approach Traductor uses for Dictionaries, so manifest.json + lesson
        // bodies load in both dev and production Web deployments.
        builder.Services.AddSingleton<LessonCatalog>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var logger = provider.GetRequiredService<ILoggerFactory>()
                                 .CreateLogger<LessonCatalog>();
            return new LessonCatalog(async path =>
            {
                var pathsToTry = new List<string>
                {
                    Path.Combine(env.ContentRootPath, "..", "Kmila.Shared", "wwwroot", path),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "Kmila.Shared", "wwwroot", path)
                };
                if (!string.IsNullOrEmpty(env.WebRootPath) && Directory.Exists(env.WebRootPath))
                {
                    pathsToTry.Add(Path.Combine(env.WebRootPath, "_content", "Kmila.Shared", path));
                    pathsToTry.Add(Path.Combine(env.WebRootPath, path));
                }
                foreach (var candidate in pathsToTry)
                {
                    try
                    {
                        var normalized = Path.GetFullPath(candidate);
                        if (File.Exists(normalized)) return File.OpenRead(normalized);
                    }
                    catch { /* try next */ }
                }
                throw new FileNotFoundException($"Lesson asset not found: {path}");
            }, logger);
        });

        // Progress tracker for lessons — scoped so each circuit gets its own
        // in-memory cache layered over the shared AppSettings table.
        builder.Services.AddScoped<LessonProgressStore>();

        // Sidebar MRU — scoped so it reads from the same per-circuit
        // ApplicationDbContext the rest of the UI is using.
        builder.Services.AddScoped<RecentProjectsTracker>();

        // Captured-run history store — hybrid SQLite metadata + inline
        // WaveformData JSON. Reads by Runs tab, writes after every sim.
        builder.Services.AddScoped<SimulationSnapshotStore>();

        // .kmila project bundle export / import. Scoped so the inner
        // repositories resolve to the current circuit's DB context.
        builder.Services.AddScoped<ProjectBundleService>();

        // BlazorBootstrap provides Bootstrap-based Blazor UI components
        builder.Services.AddBlazorBootstrap();

        var app = builder.Build();

        // Redirect Console.Out into an ILogger so that Console.WriteLine calls
        // from the referenced Parser / Interpreter / TimeMachine projects (e.g.
        // "[VariableSyntetizer] …", "[Resources.IO] …") land in the in-app
        // /logs viewer at Debug level instead of spamming the terminal.
        // Keep Console.Error untouched — real errors should still surface loudly.
        var consoleLogger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Console");
        Console.SetOut(new Kmila.Shared.Services.ConsoleToLoggerWriter(
            consoleLogger, LogLevel.Debug));

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        // Map Razor components with interactive server-side rendering
        // and include the shared component library (Kmila.Shared)
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(Kmila.Shared._Imports).Assembly);

        app.Run();
    }
}
