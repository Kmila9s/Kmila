using Kmila.Web.Components;
using Kmila.Shared.Services;
using Kmila.Shared.Services.Export;
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
        //
        // MaximumReceiveMessageSize: Blazor Server's default is 32 KB, which
        // is fine for typical JSInterop chatter but kills the SignalR circuit
        // when JS returns a serialised mermaid SVG (~1-3 MB for a flow with
        // 800+ nodes) or a base64-encoded PNG export (~5-20 MB). The circuit
        // crashes with "Server returned an error on close" and the export
        // silently fails. Lift the cap to 64 MB so Flow / Schematic / PNG
        // exports for non-trivial designs make it through the wire. Backlog #2.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddHubOptions(o => o.MaximumReceiveMessageSize = 64L * 1024 * 1024);

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

        // VHDL library compiler — owns IEEE builtins + user-imported library
        // packs, persists compiled artifacts under
        // UserAssetStore.LibraryCacheDir so subsequent app starts skip the
        // cold build. The OpenLibrarySource callback handles two cases: an
        // absolute path (user plugin under UserLibrariesDir) opens with
        // File.OpenRead; an embedded-resource key (Builtins/IEEE/*.vhd
        // shipped by the LibraryCompiler module) returns null so the
        // module's own embedded-resource fallback takes over.
        builder.Services.AddSingleton<LibraryCompiler.Services.LibraryCompiler>(provider =>
        {
            var userAssets = provider.GetRequiredService<UserAssetStore>();
            var blobLogger = provider.GetService<ILogger<FileSystemBlobStore>>();
            var blobStore  = new FileSystemBlobStore(userAssets.LibraryCacheDir, blobLogger);

            return new LibraryCompiler.Services.LibraryCompiler(
                new LibraryCompiler.Services.LibraryCompilerOptions(
                    OpenLibrarySource: (key, ct) =>
                    {
                        // SourceKey may carry a #package=X fragment when one
                        // physical .vhd declares several packages. Trim to
                        // just that package's bytes so each compiled artifact
                        // hashes deterministically and survives the addition
                        // / removal of unrelated packages in the same file.
                        var (path, package) = UserAssetStore.ParseLibrarySourceKey(key);
                        if (!string.IsNullOrEmpty(path) && Path.IsPathRooted(path) && File.Exists(path))
                        {
                            var bytes = UserAssetStore.ReadPackageBytes(path, package);
                            if (bytes != null) return Task.FromResult<Stream?>(new MemoryStream(bytes));
                        }
                        // Embedded-resource key for IEEE builtins, or unknown
                        // path — fall through; the LibraryBuilder's embedded
                        // resource fallback handles the IEEE case.
                        return Task.FromResult<Stream?>(null);
                    },
                    CacheStore:        blobStore,
                    AdditionalSources: userAssets.ListUserLibrarySources(),
                    Policy:            LibraryCompiler.Services.LibraryBuildPolicy.AlwaysPersistent));
        });

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

        // Cross-component cursor bus tying the schematic's Debug mode to
        // the waveform viewer (cursor tick + selected net).
        builder.Services.AddScoped<SignalCursorBus>();

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

        // Concepts catalog — execution-models mini-curriculum, sibling to
        // LessonCatalog. Same file-resolution strategy so the manifest +
        // per-language markdown + per-language timeline JSON all load from
        // the RCL's wwwroot/Concepts/ tree.
        builder.Services.AddSingleton<ConceptCatalog>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var logger = provider.GetRequiredService<ILoggerFactory>()
                                 .CreateLogger<ConceptCatalog>();
            return new ConceptCatalog(async path =>
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
                throw new FileNotFoundException($"Concept asset not found: {path}");
            }, logger);
        });

        // Concept progress — scoped sibling to LessonProgressStore, distinct
        // key prefix so the two tracks never collide in AppSettings.
        builder.Services.AddScoped<ConceptProgressStore>();

        // BlockProgram — pure function over Blockly workspace JSON; safe to
        // register as a singleton.
        builder.Services.AddSingleton<BlockProgram>();

        // ReplayState — scoped slot for the most recent step-replay timeline,
        // produced after each simulation in /editor/{id}. The dock's new
        // "Replay" tab subscribes here and rerenders the ExecutionPlayer
        // when a new doc lands. Scoped per circuit so two projects open in
        // two tabs don't trample each other.
        builder.Services.AddScoped<ReplayState>();

        // Practice — coding-challenge catalog + grader. Singletons: the
        // catalog is read-only after manifest load, and the evaluator's
        // expected-waveform cache is fine to share across circuits since
        // exercise IDs are global. Reuses the same "try a handful of paths"
        // approach Traductor / LessonCatalog use so manifest + per-exercise
        // assets load in both dev and production Web deployments.
        builder.Services.AddSingleton<Kmila.Shared.Services.Practice.PracticeCatalog>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var logger = provider.GetRequiredService<ILoggerFactory>()
                                 .CreateLogger<Kmila.Shared.Services.Practice.PracticeCatalog>();
            return new Kmila.Shared.Services.Practice.PracticeCatalog(async path =>
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
                throw new FileNotFoundException($"Practice asset not found: {path}");
            }, logger);
        });
        builder.Services.AddSingleton<Kmila.Shared.Services.Practice.PracticeEvaluator>();

        // Practice persistence — scoped so each circuit shares its
        // ApplicationDbContext. One row per Submit click, plus the
        // aggregate progress chip data.
        builder.Services.AddScoped<Kmila.Shared.Services.Practice.PracticeAttemptStore>();
        // Session export — scoped sibling that reads the store and emits
        // a Markdown report or a ZIP bundle for download.
        builder.Services.AddScoped<Kmila.Shared.Services.Practice.PracticeSessionExportService>();

        // Sidebar MRU — scoped so it reads from the same per-circuit
        // ApplicationDbContext the rest of the UI is using.
        builder.Services.AddScoped<RecentProjectsTracker>();

        // Captured-run history store — hybrid SQLite metadata + inline
        // WaveformData JSON. Reads by Runs tab, writes after every sim.
        builder.Services.AddScoped<SimulationSnapshotStore>();

        // .kmila project bundle export / import. Scoped so the inner
        // repositories resolve to the current circuit's DB context.
        builder.Services.AddScoped<ProjectBundleService>();

        // Cross-platform file export — Web target delegates to the JS
        // anchor-tag download helpers (kmila-shared-head.js). MAUI targets
        // get a filesystem-writing implementation registered in MauiProgram.
        builder.Services.AddScoped<IFileExportService, WebFileExportService>();

        // Per-project editor session — open tabs, active index, split
        // state. Stored in browser localStorage keyed by project id.
        builder.Services.AddScoped<EditorSessionStore>();

        // GitHub Device-Flow OAuth + issue submission. Login is optional;
        // unconfigured Client ID leaves the in-app submit disabled but
        // the Tier 1+2 feedback paths still work.
        builder.Services.AddScoped<GitHubAuth>();

        // Auto error reporter. Eagerly constructed via MainLayout's
        // injection so the subscription is live before pages can throw.
        builder.Services.AddScoped<AutoErrorReporter>();

        // BlazorBootstrap provides Bootstrap-based Blazor UI components
        builder.Services.AddBlazorBootstrap();

        var app = builder.Build();

        // Apply the saved library-build mode (Settings → Build mode) to the
        // singleton LibraryCompiler. Pushed onto a thread-pool thread so
        // the same code path can be shared with the MAUI host without
        // sync-over-async deadlocking the UI startup thread there. The
        // user's first build will use the default Persistent policy if the
        // hydration loses the race; the Settings page re-applies on mount.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var compiler = app.Services.GetRequiredService<LibraryCompiler.Services.LibraryCompiler>();
                var raw      = await db.GetSettingAsync(AppSettings.KEY_LIBRARY_BUILD_MODE);
                var policy   = LibraryBuildModeExtensions.ParseToken(raw);
                compiler.Orchestrator.Policy = LibraryBuildModeExtensions.ToPolicy(policy);
            }
            catch
            {
                // First-launch path: AppSettings table may not exist yet.
            }
        });

        // Keep the LibraryCompiler's resolver in sync with disk: every time
        // UserAssetStore notifies a change (Settings import / delete, or an
        // external file mutation under UserLibrariesDir), reconcile the
        // resolver's user-vendor entries against the current listing. Without
        // this, a deleted plugin would still pass the "library not found"
        // check and produce a confusing fallback warning at build time.
        try
        {
            var compiler   = app.Services.GetRequiredService<LibraryCompiler.Services.LibraryCompiler>();
            var userAssets = app.Services.GetRequiredService<UserAssetStore>();
            userAssets.OnChange += () => userAssets.SyncResolver(compiler.Resolver);
        }
        catch
        {
            // Subscription is best-effort. Settings UI also calls
            // SyncResolver directly after each import / delete as a belt-
            // and-braces fallback.
        }

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
