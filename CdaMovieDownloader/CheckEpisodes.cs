using CdaMovieDownloader.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CdaMovieDownloader
{
    internal class CheckEpisodes : ICheckEpisodes
    {
        private readonly ConfigurationOptions _options;

        public CheckEpisodes(IOptions<ConfigurationOptions> options)
        {
            _options = options.Value;
        }

        public List<int> GetMissingEpisodesNumber()
        {
            var result = new List<int>();
            if (!Directory.Exists(_options.OutputDirectory))
            {
                Directory.CreateDirectory(_options.OutputDirectory);
            }
            if (!Directory.GetFiles(_options.OutputDirectory).Any())
            {
                return result;
            }
            var allDownloadedEpisodes = GetDownloadedEpisodesNumbers();
            if (allDownloadedEpisodes.Any())
            {
                var isSequential = Enumerable.Range(allDownloadedEpisodes.Min(), allDownloadedEpisodes.Count).SequenceEqual(allDownloadedEpisodes);
                int previousEpisode = allDownloadedEpisodes.First();
                foreach (var episode in allDownloadedEpisodes.Skip(1))
                {
                    var diff = episode - previousEpisode;
                    if (diff != 1)
                    {
                        var rangeToAdd = Enumerable.Range(previousEpisode + 1, diff - 1);
                        result.AddRange(rangeToAdd);
                    }
                    previousEpisode = episode;
                }
                return result.OrderBy(x => x).ToList();
            }
            return result;
        }

        public List<int> GetDownloadedEpisodesNumbers()
        {
            return Directory.GetFiles(_options.OutputDirectory, "*.mp4")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Select(x => Regex.Match(x, @"\d{1,4}").Value)
            .Select(x => int.Parse(x))
            .OrderBy(x => x).ToList();
        }

        public List<EpisodeDetails> GetMissingEpisodes(List<EpisodeDetails> episodeDetails)
        {
            var allDownloadedEpisodes = GetDownloadedEpisodesNumbers();
            var missingEpisodes = episodeDetails.Select(x => x.Number).Except(allDownloadedEpisodes);
            return episodeDetails.Where(ep => missingEpisodes.Contains(ep.Number)).ToList();
        }
    }
}
