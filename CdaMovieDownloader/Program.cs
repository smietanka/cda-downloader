using CdaMovieDownloader.Extractors;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using CdaMovieDownloader.Data;
using System.Text.Json;
using System.Threading;
using PubSub;
using CdaMovieDownloader.Subscribers;
using System.Threading.Tasks;
using OpenQA.Selenium.Edge;
using Spectre.Console;
using System.Net;
using OpenQA.Selenium.Support.UI;
using System.Net.Http;

namespace CdaMovieDownloader
{
	public class Program
	{
		private const int OnePieceFromWhatEpisone = 995;
		private const int ChunkGroup = 5;
		private static readonly ILogger _logger;
		private static readonly Hub _downloader = Hub.Default;
		private static readonly string _onePieceDirectory = @"X:\MOJE\One Piece";
		private static HttpClient _httpClient;
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
			new EpisodeDetailsSubscriber(_logger, _downloader);
			_httpClient = new HttpClient();
		}

		static async Task Main(string[] args)
		{
			CheckDownloadedEpisodes();
			
			Uri startPage = new("https://op.wbijam.pl/pierwsza_seria.html");
			var episodeDetails = new List<EpisodeDetails>();

			if (File.Exists("./episodeDetails.json"))
			{
				using (var reader = new StreamReader("./episodeDetails.json"))
				{
					string json = reader.ReadToEnd();
					episodeDetails = JsonSerializer.Deserialize<List<EpisodeDetails>>(json);
				}
				if (File.Exists("./episodeDetailsWithDirect.json"))
				{
					using (var reader = new StreamReader("./episodeDetailsWithDirect.json"))
					{
						string json = reader.ReadToEnd();
						episodeDetails = JsonSerializer.Deserialize<List<EpisodeDetails>>(json);
					}
				}
				else
				{
					episodeDetails = await EnrichCdaDirectLink(episodeDetails);
				}
			}

			if (episodeDetails.Any())
			{
				_logger.Information("There was found a file with downloaded episodes details. Do you want to overwrite this file and proceed again? [y/n]");
				var userChoice = Console.ReadLine();
				if (userChoice == "y")
				{
					episodeDetails = await EnrichCdaDirectLink(ReadEpisodedDetailsFromExternal(startPage));
				}
			}
			else
			{
				episodeDetails = await EnrichCdaDirectLink(ReadEpisodedDetailsFromExternal(startPage));
			}

			var missingDownloadedEpisodes = CheckDownloadedEpisodes();
			if (missingDownloadedEpisodes.Any())
			{
				_logger.Warning("There was missing episodes! {missingEpisodes}. Do you want to downlaod onlyu missing episodes? [y/n]", missingDownloadedEpisodes);
				var userChoice = Console.ReadLine();
				if (userChoice == "y")
				{
					episodeDetails = episodeDetails.Where(x => missingDownloadedEpisodes.Contains(x.Number)).ToList();
				}
			}

			var leftEpisodesToDownload = GetMissingEpisodes(episodeDetails);
            if (leftEpisodesToDownload.Any())
            {
				episodeDetails = leftEpisodesToDownload.ToList();
            }
			_logger.Information("Found episodes with direct links. Starting download.");
			await DownloadFiles(episodeDetails);
			_logger.Information("Finished downloading.");

			Console.ReadKey();
		}

		private static async Task<List<EpisodeDetails>> EnrichCdaDirectLink(List<EpisodeDetails> episodeDetails)
		{
			var opts = new EdgeOptions();
			opts.AddArgument("log-level=3");
			using (var browser = new EdgeDriver(opts))
			{
				foreach (var episode in episodeDetails)
				{
					_logger.Information($"Getting direct link of {episode.Number}:{episode.Name}");
					await FindDirectLinkForEpisode(browser, episode);
					await Task.Delay(20000);
				}
				var episodeDetailsWithDirectJson = JsonSerializer.Serialize(episodeDetails);
				File.WriteAllText("./episodeDetailsWithDirect.json", episodeDetailsWithDirectJson);
				return episodeDetails;
			}
		}

