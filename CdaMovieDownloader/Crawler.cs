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

            if (episodeDetailsToProcess.Any(e => !string.IsNullOrWhiteSpace(e.DirectUrl)))
            {
                AnsiConsole.WriteLine("Do you want to download missing episodes? [y/n] ");
                if (Console.ReadLine() == "y")
                {
                    _downloader.DownloadFiles(progressContext, episodeDetailsToProcess);
                }
            }

            //Check episodes that doesn't have CDA Direct Link to the movie to be able download it later.
            var episodeDetailsWithMissingDirect = _configuration.Episodes.Where(ep => string.IsNullOrWhiteSpace(ep.DirectUrl)).ToList();
            episodeDetailsWithMissingDirect = _checkEpisodes.GetMissingEpisodes(episodeDetailsWithMissingDirect);
            var episodesToDownload = await _episodeDetailsExtractor.EnrichDirectLink(progressContext, episodeDetailsWithMissingDirect);
        }
    }
}