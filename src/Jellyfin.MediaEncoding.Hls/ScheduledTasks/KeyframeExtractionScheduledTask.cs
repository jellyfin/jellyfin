using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.MediaEncoding.Hls.Extractors;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.MediaEncoding.Hls.ScheduledTasks;

/// <inheritdoc />
public class KeyframeExtractionScheduledTask : IScheduledTask
{
    private readonly ILocalizationManager _localizationManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IKeyframeExtractor[] _keyframeExtractors;
    private static readonly BaseItemKind[] _itemTypes = { BaseItemKind.Episode, BaseItemKind.Movie };

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyframeExtractionScheduledTask"/> class.
    /// </summary>
    /// <param name="localizationManager">An instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="libraryManager">An instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="keyframeExtractors">The keyframe extractors.</param>
    public KeyframeExtractionScheduledTask(ILocalizationManager localizationManager, ILibraryManager libraryManager, IEnumerable<IKeyframeExtractor> keyframeExtractors)
    {
        _localizationManager = localizationManager;
        _libraryManager = libraryManager;
        _keyframeExtractors = keyframeExtractors.ToArray();
    }

    /// <inheritdoc />
    public string Name => "Keyframe Extractor";

    /// <inheritdoc />
    public string Key => "KeyframeExtraction";

    /// <inheritdoc />
    public string Description => "Extracts keyframes from video files to create more precise HLS playlists";

    /// <inheritdoc />
    public string Category => _localizationManager.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var query = new InternalItemsQuery
        {
            MediaTypes = new[] { MediaType.Video },
            IsVirtualItem = false,
            IncludeItemTypes = _itemTypes,
            DtoOptions = new DtoOptions(true),
            SourceTypes = new[] { SourceType.Library },
            Recursive = true
        };

        var videos = _libraryManager.GetItemList(query);
        var numComplete = 0;

        // TODO parallelize with Parallel.ForEach?
        for (var i = 0; i < videos.Count; i++)
        {
            var video = videos[i];
            // Only local files supported
            if (!video.IsFileProtocol || !File.Exists(video.Path))
            {
                continue;
            }

            for (var j = 0; j < _keyframeExtractors.Length; j++)
            {
                var extractor = _keyframeExtractors[j];
                // The cache decorator will make sure to save them in the data dir
                if (extractor.TryExtractKeyframes(video.Path, out _))
                {
                    break;
                }
            }

            // Update progress
            numComplete++;
            double percent = (double)numComplete / videos.Count;

            progress.Report(100 * percent);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();
}
