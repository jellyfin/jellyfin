using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Data.Enums;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.IO;
using Jellyfin.LiveTv.Timers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Recordings;

/// <inheritdoc cref="IRecordingsManager" />
public sealed class RecordingsManager : IRecordingsManager, IDisposable
{
    private readonly ILogger<RecordingsManager> _logger;
    private readonly IServerConfigurationManager _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryManager _libraryManager;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly IProviderManager _providerManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IStreamHelper _streamHelper;
    private readonly TimerManager _timerManager;
    private readonly SeriesTimerManager _seriesTimerManager;
    private readonly RecordingsMetadataManager _recordingsMetadataManager;

    private readonly ConcurrentDictionary<string, ActiveRecordingInfo> _activeRecordings = new(StringComparer.OrdinalIgnoreCase);
    private readonly AsyncNonKeyedLocker _recordingDeleteSemaphore = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingsManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="config">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="libraryMonitor">The <see cref="ILibraryMonitor"/>.</param>
    /// <param name="providerManager">The <see cref="IProviderManager"/>.</param>
    /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/>.</param>
    /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/>.</param>
    /// <param name="streamHelper">The <see cref="IStreamHelper"/>.</param>
    /// <param name="timerManager">The <see cref="TimerManager"/>.</param>
    /// <param name="seriesTimerManager">The <see cref="SeriesTimerManager"/>.</param>
    /// <param name="recordingsMetadataManager">The <see cref="RecordingsMetadataManager"/>.</param>
    public RecordingsManager(
        ILogger<RecordingsManager> logger,
        IServerConfigurationManager config,
        IHttpClientFactory httpClientFactory,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        ILibraryMonitor libraryMonitor,
        IProviderManager providerManager,
        IMediaEncoder mediaEncoder,
        IMediaSourceManager mediaSourceManager,
        IStreamHelper streamHelper,
        TimerManager timerManager,
        SeriesTimerManager seriesTimerManager,
        RecordingsMetadataManager recordingsMetadataManager)
    {
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _fileSystem = fileSystem;
        _libraryManager = libraryManager;
        _libraryMonitor = libraryMonitor;
        _providerManager = providerManager;
        _mediaEncoder = mediaEncoder;
        _mediaSourceManager = mediaSourceManager;
        _streamHelper = streamHelper;
        _timerManager = timerManager;
        _seriesTimerManager = seriesTimerManager;
        _recordingsMetadataManager = recordingsMetadataManager;

        _config.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
    }

    private string DefaultRecordingPath
    {
        get
        {
            var path = _config.GetLiveTvConfiguration().RecordingPath;

            return string.IsNullOrWhiteSpace(path)
                ? Path.Combine(_config.CommonApplicationPaths.DataPath, "livetv", "recordings")
                : path;
        }
    }

    /// <inheritdoc />
    public string? GetActiveRecordingPath(string id)
        => _activeRecordings.GetValueOrDefault(id)?.Path;

