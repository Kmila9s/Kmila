using Kmila.Web.Components;
using Kmila.Shared.Services;
using Kmila.Web.Services;
using Kmila.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Kmila.Shared.Data;
using Kmila.Shared.Models;

namespace Kmila;

public class Program
{
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
        builder.Services.AddScoped<ApplicationDbContext>(sp =>
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "kmila.db");
            return new(dbPath);
        });

        builder.Services.AddScoped<Projects>();

        builder.Services.AddScoped<ProjectFiles>();

        builder.Services.AddSingleton<JSONFileReader>();

        builder.Services.AddSingleton<ProgramBuilder>();

        builder.Services.AddSingleton<SelectedFPGA>();

        // Configuration service for saving/loading simulation configurations
        builder.Services.AddScoped<ConfigurationService>();

        // Translation service for Web - reads from wwwroot/Dictionaries
        // Tries multiple paths to support both development and production environments
        builder.Services.AddSingleton<Traductor>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            return new Traductor(async path =>
            {
                // path comes as "Dictionaries/{lang}.json"
                var pathsToTry = new List<string>();

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

                Console.WriteLine($"[Traductor-Web] Looking for: {path}");
                Console.WriteLine($"[Traductor-Web] ContentRootPath: {env.ContentRootPath}");
                Console.WriteLine($"[Traductor-Web] WebRootPath: {env.WebRootPath ?? "(null)"}");

                foreach (var fullPath in pathsToTry)
                {
                    try
                    {
                        var normalizedPath = Path.GetFullPath(fullPath);
                        Console.WriteLine($"[Traductor-Web] Checking: {normalizedPath}");
                        if (File.Exists(normalizedPath))
                        {
                            Console.WriteLine($"[Traductor-Web] Found dictionary at: {normalizedPath}");
                            return File.OpenRead(normalizedPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Traductor-Web] Error checking path {fullPath}: {ex.Message}");
                    }
                }

                var triedPaths = string.Join("\n  - ", pathsToTry.Select(p => {
                    try { return Path.GetFullPath(p); }
                    catch { return p; }
                }));
                throw new FileNotFoundException($"Dictionary file not found. Tried:\n  - {triedPaths}");
            });
        });

        builder.Services.AddBlazorBootstrap();
        
        var app = builder.Build();

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
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(Kmila.Shared._Imports).Assembly);

        app.Run();
    }
}
