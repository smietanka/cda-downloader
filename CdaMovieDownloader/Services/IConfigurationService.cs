using System;

namespace CdaMovieDownloader.Services
{
    public interface IConfigurationService
    {
        public Configuration GetConfiguration(Guid id);
    }
}
