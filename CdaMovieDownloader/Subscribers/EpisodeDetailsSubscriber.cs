using PubSub;
using Serilog;

namespace CdaMovieDownloader.Subscribers
{
    public class EpisodeDetailsSubscriber
    {
        private readonly ILogger _logger;
        private readonly Hub _hub;
        private readonly IDownloader _downloader;

        public EpisodeDetailsSubscriber(ILogger logger, Hub hub, IDownloader downloader)
        {
            _logger = logger;
            _hub = hub;
            _downloader = downloader;

            _hub.Subscribe<EpisodeWithContext>(this, async data =>
            {
                data.Deconstruct(out var progressContext, out var episode);
                await _downloader.Download(progressContext, episode);
            });
        }
    }
}
