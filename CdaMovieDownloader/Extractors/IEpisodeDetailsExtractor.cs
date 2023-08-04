using CdaMovieDownloader.Data;
using HtmlAgilityPack;
using OpenQA.Selenium.Edge;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public interface IEpisodeDetailsExtractor
    {
        Task<EpisodeDetails> GetEpisodeDetails(HtmlDocument document);
        Task EnrichDirectLinkForEpisode(EpisodeDetails episode);
        List<EpisodeDetails> ReadEpisodeDetailsFromExternal(Uri startPage);
        Task<List<EpisodeDetails>> EnrichCdaDirectLink(ProgressContext progressContext, List<EpisodeDetails> episodeDetails);
        Task<EpisodeDetails> EnrichCdaDirectLink(ProgressContext progressContext, EpisodeDetails episodeDetail);
        Task<string> GetMovieUrl(string urlWithVideo);
    }
}
