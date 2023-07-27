using CdaMovieDownloader.Data;
using Serilog;
using PubSub;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using System.Net.Http;
using CdaMovieDownloader.Extensions;

namespace CdaMovieDownloader.Subscribers
{
    public class EpisodeDetailsSubscriber
    {
        private readonly ILogger _logger;
        private readonly Hub _hub;

        public EpisodeDetailsSubscriber(ILogger logger, Hub hub)
        {
            _logger = logger;
            _hub = hub;

            _hub.Subscribe<EpisodeDetails>(this, async episode =>
            {
                //await AnsiConsole.Progress()
                //    .AutoClear(false)
                //    .Columns(new ProgressColumn[]
                //    {
                //        new TaskDescriptionColumn(),
                //        new ProgressBarColumn(),
                //        new PercentageColumn(),
                //        new RemainingTimeColumn(),
                //        new SpinnerColumn(),
                //    })
                //    .StartAsync(async ctx =>
                //    {
                //        var progressTask = ctx.AddTask($"{episode.Number} - {episode.Name}", new ProgressTaskSettings
                //        {
                //            AutoStart = false
                //        });

                //    });
                await DownloadFileAsync(episode);
            });
        }

        private Task DownloadFileAsync(EpisodeDetails episode)
        {
            try
            {
                var fileNameExtension = Path.GetExtension(episode.DirectUrl);
                var fileName = $"{episode.Number}{fileNameExtension}";
                
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += (ps, pe) =>
                    {
                        //if (!progressTask.IsStarted)
                        //{
                        //    progressTask.MaxValue(pe.TotalBytesToReceive);
                        //    progressTask.StartTask();
                        //}
                        //progressTask.Increment(pe.BytesReceived);
                        Log.Logger.Information("BytesReceived: {bytes}, ProgressPercentage: {percentage}, TotalBytesToReceive: {total}", pe.BytesReceived, pe.ProgressPercentage, pe.TotalBytesToReceive);
                    };
                    webClient.Credentials = CredentialCache.DefaultCredentials;

                    return webClient.DownloadFileTaskAsync(episode.DirectUrl, fileName);
                }
            }
            catch(Exception e)
            {
                _logger.Error(e, "Something wrong with downlaod file {episode}", episode.DirectUrl);
                return Task.CompletedTask;
            }
        }
    }
}
