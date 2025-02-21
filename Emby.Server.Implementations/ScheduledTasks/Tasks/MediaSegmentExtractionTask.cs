using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Task to obtain media segments.
/// </summary>
public class MediaSegmentExtractionTask : IScheduledTask
{
    /// <summary>
    /// The library manager.
    /// </summary>
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IMediaSegmentManager _mediaSegmentManager;
    private static readonly BaseItemKind[] _itemTypes = [BaseItemKind.Episode, BaseItemKind.Movie, BaseItemKind.Audio, BaseItemKind.AudioBook];

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentExtractionTask" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="localization">The localization manager.</param>
    /// <param name="mediaSegmentManager">The segment manager.</param>
    public MediaSegmentExtractionTask(ILibraryManager libraryManager, ILocalizationManager localization, IMediaSegmentManager mediaSegmentManager)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _mediaSegmentManager = mediaSegmentManager;
    }

    /// <inheritdoc/>
    public string Name => _localization.GetLocalizedString("TaskExtractMediaSegments");

    /// <inheritdoc/>
    public string Description => _localization.GetLocalizedString("TaskExtractMediaSegmentsDescription");

    /// <inheritdoc/>
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc/>
    public string Key => "TaskExtractMediaSegments";

    /// <inheritdoc/>
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        progress.Report(0);

        var pagesize = 100;

        var query = new InternalItemsQuery
        {
            MediaTypes = new[] { MediaType.Video, MediaType.Audio },
            IsVirtualItem = false,
            IncludeItemTypes = _itemTypes,
            DtoOptions = new DtoOptions(true),
            SourceTypes = new[] { SourceType.Library },
            Recursive = true,
            Limit = pagesize
        };

        var numberOfVideos = _libraryManager.GetCount(query);

        var startIndex = 0;
        var numComplete = 0;

        while (startIndex < numberOfVideos)
        {
            query.StartIndex = startIndex;

            var baseItems = _libraryManager.GetItemList(query);
            var currentPageCount = baseItems.Count;
            // TODO parallelize with Parallel.ForEach?
            for (var i = 0; i < currentPageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = baseItems[i];
                // Only local files supported
                if (item.IsFileProtocol && File.Exists(item.Path))
                {
                    await _mediaSegmentManager.RunSegmentPluginProviders(item, false, cancellationToken).ConfigureAwait(false);
                }

                // Update progress
                numComplete++;
                double percent = (double)numComplete / numberOfVideos;
                progress.Report(100 * percent);
            }

            startIndex += pagesize;
        }

        progress.Report(100);
    }

    /// <inheritdoc/>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(12).Ticks
        };
    }
}
