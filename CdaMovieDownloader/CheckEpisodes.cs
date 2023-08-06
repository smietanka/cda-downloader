using CdaMovieDownloader.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CdaMovieDownloader
{
    internal class CheckEpisodes : ICheckEpisodes
    {
        private readonly Configuration _configuration;
        private readonly IEpisodeService _episodeService;

        public CheckEpisodes(Configuration configuration, IEpisodeService episodeService)
        {
            _configuration = configuration;
            _episodeService = episodeService;
        }

        public List<int> GetDownloadedEpisodesNumbers()
        {
            return Directory.GetFiles(_configuration.OutputDirectory, "*.mp4")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Select(x => Regex.Match(x, @"^(\d{1,4})").Value)
            .Select(x => int.Parse(x))
            .OrderBy(x => x).ToList();
        }

        public List<Episode> GetMissingEpisodes(List<Episode> episodeDetails)
        {
            var baseEpisodes = _episodeService.GetAll(ep => !string.IsNullOrWhiteSpace(ep.Url));
            var allDownloadedEpisodes = GetDownloadedEpisodesNumbers();
            var missingEpisodesNumbers = baseEpisodes.Where(ep => !allDownloadedEpisodes.Contains(ep.Number)).Select(e => e.Number);
            var missingEpisodes = episodeDetails.Where(e => missingEpisodesNumbers.Contains(e.Number)).ToList();
            return missingEpisodes;
        }

        public bool IsEpisodeDownloaded(Episode episodeDetail)
        {
            return !GetMissingEpisodes(new List<Episode>() { episodeDetail }).Any();
        }
    }
}
