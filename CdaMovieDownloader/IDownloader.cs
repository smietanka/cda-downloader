using CdaMovieDownloader.Data;
using Spectre.Console;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    public interface IDownloader
    {
        Task DownloadFiles(ProgressContext progressContext, List<EpisodeDetails> episodeDetailsWithDirectLink);
        Task DownloadFile(EpisodeDetails episode);
        Task Download(ProgressTask task, EpisodeDetails episode);
        Task Download(EpisodeDetails episode);
    }
}
