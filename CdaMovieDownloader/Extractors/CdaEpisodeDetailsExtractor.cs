using CdaMovieDownloader.Data;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public class CdaEpisodeDetailsExtractor : BaseEpisodeDetailsExtractor
    {
        private readonly EdgeOptions _edgeOptions;
        private readonly IHttpClientFactory _httpClientFactory;

        public CdaEpisodeDetailsExtractor(ILogger logger, EdgeOptions edgeOptions, IOptions<ConfigurationOptions> options, IHttpClientFactory httpClientFactory, ICheckEpisodes checkEpisodes)
            :base(logger, options.Value, checkEpisodes)
        {
            _edgeOptions = edgeOptions;
            _httpClientFactory = httpClientFactory;
        }

        public override Provider Provider => Provider.cda;

        public override string playerElementXPath => throw new NotImplementedException();

        public override async Task EnrichDirectLinkForEpisode(EpisodeDetails episode)
        {
            using(var browser = new EdgeDriver(_edgeOptions))
            {
                var waiter = new WebDriverWait(browser, TimeSpan.FromSeconds(2));
                foreach (var quality in Constants.Qualities.SkipWhile(x => x.Key != _options.MaxQuality))
                {
                    _logger.Information("Looking for direct link for episode number {number} in {quality}", episode.Number, quality);
                    try
                    {
                        var urlToEpisode = $"{episode.Url}?wersja={quality.Value}";
    
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
                                episode.DirectUrl = cdaDirectLinkToMovie;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Something goes wrong for {episode}, {episodeUrl}", episode.Number, episode.Url);
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
