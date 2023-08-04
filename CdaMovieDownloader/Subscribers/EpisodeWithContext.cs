using CdaMovieDownloader.Data;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Subscribers
{
    public readonly record struct EpisodeWithContext(ProgressContext progressContext, EpisodeDetails episodeDetails);
}
