using BlazorBootstrap;
using Kmila.Shared;
using Kmila.Shared.Data;
using Kmila.Shared.Models;
using Kmila.Shared.Repositories;
using Kmila.Shared.Services;
using Kmila.Shared.Services.Export;
using Kmila.Wasm.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kmila.Wasm;

/// <summary>
/// Entry point for the Kmila Blazor WebAssembly head — the browser-hosted demo.
/// Mirrors Kmila.Web/Program.cs's service graph (same lifetimes, validated for a
/// scoped browser environment) with three substitutions: asset loaders fetch the
/// RCL's static files over HTTP instead of the server filesystem; host services
/// are the WASM ones; and all ASP.NET Core server wiring (SignalR render mode,
/// /api/v1, OpenAPI, HTTPS/antiforgery) is gone. The VHDL engine itself runs
/// unchanged, in the browser.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Mount the SAME shared root component the MAUI head mounts (#app), plus
        // the head outlet for <PageTitle> / <HeadContent>.
        builder.RootComponents.Add<Routes>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var baseAddress = builder.HostEnvironment.BaseAddress;
        var http = new HttpClient { BaseAddress = new Uri(baseAddress) };
        builder.Services.AddSingleton(http);

        // ── Asset loader: fetch RCL static web assets (_content/Kmila.Shared/...)
        //    over HTTP. Converts a non-success response into FileNotFoundException
        //    so the catalogs keep the same failure contract they had on the server
        //    (where the loader was File.OpenRead). ──────────────────────────────
        static Func<string, Task<Stream>> AssetLoader(HttpClient client) => async path =>
        {
            var resp = await client.GetAsync($"_content/Kmila.Shared/{path}");
            if (!resp.IsSuccessStatusCode)
                throw new FileNotFoundException($"Asset not found: {path} ({(int)resp.StatusCode})");
            return await resp.Content.ReadAsStreamAsync();
        };

        // Demo capabilities + browser filesystem durability (WASM-only).
        // Shared UI injects IDemoCapabilities (nullable) to gate demo features.
        builder.Services.AddSingleton<DemoMode>();
        builder.Services.AddSingleton<IDemoCapabilities>(sp => sp.GetRequiredService<DemoMode>());
        builder.Services.AddSingleton<BrowserFsSync>();

        // Device / platform info.
        builder.Services.AddSingleton<IFormFactor, WasmFormFactor>();

        // SQLite (sqlite-net-pcl) over the browser-wasm e_sqlite3 native asset,
        // file anchored in the MEMFS /kmila root that BrowserFsSync persists.
        builder.Services.AddScoped<ApplicationDbContext>(sp =>
        {
            Directory.CreateDirectory(BrowserFsSync.Root);
            var dbPath = Path.Combine(BrowserFsSync.Root, "kmila.db");
            return new ApplicationDbContext(dbPath, sp.GetService<ILogger<ApplicationDbContext>>());
        });

        // Project repositories.
        builder.Services.AddScoped<Projects>();
        builder.Services.AddScoped<ProjectFiles>();

        // Writable user assets (Devices/Dictionaries/Libraries/LibraryCache) under
        // /kmila/UserAssets. The FileSystemWatcher it tries to start throws in the
        // browser and is swallowed (logged) — assets just don't auto-refresh.
        builder.Services.AddSingleton<UserAssetStore>();

        // FPGA JSON reader — the non-ASPNETCORE (delegate) constructor, fed an
        // HTTP loader instead of MAUI's FileSystem.OpenAppPackageFileAsync.
        builder.Services.AddSingleton<JSONFileReader>(sp =>
            new JSONFileReader(
                openPackageFileAsync: AssetLoader(http),
                logger: sp.GetService<ILogger<JSONFileReader>>(),
                userAssets: sp.GetRequiredService<UserAssetStore>()));

        // VHDL library compiler — same factory as the other heads; its blob cache
        // lands under /kmila/UserAssets/LibraryCache (MEMFS, persisted).
        builder.Services.AddSingleton<LibraryCompiler.Services.LibraryCompiler>(sp =>
            LibraryCompilerFactory.Create(
                sp.GetRequiredService<UserAssetStore>(),
                sp.GetService<ILogger<FileSystemBlobStore>>()));

        builder.Services.AddSingleton<ProgramBuilder>();
        builder.Services.AddSingleton<SelectedFPGA>();
        builder.Services.AddScoped<ConfigurationService>();
        builder.Services.AddScoped<SidebarState>();
        builder.Services.AddScoped<EditorDockState>();
        builder.Services.AddScoped<SignalCursorBus>();
        builder.Services.AddScoped<SimulationParameters>();
        builder.Services.AddScoped<BuildLog>();
        builder.Services.AddScoped<SimulationCoordinator>();
        builder.Services.AddScoped<Kmila.Shared.Services.VisualRun.VisualRunRegistry>();

        // Sim-end notifier — the shared browser-Notification implementation.
        builder.Services.AddScoped<ISimEndNotifier, WebSimEndNotifier>();

        // In-app logs (/logs) — one AppLogStore shared with the logger provider.
        var appLog = new AppLogStore();
        builder.Services.AddSingleton(appLog);
        builder.Logging.AddProvider(new InMemoryLoggerProvider(appLog));

        // Translations — fetch Dictionaries/{lang}.json from the RCL over HTTP.
        builder.Services.AddSingleton<Traductor>(sp =>
        {
            var userAssets = sp.GetRequiredService<UserAssetStore>();
            foreach (var userLang in userAssets.ListUserLanguagePaths())
            {
                var code = Path.GetFileNameWithoutExtension(userLang);
                var display = UserAssetStore.ReadLanguageDisplayName(userLang);
                Traductor.AddUserLanguage(display, code);
            }
            return new Traductor(AssetLoader(http));
        });

        // Learn / Concepts / Practice catalogs — HTTP loaders.
        builder.Services.AddSingleton<LessonCatalog>(sp =>
            new LessonCatalog(AssetLoader(http),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<LessonCatalog>()));
        builder.Services.AddScoped<LessonProgressStore>();

        builder.Services.AddSingleton<ConceptCatalog>(sp =>
            new ConceptCatalog(AssetLoader(http),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConceptCatalog>()));
        builder.Services.AddScoped<ConceptProgressStore>();

        builder.Services.AddSingleton<BlockProgram>();
        builder.Services.AddScoped<ReplayState>();

        builder.Services.AddSingleton<Kmila.Shared.Services.Practice.PracticeCatalog>(sp =>
            new Kmila.Shared.Services.Practice.PracticeCatalog(AssetLoader(http),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<Kmila.Shared.Services.Practice.PracticeCatalog>()));
        builder.Services.AddSingleton<Kmila.Shared.Services.Practice.PracticeEvaluator>();

        // Project templates gallery — HTTP loader + repositories.
        builder.Services.AddScoped<ProjectTemplateService>(sp =>
            new ProjectTemplateService(
                AssetLoader(http),
                sp.GetRequiredService<Projects>(),
                sp.GetRequiredService<ProjectFiles>(),
                sp.GetRequiredService<JSONFileReader>(),
                sp.GetRequiredService<SelectedFPGA>(),
                sp.GetRequiredService<SimulationParameters>(),
                sp.GetRequiredService<UserAssetStore>(),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ProjectTemplateService>()));

        builder.Services.AddScoped<Kmila.Shared.Services.Practice.PracticeAttemptStore>();
        builder.Services.AddScoped<Kmila.Shared.Services.Practice.PracticeSessionExportService>();
        builder.Services.AddScoped<RecentProjectsTracker>();
        builder.Services.AddScoped<SimulationSnapshotStore>();
        builder.Services.AddScoped<ProjectBundleService>();

        // File export — browser anchor-download via JS interop.
        builder.Services.AddScoped<IFileExportService, WasmFileExportService>();

        builder.Services.AddScoped<EditorSessionStore>();
        builder.Services.AddScoped<GitHubAuth>();
        builder.Services.AddScoped<AutoErrorReporter>();

        builder.Services.AddBlazorBootstrap();
        builder.Services.AddScoped<KmilaToastService>();

        var host = builder.Build();

        // Ensure the SQLitePCLRaw provider is registered before any connection
        // opens (belt-and-braces — sqlite-net's bundle normally auto-inits).
        SQLitePCL.Batteries_V2.Init();

        // ── Startup order matters ────────────────────────────────────────────
        // 1) Pull the persisted /kmila tree (DB + project files) back into MEMFS
        //    BEFORE anything opens the SQLite file.
        var fsSync = host.Services.GetRequiredService<BrowserFsSync>();
        await fsSync.HydrateAsync();

        // 2) Create the schema (no-op if the hydrated DB already has it).
        try
        {
            var db = host.Services.GetRequiredService<ApplicationDbContext>();
            await db.InitDBAsync();

            // 3) Honour the saved library-build mode for the first build.
            var compiler = host.Services.GetRequiredService<LibraryCompiler.Services.LibraryCompiler>();
            var raw = await db.GetSettingAsync(AppSettings.KEY_LIBRARY_BUILD_MODE);
            var policy = LibraryBuildModeExtensions.ParseToken(raw);
            compiler.Orchestrator.Policy = LibraryBuildModeExtensions.ToPolicy(policy);
        }
        catch
        {
            // First-run: the AppSettings table may not exist yet.
        }

        // Keep the library resolver in sync with disk on user-asset changes.
        try
        {
            var compiler = host.Services.GetRequiredService<LibraryCompiler.Services.LibraryCompiler>();
            var userAssets = host.Services.GetRequiredService<UserAssetStore>();
            userAssets.OnChange += () => userAssets.SyncResolver(compiler.Resolver);
        }
        catch
        {
            // Best-effort.
        }

        // Route Console.WriteLine from the engine libs into the in-app /logs view.
        var consoleLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Console");
        Console.SetOut(new ConsoleToLoggerWriter(consoleLogger, LogLevel.Debug));

        // 4) Begin change-detected persistence back to IndexedDB.
        fsSync.StartAutoFlush();

        await host.RunAsync();
    }
}
