using CdaMovieDownloader.Data;
using HtmlAgilityPack;
using Serilog;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CdaMovieDownloader.Extractors
{
    public class EpisodeDetailsExtractor
    {
        private readonly ILogger _logger;
        private readonly Uri _startPage;
        private readonly Regex _episodeNumber = new Regex(@"\d{2,4}", RegexOptions.Compiled);
        private readonly Regex _episodeName = new Regex(@"""(.*)""", RegexOptions.Compiled);
        public EpisodeDetailsExtractor(ILogger logger, Uri startPage)
        {
            _logger = logger;
            _startPage = startPage;
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
                    var episodeName = _episodeName.Match(episodeNameElement.InnerText);

                    if (cdaMovie != null && episodeNumber.Success && episodeName.Success)
                    {
                        var attributeRel = cdaMovie.Attributes["rel"].Value;
                        if (attributeRel != null)
                        {
                            var moviePath = $"odtwarzacz-{attributeRel}.html";
                            var readyLinkToCdaMovie = new UriBuilder(_startPage.Scheme, _startPage.Host, _startPage.Port, moviePath).ToString();
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
                                        Name = episodeName.Value,
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
    }
}
