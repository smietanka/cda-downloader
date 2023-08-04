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
        public int ParallelThreads { get; set; }
        public Quality MaxQuality { get; set; } = Quality.FHD;
    }
}
