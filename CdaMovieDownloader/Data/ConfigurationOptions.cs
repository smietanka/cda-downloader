using System;
using System.IO;

namespace CdaMovieDownloader.Data
{
    public class ConfigurationOptions
    {
        public Uri Url { get; set; }
        public string OutputDirectory { get; set; }
        public Provider Provider { get; set; }
        public int ChunkGroup { get; set; }
        public string EpisodeDetailsPath => Path.Combine(OutputDirectory, $"{Provider}_episodeDetails.json");
        public string EpisodeDetailsWithDirectPath => Path.Combine(OutputDirectory, $"{Provider}_episodeDetailsWithDirect.json");
        public Quality MaxQuality { get; set; } = Quality.FHD;
    }
}
