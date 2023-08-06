using HtmlAgilityPack;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public interface IEpisodeDetailsExtractor
    {
        Task<Episode> GetEpisodeDetails(HtmlDocument document);
        Task EnrichDirectLinkForEpisode(Episode episode);
        List<Episode> ReadEpisodeDetailsFromExternal(Uri startPage);
        Task<List<Episode>> EnrichDirectLink(ProgressContext progressContext, List<Episode> episodeDetails);
        Task<Episode> EnrichDirectLink(ProgressContext progressContext, Episode episodeDetail);
        Task<string> GetMovieUrl(string urlWithVideo);
    }
}
