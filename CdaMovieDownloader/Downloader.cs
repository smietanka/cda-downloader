using CdaMovieDownloader.Common.Options;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    internal class Downloader : IDownloader
    {
        private readonly ConfigurationOptions _options;
        private readonly HttpClient _client;
        private readonly ICheckEpisodes _checkEpisodes;
        private readonly Configuration _configuration;
        public Downloader(Configuration configuration, HttpClient client, ICheckEpisodes checkEpisodes)
        {
            _configuration = configuration;
            _client = client;
            _checkEpisodes = checkEpisodes;
        }

        public async Task Download(ProgressContext progressContext, Episode episode)
        {
            try
            {
                var progressTask = progressContext.AddTask($"{episode.Number} - {episode.Name}", new ProgressTaskSettings
                {
                    AutoStart = false,
                    MaxValue = 100,
                });

                if (_checkEpisodes.IsEpisodeDownloaded(episode))
                    return;

                using (HttpResponseMessage response = await _client.GetAsync(episode.DirectUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    progressTask.MaxValue(response.Content.Headers.ContentLength ?? 0);

                    progressTask.StartTask();

                    var fileNameExtension = Path.GetExtension(episode.DirectUrl);
                    var fileName = Path.Combine(_configuration.OutputDirectory, $"{episode.Number}-{episode.Name.Replace("\"", "")}{fileNameExtension}");

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
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
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex}");
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
}
