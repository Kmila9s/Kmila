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

        // Translation service with MAUI's FileSystem for reading bundled dictionary files
        builder.Services.AddSingleton<Traductor>(provider =>
            new Traductor(path => FileSystem.OpenAppPackageFileAsync(path)));

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddBlazorBootstrap();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
