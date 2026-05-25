using Microsoft.Extensions.Logging;
using Kmila.Shared.Services;
using Kmila.Shared.Services.Export;
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

        // VHDL library compiler — same wiring as the Web host. Persistent
        // policy points at UserAssetStore.LibraryCacheDir under
        // FileSystem.AppDataDirectory, so compiled artifacts survive app
        // updates the same way user-imported FPGAs and language packs do.
        builder.Services.AddSingleton<LibraryCompiler.Services.LibraryCompiler>(provider =>
        {
            var userAssets = provider.GetRequiredService<UserAssetStore>();
            var blobLogger = provider.GetService<ILogger<FileSystemBlobStore>>();
            var blobStore  = new FileSystemBlobStore(userAssets.LibraryCacheDir, blobLogger);

            return new LibraryCompiler.Services.LibraryCompiler(
                new LibraryCompiler.Services.LibraryCompilerOptions(
                    OpenLibrarySource: (key, ct) =>
                    {
                        // Same multi-package #package= fragment handling as
                        // the Web host — see Program.cs for the full
                        // commentary.
                        var (path, package) = UserAssetStore.ParseLibrarySourceKey(key);
                        if (!string.IsNullOrEmpty(path) && Path.IsPathRooted(path) && File.Exists(path))
                        {
                            var bytes = UserAssetStore.ReadPackageBytes(path, package);
                            if (bytes != null) return Task.FromResult<Stream?>(new MemoryStream(bytes));
                        }
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
        builder.Services.AddSingleton<ConfigurationService>();

        // Shared UI state for the custom sidebar (collapse / mobile drawer)
        builder.Services.AddSingleton<SidebarState>();

        // Shared state for the Editor bottom dock (active tab + collapse)
        builder.Services.AddSingleton<EditorDockState>();

        // Cross-component cursor bus tying the schematic's Debug mode to
        // the waveform viewer (cursor tick + selected net).
        builder.Services.AddSingleton<SignalCursorBus>();

        // Run-bar state (time/scale/run-state/progress/port moments) + build log
        builder.Services.AddSingleton<SimulationParameters>();
        builder.Services.AddSingleton<BuildLog>();

        // Persistent bridge from ProgramBuilder engine events into Sim. Must
        // live for the app lifetime so navigating away from the Editor doesn't
        // break the progress / sim-end pipeline. Eagerly resolved by
        // MainLayout via @inject so its constructor (which wires the
        // subscriptions) actually runs at app startup.
        builder.Services.AddSingleton<SimulationCoordinator>();

        // Sim-end notifier — native iOS / Android local notifications. Both
        // platforms request authorization lazily on first fire so the prompt
        // has clear context (the user just finished a sim).
        builder.Services.AddSingleton<ISimEndNotifier, MauiSimEndNotifier>();

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

        // Concepts catalog — execution-models mini-curriculum. Reads its
        // manifest + per-language markdown + timeline JSON from the bundled
        // app package via FileSystem.OpenAppPackageFileAsync — same pattern
        // as LessonCatalog. The MauiAsset glob in Kmila.csproj is what
        // actually ships the files into the bundle.
        builder.Services.AddSingleton<ConceptCatalog>(provider =>
            new ConceptCatalog(
                async path =>
                {
                    try
                    {
                        return await FileSystem.OpenAppPackageFileAsync(path);
                    }
                    catch (FileNotFoundException)
                    {
                        throw new FileNotFoundException($"Concept asset not found: {path}");
                    }
                },
                provider.GetRequiredService<ILoggerFactory>().CreateLogger<ConceptCatalog>()));

        // Concept progress — scoped sibling to LessonProgressStore.
        builder.Services.AddScoped<ConceptProgressStore>();

        // BlockProgram — pure function over Blockly workspace JSON; safe to
        // register as a singleton.
        builder.Services.AddSingleton<BlockProgram>();

        // ReplayState — scoped slot for the most recent step-replay timeline,
        // populated after each Editor simulation run. The dock's "Replay"
        // tab subscribes here.
        builder.Services.AddScoped<ReplayState>();

        // Practice — coding-challenge catalog + grader. The catalog reads
        // its manifest + per-exercise assets from the bundled app package
        // (`Practice/manifest.json`, `Practice/{folder}/*.{vhdl,json,md}`)
        // via FileSystem.OpenAppPackageFileAsync — same pattern as
        // LessonCatalog. Evaluator caches generated expected waveforms per
        // exercise and serializes runs through a semaphore.
        builder.Services.AddSingleton<Kmila.Shared.Services.Practice.PracticeCatalog>(provider =>
            new Kmila.Shared.Services.Practice.PracticeCatalog(
                async path =>
                {
                    try
                    {
                        return await FileSystem.OpenAppPackageFileAsync(path);
                    }
                    catch (FileNotFoundException)
                    {
                        throw new FileNotFoundException($"Practice asset not found: {path}");
                    }
                },
                provider.GetRequiredService<ILoggerFactory>().CreateLogger<Kmila.Shared.Services.Practice.PracticeCatalog>()));
        builder.Services.AddSingleton<Kmila.Shared.Services.Practice.PracticeEvaluator>();

        // Practice persistence — scoped so it shares the circuit's DbContext.
        // Stores every Submit click + the aggregate progress row.
        builder.Services.AddScoped<Kmila.Shared.Services.Practice.PracticeAttemptStore>();
        // Session export — scoped so it composes the same DbContext the
        // store reads from when emitting Markdown/ZIP reports.
        builder.Services.AddScoped<Kmila.Shared.Services.Practice.PracticeSessionExportService>();

        // Sidebar MRU — caps the sidebar project list at 10 most-recent.
        builder.Services.AddScoped<RecentProjectsTracker>();

        // Captured-run history: every simulation lands in SQLite so the Runs
        // dock tab can replay and compare past executions.
        builder.Services.AddScoped<SimulationSnapshotStore>();

        // Single-file .kmila project bundle — export/import all state in one JSON blob.
        builder.Services.AddScoped<ProjectBundleService>();

        // Cross-platform file export — MAUI heads write to
        // ~/Documents/Kmila/<Category>/ on macOS, iOS, and Android (via
        // MediaStore on API 29+). Web target registers WebFileExportService
        // in Kmila.Web/Program.cs which delegates to the JS download helpers.
        builder.Services.AddSingleton<IFileExportService, MauiFileExportService>();

        // Per-project editor session — open tabs, active index, split state.
        // Stored in localStorage (browser/WebView) keyed by project id.
        builder.Services.AddScoped<EditorSessionStore>();

        // GitHub Device-Flow OAuth + issue submission. Login is optional;
        // the auth state propagates via the service's OnChange event so
        // the TopBar pill / Settings card can reactively re-render.
        builder.Services.AddScoped<GitHubAuth>();

        // Auto error reporter — opt-in per Settings → Account toggle,
        // gated additionally on GitHub login. Eagerly constructed by
        // MainLayout so its AppLogStore subscription is live before
        // any page-level code can throw.
        builder.Services.AddScoped<AutoErrorReporter>();

        // Register the Blazor WebView for hosting Razor components inside MAUI
        builder.Services.AddMauiBlazorWebView();

        // BlazorBootstrap provides Bootstrap-based Blazor UI components
        builder.Services.AddBlazorBootstrap();

        // Custom toast service — replaces BB's ToastService at the inject layer
        // (see _Imports.razor). Singleton in MAUI to match the host's lifetime
        // (matches the surrounding state-service pattern in this file).
        builder.Services.AddSingleton<KmilaToastService>();

        // Enable Blazor developer tools and debug logging in DEBUG builds only
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Apply the saved library-build mode so the FIRST build of a session
        // honours the user's Settings → Build mode choice. The DB read is
        // pushed onto a thread-pool thread and fire-and-forget: blocking
        // the Mac Catalyst startup thread on `GetSettingAsync(...)` causes
        // a sync-over-async deadlock (the SQLite continuation waits for the
        // UI sync context that's currently blocked on .GetResult). The
        // user's first build will use the default Persistent policy if the
        // hydration loses the race; the Settings page re-applies on mount.
        _ = Task.Run(async () =>
        {
            try
            {
                var db       = app.Services.GetRequiredService<Kmila.Shared.Data.ApplicationDbContext>();
                var compiler = app.Services.GetRequiredService<LibraryCompiler.Services.LibraryCompiler>();
                var raw      = await db.GetSettingAsync(Kmila.Shared.Models.AppSettings.KEY_LIBRARY_BUILD_MODE);
                var policy   = Kmila.Shared.Models.LibraryBuildModeExtensions.ParseToken(raw);
                compiler.Orchestrator.Policy = Kmila.Shared.Models.LibraryBuildModeExtensions.ToPolicy(policy);
            }
            catch
            {
                // First-launch path: AppSettings table may not exist yet.
            }
        });

        // Reconcile the resolver against disk on every UserAssetStore
        // change (Settings import / delete, external file mutations).
        // See Web Program.cs for the rationale.
        try
        {
            var compiler   = app.Services.GetRequiredService<LibraryCompiler.Services.LibraryCompiler>();
            var userAssets = app.Services.GetRequiredService<UserAssetStore>();
            userAssets.OnChange += () => userAssets.SyncResolver(compiler.Resolver);
        }
        catch
        {
            // Subscription is best-effort.
        }

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
