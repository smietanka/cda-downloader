using CdaMovieDownloader.Common.Options;
using CdaMovieDownloader.Extractors;
using CdaMovieDownloader.Services;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using OpenQA.Selenium.Edge;
using Polly;
using Serilog;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CdaMovieDownloader;

public interface ICrawler
{
    Task Start(ProgressContext progressContext);
}

internal class Crawler(ILogger logger, IDownloader downloader, IEpisodeDetailsExtractor episodeDetailsExtractor, IEpisodeService episodeService,
    ICheckEpisodes checkEpisodes, IConfigurationService configurationService,
    IOptions<ConfigurationOptions> options) : ICrawler
{
    private readonly ILogger _logger = logger;
    private readonly IDownloader _downloader = downloader;
    private readonly IEpisodeDetailsExtractor _episodeDetailsExtractor = episodeDetailsExtractor;
    private readonly IEpisodeService _episodeService = episodeService;
    private readonly ICheckEpisodes _checkEpisodes = checkEpisodes;
    private readonly IConfigurationService _configurationService = configurationService;
    private readonly ConfigurationOptions _options = options.Value;

    public async Task Start(ProgressContext progressContext)
    {
        var config = await _configurationService.GetConfigurationAsync(_options.Id);
        if (!Directory.Exists(config.OutputDirectory))
        {
            Directory.CreateDirectory(config.OutputDirectory);
        }

        var episodesToDownload = new List<Episode>();
        // not downloaded episodes without direct url
        var episodesToExtractDirect = config.Episodes.Where(x => !x.IsDownloaded && string.IsNullOrEmpty(x.DirectUrl)).ToList();
        if (episodesToExtractDirect.Any())
        {
            episodesToDownload.AddRange(await _episodeDetailsExtractor.EnrichDirectLink(progressContext, episodesToExtractDirect));
        }

        var notDownloadedEpisodes = config.Episodes.Where(x => !x.IsDownloaded).ToList();
        if (notDownloadedEpisodes.Any())
        {
            episodesToDownload.AddRange(notDownloadedEpisodes);
        }

        //filter episodes that are missed on the disk
        episodesToDownload = await _checkEpisodes.GetMissingEpisodesAsync(episodesToDownload);

        Task downloadTask = null;
        if (episodesToDownload.Any(e => !string.IsNullOrWhiteSpace(e.DirectUrl)))
        {
            AnsiConsole.WriteLine($"Downloading {episodesToDownload.Count} episodes");
            downloadTask = _downloader.DownloadFiles(progressContext, episodesToDownload);
        }

        if(downloadTask != null)
        {
            await downloadTask;
        }
    }
}