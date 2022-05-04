using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Scheduled task to extract embedded subtitles for immediate access in web player.
/// </summary>
public class ExtractSubtitlesTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ISubtitleEncoder _subtitleEncoder;
    private readonly ILocalizationManager _localization;
    private static readonly BaseItemKind[] _itemTypes = { BaseItemKind.Episode, BaseItemKind.Movie };

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractSubtitlesTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// /// <param name="subtitleEncoder">Instance of <see cref="ISubtitleEncoder"/> interface.</param>
    /// <param name="localization">Instance of <see cref="ILocalizationManager"/> interface.</param>
    public ExtractSubtitlesTask(
        ILibraryManager libraryManager,
        ISubtitleEncoder subtitleEncoder,
        ILocalizationManager localization)
    {
        _libraryManager = libraryManager;
        _subtitleEncoder = subtitleEncoder;
        _localization = localization;
    }

    /// <inheritdoc />
    public string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskExtractSubtitles");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskExtractSubtitlesDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            Recursive = true,
            HasSubtitles = true,
            IsVirtualItem = false,
            IncludeItemTypes = _itemTypes,
            DtoOptions = new DtoOptions(false),
            MediaTypes = new[] { MediaType.Video },
            SourceTypes = new[] { SourceType.Library },
        };

        var completedVideos = 0;
        var videos = _libraryManager.GetItemList(query);

        foreach (var video in videos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var streams = video.GetMediaStreams()
                .Where(stream => stream.IsTextSubtitleStream
                                 && stream.SupportsExternalStream
                                 && !stream.IsExternal);
            foreach (var stream in streams)
            {
                var index = stream.Index;
                var format = stream.Codec;
                var mediaSourceId = video.Id.ToString("N", CultureInfo.InvariantCulture);

                // SubtitleEncoder has readers only for this formats, everything else converted to SRT.
                if (!string.Equals(format, "ass", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(format, "ssa", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(format, "srt", StringComparison.OrdinalIgnoreCase))
                {
                    format = "srt";
                }

                await _subtitleEncoder.GetSubtitles(video, mediaSourceId, index, format, 0, 0, false, cancellationToken).ConfigureAwait(false);
            }

            completedVideos++;
            double percent = (double)completedVideos / videos.Count;
            progress.Report(100 * percent);
        }

        progress.Report(100);
    }
}
