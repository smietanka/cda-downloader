using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CdaMovieDownloader.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly MovieContext _movieContext;
        public ConfigurationService(MovieContext movieContext)
        {
            _movieContext = movieContext;
        }

        public Configuration GetConfiguration(Guid id)
        {
            return _movieContext.Configurations
                .Include(e => e.Episodes)
                .SingleOrDefault(c => c.Id == id);
        }
    }
}
