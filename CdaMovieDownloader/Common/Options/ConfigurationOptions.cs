using System;

namespace CdaMovieDownloader.Common.Options;

public class ConfigurationOptions
{
    public Guid Id { get; set; }
    public int ChunkGroup { get; set; } = 5;
    public int ParallelThreads { get; set; } = 1;
}
