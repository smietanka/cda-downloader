﻿using CdaMovieDownloader.Data;
using CdaMovieDownloader.Extractors;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        public Crawler(ILogger logger, IDownloader downloader, IEpisodeDetailsExtractor episodeDetailsExtractor, ICheckEpisodes checkEpisodes, IOptions<ConfigurationOptions> options)
        {
            _logger = logger;
            _downloader = downloader;
            _episodeDetailsExtractor = episodeDetailsExtractor;
            _checkEpisodes = checkEpisodes;
            _options = options.Value;
        }

        public async Task Start()
        {
            var episodeDetails = new List<EpisodeDetails>();
            if(!Directory.Exists(_options.OutputDirectory))
            {
                Directory.CreateDirectory(_options.OutputDirectory);
            }

            if (File.Exists(_options.EpisodeDetailsPath))
            {
                using (var reader = new StreamReader(_options.EpisodeDetailsPath))
                {
                    string json = reader.ReadToEnd();
                    episodeDetails = JsonSerializer.Deserialize<List<EpisodeDetails>>(json);
                }
                if (File.Exists(_options.EpisodeDetailsWithDirectPath))
                {
                    using (var reader = new StreamReader(_options.EpisodeDetailsWithDirectPath))
                    {
                        string json = reader.ReadToEnd();
                        episodeDetails = JsonSerializer.Deserialize<List<EpisodeDetails>>(json);
                    }
                }
                else
                {
                    episodeDetails = await _episodeDetailsExtractor.EnrichCdaDirectLink(episodeDetails);
                }
            }

            if (episodeDetails.Any())
            {
                _logger.Information("There was found a file with downloaded episodes details. Do you want to overwrite this file and proceed again? [y/n]");
                var userChoice = Console.ReadLine();
                if (userChoice == "y")
                {
                    episodeDetails = await _episodeDetailsExtractor.EnrichCdaDirectLink(_episodeDetailsExtractor.ReadEpisodeDetailsFromExternal(_options.Url));
                }
            }
            else
            {
                episodeDetails = await _episodeDetailsExtractor.EnrichCdaDirectLink(_episodeDetailsExtractor.ReadEpisodeDetailsFromExternal(_options.Url));
            }

            var episodeDetailsWithMissingDirect = episodeDetails.Where(x => string.IsNullOrWhiteSpace(x.CdaDirectUrl)).ToList();
            if (File.Exists(_options.EpisodeDetailsWithDirectPath) && episodeDetailsWithMissingDirect.Any())
            {
                _logger.Warning("Found direct links but there are empty. Trying to refresh");
                foreach(var missingDirect in episodeDetailsWithMissingDirect)
                {
                    _logger.Information("Refreshing direct link for {number} - {name}", missingDirect.Number, missingDirect.Name);
                    await _episodeDetailsExtractor.EnrichCdaDirectLink(missingDirect);
                }
            }

            var missingDownloadedEpisodes = _checkEpisodes.GetMissingEpisodesNumber();
            if (missingDownloadedEpisodes.Any())
            {
                _logger.Warning("There was missing episodes! {missingEpisodes}. Do you want to downlaod only missing episodes? [y/n]", missingDownloadedEpisodes);
                var userChoice = Console.ReadLine();
                if (userChoice == "y")
                {
                    episodeDetails = episodeDetails.Where(x => missingDownloadedEpisodes.Contains(x.Number)).ToList();
                }
            }

            var leftEpisodesToDownload = _checkEpisodes.GetMissingEpisodes(episodeDetails);
            if (leftEpisodesToDownload.Any())
            {
                episodeDetails = leftEpisodesToDownload.ToList();
            }
            _logger.Information("Found episodes with direct links. Starting download.");
            await _downloader.DownloadFiles(episodeDetails);
            _logger.Information("Finished downloading.");
        }
    }
}