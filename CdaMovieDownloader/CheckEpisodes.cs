using CdaMovieDownloader.Common.Options;
using CdaMovieDownloader.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CdaMovieDownloader;

public interface ICheckEpisodes
{
    Task<List<(double number, long fileSize)>> GetDownloadedEpisodesNumbersAsync();
    Task<List<Episode>> GetMissingEpisodesAsync(List<Episode> episodeDetails);
    Task<bool> IsEpisodeDownloaded(Episode episodeDetail);
}

internal class CheckEpisodes(IOptions<ConfigurationOptions> configuration, IEpisodeService episodeService, IConfigurationService configurationService) 
    : ICheckEpisodes
{
    private readonly ConfigurationOptions _configuration = configuration.Value;
    private readonly IEpisodeService _episodeService = episodeService;
    private readonly IConfigurationService _configurationService = configurationService;

    public async Task<List<(double number, long fileSize)>> GetDownloadedEpisodesNumbersAsync()
    {
        var config = await _configurationService.GetConfigurationAsync(_configuration.Id);

        return Directory.GetFiles(config.OutputDirectory, "*.mp4")
            .Select(filePath => new FileInfo(filePath))
            .Select(fileInfo => (double.Parse(Regex.Match(Path.GetFileNameWithoutExtension(fileInfo.FullName), @"^(\d{1,4}\.?\d?)").Value), fileInfo.Length))
            .OrderBy(x => x.Item1)
            .ToList();
    }

    public async Task<List<Episode>> GetMissingEpisodesAsync(List<Episode> episodeDetails)
    {
        var baseEpisodes = _episodeService.GetAllForConfiguration(ep => !string.IsNullOrWhiteSpace(ep.Url));
        var allDownloadedEpisodes = await GetDownloadedEpisodesNumbersAsync();

        // check if current downloaded episodes has the same file size as on the DB
        var baseEpisodesWithFileSize = baseEpisodes
            .Where(ep => ep.FileSize.HasValue).ToList();
        foreach (var (number, fileSize) in allDownloadedEpisodes)
        {
            var downloadedEpWithFileSizeInfo = baseEpisodesWithFileSize.FirstOrDefault(x => x.Number == number);
            if (downloadedEpWithFileSizeInfo != null && fileSize != downloadedEpWithFileSizeInfo.FileSize) 
            {
                var fileName = await GetFileName(downloadedEpWithFileSizeInfo.Number);
                File.Delete(fileName);
            }
        }

        var missingEpisodesNumbers = baseEpisodes
            .Where(ep => !allDownloadedEpisodes.Select(e => e.number).Contains(ep.Number))
            .Select(e => e.Number);

        var missingEpisodes = episodeDetails
            .Where(e => missingEpisodesNumbers.Contains(e.Number)).ToList();

        return missingEpisodes;
    }

    public async Task<bool> IsEpisodeDownloaded(Episode episodeDetail)
    {
        var result = await GetMissingEpisodesAsync([episodeDetail]);
        return result.Count == 0;
    }

    private async Task<string> GetFileName(double number)
    {
        var config = await _configurationService.GetConfigurationAsync(_configuration.Id);
        return Directory.GetFiles(config.OutputDirectory, $"{number}-*.mp4").FirstOrDefault();
    }
}
