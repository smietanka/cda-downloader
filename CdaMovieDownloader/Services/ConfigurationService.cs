using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Services;

public interface IConfigurationService
{
    public Task<List<Configuration>> GetConfigurationsAsync();
    public Task<Configuration> GetConfigurationAsync(Guid id);
    public Task<Guid> AddConfigurationAsync(Configuration configuration);
}

public class ConfigurationService(MovieContext movieContext) : IConfigurationService
{
    private readonly MovieContext _movieContext = movieContext;

    public async Task<Guid> AddConfigurationAsync(Configuration configuration)
    {
        var result = await _movieContext.Configurations.AddAsync(configuration);
        await _movieContext.SaveChangesAsync();
        return result.Entity.Id;
    }

    public Task<Configuration> GetConfigurationAsync(Guid id)
    {
        return _movieContext.Configurations
            .Include(e => e.Episodes)
            .SingleOrDefaultAsync(c => c.Id == id);
    }

    public Task<List<Configuration>> GetConfigurationsAsync()
    {
        return _movieContext.Configurations
            .Include(e => e.Episodes)
            .ToListAsync();
    }
}
