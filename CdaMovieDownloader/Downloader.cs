using CdaMovieDownloader.Data;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    internal class Downloader : IDownloader
    {
        private readonly ConfigurationOptions _options;
        private readonly HttpClient _client;
        public Downloader(IOptions<ConfigurationOptions> options, HttpClient client)
        {
            _options = options.Value;
            _client = client;
        }

        public async Task Download(ProgressTask task, EpisodeDetails episode)
        {
            try
            {
                using (HttpResponseMessage response = await _client.GetAsync(episode.DirectUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    task.MaxValue(response.Content.Headers.ContentLength ?? 0);

                    task.StartTask();

                    var fileNameExtension = Path.GetExtension(episode.DirectUrl);
                    var fileName = Path.Combine(_options.OutputDirectory, $"{episode.Number}-{episode.Name.Replace("\"", "")}{fileNameExtension}");

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
                            task.Increment(read);
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

        public async Task Download(EpisodeDetails episode)
        {
            try
            {
                using (HttpResponseMessage response = await _client.GetAsync(episode.DirectUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var fileNameExtension = Path.GetExtension(episode.DirectUrl);
                    var fileName = Path.Combine(_options.OutputDirectory, $"{episode.Number}-{episode.Name.Replace("\"", "")}{fileNameExtension}");

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

        public async Task DownloadFile(EpisodeDetails episode)
        {
            await Download(episode);
        }

        public async Task DownloadFiles(ProgressContext progressContext, List<EpisodeDetails> episodeDetailsWithDirectLink)
        {
            foreach (var group in episodeDetailsWithDirectLink
                .Where(ep => !string.IsNullOrWhiteSpace(ep.DirectUrl))
                .Chunk(_options.ChunkGroup))
            {
                var tasks = group.Select(async episode =>
                {
                    var progressTask = progressContext.AddTask($"{episode.Number} - {episode.Name}", new ProgressTaskSettings
                    {
                        AutoStart = true,
                        MaxValue = 100,
                    });
                    await Download(progressTask, episode);
                    return true;
                });
                var isSuccess = await Task.WhenAll(tasks);
                await Task.Delay(1000);
            }
        }
    }
}
