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

namespace CdaMovieDownloader
{
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

            if(!Guid.TryParse(args.FirstOrDefault(), out var configurationId))
            {
                _logger.Error("There is no first argument");
                return;
            }
            
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.Configure<ConfigurationOptions>(configuration.GetSection("MainSettings"));
                    services.AddSingleton<IDownloader, Downloader>();
                    services.AddDbContext<MovieContext>(builder => builder.UseNpgsql(configuration.GetConnectionString("CdaMovieDatabase"))
                        .EnableSensitiveDataLogging(true)
                        .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll)
                    );
                    services.AddSingleton<ICheckEpisodes, CheckEpisodes>();
                    services.AddSingleton<IEpisodeService, EpisodeService>();
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<EpisodeDetailsSubscriber>();
                    services.AddSingleton<Hub>();
                    var sp = services.BuildServiceProvider();
                    var settings = sp.GetService<IConfigurationService>()
                        .GetConfiguration(configurationId);

                    services.AddSingleton(settings);

                    if (settings?.Provider == Provider.cda)
                        services.AddSingleton<IEpisodeDetailsExtractor, CdaEpisodeDetailsExtractor>();
                    if (settings?.Provider == Provider.mp4up)
                        services.AddSingleton<IEpisodeDetailsExtractor, Mp4upEpisodeDetailsExtractor>();

                    services.AddSingleton<ICrawler, Crawler>();
                    services.AddSingleton(_logger);
                    services.AddHttpClient();
                    services.AddEdgeBrowser();
                })
                .Build();

            var crawler = host.Services.GetService<ICrawler>();
            await AnsiConsole
                .Progress()
                .AutoClear(false)
                .HideCompleted(true)
                .Columns(new ProgressColumn[]
                {
                    new SpinnerColumn(),
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn()
                })
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
}