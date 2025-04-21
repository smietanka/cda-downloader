using CdaMovieDownloader.Common.Options;
using CdaMovieDownloader.Services;
using CdaMovieDownloader.Subscribers;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using Polly;
using PubSub;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors;

public interface IEpisodeDetailsExtractor
{
    Task<Episode> GetEpisodeDetails(HtmlDocument document);
    Task<Dictionary<string, object>> GetMetadata(Episode episode);
    Task<List<Episode>> ReadEpisodeDetailsFromExternalAsync(Uri startPage);
    Task<List<Episode>> EnrichDirectLink(ProgressContext progressContext, List<Episode> episodeDetails);
}

public class NanasubsExtractor : IEpisodeDetailsExtractor
{    
    private readonly EdgeOptions _edgeOptions;
    private readonly EdgeDriverService _edgeDriverService;
    private readonly ConfigurationOptions _options;
    private readonly Hub _hub;
    private readonly IConfigurationService _configurationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EpisodeDetailsSubscriber _subscriber;
    private readonly IEpisodeService _episodeService;
    private readonly EdgeDriver _edgeDriver;

    public NanasubsExtractor(EdgeOptions edgeOptions,
            EdgeDriverService edgeDriverService,
            IOptions<ConfigurationOptions> options,
            IConfigurationService configurationService,
            ICheckEpisodes checkEpisodes,
            IHttpClientFactory httpClientFactory,
            EpisodeDetailsSubscriber subscriber,
            Hub hub,
            IEpisodeService episodeService)
    {
        _edgeOptions = edgeOptions;
        _edgeDriverService = edgeDriverService;
        _options = options.Value;
        _hub = hub;
        _configurationService = configurationService;
        _httpClientFactory = httpClientFactory;
        _subscriber = subscriber;
        _episodeService = episodeService;
        AnsiConsole.WriteLine("Creating driver..");
        _edgeDriver = new EdgeDriver(_edgeDriverService, edgeOptions);
        _edgeDriver.Navigate().GoToUrl("https://nanasubs.com.pl");
        OpenQA.Selenium.Cookie mainPlayerCookie = new("mainPlayer", "cda", "nanasubs.com.pl", "/", null);
        _edgeDriver.Manage().Cookies.AddCookie(mainPlayerCookie);
    }

    public async Task<List<Episode>> EnrichDirectLink(ProgressContext progressContext, List<Episode> episodeDetails)
    {
        var result = new List<Episode>();
        foreach(var episode in episodeDetails)
        {
            if (string.IsNullOrWhiteSpace(episode.DirectUrl))
            {
                try
                {
                    AnsiConsole.WriteLine($"Getting direct link of {episode.Number}:{episode.Name}");
                    var ep = await EnrichDirectLink(progressContext, episode);
                    await Task.Delay(15000);
                    result.Add(ep);
                }
                catch(Exception ex)
                {
                    AnsiConsole.WriteLine($"Error occured for episode number: {episode.Number}.");
                    AnsiConsole.WriteException(ex);
                }
            }
            else
            {
                AnsiConsole.WriteLine($"Episode number: {episode.Number} includes already direct link to video. Skip.");
            }
        }

        return result;
    }

    public Task<Episode> GetEpisodeDetails(HtmlDocument document)
    {
        string jsonLdScript = _edgeDriver
            .FindElement(By.XPath("//script[@type='application/ld+json'][contains(text(), 'TVEpisode')]"))
            .GetAttribute("innerHTML");

        string cleanJson = jsonLdScript
            .Replace("\r\n", string.Empty) // Windows line breaks
            .Replace("\n", string.Empty)   // Unix line breaks
            .Replace("\r", string.Empty);

        string safeJson = Regex.Replace(cleanJson, @":\s*""(.*?)""(?=\s*[,}])",
            match => {
                string value = match.Groups[1].Value;
                value = value.Replace("\"", "\\\"");
                return ": \"" + value + "\"";
            }
        );

        using JsonDocument jsonMetadata = JsonDocument.Parse(safeJson);
        JsonElement root = jsonMetadata.RootElement;
        double episodeNumber = root.GetProperty("episodeNumber").GetDouble();
        string episodeName = root.GetProperty("name").GetString();
        string episodeUrl = root.GetProperty("url").GetString();

        string embedUrl = root.GetProperty("video").GetProperty("embedUrl").GetString();
        var embedUri = new Uri(embedUrl);
        var currentEpisode = new Episode
        {
            Name = episodeName,
            Number = episodeNumber,
            Url = episodeUrl,
            ConfigurationId = _options.Id,
            Id = Guid.NewGuid(),
            Metadata = new Dictionary<string, object>()
            {
                ["embedUri"] = embedUri
            }
        };

        return Task.FromResult(currentEpisode);
    }

    public async Task<List<Episode>> ReadEpisodeDetailsFromExternalAsync(Uri startPage)
    {
        var config = await _configurationService.GetConfigurationAsync(_options.Id);
        var result = new List<Episode>();

        var retryPolicy = new List<int>() { 10, 20, 30, 50, 80 }
            .Select(w => TimeSpan.FromSeconds(w));
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retryPolicy, (ex, num) =>
            {
                AnsiConsole.WriteException(ex);
            });

        AnsiConsole.WriteLine($"Go to {startPage}");
        
        await _edgeDriver.Navigate().GoToUrlAsync(startPage);

        

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(_edgeDriver.PageSource);

