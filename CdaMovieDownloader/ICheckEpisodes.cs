using System.Collections.Generic;

namespace CdaMovieDownloader
{
    public interface ICheckEpisodes
    {
        List<int> GetDownloadedEpisodesNumbers();
        List<Episode> GetMissingEpisodes(List<Episode> episodeDetails);
        bool IsEpisodeDownloaded(Episode episodeDetail);
    }
}
