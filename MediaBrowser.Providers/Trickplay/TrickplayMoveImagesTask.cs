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
        const int Limit = 100;
        int itemCount = 0, offset = 0, previousCount;

        // This count may not be accurate, but just get something to show progress on the dashboard.
        var totalVideoCount = _libraryManager.GetCount(new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false,
            Recursive = true
        });

        var trickplayQuery = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false
        };

        do
        {
            var trickplayInfos = await _trickplayManager.GetTrickplayItemsAsync(Limit, offset).ConfigureAwait(false);
            previousCount = trickplayInfos.Count;
            offset += Limit;

            trickplayQuery.ItemIds = trickplayInfos.Select(i => i.ItemId).Distinct().ToArray();
            var items = _libraryManager.GetItemList(trickplayQuery);
            foreach (var trickplayInfo in trickplayInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var video = items.OfType<Video>().FirstOrDefault(i => i.Id.Equals(trickplayInfo.ItemId));
                if (video is null)
                {
                    continue;
                }

                itemCount++;
                try
                {
                    var libraryOptions = _libraryManager.GetLibraryOptions(video);
                    await _trickplayManager.MoveGeneratedTrickplayDataAsync(video, libraryOptions, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error moving trickplay files for {ItemName}", video.Name);
                }
            }

            progress.Report(100d * itemCount / totalVideoCount);
        } while (previousCount == Limit);

        progress.Report(100);
    }
}
