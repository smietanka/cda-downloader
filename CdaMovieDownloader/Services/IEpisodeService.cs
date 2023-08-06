using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Services
{
    public interface IEpisodeService
    {
        Episode GetEpisodeDetails(Episode episodeDetails);
        Task EditDirectLinkForEpisode(Episode episodeDetails);
        Task AddEpisode(Episode episodeDetails);
        List<Episode> GetAll();
        List<Episode> GetAll(Expression<Func<Episode, bool>> prediction);
    }
}
