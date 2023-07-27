using CdaMovieDownloader.Data;
using HtmlAgilityPack;
using Polly;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public abstract class BaseEpisodeDetailsExtractor : IEpisodeDetailsExtractor
    {
        public abstract Provider Provider { get; }
        public abstract string playerElementXPath { get; }
        public readonly ILogger _logger;
        public readonly ConfigurationOptions _options;
        public readonly ICheckEpisodes _checkEpisodes;
        private readonly Regex _episodeNumber = new(@"\d{2,4}", RegexOptions.Compiled);
        private readonly Regex _episodeName = new(@"""(.*)""", RegexOptions.Compiled);
        protected BaseEpisodeDetailsExtractor(ILogger logger, ConfigurationOptions options, ICheckEpisodes checkEpisodes)
        {
            _logger = logger;
            _options = options;
            _checkEpisodes = checkEpisodes;
        }

        public abstract Task EnrichDirectLinkForEpisode(EpisodeDetails episode);
        public abstract Task<string> GetMovieUrl(string document);

        public virtual async Task<EpisodeDetails> GetEpisodeDetails(HtmlDocument document)
        {
            if (document != null)
            {
                var playerRelLink = document.DocumentNode.SelectSingleNode($"//td[@class='center'][contains(text(),'{Provider}')]/parent::* //span[@rel]").Attributes["rel"].Value;
                var moviePath = $"odtwarzacz-{playerRelLink}.html";
                var readyLinkToMovie = new UriBuilder(_options.Url.Scheme, _options.Url.Host, _options.Url.Port, moviePath).ToString();

                var episodeDescriptionElement = document.DocumentNode.SelectSingleNode("//head/meta[@property='og:title']").Attributes["content"];
                var episodeNameElement = document.DocumentNode.SelectSingleNode("//h1[@class='pod_naglowek']");

                var episodeNumber = _episodeNumber.Match(episodeDescriptionElement.Value);
                if (!episodeNumber.Success)
                {
                    episodeNumber = _episodeNumber.Match(episodeNameElement.InnerText);
                }
                var episodeName = _episodeName.Match(episodeNameElement.InnerText);

                if (episodeNumber.Success && episodeName.Success)
                {
                    if (!string.IsNullOrWhiteSpace(playerRelLink))
                    {
                        
                        var movieUrl = await GetMovieUrl(readyLinkToMovie);
                        if (movieUrl != null)
                        {
                            return new EpisodeDetails
                            {
                                Url = movieUrl,
                                Name = string.Join("_", episodeName.Value.Split(Path.GetInvalidFileNameChars())),
                                Number = int.Parse(episodeNumber.Value)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public virtual async Task<List<EpisodeDetails>> EnrichCdaDirectLink(List<EpisodeDetails> episodeDetails)
        {
            foreach (var episode in episodeDetails)
            {
                var currentSavedEpisodes = new List<EpisodeDetails>();
                if (File.Exists(_options.EpisodeDetailsWithDirectPath))
                {
                    var ca = File.ReadAllText(_options.EpisodeDetailsWithDirectPath);
                    currentSavedEpisodes = JsonSerializer.Deserialize<List<EpisodeDetails>>(ca);
                    if (currentSavedEpisodes.Any(savedEpisode => savedEpisode.Number == episode.Number))
                    {
                        continue;
                    }
                }

                _logger.Information($"Getting direct link of {episode.Number}:{episode.Name}");
                await EnrichDirectLinkForEpisode(episode);
                if (!string.IsNullOrEmpty(episode.DirectUrl))
                {
                    currentSavedEpisodes.Add(episode);
                    var jsonWithNewEpisode = JsonSerializer.Serialize(currentSavedEpisodes);
                    File.WriteAllText(_options.EpisodeDetailsWithDirectPath, jsonWithNewEpisode);
                    await Task.Delay(20000);
                }
            }

            return episodeDetails;
        }

        public async Task<EpisodeDetails> EnrichCdaDirectLink(EpisodeDetails episodeDetail)
        {
            await EnrichDirectLinkForEpisode(episodeDetail);
            return episodeDetail;
        }

        public List<EpisodeDetails> ReadEpisodeDetailsFromExternal(Uri startPage)
        {
            var web = new HtmlWeb();
            var concurrentResult = new ConcurrentBag<EpisodeDetails>();
            var u = new List<int>() { 1, 2, 3, 5, 8 }
            .Select(w => TimeSpan.FromSeconds(w));
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(u, (ex, num) =>
                {
                    _logger.Warning("Error {ex}. Waiting {num}", ex, num);
                });
            var startPageDocument = web.Load(startPage);
            var episodeList = startPageDocument.DocumentNode.SelectNodes("//tr[@class='lista_hover']");
            if (episodeList != null && episodeList.Any())
            {
                Parallel.ForEach(episodeList, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async episode =>
                //foreach (var episode in episodeList)
                {
                    var hrefToMovie = episode.SelectSingleNode(".//a[@class='sub_inner_link']");
                    if (hrefToMovie == null)
                    {
                        hrefToMovie = episode.SelectSingleNode(".//a[@href]");
                    }

                    if (hrefToMovie != null)
                    {
                        var episodePageHref = hrefToMovie.Attributes["href"].Value;
                        _logger.Information("Found episodePageHref {episodePageHref}", episodePageHref);
                        var uriBuilder = new UriBuilder(startPage.Scheme, startPage.Host, startPage.Port, episodePageHref);
                        var episodePageUrl = uriBuilder.ToString();

                        await policy.ExecuteAsync(async () =>
                        {
                            var episodePageWeb = new HtmlWeb();
                            var concreteEpisodePage = episodePageWeb.Load(episodePageUrl);
                            var episodeDetail = await GetEpisodeDetails(concreteEpisodePage);
                            if (episodeDetail is not null)
                            {
                                concurrentResult.Add(episodeDetail);
                            }
                            else
                            {
                                _logger.Error("Something wrong with parsing episode details.");
                                //throw new Exception("Something wrong with parsing episode details.");
                            }
                        });
                    }
                    //}
                });
            }

            var result = new List<EpisodeDetails>();
            //Check gaps between found episodes on website and downloaded ones 
            var leftEpisodes = concurrentResult.OrderBy(x => x.Number)
                .Select(x => x.Number).Except(_checkEpisodes.GetDownloadedEpisodesNumbers());
            if (leftEpisodes.Any())
            {
                result = concurrentResult.Where(w => leftEpisodes.Contains(w.Number)).ToList();
            }

            var episodeDetailsJson = JsonSerializer.Serialize(concurrentResult.OrderBy(x => x.Number));
            File.WriteAllText(_options.EpisodeDetailsPath, episodeDetailsJson);
            return result;
        }
    }
}
