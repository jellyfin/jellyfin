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
/// Class TrickplayMoveImagesTask.
/// </summary>
public class TrickplayMoveImagesTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private readonly ILogger<TrickplayMoveImagesTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly ITrickplayManager _trickplayManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayMoveImagesTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="localization">The localization manager.</param>
    /// <param name="trickplayManager">The trickplay manager.</param>
    public TrickplayMoveImagesTask(
        ILogger<TrickplayMoveImagesTask> logger,
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
    public string Name => _localization.GetLocalizedString("TaskMoveTrickplayImages");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskMoveTrickplayImagesDescription");

    /// <inheritdoc />
    public string Key => "MoveTrickplayImages";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var trickplayItems = await _trickplayManager.GetTrickplayItemsAsync().ConfigureAwait(false);
        var query = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
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
            var videos = _libraryManager.GetItemList(query).OfType<Video>().ToList();
            videos.RemoveAll(i => !trickplayItems.Contains(i.Id));

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var libraryOptions = _libraryManager.GetLibraryOptions(video);
                    await _trickplayManager.MoveGeneratedTrickplayDataAsync(video, libraryOptions, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error moving trickplay files for {ItemName}", video.Name);
                }

                numComplete++;
                progress.Report(100d * numComplete / numberOfVideos);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }
}
