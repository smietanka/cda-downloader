using CdaMovieDownloader.Common.Options;
using CdaMovieDownloader.Services;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CdaMovieDownloader;

public interface IDownloader
{
    Task DownloadFiles(ProgressContext progressContext, List<Episode> episodeDetailsWithDirectLink);
    Task Download(ProgressContext progressContext, Episode episode);
}

internal class Downloader(IConfigurationService configurationService, 
    HttpClient client, 
    ICheckEpisodes checkEpisodes, 
    IEpisodeService episodeService, 
    IOptions<ConfigurationOptions> options) : IDownloader
{
    private readonly ConfigurationOptions _options = options.Value;
    private readonly HttpClient _client = client;
    private readonly ICheckEpisodes _checkEpisodes = checkEpisodes;
    private readonly IConfigurationService _configurationService = configurationService;
    private readonly IEpisodeService _episodeService = episodeService;

    public async Task Download(ProgressContext progressContext, Episode episode)
    {
        var config = await _configurationService.GetConfigurationAsync(_options.Id);

        var fileNameExtension = Path.GetExtension(episode.DirectUrl);
        var fileName = Path.Combine(config.OutputDirectory, $"{episode.Number}-{episode.Name.Replace("\"", "")}{fileNameExtension}");
        
        if (await _checkEpisodes.IsEpisodeDownloaded(episode))
            return;

        try
        {
            using HttpResponseMessage response = await _client.GetAsync(episode.DirectUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var progressTask = progressContext.AddTask($"{episode.Number} - {episode.Name}", new ProgressTaskSettings
            {
                AutoStart = false,
                MaxValue = 100,
            });
            if (response.Content.Headers.ContentLength.HasValue)
            {
                progressTask.MaxValue(response.Content.Headers.ContentLength.Value);
                await _episodeService.EditFileSize(episode.Id, (int)response.Content.Headers.ContentLength.Value);
            }

            progressTask.StartTask();
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            var buffer = new byte[8192];
            while (true)
            {
                var read = await contentStream.ReadAsync(buffer);
                if (read == 0)
                {
                    break;
                }
                progressTask.Increment(read);
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
            }

            await _episodeService.EditIsDownloaded(episode.Id, true);
        }
        catch(HttpRequestException request)
        {
            AnsiConsole.WriteException(request);
            AnsiConsole.MarkupLine($"Removing direct link for {episode.Number} because it failed.");
            episode.DirectUrl = null;
            await _episodeService.EditDirectLinkForEpisode(episode);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            AnsiConsole.MarkupLine($"Removing {fileName}.");
            File.Delete(fileName);
        }
    }

    public async Task DownloadFiles(ProgressContext progressContext, List<Episode> episodeDetailsWithDirectLink)
    {
        foreach (var group in episodeDetailsWithDirectLink
            .Where(ep => !string.IsNullOrWhiteSpace(ep.DirectUrl))
            .Chunk(_options.ChunkGroup))
        {
            var tasks = group.Select(async episode =>
            {
                await Download(progressContext, episode);
                return true;
            });
            var isSuccess = await Task.WhenAll(tasks);
            await Task.Delay(1000);
        }
    }
}
