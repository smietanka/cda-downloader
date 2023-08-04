using CdaMovieDownloader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Services
{
    public interface IEpisodeService
    {
        EpisodeDetails GetEpisodeDetails(int number);
        Task EditDirectLinkForEpisode(EpisodeDetails episodeDetails);
        Task AddEpisode(EpisodeDetails episodeDetails);
        List<EpisodeDetails> GetAll();
        List<EpisodeDetails> GetAll(Expression<Func<EpisodeDetails, bool>> prediction);
    }
}