        var episodeList = htmlDocument.DocumentNode.SelectNodes("//div[@class='ns-anime-episode']");
        var orderedEpisodeList = episodeList.Reverse().ToList();
        if (orderedEpisodeList != null && orderedEpisodeList.Count != 0)
        {
            foreach (var episode in orderedEpisodeList)
            //Parallel.ForEach(episodeList, new ParallelOptions { MaxDegreeOfParallelism = _options.ParallelThreads }, async episode =>
            {
                var hrefToMovie = episode.SelectSingleNode(".//a[@class='ns-anime-episode-link']");

                if (hrefToMovie != null)
                {
                    var episodePageHref = hrefToMovie.Attributes["href"].Value;
                    var dbEpisode = config.Episodes.SingleOrDefault(e => e.Url == episodePageHref);

                    if (dbEpisode != null && dbEpisode.IsDownloaded) continue;

                    AnsiConsole.WriteLine($"Found episodePageHref {episodePageHref}");
                    var uriBuilder = new UriBuilder(episodePageHref);
                    var episodePageUrl = uriBuilder.ToString();

                    await policy.ExecuteAsync(async () =>
                    {
                        AnsiConsole.WriteLine($"Go to {episodePageUrl}");
                        await _edgeDriver.Navigate().GoToUrlAsync(episodePageUrl);
                        var htmlDocument = new HtmlDocument();

                        htmlDocument.LoadHtml(_edgeDriver.PageSource);
                        var episodeDetail = await GetEpisodeDetails(htmlDocument);

                        if (episodeDetail != null)
                        {
                            if (episodeDetail.Number < 1073) return;
                            await _episodeService.AddEpisode(episodeDetail);
                            result.Add(episodeDetail);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red][bold]Something wrong with parsing episode details.[/]");
                        }
                    });
                }
                //});
                AnsiConsole.WriteLine("Sleep 5 seconds");
                await Task.Delay(5000);
            }
        }

        return result;
    }

    private async Task EnrichDirectLinkForEpisode(Episode episode)
    {
        try
        {
            episode.Metadata.TryGetValue("embedUri", out var embedUri);
            var embeddedUri = new Uri(embedUri.ToString());
            _edgeDriver.Navigate().GoToUrl(embeddedUri);
            var hashElement = _edgeDriver.FindElement(By.CssSelector($"div[id='mediaplayer{embeddedUri.Segments.Last()}']"));
            var playerDataRaw = hashElement.GetAttribute("player_data");
            string playerDataJson = WebUtility.HtmlDecode(playerDataRaw);
            using JsonDocument playerData = JsonDocument.Parse(playerDataJson);

            var hash2 = playerData.RootElement.GetProperty("video").GetProperty("hash2").GetString();
            int ts = playerData.RootElement.GetProperty("video").GetProperty("ts").GetInt32();
            AnsiConsole.WriteLine($"Found hash2 for video: {hash2}");

            using var client = _httpClientFactory.CreateClient();
            JsonObject jsonObject = new()
            {
                ["jsonrpc"] = "2.0",
                ["method"] = "videoGetLink",
                ["params"] = new JsonArray
            {
                embeddedUri.Segments.Last(),
                "hd",
                ts,
                hash2,
                new JsonObject()
            },
                ["id"] = 1
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            client.DefaultRequestHeaders.Referrer = new Uri("https://example.com"); // if needed
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Convert to string
            string jsonString = jsonObject.ToJsonString();
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://www.cda.pl/", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadFromJsonAsync<CdaResponse>();
            episode.DirectUrl = responseJson.Result.Resp;
            await _episodeService.EditDirectLinkForEpisode(episode);
        }
        catch (Exception e) 
        {
            AnsiConsole.WriteException(e);
            throw;
        }

    }

    private async Task<Episode> EnrichDirectLink(ProgressContext progressContext, Episode episodeDetail)
    {
        await EnrichDirectLinkForEpisode(episodeDetail);
        if (!string.IsNullOrWhiteSpace(episodeDetail.DirectUrl))
        {
            await _hub.PublishAsync(new EpisodeWithContext(progressContext, episodeDetail));
        }
        return episodeDetail;
    }

    public async Task<Dictionary<string, object>> GetMetadata(Episode episode)
    {
        await _edgeDriver.Navigate().GoToUrlAsync(episode.Url);

        string jsonLdScript = _edgeDriver
            .FindElement(By.XPath("//script[@type='application/ld+json'][contains(text(), 'TVEpisode')]"))
            .GetAttribute("innerHTML");

        string cleanJson = jsonLdScript
            .Replace("\r\n", string.Empty) // Windows line breaks
            .Replace("\n", string.Empty)   // Unix line breaks
            .Replace("\r", string.Empty);

        string safeJson = Regex.Replace(cleanJson, @":\s*""(.*?)""(?=\s*[,}])",
            match => {
                string value = match.Groups[1].Value;
                value = value.Replace("\"", "\\\"");
                return ": \"" + value + "\"";
            }
        );

        using JsonDocument jsonMetadata = JsonDocument.Parse(safeJson);
        JsonElement root = jsonMetadata.RootElement;

        string embedUrl = root.GetProperty("video").GetProperty("embedUrl").GetString();
        var embedUri = new Uri(embedUrl);
        return new Dictionary<string, object>()
        {
            ["embedUri"] = embedUri
        };
    }
}

public record CdaResponse(CdaResult Result, string Id, string Jsonrpc);

public record CdaResult(string Status, string Resp);