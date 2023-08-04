using CdaMovieDownloader.Data;
using CdaMovieDownloader.Services;
using CdaMovieDownloader.Subscribers;
using HtmlAgilityPack;
using Polly;
using PubSub;
using Serilog;
using Spectre.Console;
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
        private static readonly object _locker = new();
        public readonly ILogger _logger;
        public readonly Hub _hub;
        public readonly ConfigurationOptions _options;
        public readonly ICheckEpisodes _checkEpisodes;
        private readonly Regex _episodeNumber = new(@"\d{2,4}", RegexOptions.Compiled);
        private readonly Regex _episodeName = new(@"""(.*)""", RegexOptions.Compiled);
        private readonly IEpisodeService _episodeService;
        protected BaseEpisodeDetailsExtractor(ILogger logger, ConfigurationOptions options, ICheckEpisodes checkEpisodes, Hub hub, IEpisodeService episodeService)
        {
            _logger = logger;
            _options = options;
            _checkEpisodes = checkEpisodes;
            _hub = hub;
            _episodeService = episodeService;
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
                                AnimeUrl = _options.Url.ToString(),
                                Id = Guid.NewGuid(),
                                Url = movieUrl,
                                Name = string.Join("_", episodeName.Value.Split(Path.GetInvalidFileNameChars())),
                                Number = int.Parse(episodeNumber.Value)
                            };
                        }
                    }
                }
            }
            return default;
        }

        public virtual Task<List<EpisodeDetails>> EnrichCdaDirectLink(ProgressContext progressContext, List<EpisodeDetails> episodeDetails)
        {
            Parallel.ForEach(episodeDetails, new ParallelOptions() { MaxDegreeOfParallelism = _options.ParallelThreads },
                async episode =>
                {
                    AnsiConsole.WriteLine($"Getting direct link of {episode.Number}:{episode.Name}");
                    await EnrichCdaDirectLink(progressContext, episode);
                });

            return Task.FromResult(episodeDetails);
        }

        public async Task<EpisodeDetails> EnrichCdaDirectLink(ProgressContext progressContext, EpisodeDetails episodeDetail)
        {
            await EnrichDirectLinkForEpisode(episodeDetail);
            if (!string.IsNullOrWhiteSpace(episodeDetail.DirectUrl))
            {
                await EditEntityAndDownload(progressContext, episodeDetail);
            }
            return episodeDetail;
        }

        public List<EpisodeDetails> ReadEpisodeDetailsFromExternal(Uri startPage)
        {
            var result = new List<EpisodeDetails>();
            var web = new HtmlWeb();
            var retryPolicy = new List<int>() { 1, 2, 3, 5, 8 }
            .Select(w => TimeSpan.FromSeconds(w));
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retryPolicy, (ex, num) =>
                {
                    AnsiConsole.WriteException(ex);
                });
            var startPageDocument = web.Load(startPage);
            var episodeList = startPageDocument.DocumentNode.SelectNodes("//tr[@class='lista_hover']");
            if (episodeList != null && episodeList.Any())
            {
                Parallel.ForEach(episodeList, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async episode =>
                {
                    var hrefToMovie = episode.SelectSingleNode(".//a[@class='sub_inner_link']");
                    if (hrefToMovie == null)
                    {
                        hrefToMovie = episode.SelectSingleNode(".//a[@href]");
                    }

                    if (hrefToMovie != null)
                    {
                        var episodePageHref = hrefToMovie.Attributes["href"].Value;
                        AnsiConsole.WriteLine($"Found episodePageHref {episodePageHref}");
                        var uriBuilder = new UriBuilder(startPage.Scheme, startPage.Host, startPage.Port, episodePageHref);
                        var episodePageUrl = uriBuilder.ToString();

                        await policy.ExecuteAsync(async () =>
                        {
                            var episodePageWeb = new HtmlWeb();
                            var concreteEpisodePage = episodePageWeb.Load(episodePageUrl);
                            var episodeDetail = await GetEpisodeDetails(concreteEpisodePage);
                            if (!episodeDetail.Equals(default))
                            {
                                await _episodeService.AddEpisode(episodeDetail);
                                result.Add(episodeDetail);
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[red][bold]Something wrong with parsing episode details.[/]");
                            }
                        });
                    }
                });
            }
            return result;
        }

        private async Task EditEntityAndDownload(ProgressContext progressContext, EpisodeDetails episode)
        {
            await _episodeService.EditDirectLinkForEpisode(episode);
            _hub.Publish(new EpisodeWithContext(progressContext, episode));
            await Task.Delay(10000);
        }
    }
}
