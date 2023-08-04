using CdaMovieDownloader.Data;
using CdaMovieDownloader.Services;
using Microsoft.Extensions.Options;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using PubSub;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    internal class Mp4upEpisodeDetailsExtractor : BaseEpisodeDetailsExtractor
    {
        private readonly EdgeOptions _edgeOptions;

        public Mp4upEpisodeDetailsExtractor(ILogger logger, 
            IOptions<ConfigurationOptions> options, 
            EdgeOptions edgeOptions, 
            ICheckEpisodes checkEpisodes,
            IEpisodeService episodeService,
            Hub hub) : base(logger, options.Value, checkEpisodes, hub, episodeService)
        {
            _edgeOptions = edgeOptions;
        }

        public override Provider Provider => Provider.mp4up;

        public override string playerElementXPath => "//video[@class='vjs-tech']";

        public override Task EnrichDirectLinkForEpisode(EpisodeDetails episode)
        {
            episode.DirectUrl = episode.Url;
            return Task.CompletedTask;
        }

        public override async Task<string> GetMovieUrl(string urlWithVideo)
        {
            using (var browser = new EdgeDriver(_edgeOptions))
            {
                var waiter = new WebDriverWait(browser, TimeSpan.FromSeconds(2));
                browser.Navigate().GoToUrl(urlWithVideo);
                var videoElement = waiter.Until(x => browser.FindElement(OpenQA.Selenium.By.XPath("//video[@class='vjs-tech']")));
                var url = videoElement.GetAttribute("src");
                if (url != null)
                {
                    await Task.Delay(3000);
                    return url;
                }
                return string.Empty;
            }
        }
    }
}
