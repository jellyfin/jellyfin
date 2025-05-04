using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Trickplay;

/// <summary>
/// Class TrickplayImagesTask.
/// </summary>
public class TrickplayImagesTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private readonly ILogger<TrickplayImagesTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly ITrickplayManager _trickplayManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayImagesTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="localization">The localization manager.</param>
    /// <param name="trickplayManager">The trickplay manager.</param>
    public TrickplayImagesTask(
        ILogger<TrickplayImagesTask> logger,
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        ITrickplayManager trickplayManager)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _localization = localization;
        _trickplayManager = trickplayManager;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshTrickplayImages");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRefreshTrickplayImagesDescription");

    /// <inheritdoc />
    public string Key => "RefreshTrickplayImages";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            MediaTypes = new[] { MediaType.Video },
            SourceTypes = new[] { SourceType.Library },
            IsVirtualItem = false,
            IsFolder = false,
            Recursive = true,
            Limit = QueryPageLimit
        };

        var numberOfVideos = _libraryManager.GetCount(query);

        var startIndex = 0;
        var numComplete = 0;

        while (startIndex < numberOfVideos)
        {
            query.StartIndex = startIndex;
            var videos = _libraryManager.GetItemList(query).OfType<Video>();

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var libraryOptions = _libraryManager.GetLibraryOptions(video);
                    await _trickplayManager.RefreshTrickplayDataAsync(video, false, libraryOptions, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating trickplay files for {ItemName}", video.Name);
                }

                numComplete++;
                progress.Report(100d * numComplete / numberOfVideos);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }
}
