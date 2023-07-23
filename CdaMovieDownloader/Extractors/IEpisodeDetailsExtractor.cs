using CdaMovieDownloader.Data;
using HtmlAgilityPack;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Extractors
{
    public interface IEpisodeDetailsExtractor
    {
        EpisodeDetails GetEpisodeDetails(HtmlDocument document);
        Task EnrichDirectLinkForEpisode(EpisodeDetails episode);
        List<EpisodeDetails> ReadEpisodeDetailsFromExternal(Uri startPage);
        Task<List<EpisodeDetails>> EnrichCdaDirectLink(List<EpisodeDetails> episodeDetails);
        Task<EpisodeDetails> EnrichCdaDirectLink(EpisodeDetails episodeDetail);
    }
}
