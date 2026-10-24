using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo;

/// <summary>
/// Extracts embedded subtitles for videos in libraries with subtitle extraction enabled.
/// Covers videos that were scanned before the option was turned on.
/// </summary>
public class SubtitleExtractionTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private readonly ILogger<SubtitleExtractionTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly ISubtitleEncoder _subtitleEncoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractionTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="localization">The localization manager.</param>
    /// <param name="subtitleEncoder">The subtitle encoder.</param>
    public SubtitleExtractionTask(
        ILogger<SubtitleExtractionTask> logger,
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        ISubtitleEncoder subtitleEncoder)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _localization = localization;
        _subtitleEncoder = subtitleEncoder;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskExtractSubtitles");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskExtractSubtitlesDescription");

    /// <inheritdoc />
    public string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // The first run over an existing library can take many hours. Already extracted
        // subtitles are skipped, so a run cut short by the runtime limit resumes the next night.
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks,
                MaxRuntimeTicks = TimeSpan.FromHours(4).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        // Newest first, so recently added videos are ready before older ones on the first run.
        var query = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false,
            Recursive = true,
            IncludeOwnedItems = true,
            OrderBy = [(ItemSortBy.DateCreated, SortOrder.Descending)],
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
                    if (libraryOptions is not null && libraryOptions.ExtractSubtitlesDuringLibraryScan)
                    {
                        await SubtitleExtractionProvider.ExtractSubtitlesAsync(video, _subtitleEncoder, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error extracting subtitles for {ItemName}", video.Name);
                }

                numComplete++;
                progress.Report(100d * numComplete / numberOfVideos);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }
}
