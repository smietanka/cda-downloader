using CdaMovieDownloader.Data;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public class EpisodeDetailsExtractor : IEpisodeDetailsExtractor
    {
        private readonly ILogger _logger;
        private readonly EdgeOptions _edgeOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConfigurationOptions _options;
        private readonly Regex _episodeNumber = new(@"\d{2,4}", RegexOptions.Compiled);
        private readonly Regex _episodeName = new(@"""(.*)""", RegexOptions.Compiled);

        public EpisodeDetailsExtractor(ILogger logger, EdgeOptions edgeOptions, IOptions<ConfigurationOptions> options, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _edgeOptions = edgeOptions;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<List<EpisodeDetails>> EnrichCdaDirectLink(List<EpisodeDetails> episodeDetails)
        {

            foreach (var episode in episodeDetails)
            {
                _logger.Information($"Getting direct link of {episode.Number}:{episode.Name}");
                await EnrichDirectLinkForEpisode(episode);
                await Task.Delay(20000);
            }
            var episodeDetailsWithDirectJson = JsonSerializer.Serialize(episodeDetails);
            File.WriteAllText(_options.EpisodeDetailsWithDirectPath, episodeDetailsWithDirectJson);
            return episodeDetails;
        }

        public async Task<EpisodeDetails> EnrichCdaDirectLink(EpisodeDetails episodeDetail)
        {
            await EnrichDirectLinkForEpisode(episodeDetail);
            return episodeDetail;
        }

        public async Task EnrichDirectLinkForEpisode(EpisodeDetails episode)
        {
            using(var browser = new EdgeDriver(_edgeOptions))
            {
                var waiter = new WebDriverWait(browser, TimeSpan.FromSeconds(20));
                foreach (var quality in Constants.Qualities.SkipWhile(x => x.Key != _options.MaxQuality))
                {
                    _logger.Information("Looking for direct link for episode number {number} in {quality}", episode.Number, quality);
                    try
                    {
                        var urlToEpisode = $"{episode.CdaUrl}?wersja={quality.Value}";
    
                        browser.Navigate().GoToUrl(urlToEpisode);

                        var cdaVideoElement = waiter.Until(x => browser.FindElement(OpenQA.Selenium.By.XPath("//video[@class='pb-video-player']")));
                        if (cdaVideoElement != null)
                        {
                            var cdaDirectLinkToMovie = cdaVideoElement.GetAttribute("src");
                            using (HttpResponseMessage response = await _httpClientFactory.CreateClient().GetAsync(cdaDirectLinkToMovie, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                            }
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
            }
        }

        public EpisodeDetails GetEpisodeDetails(HtmlDocument document)
        {
            if(document != null)
            {
                var cdaLink = document.DocumentNode.SelectSingleNode("//td[@class='center'][contains(text(),'cda')]");

                if (cdaLink != null)
                {
                    var cdaMovie = cdaLink.SelectNodes("//span[@rel]").FirstOrDefault(x => x.Attributes["rel"].Value.StartsWith("_PLU_"));
                    var episodeDescriptionElement = document.DocumentNode.SelectSingleNode("//head/meta[@property='og:title']").Attributes["content"];
                    var episodeNameElement = document.DocumentNode.SelectSingleNode("//h1[@class='pod_naglowek']");

                    var episodeNumber = _episodeNumber.Match(episodeDescriptionElement.Value);
                    if(!episodeNumber.Success)
                    {
                        episodeNumber = _episodeNumber.Match(episodeNameElement.InnerText);
                    }
                    var episodeName = _episodeName.Match(episodeNameElement.InnerText);

                    if (cdaMovie != null && episodeNumber.Success && episodeName.Success)
                    {
                        var attributeRel = cdaMovie.Attributes["rel"].Value;
                        if (attributeRel != null)
                        {
                            var moviePath = $"odtwarzacz-{attributeRel}.html";
                            var readyLinkToCdaMovie = new UriBuilder(_options.Url.Scheme, _options.Url.Host, _options.Url.Port, moviePath).ToString();
                            if (readyLinkToCdaMovie != null)
                            {
                                var playerMovie = new HtmlWeb();

                                var documentCdaPlayerMovie = playerMovie.Load(readyLinkToCdaMovie);
                                var cdaIframeNode = documentCdaPlayerMovie.DocumentNode.SelectSingleNode("//iframe");
                                var cdaUrl = cdaIframeNode.Attributes["src"].Value;
                                if(cdaUrl != null)
                                {
                                    
                                    _logger.Information("Found all details for episode number {ep}", episodeNumber.Value);
                                    return new EpisodeDetails()
                                    {
                                        Name = string.Join("_", episodeName.Value.Split(Path.GetInvalidFileNameChars())),
                                        Number = int.Parse(episodeNumber.Value),
                                        CdaUrl = cdaUrl
                                    };
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public List<EpisodeDetails> ReadEpisodeDetailsFromExternal(Uri startPage)
        {
            var web = new HtmlWeb();
            var result = new List<EpisodeDetails>();

            var startPageDocument = web.Load(startPage);
            var episodeList = startPageDocument.DocumentNode.SelectNodes("//tr[@class='lista_hover']");
            if (episodeList != null && episodeList.Any())
            {
                EpisodeDetails previousEpisode = null;
                foreach (var episode in episodeList)
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

                        var episodePageWeb = new HtmlWeb();
                        var concreteEpisodePage = episodePageWeb.Load(episodePageUrl);
                        var episodeDetail = GetEpisodeDetails(concreteEpisodePage);
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
                            if (episodeDetail.Number == _options.EpisodeNumber)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            var episodeDetailsJson = JsonSerializer.Serialize(result);
            File.WriteAllText(_options.EpisodeDetailsPath, episodeDetailsJson);
            return result;
        }
    }
}
