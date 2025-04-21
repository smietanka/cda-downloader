global using CdaMovieDownloader.EF.Models;
using CdaMovieDownloader.Common.Options;
using CdaMovieDownloader.Data;
using CdaMovieDownloader.Extensions;
using CdaMovieDownloader.Extractors;
using CdaMovieDownloader.Services;
using CdaMovieDownloader.Subscribers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PubSub;
using Serilog;
using Serilog.Events;
using Spectre.Console;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CdaMovieDownloader;

public class Program
{
    private static readonly ILogger _logger;
    static Program()
    {
        _logger = new LoggerConfiguration()
            .WriteTo.Console(LogEventLevel.Verbose)
            .WriteTo.Async(
                a => a.File("Logs/", flushToDiskInterval: TimeSpan.FromSeconds(5), restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 3,
                rollOnFileSizeLimit: true,
                shared: true))
            .MinimumLevel.Verbose()
            .CreateLogger();
        Log.Logger = _logger;
    }

    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.Configure<ConfigurationOptions>(configuration.GetSection("MainSettings"));
                services.AddScoped<IDownloader, Downloader>();
                services.AddDbContext<MovieContext>();
                services.AddScoped<ICheckEpisodes, CheckEpisodes>();
                services.AddScoped<IEpisodeService, EpisodeService>();
                services.AddScoped<IConfigurationService, ConfigurationService>();
                services.AddScoped<EpisodeDetailsSubscriber>();
                services.AddScoped<Hub>();

                services.AddScoped<IEpisodeDetailsExtractor, NanasubsExtractor>();

                services.AddScoped<ICrawler, Crawler>();
                services.AddSingleton(_logger);
                services.AddHttpClient();
                services.AddEdgeBrowser();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MovieContext>();
        db.Database.EnsureCreated();
        db.Database.Migrate();

        var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
        var settings = await configurationService.GetConfigurationsAsync();
        if (settings.Count == 0)
        {
            var result = await configurationService.AddConfigurationAsync(new Configuration
            {
                Provider = Provider.cda,
                MaxQuality = Quality.FHD,
                OutputDirectory = "one piece",
                Url = "https://nanasubs.com.pl/anime/one-piece"
            });
        }

        var crawler = scope.ServiceProvider.GetService<ICrawler>();
        await AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(true)
            .Columns(
            [
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn()
            ])
            .StartAsync(async ctx =>
            {
                await crawler.Start(ctx);
                while (!ctx.IsFinished)
                {
                    await Task.Delay(1000);
                }
            });
        AnsiConsole.WriteLine("Finished");
    }
}