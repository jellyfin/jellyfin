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

    private string? _currentDescription;

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshTrickplayImages");

    /// <inheritdoc />
    public string Description =>
        string.IsNullOrEmpty(_currentDescription)
            ? _localization.GetLocalizedString("TaskRefreshTrickplayImagesDescription")
            : _currentDescription;

    /// <inheritdoc />
    public string Key => "RefreshTrickplayImages";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var virtualFolders = _libraryManager.GetVirtualFolders();
        var eligibleFolders = virtualFolders
            .Where(vf => vf.LibraryOptions?.EnableTrickplayImageExtraction ?? false)
            .ToList();

        if (eligibleFolders.Count == 0)
        {
            _logger.LogInformation("No libraries have trickplay extraction enabled.");
            progress.Report(100);
            return;
        }

        var allVideos = new List<Video>();
        foreach (var folder in eligibleFolders)
        {
            if (Guid.TryParse(folder.ItemId, out var folderId))
            {
                var query = new InternalItemsQuery
                {
                    MediaTypes = [MediaType.Video],
                    IsVirtualItem = false,
                    IsFolder = false,
                    Recursive = true,
                    ParentId = folderId
                };

                allVideos.AddRange(_libraryManager.GetItemList(query).OfType<Video>());
            }
        }

        var numberOfVideos = allVideos.Count;
        _logger.LogInformation("Found {Count} videos in trickplay-enabled libraries.", numberOfVideos);

        if (numberOfVideos == 0)
        {
            progress.Report(100);
            return;
        }

        var completedCount = 0;

        try
        {
            for (var i = 0; i < allVideos.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var video = allVideos[i];
                var videoIndex = i + 1;

                _currentDescription = $"Video #{videoIndex}/{numberOfVideos}: {video.Name} - 0%";
                progress.Report(100d * completedCount / numberOfVideos);

                try
                {
                    var libraryOptions = _libraryManager.GetLibraryOptions(video);
                    var videoProgress = new Progress<double>(currentProgress =>
                    {
                        _currentDescription = $"Video #{videoIndex}/{numberOfVideos}: {video.Name} - {currentProgress:F0}%";
                        var overall = 100d * (completedCount + (currentProgress / 100d)) / numberOfVideos;
                        progress.Report(Math.Min(100d, overall));
                    });

                    _logger.LogInformation("Starting trickplay extraction for Video #{Index}/{Total}: \"{Name}\"", videoIndex, numberOfVideos, video.Name);

                    await _trickplayManager.RefreshTrickplayDataAsync(
                        video,
                        false,
                        libraryOptions,
                        cancellationToken,
                        videoProgress).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating trickplay files for {ItemName}", video.Name);
                }

                completedCount++;
                progress.Report(100d * completedCount / numberOfVideos);
            }
        }
        finally
        {
            _currentDescription = null;
        }

        progress.Report(100);
    }
}
