using CdaMovieDownloader.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Services
{
    public class EpisodeService : IEpisodeService
    {
        private readonly MovieContext _movieContext;
        private readonly Configuration _configuration;

        public EpisodeService(MovieContext movieContext, Configuration configuration)
        {
            _movieContext = movieContext;
            _configuration = configuration;
        }

        public Episode GetEpisodeDetails(Episode episodeDetails)
        {
            return _movieContext.Episodes
                .Include(e => e.Configuration)
                .FirstOrDefault(ep => ep.Number == episodeDetails.Number && ep.ConfigurationId == _configuration.Id);
        }

        public async Task EditDirectLinkForEpisode(Episode episodeDetails)
        {
            var episodeToEdit = GetEpisodeDetails(episodeDetails);
            if (episodeToEdit is not null)
            {
                episodeToEdit.DirectUrl = episodeDetails.DirectUrl;
            }
            await _movieContext.SaveChangesAsync();
        }

        public async Task AddEpisode(Episode episodeDetails)
        {
            await _movieContext.AddAsync(episodeDetails);
            await _movieContext.SaveChangesAsync();
        }

        public List<Episode> GetAll()
        {
            return _movieContext.Episodes
                .Include(_ => _.Configuration)
                //.Where(ep => ep.AnimeUrl == _configuration.Url.ToString())
                .ToList();
        }

        public List<Episode> GetAll(Expression<Func<Episode, bool>> prediction)
        {
            return _movieContext.Episodes
                //.Where(ep => ep.AnimeUrl == _configuration.Url.ToString())
                .Where(prediction).ToList();
        }
    }
}
