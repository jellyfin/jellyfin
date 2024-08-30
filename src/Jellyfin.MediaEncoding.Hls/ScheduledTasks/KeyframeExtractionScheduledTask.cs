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
    private const int Pagesize = 1000;

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
        _keyframeExtractors = keyframeExtractors.OrderByDescending(e => e.IsMetadataBased).ToArray();
    }

    /// <inheritdoc />
    public string Name => _localizationManager.GetLocalizedString("TaskKeyframeExtractor");

    /// <inheritdoc />
    public string Key => "KeyframeExtraction";

    /// <inheritdoc />
    public string Description => _localizationManager.GetLocalizedString("TaskKeyframeExtractorDescription");

    /// <inheritdoc />
    public string Category => _localizationManager.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            MediaTypes = new[] { MediaType.Video },
            IsVirtualItem = false,
            IncludeItemTypes = _itemTypes,
            DtoOptions = new DtoOptions(true),
            SourceTypes = new[] { SourceType.Library },
            Recursive = true,
            Limit = Pagesize
        };

        var numberOfVideos = _libraryManager.GetCount(query);

        var startIndex = 0;
        var numComplete = 0;

        while (startIndex < numberOfVideos)
        {
            query.StartIndex = startIndex;

            var videos = _libraryManager.GetItemList(query);
            var currentPageCount = videos.Count;
            // TODO parallelize with Parallel.ForEach?
            for (var i = 0; i < currentPageCount; i++)
            {
                var video = videos[i];
                // Only local files supported
                if (video.IsFileProtocol && File.Exists(video.Path))
                {
                    for (var j = 0; j < _keyframeExtractors.Length; j++)
                    {
                        var extractor = _keyframeExtractors[j];
                        // The cache decorator will make sure to save them in the data dir
                        if (extractor.TryExtractKeyframes(video.Path, out _))
                        {
                            break;
                        }
                    }
                }

                // Update progress
                numComplete++;
                double percent = (double)numComplete / numberOfVideos;
                progress.Report(100 * percent);
            }

            startIndex += Pagesize;
        }

        progress.Report(100);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();
}
