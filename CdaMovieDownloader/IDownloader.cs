using Spectre.Console;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    public interface IDownloader
    {
        Task DownloadFiles(ProgressContext progressContext, List<Episode> episodeDetailsWithDirectLink);
        Task Download(ProgressContext progressContext, Episode episode);
    }
}
