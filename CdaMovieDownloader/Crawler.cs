using CdaMovieDownloader.Extractors;
using CdaMovieDownloader.Services;
using Serilog;
using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    public interface ICrawler
    {
        Task Start(ProgressContext progressContext);
    }

    internal class Crawler : ICrawler
    {
        private readonly ILogger _logger;
        private readonly IDownloader _downloader;
        private readonly IEpisodeDetailsExtractor _episodeDetailsExtractor;
        private readonly ICheckEpisodes _checkEpisodes;
        private readonly IEpisodeService _episodeService;
        private readonly Configuration _configuration;

        public Crawler(ILogger logger, IDownloader downloader, IEpisodeDetailsExtractor episodeDetailsExtractor, 
            ICheckEpisodes checkEpisodes, Configuration configuration, IEpisodeService episodeService)
        {
            _logger = logger;
            _downloader = downloader;
            _episodeDetailsExtractor = episodeDetailsExtractor;
            _checkEpisodes = checkEpisodes;
            _configuration = configuration;
            _episodeService = episodeService;
        }

        public async Task Start(ProgressContext progressContext)
        {
            if (!Directory.Exists(_configuration.OutputDirectory))
            {
                Directory.CreateDirectory(_configuration.OutputDirectory);
            }

            var episodeDetailsToProcess = _configuration.Episodes.ToList();
            if (!episodeDetailsToProcess.Any())
            {
                episodeDetailsToProcess = await _episodeDetailsExtractor.EnrichDirectLink(progressContext, _episodeDetailsExtractor.ReadEpisodeDetailsFromExternal(new Uri(_configuration.Url)));
            }

            //filter episodes that are missed on the disk
            episodeDetailsToProcess = _checkEpisodes.GetMissingEpisodes(episodeDetailsToProcess);

            Task downloadTask = null;
            if (episodeDetailsToProcess.Any(e => !string.IsNullOrWhiteSpace(e.DirectUrl)))
            {
                downloadTask = _downloader.DownloadFiles(progressContext, episodeDetailsToProcess);
            }

            var episodesToDownload = await _episodeDetailsExtractor.EnrichDirectLink(progressContext, episodeDetailsToProcess);
            if(downloadTask != null)
            {
                await downloadTask;
            }
        }
    }
}