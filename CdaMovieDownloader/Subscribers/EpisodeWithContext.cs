using Spectre.Console;

namespace CdaMovieDownloader.Subscribers;

public readonly record struct EpisodeWithContext(ProgressContext progressContext, Episode episodeDetails);
