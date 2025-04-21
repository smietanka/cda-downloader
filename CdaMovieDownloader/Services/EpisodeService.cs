using CdaMovieDownloader.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Services;

public interface IEpisodeService
{
    Task<Episode> GetEpisodeForConfiguration(double number);
    Episode GetEpisode(Guid id);
    Task EditDirectLinkForEpisode(Episode episodeDetails);
    Task AddEpisode(Episode episodeDetails);
    Task EditMetadata(Guid id, Dictionary<string, object> metadata);
    List<Episode> GetAllForConfiguration();
    List<Episode> GetAllForConfiguration(Expression<Func<Episode, bool>> prediction);
    Task EditFileSize(Guid id, int? size);
    Task EditIsDownloaded(Guid id, bool isDownloaded);
}

public class EpisodeService(MovieContext movieContext, IOptions<ConfigurationOptions> configuration) : IEpisodeService
{
    private readonly MovieContext _movieContext = movieContext;
    private readonly ConfigurationOptions _configuration = configuration.Value;
    private static readonly object _locker = new object();

    public async Task<Episode> GetEpisodeForConfiguration(double number)
    {
        return await _movieContext.Episodes
            .Include(e => e.Configuration)
            .FirstOrDefaultAsync(ep => ep.Number == number && ep.ConfigurationId == _configuration.Id);
    }

    public async Task EditDirectLinkForEpisode(Episode episodeDetails)
    {
        var episodeToEdit = await GetEpisodeForConfiguration(episodeDetails.Number);
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

    public async Task EditMetadata(Guid id, Dictionary<string, object> metadata)
    {
        var episode = GetEpisode(id);
        if(episode is not null)
        {
            episode.Metadata = metadata;
            await _movieContext.SaveChangesAsync();
        }
    }

    public async Task EditIsDownloaded(Guid id, bool isDownloaded)
    {
        var episode = GetEpisode(id);
        if (episode is not null)
        {
            episode.IsDownloaded = isDownloaded;
            await _movieContext.SaveChangesAsync();
        }
    }
}