    /// <inheritdoc />
    public ActiveRecordingInfo? GetActiveRecordingInfo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || _activeRecordings.IsEmpty)
        {
            return null;
        }

        foreach (var (_, recordingInfo) in _activeRecordings)
        {
            if (string.Equals(recordingInfo.Path, path, StringComparison.Ordinal)
                && !recordingInfo.CancellationTokenSource.IsCancellationRequested)
            {
                return recordingInfo.Timer.Status == RecordingStatus.InProgress ? recordingInfo : null;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerable<VirtualFolderInfo> GetRecordingFolders()
    {
        if (Directory.Exists(DefaultRecordingPath))
        {
            yield return new VirtualFolderInfo
            {
                Locations = [DefaultRecordingPath],
                Name = "Recordings"
            };
        }

        var customPath = _config.GetLiveTvConfiguration().MovieRecordingPath;
        if (!string.IsNullOrWhiteSpace(customPath)
            && !string.Equals(customPath, DefaultRecordingPath, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(customPath))
        {
            yield return new VirtualFolderInfo
            {
                Locations = [customPath],
                Name = "Recorded Movies",
                CollectionType = CollectionTypeOptions.movies
            };
        }

        customPath = _config.GetLiveTvConfiguration().SeriesRecordingPath;
        if (!string.IsNullOrWhiteSpace(customPath)
            && !string.Equals(customPath, DefaultRecordingPath, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(customPath))
        {
            yield return new VirtualFolderInfo
            {
                Locations = [customPath],
                Name = "Recorded Shows",
                CollectionType = CollectionTypeOptions.tvshows
            };
        }
    }

    /// <inheritdoc />
    public async Task CreateRecordingFolders()
    {
        try
        {
            var recordingFolders = GetRecordingFolders().ToArray();
            var virtualFolders = _libraryManager.GetVirtualFolders();

            var allExistingPaths = virtualFolders.SelectMany(i => i.Locations).ToList();

            var pathsAdded = new List<string>();

            foreach (var recordingFolder in recordingFolders)
            {
                var pathsToCreate = recordingFolder.Locations
                    .Where(i => !allExistingPaths.Any(p => _fileSystem.AreEqual(p, i)))
                    .ToList();

                if (pathsToCreate.Count == 0)
                {
                    continue;
                }

                var mediaPathInfos = pathsToCreate.Select(i => new MediaPathInfo(i)).ToArray();
                var libraryOptions = new LibraryOptions
                {
                    PathInfos = mediaPathInfos
                };

                try
                {
                    await _libraryManager
                        .AddVirtualFolder(recordingFolder.Name, recordingFolder.CollectionType, libraryOptions, true)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating virtual folder");
                }

                pathsAdded.AddRange(pathsToCreate);
            }

            var config = _config.GetLiveTvConfiguration();

            var pathsToRemove = config.MediaLocationsCreated
                .Except(recordingFolders.SelectMany(i => i.Locations))
                .ToList();

            if (pathsAdded.Count > 0 || pathsToRemove.Count > 0)
            {
                pathsAdded.InsertRange(0, config.MediaLocationsCreated);
                config.MediaLocationsCreated = pathsAdded.Except(pathsToRemove).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                _config.SaveConfiguration("livetv", config);
            }

            foreach (var path in pathsToRemove)
            {
                await RemovePathFromLibraryAsync(path).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recording folders");
        }
    }

    private async Task RemovePathFromLibraryAsync(string path)
    {
        _logger.LogDebug("Removing path from library: {0}", path);

        var requiresRefresh = false;
        var virtualFolders = _libraryManager.GetVirtualFolders();

        foreach (var virtualFolder in virtualFolders)
        {
            if (!virtualFolder.Locations.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (virtualFolder.Locations.Length == 1)
            {
                try
                {
                    await _libraryManager.RemoveVirtualFolder(virtualFolder.Name, true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing virtual folder");
                }
            }
            else
            {
                try
                {
                    _libraryManager.RemoveMediaPath(virtualFolder.Name, path);
                    requiresRefresh = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing media path");
                }
            }
        }

        if (requiresRefresh)
        {
            await _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void CancelRecording(string timerId, TimerInfo? timer)
    {
        if (_activeRecordings.TryGetValue(timerId, out var activeRecordingInfo))
        {
            activeRecordingInfo.Timer = timer;
            activeRecordingInfo.CancellationTokenSource.Cancel();
        }
    }

    /// <inheritdoc />
    public async Task RecordStream(ActiveRecordingInfo recordingInfo, BaseItem channel, DateTime recordingEndDate)
    {
        ArgumentNullException.ThrowIfNull(recordingInfo);
        ArgumentNullException.ThrowIfNull(channel);

        var timer = recordingInfo.Timer;
        var remoteMetadata = await FetchInternetMetadata(timer, CancellationToken.None).ConfigureAwait(false);
        var recordingPath = GetRecordingPath(timer, remoteMetadata, out var seriesPath);

        string? liveStreamId = null;
        RecordingStatus recordingStatus;
        try
        {
            var allMediaSources = await _mediaSourceManager
                .GetPlaybackMediaSources(channel, null, true, false, CancellationToken.None).ConfigureAwait(false);

            var mediaStreamInfo = allMediaSources[0];
            IDirectStreamProvider? directStreamProvider = null;
            if (mediaStreamInfo.RequiresOpening)
            {
                var liveStreamResponse = await _mediaSourceManager.OpenLiveStreamInternal(
                    new LiveStreamRequest
                    {
                        ItemId = channel.Id,
                        OpenToken = mediaStreamInfo.OpenToken
                    },
                    CancellationToken.None).ConfigureAwait(false);

                mediaStreamInfo = liveStreamResponse.Item1.MediaSource;
                liveStreamId = mediaStreamInfo.LiveStreamId;
                directStreamProvider = liveStreamResponse.Item2;
            }

            using var recorder = GetRecorder(mediaStreamInfo);

            recordingPath = recorder.GetOutputPath(mediaStreamInfo, recordingPath);
            recordingPath = EnsureFileUnique(recordingPath, timer.Id);

            _libraryMonitor.ReportFileSystemChangeBeginning(recordingPath);

            var duration = recordingEndDate - DateTime.UtcNow;

            _logger.LogInformation("Beginning recording. Will record for {Duration} minutes.", duration.TotalMinutes);
            _logger.LogInformation("Writing file to: {Path}", recordingPath);

            async void OnStarted()
            {
                recordingInfo.Path = recordingPath;
                _activeRecordings.TryAdd(timer.Id, recordingInfo);

                timer.Status = RecordingStatus.InProgress;
                _timerManager.AddOrUpdate(timer, false);

                await _recordingsMetadataManager.SaveRecordingMetadata(timer, recordingPath, seriesPath).ConfigureAwait(false);
                await CreateRecordingFolders().ConfigureAwait(false);

                TriggerRefresh(recordingPath);
                await EnforceKeepUpTo(timer, seriesPath).ConfigureAwait(false);
            }

            await recorder.Record(
                directStreamProvider,
                mediaStreamInfo,
                recordingPath,
                duration,
                OnStarted,
                recordingInfo.CancellationTokenSource.Token).ConfigureAwait(false);

            recordingStatus = RecordingStatus.Completed;
            _logger.LogInformation("Recording completed: {RecordPath}", recordingPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recording stopped: {RecordPath}", recordingPath);
            recordingStatus = RecordingStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording to {RecordPath}", recordingPath);
            recordingStatus = RecordingStatus.Error;
        }

        if (!string.IsNullOrWhiteSpace(liveStreamId))
        {
            try
            {
                await _mediaSourceManager.CloseLiveStream(liveStreamId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing live stream");
            }
        }

        DeleteFileIfEmpty(recordingPath);
        TriggerRefresh(recordingPath);
        _libraryMonitor.ReportFileSystemChangeComplete(recordingPath, false);
        _activeRecordings.TryRemove(timer.Id, out _);

        if (recordingStatus != RecordingStatus.Completed && DateTime.UtcNow < timer.EndDate && timer.RetryCount < 10)
        {
            const int RetryIntervalSeconds = 60;
            _logger.LogInformation("Retrying recording in {0} seconds.", RetryIntervalSeconds);

            timer.Status = RecordingStatus.New;
            timer.PrePaddingSeconds = 0;
            timer.StartDate = DateTime.UtcNow.AddSeconds(RetryIntervalSeconds);
            timer.RetryCount++;
            _timerManager.AddOrUpdate(timer);
        }
        else if (File.Exists(recordingPath))
        {
            timer.RecordingPath = recordingPath;
            timer.Status = RecordingStatus.Completed;
            _timerManager.AddOrUpdate(timer, false);
            await PostProcessRecording(recordingPath).ConfigureAwait(false);
        }
        else
        {
            _timerManager.Delete(timer);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _recordingDeleteSemaphore.Dispose();

        foreach (var pair in _activeRecordings.ToList())
        {
            pair.Value.CancellationTokenSource.Cancel();
        }

        _disposed = true;
    }

    private async void OnNamedConfigurationUpdated(object? sender, ConfigurationUpdateEventArgs e)
    {
        if (string.Equals(e.Key, "livetv", StringComparison.OrdinalIgnoreCase))
        {
            await CreateRecordingFolders().ConfigureAwait(false);
        }
    }

    private async Task<RemoteSearchResult?> FetchInternetMetadata(TimerInfo timer, CancellationToken cancellationToken)
    {
        if (!timer.IsSeries || timer.SeriesProviderIds.Count == 0)
        {
            return null;
        }

        var query = new RemoteSearchQuery<SeriesInfo>
        {
            SearchInfo = new SeriesInfo
            {
                ProviderIds = timer.SeriesProviderIds,
                Name = timer.Name,
                MetadataCountryCode = _config.Configuration.MetadataCountryCode,
                MetadataLanguage = _config.Configuration.PreferredMetadataLanguage
            }
        };

        var results = await _providerManager.GetRemoteSearchResults<Series, SeriesInfo>(query, cancellationToken).ConfigureAwait(false);

        return results.FirstOrDefault();
    }

    private string GetRecordingPath(TimerInfo timer, RemoteSearchResult? metadata, out string? seriesPath)
    {
        var recordingPath = DefaultRecordingPath;
        var config = _config.GetLiveTvConfiguration();
        seriesPath = null;

        if (timer.IsProgramSeries)
        {
            var customRecordingPath = config.SeriesRecordingPath;
            var allowSubfolder = true;
            if (!string.IsNullOrWhiteSpace(customRecordingPath))
            {
                allowSubfolder = string.Equals(customRecordingPath, recordingPath, StringComparison.OrdinalIgnoreCase);
                recordingPath = customRecordingPath;
            }

            if (allowSubfolder && config.EnableRecordingSubfolders)
            {
                recordingPath = Path.Combine(recordingPath, "Series");
            }

            // trim trailing period from the folder name
            var folderName = _fileSystem.GetValidFilename(timer.Name).Trim().TrimEnd('.').Trim();

            if (metadata is not null && metadata.ProductionYear.HasValue)
            {
                folderName += " (" + metadata.ProductionYear.Value.ToString(CultureInfo.InvariantCulture) + ")";
            }

            // Can't use the year here in the folder name because it is the year of the episode, not the series.
            recordingPath = Path.Combine(recordingPath, folderName);

            seriesPath = recordingPath;

            if (timer.SeasonNumber.HasValue)
            {
                folderName = string.Format(
                    CultureInfo.InvariantCulture,
                    "Season {0}",
                    timer.SeasonNumber.Value);
                recordingPath = Path.Combine(recordingPath, folderName);
            }
        }
        else if (timer.IsMovie)
        {
            var customRecordingPath = config.MovieRecordingPath;
            var allowSubfolder = true;
            if (!string.IsNullOrWhiteSpace(customRecordingPath))
            {
                allowSubfolder = string.Equals(customRecordingPath, recordingPath, StringComparison.OrdinalIgnoreCase);
                recordingPath = customRecordingPath;
            }

            if (allowSubfolder && config.EnableRecordingSubfolders)
            {
                recordingPath = Path.Combine(recordingPath, "Movies");
            }

            var folderName = _fileSystem.GetValidFilename(timer.Name).Trim();
            if (timer.ProductionYear.HasValue)
            {
                folderName += " (" + timer.ProductionYear.Value.ToString(CultureInfo.InvariantCulture) + ")";
            }

            // trim trailing period from the folder name
            folderName = folderName.TrimEnd('.').Trim();

            recordingPath = Path.Combine(recordingPath, folderName);
        }
        else if (timer.IsKids)
        {
            if (config.EnableRecordingSubfolders)
            {
                recordingPath = Path.Combine(recordingPath, "Kids");
            }

            var folderName = _fileSystem.GetValidFilename(timer.Name).Trim();
            if (timer.ProductionYear.HasValue)
            {
                folderName += " (" + timer.ProductionYear.Value.ToString(CultureInfo.InvariantCulture) + ")";
            }

            // trim trailing period from the folder name
            folderName = folderName.TrimEnd('.').Trim();

            recordingPath = Path.Combine(recordingPath, folderName);
        }
        else if (timer.IsSports)
        {
            if (config.EnableRecordingSubfolders)
            {
                recordingPath = Path.Combine(recordingPath, "Sports");
            }

            recordingPath = Path.Combine(recordingPath, _fileSystem.GetValidFilename(timer.Name).Trim());
        }
        else
        {
            if (config.EnableRecordingSubfolders)
            {
                recordingPath = Path.Combine(recordingPath, "Other");
            }

            recordingPath = Path.Combine(recordingPath, _fileSystem.GetValidFilename(timer.Name).Trim());
        }

        var recordingFileName = _fileSystem.GetValidFilename(RecordingHelper.GetRecordingName(timer)).Trim() + ".ts";

        return Path.Combine(recordingPath, recordingFileName);
    }

    private void DeleteFileIfEmpty(string path)
    {
        var file = _fileSystem.GetFileInfo(path);

        if (file.Exists && file.Length == 0)
        {
            try
            {
                _fileSystem.DeleteFile(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting 0-byte failed recording file {Path}", path);
            }
        }
    }

    private void TriggerRefresh(string path)
    {
        _logger.LogInformation("Triggering refresh on {Path}", path);

        var item = GetAffectedBaseItem(Path.GetDirectoryName(path));
        if (item is null)
        {
            return;
        }

        _logger.LogInformation("Refreshing recording parent {Path}", item.Path);
        _providerManager.QueueRefresh(
            item.Id,
            new MetadataRefreshOptions(new DirectoryService(_fileSystem))
            {
                RefreshPaths =
                [
                    path,
                    Path.GetDirectoryName(path),
                    Path.GetDirectoryName(Path.GetDirectoryName(path))
                ]
            },
            RefreshPriority.High);
    }

    private BaseItem? GetAffectedBaseItem(string? path)
    {
        BaseItem? item = null;
        var parentPath = Path.GetDirectoryName(path);
        while (item is null && !string.IsNullOrEmpty(path))
        {
            item = _libraryManager.FindByPath(path, null);
            path = Path.GetDirectoryName(path);
        }

        if (item is not null
            && item.GetType() == typeof(Folder)
            && string.Equals(item.Path, parentPath, StringComparison.OrdinalIgnoreCase))
        {
            var parentItem = item.GetParent();
            if (parentItem is not null && parentItem is not AggregateFolder)
            {
                item = parentItem;
            }
        }

        return item;
    }

    private async Task EnforceKeepUpTo(TimerInfo timer, string? seriesPath)
    {
        if (string.IsNullOrWhiteSpace(timer.SeriesTimerId)
            || string.IsNullOrWhiteSpace(seriesPath))
        {
            return;
        }

        var seriesTimerId = timer.SeriesTimerId;
        var seriesTimer = _seriesTimerManager.GetAll()
            .FirstOrDefault(i => string.Equals(i.Id, seriesTimerId, StringComparison.OrdinalIgnoreCase));

        if (seriesTimer is null || seriesTimer.KeepUpTo <= 0)
        {
            return;
        }

        if (_disposed)
        {
            return;
        }

        using (await _recordingDeleteSemaphore.LockAsync().ConfigureAwait(false))
        {
            if (_disposed)
            {
                return;
            }

            var timersToDelete = _timerManager.GetAll()
                .Where(timerInfo => timerInfo.Status == RecordingStatus.Completed
                    && !string.IsNullOrWhiteSpace(timerInfo.RecordingPath)
                    && string.Equals(timerInfo.SeriesTimerId, seriesTimerId, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(timerInfo.RecordingPath))
                .OrderByDescending(i => i.EndDate)
                .Skip(seriesTimer.KeepUpTo - 1)
                .ToList();

            DeleteLibraryItemsForTimers(timersToDelete);

            if (_libraryManager.FindByPath(seriesPath, true) is not Folder librarySeries)
            {
                return;
            }

            var episodesToDelete = librarySeries.GetItemList(
                    new InternalItemsQuery
                    {
                        OrderBy = [(ItemSortBy.DateCreated, SortOrder.Descending)],
                        IsVirtualItem = false,
                        IsFolder = false,
                        Recursive = true,
                        DtoOptions = new DtoOptions(true)
                    })
                .Where(i => i.IsFileProtocol && File.Exists(i.Path))
                .Skip(seriesTimer.KeepUpTo - 1);

            foreach (var item in episodesToDelete)
            {
                try
                {
                    _libraryManager.DeleteItem(
                        item,
                        new DeleteOptions
                        {
                            DeleteFileLocation = true
                        },
                        true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting item");
                }
            }
        }
    }

    private void DeleteLibraryItemsForTimers(List<TimerInfo> timers)
    {
        foreach (var timer in timers)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                DeleteLibraryItemForTimer(timer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recording");
            }
        }
    }

    private void DeleteLibraryItemForTimer(TimerInfo timer)
    {
        var libraryItem = _libraryManager.FindByPath(timer.RecordingPath, false);
        if (libraryItem is not null)
        {
            _libraryManager.DeleteItem(
                libraryItem,
                new DeleteOptions
                {
                    DeleteFileLocation = true
                },
                true);
        }
        else if (File.Exists(timer.RecordingPath))
        {
            _fileSystem.DeleteFile(timer.RecordingPath);
        }

        _timerManager.Delete(timer);
    }

    private string EnsureFileUnique(string path, string timerId)
    {
        var parent = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var index = 1;
        while (File.Exists(path) || _activeRecordings.Any(i
                   => string.Equals(i.Value.Path, path, StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(i.Value.Timer.Id, timerId, StringComparison.OrdinalIgnoreCase)))
        {
            name += " - " + index.ToString(CultureInfo.InvariantCulture);

            path = Path.ChangeExtension(Path.Combine(parent, name), extension);
            index++;
        }

        return path;
    }

    private IRecorder GetRecorder(MediaSourceInfo mediaSource)
    {
        if (mediaSource.RequiresLooping
            || !(mediaSource.Container ?? string.Empty).EndsWith("ts", StringComparison.OrdinalIgnoreCase)
            || (mediaSource.Protocol != MediaProtocol.File && mediaSource.Protocol != MediaProtocol.Http))
        {
            return new EncodedRecorder(_logger, _mediaEncoder, _config.ApplicationPaths, _config);
        }

        return new DirectRecorder(_logger, _httpClientFactory, _streamHelper);
    }

    private async Task PostProcessRecording(string path)
    {
        var options = _config.GetLiveTvConfiguration();
        if (string.IsNullOrWhiteSpace(options.RecordingPostProcessor))
        {
            return;
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = options.RecordingPostProcessorArguments
                    .Replace("{path}", path, StringComparison.OrdinalIgnoreCase),
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = options.RecordingPostProcessor,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };
            process.EnableRaisingEvents = true;

            _logger.LogInformation("Running recording post processor {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation("Recording post-processing script completed with exit code {ExitCode}", process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running recording post processor");
        }
    }
}
