using CdaMovieDownloader.Data;
using CdaMovieDownloader.Extractors;
using CdaMovieDownloader.Services;
using Microsoft.Extensions.Options;
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
        private readonly ConfigurationOptions _options;
        private readonly IEpisodeService _episodeService;

        public Crawler(ILogger logger, IDownloader downloader, IEpisodeDetailsExtractor episodeDetailsExtractor, ICheckEpisodes checkEpisodes, IOptions<ConfigurationOptions> options, IEpisodeService episodeService)
        {
            _logger = logger;
            _downloader = downloader;
            _episodeDetailsExtractor = episodeDetailsExtractor;
            _checkEpisodes = checkEpisodes;
            _episodeService = episodeService;
            _options = options.Value;
        }

        public async Task Start(ProgressContext progressContext)
        {
            
            if (!Directory.Exists(_options.OutputDirectory))
            {
                Directory.CreateDirectory(_options.OutputDirectory);
            }

            var episodeDetailsToProcess = _episodeService.GetAll();
            if(!episodeDetailsToProcess.Any())
            {
                episodeDetailsToProcess = await _episodeDetailsExtractor.EnrichCdaDirectLink(progressContext, _episodeDetailsExtractor.ReadEpisodeDetailsFromExternal(_options.Url));
            }

            //filter episodes that are missed on the disk
            episodeDetailsToProcess = _checkEpisodes.GetMissingEpisodes(episodeDetailsToProcess);

            if (episodeDetailsToProcess.Any(e => !string.IsNullOrWhiteSpace(e.DirectUrl)))
            {
                AnsiConsole.WriteLine("Do you want to download missing episodes? [y/n] ");
                if (Console.ReadLine() == "y")
                {
                    await _downloader.DownloadFiles(progressContext, episodeDetailsToProcess);
                }
            }

            //Check episodes that doesn't have CDA Direct Link to the movie to be able download it later.
            var episodeDetailsWithMissingDirect = _episodeService.GetAll(ep => string.IsNullOrWhiteSpace(ep.DirectUrl)).ToList();
            var episodesToDownload = await _episodeDetailsExtractor.EnrichCdaDirectLink(progressContext, episodeDetailsWithMissingDirect);

        }
    }
}