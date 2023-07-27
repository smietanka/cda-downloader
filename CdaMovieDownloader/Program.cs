using CdaMovieDownloader.Extractors;
using System;
using Serilog;
using CdaMovieDownloader.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CdaMovieDownloader.Extensions;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using Microsoft.Extensions.Options;

namespace CdaMovieDownloader
{
    public class Program
	{
		private static readonly ILogger _logger;

		static Program()
		{
			_logger = new LoggerConfiguration()
				.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
				.WriteTo.Async(
					a => a.File("Logs/", flushToDiskInterval: TimeSpan.FromSeconds(5), restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
					rollingInterval: RollingInterval.Hour,
					retainedFileCountLimit: 3,
					rollOnFileSizeLimit: true,
					shared: true))
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
					services.AddSingleton<ICheckEpisodes, CheckEpisodes>();
					var sp = services.BuildServiceProvider();
					var settings = sp.GetService<IOptions<ConfigurationOptions>>();

					if (settings.Value.Provider == Provider.cda)
						services.AddSingleton<IEpisodeDetailsExtractor, CdaEpisodeDetailsExtractor>();
					if (settings.Value.Provider == Provider.mp4up)
						services.AddSingleton<IEpisodeDetailsExtractor, Mp4upEpisodeDetailsExtractor>();

                    services.AddSingleton<ICrawler, Crawler>();
					services.AddSingleton<Serilog.ILogger>(_logger);
					services.AddHttpClient();
					services.AddEdgeBrowser();
				})
				.Build();

			var crawler = host.Services.GetService<ICrawler>();
            await crawler.Start();
		}
	}
}