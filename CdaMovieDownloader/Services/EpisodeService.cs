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
    public interface IEpisodeService
    {
        Episode GetEpisodeForConfiguration(int number);
        Episode GetEpisode(Guid id);
        Task EditDirectLinkForEpisode(Episode episodeDetails);
        Task AddEpisode(Episode episodeDetails);
        List<Episode> GetAllForConfiguration();
        List<Episode> GetAllForConfiguration(Expression<Func<Episode, bool>> prediction);
        Task EditFileSize(Guid id, int? size);
    }

    public class EpisodeService : IEpisodeService
    {
        private readonly MovieContext _movieContext;
        private readonly Configuration _configuration;

        public EpisodeService(MovieContext movieContext, Configuration configuration)
        {
            _movieContext = movieContext;
            _configuration = configuration;
        }

        public Episode GetEpisodeForConfiguration(int number)
        {
            return _movieContext.Episodes
                .Include(e => e.Configuration)
                .FirstOrDefault(ep => ep.Number == number && ep.ConfigurationId == _configuration.Id);
        }

        public async Task EditDirectLinkForEpisode(Episode episodeDetails)
        {
            var episodeToEdit = GetEpisodeForConfiguration(episodeDetails.Number);
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

        public List<Episode> GetAllForConfiguration()
        {
            return _movieContext.Episodes
                .Where(ep => ep.ConfigurationId == _configuration.Id)
                .Include(_ => _.Configuration)
                .ToList();
        }

        public List<Episode> GetAllForConfiguration(Expression<Func<Episode, bool>> prediction)
        {
            return _movieContext.Episodes
                .Where(ep => ep.ConfigurationId == _configuration.Id)
                .Where(prediction)
                .Include(_ => _.Configuration)
                .ToList();
        }

        public async Task EditFileSize(Guid id, int? size)
        {
            var episodeToEdit = GetEpisode(id);
            if(episodeToEdit is not null)
            {
                episodeToEdit.FileSize = size;
                await _movieContext.SaveChangesAsync();
            }
        }

        public Episode GetEpisode(Guid id)
        {
            return _movieContext.Episodes.SingleOrDefault(w => w.Id == id);
        }
    }
}
