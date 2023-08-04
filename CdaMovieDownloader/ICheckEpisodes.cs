using CdaMovieDownloader.Data;
using System.Collections.Generic;

namespace CdaMovieDownloader
{
    public interface ICheckEpisodes
    {
        List<int> GetDownloadedEpisodesNumbers();
        List<EpisodeDetails> GetMissingEpisodes(List<EpisodeDetails> episodeDetails);
    }
}
