using CdaMovieDownloader.Contexts;
using CdaMovieDownloader.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Services
{
    public class EpisodeService : IEpisodeService
    {
        private readonly MovieContext _movieContext;
        private readonly ConfigurationOptions _configuration;

        public EpisodeService(MovieContext movieContext, IOptions<ConfigurationOptions> options)
        {
            _movieContext = movieContext;
            _configuration = options.Value;
        }

        public EpisodeDetails GetEpisodeDetails(int number)
        {
            return _movieContext.EpisodesDetails.FirstOrDefault(ep => ep.Number == number);
        }

        public async Task EditDirectLinkForEpisode(EpisodeDetails episodeDetails)
        {
            var episodeToEdit = GetEpisodeDetails(episodeDetails.Number);
            if (episodeToEdit is not null)
            {
                episodeToEdit.DirectUrl = episodeDetails.DirectUrl;
            }
            await _movieContext.SaveChangesAsync();
        }

        public async Task AddEpisode(EpisodeDetails episodeDetails)
        {
            await _movieContext.AddAsync(episodeDetails);
            await _movieContext.SaveChangesAsync();
        }

        public List<EpisodeDetails> GetAll()
        {
            return _movieContext.EpisodesDetails
                .Where(ep => ep.AnimeUrl == _configuration.Url.ToString())
                .ToList();
        }

        public List<EpisodeDetails> GetAll(Expression<Func<EpisodeDetails, bool>> prediction)
        {
            return _movieContext.EpisodesDetails
                .Where(ep => ep.AnimeUrl == _configuration.Url.ToString())
                .Where(prediction).ToList();
        }
    }
}
