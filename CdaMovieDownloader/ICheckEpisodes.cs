using CdaMovieDownloader.Data;
using System.Collections.Generic;

namespace CdaMovieDownloader
{
    public interface ICheckEpisodes
    {
        List<int> GetDownloadedEpisodesNumbers();
        List<int> GetMissingDownloadedEpisodesNumber();
        List<EpisodeDetails> GetMissingEpisodes(List<EpisodeDetails> episodeDetails);

        List<int> GetGapsBetween(List<int> start, List<int> end);
    }
}
