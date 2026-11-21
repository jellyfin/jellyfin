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
using Jellyfin.Database.Implementations.Enums;
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
public sealed class RecordingsManager : IRecordingsManager, IActiveRecordingUpdater, IDisposable
{
    private static readonly TimeSpan MaxEndMonitorWait = TimeSpan.FromHours(1);

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

    private readonly ConcurrentDictionary<string, ActiveRecording> _activeRecordings = new(StringComparer.OrdinalIgnoreCase);
    private readonly AsyncNonKeyedLocker _recordingDeleteSemaphore = new();
    private readonly Lock _recordingPathLock = new();
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
        => _activeRecordings.GetValueOrDefault(id)?.Info.Path;

    /// <inheritdoc />
    public ActiveRecordingInfo? GetActiveRecordingInfo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || _activeRecordings.IsEmpty)
        {
            return null;
        }

        foreach (var (_, recording) in _activeRecordings)
        {
            var recordingInfo = recording.Info;
            if (string.Equals(recordingInfo.Path, path, StringComparison.Ordinal)
                && !recordingInfo.CancellationTokenSource.IsCancellationRequested
                && recording.IsInProgress)
            {
                return recordingInfo;
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
                config.MediaLocationsCreated = pathsAdded.Except(pathsToRemove).Distinct().ToArray();
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
            _libraryManager.QueueLibraryScan();
        }
    }

    /// <inheritdoc />
    public void CancelRecording(string timerId, TimerInfo? timer)
    {
        if (_activeRecordings.TryGetValue(timerId, out var recording))
        {
            if (timer is not null)
            {
                recording.UpdateTimer(timer);
            }

            recording.Info.CancellationTokenSource.Cancel();
        }
    }

    /// <inheritdoc />
    bool IActiveRecordingUpdater.TryUpdateTimer(TimerInfo timer)
    {
        ArgumentNullException.ThrowIfNull(timer);

        if (!_activeRecordings.TryGetValue(timer.Id, out var recording))
        {
            return false;
        }

        if (recording.Info.CancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Recording {TimerId} is already stopping, so the timer change does not affect it.",
                timer.Id);
            return true;
        }

        var (previousEnd, newEnd) = recording.UpdateTimer(timer);
        if (previousEnd != newEnd)
        {
            _logger.LogInformation(
                "Recording {TimerId} will now end at {EndDate} instead of {PreviousEndDate}. Post-padding: {PostPadding}.",
                timer.Id,
                newEnd,
                previousEnd,
                TimeSpan.FromSeconds(timer.PostPaddingSeconds));
        }

        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="recordingEndDate"/> is not used. The end is taken from the timer, so that changes to its
    /// post-padding apply while the recording is running.
    /// </remarks>
    public async Task RecordStream(ActiveRecordingInfo recordingInfo, BaseItem channel, DateTime recordingEndDate)
    {
        ArgumentNullException.ThrowIfNull(recordingInfo);
        ArgumentNullException.ThrowIfNull(channel);

        var timer = _timerManager.GetTimer(recordingInfo.Id) ?? recordingInfo.Timer;
        recordingInfo.Path = string.Empty;
        var recording = new ActiveRecording(recordingInfo, timer);

        if (!_activeRecordings.TryAdd(timer.Id, recording))
        {
            _logger.LogInformation("Skipping RecordStream because timer {TimerId} is already being recorded.", timer.Id);
            return;
        }

        var endMonitorTask = MonitorRecordingEndAsync(recording);

        string recordingPath = string.Empty;
        string? seriesPath = null;
        string? liveStreamId = null;
        RecordingStatus recordingStatus = RecordingStatus.Error;

        try
        {
            if (recordingInfo.CancellationTokenSource.IsCancellationRequested || timer.Status == RecordingStatus.Cancelled)
            {
                recordingStatus = RecordingStatus.Completed;
                return;
            }

            var remoteMetadata = await FetchInternetMetadata(timer, recordingInfo.CancellationTokenSource.Token).ConfigureAwait(false);
            recordingPath = GetRecordingPath(timer, remoteMetadata, out seriesPath);

            var allMediaSources = await _mediaSourceManager
                .GetPlaybackMediaSources(channel, null, true, false, recordingInfo.CancellationTokenSource.Token).ConfigureAwait(false);

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

            // Reserve the path so concurrent recordings don't pick the same file name, but don't
            // publish it through ActiveRecordingInfo.Path until the recorder has started and the
            // file exists. Until then Path stays empty, which callers still treat as "active".
            recordingPath = ReserveUniqueRecordingPath(recording, recordingPath, timer.Id);

            _libraryMonitor.ReportFileSystemChangeBeginning(recordingPath);

            var scheduledRecordingStart = timer.StartDate.AddSeconds(-timer.PrePaddingSeconds);
            var scheduledRecordingEnd = timer.EndDate.AddSeconds(timer.PostPaddingSeconds);

            _logger.LogInformation(
                "Beginning recording {TimerId}. Planned duration: {Duration}, ending at {EndDate}. Pre-padding: {PrePadding}. Post-padding: {PostPadding}.",
                timer.Id,
                scheduledRecordingEnd - scheduledRecordingStart,
                scheduledRecordingEnd,
                TimeSpan.FromSeconds(timer.PrePaddingSeconds),
                TimeSpan.FromSeconds(timer.PostPaddingSeconds));
            _logger.LogInformation("Writing file to: {Path}", recordingPath);

            void OnStarted()
            {
                var activeTimer = _timerManager.GetTimer(timer.Id) ?? timer;
                if (!recording.TryMarkStarted(activeTimer))
                {
                    if (!recording.IsInProgress && !recordingInfo.CancellationTokenSource.IsCancellationRequested)
                    {
                        _logger.LogInformation("Timer {TimerId} was cancelled while its recording was starting. Stopping the recording.", timer.Id);
                        _ = recordingInfo.CancellationTokenSource.CancelAsync();
                    }

                    return;
                }

                recordingInfo.Path = recordingPath;
                activeTimer.Status = RecordingStatus.InProgress;

                try
                {
                    _timerManager.AddOrUpdate(activeTimer, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving the in-progress status of timer {TimerId}. The recording continues.", activeTimer.Id);
                }

                _ = OnRecordingStartedAsync(activeTimer, recordingPath, seriesPath);
            }

            await recorder.Record(
                directStreamProvider,
                mediaStreamInfo,
                recordingPath,
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
        finally
        {
            try
            {
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

                if (!recordingInfo.CancellationTokenSource.IsCancellationRequested)
                {
                    await recordingInfo.CancellationTokenSource.CancelAsync().ConfigureAwait(false);
                }

                await endMonitorTask.ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(recordingPath))
                {
                    DeleteFileIfEmpty(recordingPath);
                    TriggerRefresh(recordingPath);
                    _libraryMonitor.ReportFileSystemChangeComplete(recordingPath, false);
                }
            }
            finally
            {
                // Always unregister, or this timer could not be recorded again until a restart. This
                // happens before the retry/complete handling below because PostProcessRecording waits
                // for the user's post-processor, and the recording must not appear active meanwhile.
                _activeRecordings.TryRemove(timer.Id, out _);
            }

            var finalTimer = _timerManager.GetTimer(timer.Id) ?? recordingInfo.Timer;

            var isCancelled = finalTimer.Status == RecordingStatus.Cancelled;

            if (!isCancelled
                && recordingStatus != RecordingStatus.Completed
                && DateTime.UtcNow < finalTimer.EndDate
                && finalTimer.RetryCount < 10)
            {
                const int RetryIntervalSeconds = 60;
                _logger.LogInformation("Retrying recording in {0} seconds.", RetryIntervalSeconds);

                finalTimer.Status = RecordingStatus.New;
                finalTimer.PrePaddingSeconds = 0;
                finalTimer.StartDate = DateTime.UtcNow.AddSeconds(RetryIntervalSeconds);
                finalTimer.RetryCount++;
                _timerManager.AddOrUpdate(finalTimer);
            }
            else if (!string.IsNullOrWhiteSpace(recordingPath) && File.Exists(recordingPath))
            {
                finalTimer.RecordingPath = recordingPath;
                finalTimer.Status = RecordingStatus.Completed;
                _timerManager.AddOrUpdate(finalTimer, false);
                await PostProcessRecording(recordingPath).ConfigureAwait(false);
            }
            else if (!isCancelled && _timerManager.GetTimer(timer.Id) is not null)
            {
                _timerManager.Delete(finalTimer);
            }
        }
    }

    private async Task MonitorRecordingEndAsync(ActiveRecording recording)
    {
        var cancellationTokenSource = recording.Info.CancellationTokenSource;

        try
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var remaining = recording.GetTimeRemaining(DateTime.UtcNow, out var timerChanged);
                if (remaining <= TimeSpan.Zero)
                {
                    _logger.LogInformation("Recording {TimerId} has reached its scheduled end. Stopping the recording.", recording.Info.Id);
                    await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                    return;
                }

                // Cap each wait: Task.Delay rejects delays over ~49.7 days, and the loop
                // re-evaluates the end time after every wake-up anyway.
                var wait = remaining < MaxEndMonitorWait ? remaining : MaxEndMonitorWait;

                using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
                var delayTask = Task.Delay(wait, delayCancellation.Token);
                await Task.WhenAny(delayTask, timerChanged).ConfigureAwait(false);
                await delayCancellation.CancelAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            // The monitor is the only thing that ends a recording on schedule. If it fails,
            // stop the recording rather than letting it run indefinitely.
            _logger.LogError(ex, "Error monitoring recording end for {TimerId}. Stopping the recording.", recording.Info.Id);
            await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }
    }

    private async Task OnRecordingStartedAsync(TimerInfo timer, string recordingPath, string? seriesPath)
    {
        try
        {
            await _recordingsMetadataManager.SaveRecordingMetadata(timer, recordingPath, seriesPath).ConfigureAwait(false);
            await CreateRecordingFolders().ConfigureAwait(false);

            TriggerRefresh(recordingPath);
            await EnforceKeepUpTo(timer, seriesPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing recording start processing for {TimerId}.", timer.Id);
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
            pair.Value.Info.CancellationTokenSource.Cancel();
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

            if (metadata is not null && metadata.ProductionYear is not null)
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
            if (timer.ProductionYear is not null)
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
            if (timer.ProductionYear is not null)
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

    private string ReserveUniqueRecordingPath(ActiveRecording recording, string path, string timerId)
    {
        // Choosing and reserving the path in one locked step keeps two recordings that start
        // together from picking the same file.
        lock (_recordingPathLock)
        {
            var uniquePath = EnsureFileUnique(path, timerId);
            recording.ReservedPath = uniquePath;
            return uniquePath;
        }
    }

    private string EnsureFileUnique(string path, string timerId)
    {
        var parent = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var index = 1;
        while (File.Exists(path) || _activeRecordings.Any(i
                   => string.Equals(i.Value.ReservedPath ?? i.Value.Info.Path, path, StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(i.Key, timerId, StringComparison.OrdinalIgnoreCase)))
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
