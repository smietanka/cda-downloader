using CdaMovieDownloader.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CdaMovieDownloader
{
    public interface ICheckEpisodes
    {
        List<(int number, long fileSize)> GetDownloadedEpisodesNumbers();
        List<Episode> GetMissingEpisodes(List<Episode> episodeDetails);
        bool IsEpisodeDownloaded(Episode episodeDetail);
    }

    internal class CheckEpisodes : ICheckEpisodes
    {
        private readonly Configuration _configuration;
        private readonly IEpisodeService _episodeService;

        public CheckEpisodes(Configuration configuration, IEpisodeService episodeService)
        {
            _configuration = configuration;
            _episodeService = episodeService;
        }

        public List<(int number, long fileSize)> GetDownloadedEpisodesNumbers()
        {
            return Directory.GetFiles(_configuration.OutputDirectory, "*.mp4")
            .Select(filePath => new FileInfo(filePath))
            .Select(fileInfo => (int.Parse(Regex.Match(Path.GetFileNameWithoutExtension(fileInfo.FullName), @"^(\d{1,4})").Value), fileInfo.Length))
            .OrderBy(x => x.Item1).ToList();
        }

        public List<Episode> GetMissingEpisodes(List<Episode> episodeDetails)
        {
            var baseEpisodes = _episodeService.GetAllForConfiguration(ep => !string.IsNullOrWhiteSpace(ep.Url));
            var allDownloadedEpisodes = GetDownloadedEpisodesNumbers();

            // check if current downloaded episodes has the same file size as on the DB
            var baseEpisodesWithFileSize = baseEpisodes
                .Where(ep => ep.FileSize.HasValue).ToList();
            foreach(var physicalEpisode in allDownloadedEpisodes)
            {
                var downloadedEpWithFileSizeInfo = baseEpisodesWithFileSize.FirstOrDefault(x => x.Number == physicalEpisode.number);
                if (downloadedEpWithFileSizeInfo != null && physicalEpisode.fileSize != downloadedEpWithFileSizeInfo.FileSize) {
                    var fileName = GetFileName(downloadedEpWithFileSizeInfo.Number);
                    File.Delete(fileName);
                }
            }

            var missingEpisodesNumbers = baseEpisodes
                .Where(ep => !allDownloadedEpisodes.Select(e => e.number).Contains(ep.Number))
                .Select(e => e.Number);

            var missingEpisodes = episodeDetails
                .Where(e => missingEpisodesNumbers.Contains(e.Number)).ToList();
            return missingEpisodes;
        }

        public bool IsEpisodeDownloaded(Episode episodeDetail)
        {
            return !GetMissingEpisodes(new List<Episode>() { episodeDetail }).Any();
        }

        private string GetFileName(int number)
        {
            return Directory.GetFiles(_configuration.OutputDirectory, $"{number}-*.mp4").FirstOrDefault();
        }
    }
}
