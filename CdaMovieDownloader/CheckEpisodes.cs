using CdaMovieDownloader.Data;
using CdaMovieDownloader.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CdaMovieDownloader
{
    internal class CheckEpisodes : ICheckEpisodes
    {
        private readonly ConfigurationOptions _options;
        private readonly IEpisodeService _episodeService;

        public CheckEpisodes(IOptions<ConfigurationOptions> options, IEpisodeService episodeService)
        {
            _options = options.Value;
            _episodeService = episodeService;
        }

        public List<int> GetDownloadedEpisodesNumbers()
        {
            return Directory.GetFiles(_options.OutputDirectory, "*.mp4")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Select(x => Regex.Match(x, @"^(\d{1,4})").Value)
            .Select(x => int.Parse(x))
            .OrderBy(x => x).ToList();
        }

        public List<EpisodeDetails> GetMissingEpisodes(List<EpisodeDetails> episodeDetails)
        {
            var baseEpisodes = _episodeService.GetAll(ep => !string.IsNullOrWhiteSpace(ep.Url));
            var allDownloadedEpisodes = GetDownloadedEpisodesNumbers();
            var missingEpisodesNumbers = baseEpisodes.Where(ep => !allDownloadedEpisodes.Contains(ep.Number)).Select(e => e.Number);
            var missingEpisodes = episodeDetails.Where(e => missingEpisodesNumbers.Contains(e.Number)).ToList();
            return missingEpisodes;
        }
    }
}
