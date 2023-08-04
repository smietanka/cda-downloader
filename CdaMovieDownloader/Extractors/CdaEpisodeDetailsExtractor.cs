using CdaMovieDownloader.Data;
using CdaMovieDownloader.Services;
using CdaMovieDownloader.Subscribers;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using PubSub;
using Serilog;
using Spectre.Console;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public class CdaEpisodeDetailsExtractor : BaseEpisodeDetailsExtractor
    {
        private readonly EdgeOptions _edgeOptions;
        private readonly EdgeDriverService _edgeDriverService;
        private readonly EpisodeDetailsSubscriber _episodeDetailsSubscriber;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEpisodeService _episodeService;
        private static readonly object _locker = new();

        public CdaEpisodeDetailsExtractor(ILogger logger, 
            EdgeOptions edgeOptions, 
            EdgeDriverService edgeDriverService, 
            IOptions<ConfigurationOptions> options, 
            IHttpClientFactory httpClientFactory, 
            ICheckEpisodes checkEpisodes,
            Hub hub,
            IEpisodeService episodeService,
            EpisodeDetailsSubscriber episodeDetailsSubscriber)
            : base(logger, options.Value, checkEpisodes, hub, episodeService)
        {
            _edgeOptions = edgeOptions;
            _httpClientFactory = httpClientFactory;
            this._episodeService = episodeService;
            _episodeDetailsSubscriber = episodeDetailsSubscriber;
            _edgeDriverService = edgeDriverService;
        }

        public override Provider Provider => Provider.cda;

        public override string playerElementXPath => throw new NotImplementedException();

        public override async Task EnrichDirectLinkForEpisode(EpisodeDetails episode)
        {
            using(var browser = new EdgeDriver(_edgeDriverService, _edgeOptions))
            {
                var waiter = new WebDriverWait(browser, TimeSpan.FromSeconds(2));
                foreach (var quality in Constants.Qualities.SkipWhile(x => x.Key != _options.MaxQuality))
                {
                    try
                    {
                        var urlToEpisode = $"{episode.Url}?wersja={quality.Value}";
    
                        browser.Navigate().GoToUrl(urlToEpisode);

                        try
                        {
                            var isCloudFlareCheckbox = waiter.Until(x => x.FindElement(OpenQA.Selenium.By.XPath("//label[@class='ctp-checkbox-label']/input")));
                            if (isCloudFlareCheckbox != null)
                            {
                                isCloudFlareCheckbox.Click();
                            }
                        }
                        catch (Exception)
                        {

                        }

                        try
                        {
                            var isConversion = waiter.Until(b => b.FindElement(By.CssSelector("div.mediaplayer div.conversion")));
                            if (isConversion != null)
                            {
                                continue;
                            }
                        }
                        catch(Exception)
                        {

                        }
                        

                        var cdaVideoElement = waiter.Until(x => browser.FindElement(OpenQA.Selenium.By.XPath("//video[@class='pb-video-player']")));
                        if (cdaVideoElement != null)
                        {
                            var cdaDirectLinkToMovie = cdaVideoElement.GetAttribute("src");
                            if (!Uri.IsWellFormedUriString(cdaDirectLinkToMovie, UriKind.Absolute))
                                continue;
                            using (HttpResponseMessage response = await _httpClientFactory.CreateClient().GetAsync(cdaDirectLinkToMovie, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                            }
                            if (!string.IsNullOrEmpty(cdaDirectLinkToMovie))
                            {
                                lock(_locker)
                                {
                                    AnsiConsole.WriteLine($"Found direct link for episode number {episode.Number} in {quality}");
                                    //_logger.Information("Found direct link for episode number {number} in {quality}", episode.Number, quality);
                                    episode.DirectUrl = cdaDirectLinkToMovie;
                                    _episodeService.EditDirectLinkForEpisode(episode);
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //_logger.Error("Something goes wrong for {episode}, {episodeUrl}. {exception}", episode.Number, episode.Url, e.InnerException?.Message ?? e.Message);
                        continue;
                    }
                }
            }
        }

        public override Task<string> GetMovieUrl(string urlWithVideo)
        {
            var playerMovie = new HtmlWeb();
            var movieElement = playerMovie.Load(urlWithVideo);
            var cdaIframeNode = movieElement.DocumentNode.SelectSingleNode("//iframe");
            var cdaUrl = cdaIframeNode.Attributes["src"].Value;
            if (cdaUrl != null)
            {
                return Task.FromResult(cdaUrl);
            }
            return Task.FromResult(string.Empty);
        }
    }
}
