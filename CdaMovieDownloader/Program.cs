using CdaMovieDownloader.Extractors;
using System;
using Serilog;
using CdaMovieDownloader.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CdaMovieDownloader.Extensions;
using Microsoft.Extensions.Options;
using PubSub;
using CdaMovieDownloader.Subscribers;
using Spectre.Console;
using Serilog.Events;
using CdaMovieDownloader.Contexts;
using Microsoft.EntityFrameworkCore;
using CdaMovieDownloader.Services;

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

			var host = Host.CreateDefaultBuilder(args)
				.ConfigureServices(services =>
				{
					services.Configure<ConfigurationOptions>(configuration.GetSection("MainSettings"));
					services.AddSingleton<IDownloader, Downloader>();
					var cpmm = configuration.GetConnectionString("CdaMovieDatabase");

                    services.AddDbContext<MovieContext>(builder => builder.UseNpgsql(cpmm));
					services.AddSingleton<ICheckEpisodes, CheckEpisodes>();
					services.AddSingleton<IEpisodeService, EpisodeService>();
					services.AddSingleton<EpisodeDetailsSubscriber>();
                    services.AddSingleton<Hub>();
                    var sp = services.BuildServiceProvider();
					var settings = sp.GetService<IOptions<ConfigurationOptions>>();

					if (settings.Value.Provider == Provider.cda)
						services.AddSingleton<IEpisodeDetailsExtractor, CdaEpisodeDetailsExtractor>();
					if (settings.Value.Provider == Provider.mp4up)
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
				.HideCompleted(false)
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
					while(!ctx.IsFinished)
					{
						await Task.Delay(1000);
					}
                });
			AnsiConsole.WriteLine("Finished");
        }
	}
}