		private static async Task DownloadFiles(List<EpisodeDetails> episodeDetailsWithDirectLink)
		{
			await AnsiConsole
				.Progress()
				.AutoClear(true)
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
					foreach (var group in episodeDetailsWithDirectLink.Chunk(ChunkGroup))
					{
						var itemsWhereDirectLinkIs = group.Where(ep => !string.IsNullOrWhiteSpace(ep.CdaDirectUrl));
						var tasks = itemsWhereDirectLinkIs.Select(async episode =>
							{
								var progressTask = ctx.AddTask($"{episode.Number} - {episode.Name}", new ProgressTaskSettings
								{
									AutoStart = true,
									MaxValue = 100,
								});
								await Download(_httpClient, progressTask, episode);
								return true;
							});
						var isSuccess = await Task.WhenAll(tasks);
						await Task.Delay(1000);
					}
					return true;
				});

		}

		private static Task FindDirectLinkForEpisode(EdgeDriver browser, EpisodeDetails episode)
		{
			var waiter = new WebDriverWait(browser, TimeSpan.FromSeconds(20));
			foreach (var quality in Constants.Qualities)
			{
				try
				{
					browser.Navigate().GoToUrl($"{episode.CdaUrl}?wersja={quality}");

					var cdaVideoElement = waiter.Until(x => browser.FindElement(OpenQA.Selenium.By.XPath("//video[@class='pb-video-player']")));
					if (cdaVideoElement != null)
					{
						var cdaDirectLinkToMovie = cdaVideoElement.GetAttribute("src");
						if (!string.IsNullOrEmpty(cdaDirectLinkToMovie))
						{
							episode.CdaDirectUrl = cdaDirectLinkToMovie;
							break;
						}
					}
				}
				catch (Exception e)
				{
					_logger.Error(e, "Something goes wrong for {episode}, {episodeUrl}", episode.Number, episode.CdaUrl);
					_logger.Warning(browser.PageSource);
				}
			}
			return Task.CompletedTask;
		}

		async static Task Download(HttpClient client, ProgressTask task, EpisodeDetails episode)
		{
			try
			{
				using (HttpResponseMessage response = await client.GetAsync(episode.CdaDirectUrl, HttpCompletionOption.ResponseHeadersRead))
				{
					response.EnsureSuccessStatusCode();

					// Set the max value of the progress task to the number of bytes
					task.MaxValue(response.Content.Headers.ContentLength ?? 0);
					// Start the progress task
					task.StartTask();

					var fileNameExtension = Path.GetExtension(episode.CdaDirectUrl);
					var fileName = Path.Combine(_onePieceDirectory, $"{episode.Number}{fileNameExtension}");
					//AnsiConsole.MarkupLine($"Starting download of [u]{fileName}[/] ({task.MaxValue} bytes)");

					using (var contentStream = await response.Content.ReadAsStreamAsync())
					using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
					{
						var buffer = new byte[8192];
						while (true)
						{
							var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
							if (read == 0)
							{
								//AnsiConsole.MarkupLine($"Download of [u]{fileName}[/] [green]completed![/]");
								break;
							}

							// Increment the number of read bytes for the progress task
							task.Increment(read);

							// Write the read bytes to the output stream
							await fileStream.WriteAsync(buffer, 0, read);
						}
					}
				}
			}
			catch (Exception ex)
			{
				// An error occured
				AnsiConsole.MarkupLine($"[red]Error:[/] {ex}");
			}
		}

		private static List<EpisodeDetails> ReadEpisodedDetailsFromExternal(Uri startPage)
		{
			var web = new HtmlWeb();
			var episodeDetailsExtractor = new EpisodeDetailsExtractor(_logger, startPage);
			var result = new List<EpisodeDetails>();

			var startPageDocument = web.Load(startPage);
			var episodeList = startPageDocument.DocumentNode.SelectNodes("//tr[@class='lista_hover']");
			if (episodeList != null && episodeList.Any())
			{
				EpisodeDetails previousEpisode = null;
				foreach (var episode in episodeList)
				//Parallel.ForEach(episodeList, episode =>
				{
					var hrefToMovie = episode.SelectSingleNode(".//a[@class='sub_inner_link']");
					if (hrefToMovie != null)
					{
						var episodePageHref = hrefToMovie.Attributes["href"].Value;
						var uriBuilder = new UriBuilder(startPage.Scheme, startPage.Host, startPage.Port, episodePageHref);
						var episodePageUrl = uriBuilder.ToString();

						var episodePageWeb = new HtmlWeb();
						var concreteEpisodePage = episodePageWeb.Load(episodePageUrl);
						var episodeDetail = episodeDetailsExtractor.GetEpisodeDetails(concreteEpisodePage);
						if (episodeDetail is not null)
						{
							if (previousEpisode != null)
							{
								var epNumbersDiff = previousEpisode.Number - episodeDetail.Number;
								if (epNumbersDiff != 1)
								{
									_logger.Warning("There was missing some episodes! between {start} - {end}", episodeDetail.Number, previousEpisode.Number);
								}
							}
							previousEpisode = episodeDetail;
							result.Add(episodeDetail);
							if (episodeDetail.Number == OnePieceFromWhatEpisone)
							{
								break;
							}
						}
					}
				}
				//);
			}

			var episodeDetailsJson = JsonSerializer.Serialize(result);
			File.WriteAllText("./episodeDetails.json", episodeDetailsJson);
			return result;
		}

		private static List<EpisodeDetails> GetMissingEpisodes(List<EpisodeDetails> episodeDetails)
		{
			var allDownloadedEpisodes = Directory.GetFiles(_onePieceDirectory).Select(x => Path.GetFileNameWithoutExtension(x)).Select(x => int.Parse(x)).ToList();
			var missingEpisodes = episodeDetails.Select(x => x.Number).Except(allDownloadedEpisodes);
			return episodeDetails.Where(ep => missingEpisodes.Contains(ep.Number)).ToList();
		}

		private static List<int> CheckDownloadedEpisodes()
		{
			var result = new List<int>();
			if (!Directory.Exists(_onePieceDirectory))
			{
				Directory.CreateDirectory(_onePieceDirectory);
			}
			if (!Directory.GetFiles(_onePieceDirectory).Any())
			{
				return result;
			}
			var allDownloadedEpisodes = Directory.GetFiles(_onePieceDirectory).Select(x => Path.GetFileNameWithoutExtension(x)).Select(x => int.Parse(x)).OrderBy(x => x).ToList();

			var isSequential = Enumerable.Range(allDownloadedEpisodes.Min(), allDownloadedEpisodes.Count).SequenceEqual(allDownloadedEpisodes);
			int previousEpisode = allDownloadedEpisodes.First();
			foreach (var episode in allDownloadedEpisodes.Skip(1))
			{
				var diff = episode - previousEpisode;
				if (diff != 1)
				{
					var rangeToAdd = Enumerable.Range(episode + 1, diff - 1);
					result.AddRange(rangeToAdd);
				}
				previousEpisode = episode;
			}
			return result.OrderBy(x => x).ToList();
		}
	}
}