using CdaMovieDownloader.Data;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    public interface IDownloader
    {
        Task DownloadFiles(List<EpisodeDetails> episodeDetailsWithDirectLink);
        Task Download(ProgressTask task, EpisodeDetails episode);
    }
}
