using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Task to download lyrics.
/// </summary>
public class LyricScheduledTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private static readonly BaseItemKind[] _itemKinds = [BaseItemKind.Audio];
    private static readonly MediaType[] _mediaTypes = [MediaType.Audio];
    private static readonly SourceType[] _sourceTypes = [SourceType.Library];
    private static readonly DtoOptions _dtoOptions = new(false);

    private readonly ILibraryManager _libraryManager;
    private readonly ILyricManager _lyricManager;
    private readonly ILogger<LyricScheduledTask> _logger;
    private readonly ILocalizationManager _localizationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricScheduledTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{DownloaderScheduledTask}"/> interface.</param>
    /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    public LyricScheduledTask(
        ILibraryManager libraryManager,
        ILyricManager lyricManager,
        ILogger<LyricScheduledTask> logger,
        ILocalizationManager localizationManager)
    {
        _libraryManager = libraryManager;
        _lyricManager = lyricManager;
        _logger = logger;
        _localizationManager = localizationManager;
    }

    /// <inheritdoc />
    public string Name => _localizationManager.GetLocalizedString("TaskDownloadMissingLyrics");

    /// <inheritdoc />
    public string Key => "DownloadLyrics";

    /// <inheritdoc />
    public string Description => _localizationManager.GetLocalizedString("TaskDownloadMissingLyricsDescription");

    /// <inheritdoc />
    public string Category => _localizationManager.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var totalCount = _libraryManager.GetCount(new InternalItemsQuery
        {
            Recursive = true,
            IsVirtualItem = false,
            IncludeItemTypes = _itemKinds,
            DtoOptions = _dtoOptions,
            MediaTypes = _mediaTypes,
            SourceTypes = _sourceTypes
        });

        var completed = 0;

        foreach (var library in _libraryManager.RootFolder.Children.ToList())
        {
            var libraryOptions = _libraryManager.GetLibraryOptions(library);
            var itemQuery = new InternalItemsQuery
            {
                Recursive = true,
                IsVirtualItem = false,
                IncludeItemTypes = _itemKinds,
                DtoOptions = _dtoOptions,
                MediaTypes = _mediaTypes,
                SourceTypes = _sourceTypes,
                Limit = QueryPageLimit,
                Parent = library
            };

            int previousCount;
            var startIndex = 0;
            do
            {
                itemQuery.StartIndex = startIndex;
                var audioItems = _libraryManager.GetItemList(itemQuery);

                foreach (var audioItem in audioItems.OfType<Audio>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (audioItem.GetMediaStreams().All(s => s.Type != MediaStreamType.Lyric))
                        {
                            _logger.LogDebug("Searching for lyrics for {Path}", audioItem.Path);
                            var lyricResults = await _lyricManager.SearchLyricsAsync(
                                    new LyricSearchRequest
                                    {
                                        MediaPath = audioItem.Path,
                                        SongName = audioItem.Name,
                                        AlbumName = audioItem.Album,
                                        ArtistNames = audioItem.GetAllArtists().ToList(),
                                        Duration = audioItem.RunTimeTicks,
                                        IsAutomated = true,
                                        DisabledLyricFetchers = libraryOptions.DisabledLyricFetchers,
                                        LyricFetcherOrder = libraryOptions.LyricFetcherOrder
                                    },
                                    cancellationToken)
                                .ConfigureAwait(false);

                            if (lyricResults.Count != 0)
                            {
                                _logger.LogDebug("Saving lyrics for {Path}", audioItem.Path);
                                await _lyricManager.DownloadLyricsAsync(
                                        audioItem,
                                        libraryOptions,
                                        lyricResults[0].Id,
                                        cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error downloading lyrics for {Path}", audioItem.Path);
                    }

                    completed++;
                    progress.Report(100d * completed / totalCount);
                }

                startIndex += QueryPageLimit;
                previousCount = audioItems.Count;
            } while (previousCount > 0);
        }

        progress.Report(100);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        ];
    }
}
