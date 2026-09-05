#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.StudioImages;

/// <summary>
/// Scheduled task that keeps the local jellyfin-artwork bundle in sync with the latest GitHub release.
/// </summary>
public class RefreshStudioArtworkTask : IScheduledTask
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly ILogger<RefreshStudioArtworkTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshStudioArtworkTask"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{RefreshStudioArtworkTask}"/> interface.</param>
    public RefreshStudioArtworkTask(
        IHttpClientFactory httpClientFactory,
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        IServerConfigurationManager configurationManager,
        ILogger<RefreshStudioArtworkTask> logger)
    {
        _httpClientFactory = httpClientFactory;
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _configurationManager = configurationManager;
        _logger = logger;
    }

    public string Name => "Refresh studio artwork";

    public string Description => "Downloads the latest jellyfin-artwork release and queues studio image refreshes when the bundle changes.";

    public string Category => "Library";

    public string Key => "RefreshStudioArtwork";

    public bool IsHidden => false;

    public bool IsEnabled => true;

    public bool IsLogged => true;

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger },
            new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(24).Ticks }
        ];
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);

        bool updated;
        try
        {
            updated = await StudioArtworkManager.EnsureUpToDateAsync(_httpClientFactory, _logger, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh studio artwork from jellyfin-artwork");
            progress.Report(100);
            return;
        }

        // Bundle download is a tiny share of total runtime once we refresh every studio inline;
        // give it the first 5% and leave 95% for the per-studio loop so the bar tracks the work
        // that actually takes time.
        progress.Report(5);

        var config = Plugin.Instance.Configuration;
        var forceReplaceAll = config.ReplaceAllImagesOnNextRun;

        if (updated || forceReplaceAll)
        {
            await RefreshStudioImagesAsync(forceReplaceAll, progress, cancellationToken).ConfigureAwait(false);
        }

        if (forceReplaceAll)
        {
            config.ReplaceAllImagesOnNextRun = false;
            Plugin.Instance.SaveConfiguration();
        }

        progress.Report(100);
    }

    private async Task RefreshStudioImagesAsync(bool replaceAllImages, IProgress<double> progress, CancellationToken cancellationToken)
    {
        // GetItemList returns synthesized studio rows that don't have a persisted entity yet
        // (Id == Guid.Empty); RefreshSingleItem would throw on those. Resolve each by name to
        // ensure the item-by-name entity exists, then refresh only the ones with a real Id.
        var studios = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Studio]
        });

        if (studios.Count == 0)
        {
            return;
        }

        // Sharing one DirectoryService across all workers lets it cache directory listings between studios
        var directoryService = new DirectoryService(_fileSystem);
        var options = new MetadataRefreshOptions(directoryService)
        {
            ImageRefreshMode = MetadataRefreshMode.FullRefresh,
            ReplaceImages = [ImageType.Primary, ImageType.Thumb, ImageType.Logo]
        };

        // Re-use the same concurrency rule the rest of the library scheduler does.
        var parallelism = ResolveScanConcurrency();

        var processed = 0;
        var refreshed = 0;
        var total = studios.Count;

        await Parallel.ForEachAsync(
            studios,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = parallelism,
                CancellationToken = cancellationToken
            },
            async (item, ct) =>
            {
                BaseItem studio;
                if (item.Id.Equals(Guid.Empty))
                {
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        ReportProcessed(progress, ref processed, total);
                        return;
                    }

                    var resolved = _libraryManager.GetStudio(item.Name);
                    if (resolved is null || resolved.Id.Equals(Guid.Empty))
                    {
                        ReportProcessed(progress, ref processed, total);
                        return;
                    }

                    studio = resolved;
                }
                else
                {
                    studio = item;
                }

                try
                {
                    await _providerManager.RefreshSingleItem(studio, options, ct).ConfigureAwait(false);
                    Interlocked.Increment(ref refreshed);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Keep iterating: one studio failing (provider hiccup, transient I/O error, ...) shouldn't stop the rest of the bulk refresh.
                    _logger.LogWarning(ex, "Failed to refresh artwork for studio {Name}", studio.Name);
                }

                ReportProcessed(progress, ref processed, total);
            }).ConfigureAwait(false);

        _logger.LogInformation(
            "Refreshed images for {Refreshed}/{Total} studios (parallelism: {Parallelism}, replaceAllImages: {ReplaceAll})",
            refreshed,
            total,
            parallelism,
            replaceAllImages);
    }

    private static void ReportProcessed(IProgress<double> progress, ref int processed, int total)
    {
        var current = Interlocked.Increment(ref processed);
        progress.Report(5 + (current * 95.0 / total));
    }

    private int ResolveScanConcurrency()
    {
        var fanout = _configurationManager.Configuration.LibraryScanFanoutConcurrency;
        if (fanout == 1)
        {
            return 1;
        }

        if (fanout > 1)
        {
            return fanout;
        }

        return Math.Max(1, Environment.ProcessorCount - 3);
    }
}
