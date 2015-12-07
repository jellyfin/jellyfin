using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.FileOrganization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class EmbyTV : ILiveTvService, IHasRegistrationInfo, IDisposable
    {
        private readonly IApplicationHost _appHpst;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly ItemDataProvider<RecordingInfo> _recordingProvider;
        private readonly ItemDataProvider<SeriesTimerInfo> _seriesTimerProvider;
        private readonly TimerManager _timerProvider;

        private readonly LiveTvManager _liveTvManager;
        private readonly IFileSystem _fileSystem;
        private readonly ISecurityManager _security;

        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileOrganizationService _organizationService;
        private readonly IMediaEncoder _mediaEncoder;

        public static EmbyTV Current;

        public EmbyTV(IApplicationHost appHost, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient, IServerConfigurationManager config, ILiveTvManager liveTvManager, IFileSystem fileSystem, ISecurityManager security, ILibraryManager libraryManager, ILibraryMonitor libraryMonitor, IProviderManager providerManager, IFileOrganizationService organizationService, IMediaEncoder mediaEncoder)
        {
            Current = this;

            _appHpst = appHost;
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
            _fileSystem = fileSystem;
            _security = security;
            _libraryManager = libraryManager;
            _libraryMonitor = libraryMonitor;
            _providerManager = providerManager;
            _organizationService = organizationService;
            _mediaEncoder = mediaEncoder;
            _liveTvManager = (LiveTvManager)liveTvManager;
            _jsonSerializer = jsonSerializer;

            _recordingProvider = new ItemDataProvider<RecordingInfo>(fileSystem, jsonSerializer, _logger, Path.Combine(DataPath, "recordings"), (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase));
            _seriesTimerProvider = new SeriesTimerManager(fileSystem, jsonSerializer, _logger, Path.Combine(DataPath, "seriestimers"));
            _timerProvider = new TimerManager(fileSystem, jsonSerializer, _logger, Path.Combine(DataPath, "timers"));
            _timerProvider.TimerFired += _timerProvider_TimerFired;
        }

        public void Start()
        {
            _timerProvider.RestartTimers();
        }

        public event EventHandler DataSourceChanged;

        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeRecordings =
            new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

        public string Name
        {
            get { return "Emby"; }
        }

        public string DataPath
        {
            get { return Path.Combine(_config.CommonApplicationPaths.DataPath, "livetv"); }
        }

        public string HomePageUrl
        {
            get { return "http://emby.media"; }
        }

        public async Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken)
        {
            var status = new LiveTvServiceStatusInfo();
            var list = new List<LiveTvTunerInfo>();

            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    var tuners = await hostInstance.GetTunerInfos(cancellationToken).ConfigureAwait(false);

                    list.AddRange(tuners);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting tuners", ex);
                }
            }

            status.Tuners = list;
            status.Status = LiveTvServiceStatus.Ok;
            status.Version = _appHpst.ApplicationVersion.ToString();
            status.IsVisible = false;
            return status;
        }

        public async Task RefreshSeriesTimers(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var seriesTimers = await GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);

            List<ChannelInfo> channels = null;

            foreach (var timer in seriesTimers)
            {
                List<ProgramInfo> epgData;

                if (timer.RecordAnyChannel)
                {
                    if (channels == null)
                    {
                        channels = (await GetChannelsAsync(true, CancellationToken.None).ConfigureAwait(false)).ToList();
                    }
                    var channelIds = channels.Select(i => i.Id).ToList();
                    epgData = GetEpgDataForChannels(channelIds);
                }
                else
                {
                    epgData = GetEpgDataForChannel(timer.ChannelId);
                }
                await UpdateTimersForSeriesTimer(epgData, timer).ConfigureAwait(false);
            }

            var timers = await GetTimersAsync(cancellationToken).ConfigureAwait(false);

            foreach (var timer in timers.ToList())
            {
                if (DateTime.UtcNow > timer.EndDate && !_activeRecordings.ContainsKey(timer.Id))
                {
                    _timerProvider.Delete(timer);
                }
            }
        }

        private List<ChannelInfo> _channelCache = null;
        private async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(bool enableCache, CancellationToken cancellationToken)
        {
            if (enableCache && _channelCache != null)
            {

                return _channelCache.ToList();
            }

            var list = new List<ChannelInfo>();

            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    var channels = await hostInstance.GetChannels(cancellationToken).ConfigureAwait(false);

                    list.AddRange(channels);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channels", ex);
                }
            }

            if (list.Count > 0)
            {
                foreach (var provider in GetListingProviders())
                {
                    try
                    {
                        await provider.Item1.AddMetadata(provider.Item2, list, cancellationToken).ConfigureAwait(false);
                    }
                    catch (NotSupportedException)
                    {

                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error adding metadata", ex);
                    }
                }
            }
            _channelCache = list;
            return list;
        }

        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            return GetChannelsAsync(false, cancellationToken);
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var timers = _timerProvider.GetAll().Where(i => string.Equals(i.SeriesTimerId, timerId, StringComparison.OrdinalIgnoreCase));
            foreach (var timer in timers)
            {
                CancelTimerInternal(timer.Id);
            }

            var remove = _seriesTimerProvider.GetAll().FirstOrDefault(r => string.Equals(r.Id, timerId, StringComparison.OrdinalIgnoreCase));
            if (remove != null)
            {
                _seriesTimerProvider.Delete(remove);
            }
            return Task.FromResult(true);
        }

        private void CancelTimerInternal(string timerId)
        {
            var remove = _timerProvider.GetAll().FirstOrDefault(r => string.Equals(r.Id, timerId, StringComparison.OrdinalIgnoreCase));
            if (remove != null)
            {
                _timerProvider.Delete(remove);
            }
            CancellationTokenSource cancellationTokenSource;

            if (_activeRecordings.TryGetValue(timerId, out cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            CancelTimerInternal(timerId);
            return Task.FromResult(true);
        }

        public async Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            var remove = _recordingProvider.GetAll().FirstOrDefault(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));
            if (remove != null)
            {
                if (!string.IsNullOrWhiteSpace(remove.TimerId))
                {
                    var enableDelay = _activeRecordings.ContainsKey(remove.TimerId);

                    CancelTimerInternal(remove.TimerId);

                    if (enableDelay)
                    {
                        // A hack yes, but need to make sure the file is closed before attempting to delete it
                        await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
                    }
                }

                try
                {
                    _fileSystem.DeleteFile(remove.Path);
                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (FileNotFoundException)
                {

                }
                _recordingProvider.Delete(remove);
            }
            else
            {
                throw new ResourceNotFoundException("Recording not found: " + recordingId);
            }
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            info.Id = Guid.NewGuid().ToString("N");
            _timerProvider.Add(info);
            return Task.FromResult(0);
        }

        public async Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            info.Id = Guid.NewGuid().ToString("N");

            List<ProgramInfo> epgData;
            if (info.RecordAnyChannel)
            {
                var channels = await GetChannelsAsync(true, CancellationToken.None).ConfigureAwait(false);
                var channelIds = channels.Select(i => i.Id).ToList();
                epgData = GetEpgDataForChannels(channelIds);
            }
            else
            {
                epgData = GetEpgDataForChannel(info.ChannelId);
            }

            // populate info.seriesID
            var program = epgData.FirstOrDefault(i => string.Equals(i.Id, info.ProgramId, StringComparison.OrdinalIgnoreCase));

            if (program != null)
            {
                info.SeriesId = program.SeriesId;
            }
            else
            {
                throw new InvalidOperationException("SeriesId for program not found");
            }

            _seriesTimerProvider.Add(info);
            await UpdateTimersForSeriesTimer(epgData, info).ConfigureAwait(false);
        }

        public async Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _seriesTimerProvider.Update(info);
            List<ProgramInfo> epgData;
            if (info.RecordAnyChannel)
            {
                var channels = await GetChannelsAsync(true, CancellationToken.None).ConfigureAwait(false);
                var channelIds = channels.Select(i => i.Id).ToList();
                epgData = GetEpgDataForChannels(channelIds);
            }
            else
            {
                epgData = GetEpgDataForChannel(info.ChannelId);
            }

            await UpdateTimersForSeriesTimer(epgData, info).ConfigureAwait(false);
        }

        public Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            _timerProvider.Update(info);
            return Task.FromResult(true);
        }

        public Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            var recordings = _recordingProvider.GetAll().ToList();
            var updated = false;

            foreach (var recording in recordings)
            {
                if (recording.Status == RecordingStatus.InProgress)
                {
                    if (string.IsNullOrWhiteSpace(recording.TimerId) || !_activeRecordings.ContainsKey(recording.TimerId))
                    {
                        recording.Status = RecordingStatus.Cancelled;
                        recording.DateLastUpdated = DateTime.UtcNow;
                        _recordingProvider.Update(recording);
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                recordings = _recordingProvider.GetAll().ToList();
            }

            return recordings;
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<TimerInfo>)_timerProvider.GetAll());
        }

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            var config = GetConfiguration();

            var defaults = new SeriesTimerInfo()
            {
                PostPaddingSeconds = Math.Max(config.PostPaddingSeconds, 0),
                PrePaddingSeconds = Math.Max(config.PrePaddingSeconds, 0),
                RecordAnyChannel = false,
                RecordAnyTime = false,
                RecordNewOnly = false
            };

            if (program != null)
            {
                defaults.SeriesId = program.SeriesId;
                defaults.ProgramId = program.Id;
            }

            return Task.FromResult(defaults);
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<SeriesTimerInfo>)_seriesTimerProvider.GetAll());
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            try
            {
                return await GetProgramsAsyncInternal(channelId, startDateUtc, endDateUtc, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting programs", ex);
                return GetEpgDataForChannel(channelId).Where(i => i.StartDate <= endDateUtc && i.EndDate >= startDateUtc);
            }
        }

        private async Task<IEnumerable<ProgramInfo>> GetProgramsAsyncInternal(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var channels = await GetChannelsAsync(true, cancellationToken).ConfigureAwait(false);
            var channel = channels.First(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

            foreach (var provider in GetListingProviders())
            {
                var programs = await provider.Item1.GetProgramsAsync(provider.Item2, channel.Number, channel.Name, startDateUtc, endDateUtc, cancellationToken)
                        .ConfigureAwait(false);

                var list = programs.ToList();

                // Replace the value that came from the provider with a normalized value
                foreach (var program in list)
                {
                    program.ChannelId = channelId;
                }

                if (list.Count > 0)
                {
                    SaveEpgDataForChannel(channelId, list);

                    return list;
                }
            }

            return new List<ProgramInfo>();
        }

        private List<Tuple<IListingsProvider, ListingsProviderInfo>> GetListingProviders()
        {
            return GetConfiguration().ListingProviders
                .Select(i =>
                {
                    var provider = _liveTvManager.ListingProviders.FirstOrDefault(l => string.Equals(l.Type, i.Type, StringComparison.OrdinalIgnoreCase));

                    return provider == null ? null : new Tuple<IListingsProvider, ListingsProviderInfo>(provider, i);
                })
                .Where(i => i != null)
                .ToList();
        }

        public Task<MediaSourceInfo> GetRecordingStream(string recordingId, string streamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _logger.Info("Streaming Channel " + channelId);

            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    var result = await hostInstance.GetChannelStream(channelId, streamId, cancellationToken).ConfigureAwait(false);

                    result.Item2.Release();

                    return result.Item1;
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error getting channel stream", e);
                }
            }

            throw new ApplicationException("Tuner not found.");
        }

        private async Task<Tuple<MediaSourceInfo, SemaphoreSlim>> GetChannelStreamInternal(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _logger.Info("Streaming Channel " + channelId);

            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    return await hostInstance.GetChannelStream(channelId, streamId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error getting channel stream", e);
                }
            }

            throw new ApplicationException("Tuner not found.");
        }

        public async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    var sources = await hostInstance.GetChannelStreamMediaSources(channelId, cancellationToken).ConfigureAwait(false);

                    if (sources.Count > 0)
                    {
                        return sources;
                    }
                }
                catch (NotImplementedException)
                {

                }
            }

            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task RecordLiveStream(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        async void _timerProvider_TimerFired(object sender, GenericEventArgs<TimerInfo> e)
        {
            var timer = e.Argument;

            _logger.Info("Recording timer fired.");

            try
            {
                var recordingEndDate = timer.EndDate.AddSeconds(timer.PostPaddingSeconds);

                if (recordingEndDate <= DateTime.UtcNow)
                {
                    _logger.Warn("Recording timer fired for timer {0}, Id: {1}, but the program has already ended.", timer.Name, timer.Id);
                    return;
                }

                var cancellationTokenSource = new CancellationTokenSource();

                if (_activeRecordings.TryAdd(timer.Id, cancellationTokenSource))
                {
                    await RecordStream(timer, recordingEndDate, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error recording stream", ex);
            }
        }

        private async Task RecordStream(TimerInfo timer, DateTime recordingEndDate, CancellationToken cancellationToken)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            if (string.IsNullOrWhiteSpace(timer.ProgramId))
            {
                throw new InvalidOperationException("timer.ProgramId is null. Cannot record.");
            }

            var info = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);

            if (info == null)
            {
                throw new InvalidOperationException(string.Format("Program with Id {0} not found", timer.ProgramId));
            }

            var recordPath = RecordingPath;

            if (info.IsMovie)
            {
                recordPath = Path.Combine(recordPath, "Movies", _fileSystem.GetValidFilename(info.Name).Trim());
            }
            else if (info.IsSeries)
            {
                recordPath = Path.Combine(recordPath, "Series", _fileSystem.GetValidFilename(info.Name).Trim());
            }
            else if (info.IsKids)
            {
                recordPath = Path.Combine(recordPath, "Kids", _fileSystem.GetValidFilename(info.Name).Trim());
            }
            else if (info.IsSports)
            {
                recordPath = Path.Combine(recordPath, "Sports", _fileSystem.GetValidFilename(info.Name).Trim());
            }
            else
            {
                recordPath = Path.Combine(recordPath, "Other", _fileSystem.GetValidFilename(info.Name).Trim());
            }

            var recordingFileName = _fileSystem.GetValidFilename(RecordingHelper.GetRecordingName(timer, info)).Trim() + ".ts";

            recordPath = Path.Combine(recordPath, recordingFileName);
            _fileSystem.CreateDirectory(Path.GetDirectoryName(recordPath));

            var recordingId = info.Id.GetMD5().ToString("N");
            var recording = _recordingProvider.GetAll().FirstOrDefault(x => string.Equals(x.Id, recordingId, StringComparison.OrdinalIgnoreCase));

            if (recording == null)
            {
                recording = new RecordingInfo
                {
                    ChannelId = info.ChannelId,
                    Id = recordingId,
                    StartDate = info.StartDate,
                    EndDate = info.EndDate,
                    Genres = info.Genres,
                    IsKids = info.IsKids,
                    IsLive = info.IsLive,
                    IsMovie = info.IsMovie,
                    IsHD = info.IsHD,
                    IsNews = info.IsNews,
                    IsPremiere = info.IsPremiere,
                    IsSeries = info.IsSeries,
                    IsSports = info.IsSports,
                    IsRepeat = !info.IsPremiere,
                    Name = info.Name,
                    EpisodeTitle = info.EpisodeTitle,
                    ProgramId = info.Id,
                    ImagePath = info.ImagePath,
                    ImageUrl = info.ImageUrl,
                    OriginalAirDate = info.OriginalAirDate,
                    Status = RecordingStatus.Scheduled,
                    Overview = info.Overview,
                    SeriesTimerId = timer.SeriesTimerId,
                    TimerId = timer.Id,
                    ShowId = info.ShowId
                };
                _recordingProvider.AddOrUpdate(recording);
            }

            try
            {
                var result = await GetChannelStreamInternal(timer.ChannelId, null, CancellationToken.None);
                var mediaStreamInfo = result.Item1;
                var isResourceOpen = true;

                // Unfortunately due to the semaphore we have to have a nested try/finally
                try
                {
                    // HDHR doesn't seem to release the tuner right away after first probing with ffmpeg
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);

                    var duration = recordingEndDate - DateTime.UtcNow;

                    HttpRequestOptions httpRequestOptions = new HttpRequestOptions()
                    {
                        Url = mediaStreamInfo.Path
                    };

                    recording.Path = recordPath;
                    recording.Status = RecordingStatus.InProgress;
                    recording.DateLastUpdated = DateTime.UtcNow;
                    _recordingProvider.AddOrUpdate(recording);

                    _logger.Info("Beginning recording.");

                    httpRequestOptions.BufferContent = false;
                    var durationToken = new CancellationTokenSource(duration);
                    var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;
                    httpRequestOptions.CancellationToken = linkedToken;
                    _logger.Info("Writing file to path: " + recordPath);
                    using (var response = await _httpClient.SendAsync(httpRequestOptions, "GET"))
                    {
                        using (var output = _fileSystem.GetFileStream(recordPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            result.Item2.Release();
                            isResourceOpen = false;

                            await response.Content.CopyToAsync(output, StreamDefaults.DefaultCopyToBufferSize, linkedToken);
                        }
                    }

                    recording.Status = RecordingStatus.Completed;
                    _logger.Info("Recording completed");
                }
                finally
                {
                    if (isResourceOpen)
                    {
                        result.Item2.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Recording stopped");
                recording.Status = RecordingStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error recording", ex);
                recording.Status = RecordingStatus.Error;
            }
            finally
            {
                CancellationTokenSource removed;
                _activeRecordings.TryRemove(timer.Id, out removed);
            }

            recording.DateLastUpdated = DateTime.UtcNow;
            _recordingProvider.AddOrUpdate(recording);

            if (recording.Status == RecordingStatus.Completed)
            {
                OnSuccessfulRecording(recording);
                _timerProvider.Delete(timer);
            }
            else if (DateTime.UtcNow < timer.EndDate)
            {
                const int retryIntervalSeconds = 60;
                _logger.Info("Retrying recording in {0} seconds.", retryIntervalSeconds);

                _timerProvider.StartTimer(timer, TimeSpan.FromSeconds(retryIntervalSeconds));
            }
            else
            {
                _timerProvider.Delete(timer);
                _recordingProvider.Delete(recording);
            }
        }

        private async void OnSuccessfulRecording(RecordingInfo recording)
        {
            if (GetConfiguration().EnableAutoOrganize)
            {
                if (recording.IsSeries)
                {
                    try
                    {
                        var organize = new EpisodeFileOrganizer(_organizationService, _config, _fileSystem, _logger, _libraryManager, _libraryMonitor, _providerManager);

                        var result = await organize.OrganizeEpisodeFile(recording.Path, CancellationToken.None).ConfigureAwait(false);

                        if (result.Status == FileSortingStatus.Success)
                        {
                            _recordingProvider.Delete(recording);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error processing new recording", ex);
                    }
                }
            }
        }

        private ProgramInfo GetProgramInfoFromCache(string channelId, string programId)
        {
            var epgData = GetEpgDataForChannel(channelId);
            return epgData.FirstOrDefault(p => string.Equals(p.Id, programId, StringComparison.OrdinalIgnoreCase));
        }

        private string RecordingPath
        {
            get
            {
                var path = GetConfiguration().RecordingPath;

                return string.IsNullOrWhiteSpace(path)
                    ? Path.Combine(DataPath, "recordings")
                    : path;
            }
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        private async Task UpdateTimersForSeriesTimer(List<ProgramInfo> epgData, SeriesTimerInfo seriesTimer)
        {
            var newTimers = GetTimersForSeries(seriesTimer, epgData, _recordingProvider.GetAll()).ToList();

            var registration = await GetRegistrationInfo("seriesrecordings").ConfigureAwait(false);

            if (registration.IsValid)
            {
                foreach (var timer in newTimers)
                {
                    _timerProvider.AddOrUpdate(timer);
                }
            }
        }

        private IEnumerable<TimerInfo> GetTimersForSeries(SeriesTimerInfo seriesTimer, IEnumerable<ProgramInfo> allPrograms, IReadOnlyList<RecordingInfo> currentRecordings)
        {
            // Exclude programs that have already ended
            allPrograms = allPrograms.Where(i => i.EndDate > DateTime.UtcNow);

            allPrograms = GetProgramsForSeries(seriesTimer, allPrograms);

            var recordingShowIds = currentRecordings.Select(i => i.ProgramId).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();

            allPrograms = allPrograms.Where(i => !recordingShowIds.Contains(i.Id, StringComparer.OrdinalIgnoreCase));

            return allPrograms.Select(i => RecordingHelper.CreateTimer(i, seriesTimer));
        }

        private IEnumerable<ProgramInfo> GetProgramsForSeries(SeriesTimerInfo seriesTimer, IEnumerable<ProgramInfo> allPrograms)
        {
            if (!seriesTimer.RecordAnyTime)
            {
                allPrograms = allPrograms.Where(epg => Math.Abs(seriesTimer.StartDate.TimeOfDay.Ticks - epg.StartDate.TimeOfDay.Ticks) < TimeSpan.FromMinutes(5).Ticks);
            }

            if (seriesTimer.RecordNewOnly)
            {
                allPrograms = allPrograms.Where(epg => !epg.IsRepeat);
            }

            if (!seriesTimer.RecordAnyChannel)
            {
                allPrograms = allPrograms.Where(epg => string.Equals(epg.ChannelId, seriesTimer.ChannelId, StringComparison.OrdinalIgnoreCase));
            }

            allPrograms = allPrograms.Where(i => seriesTimer.Days.Contains(i.StartDate.ToLocalTime().DayOfWeek));

            if (string.IsNullOrWhiteSpace(seriesTimer.SeriesId))
            {
                _logger.Error("seriesTimer.SeriesId is null. Cannot find programs for series");
                return new List<ProgramInfo>();
            }

            return allPrograms.Where(i => string.Equals(i.SeriesId, seriesTimer.SeriesId, StringComparison.OrdinalIgnoreCase));
        }

        private string GetChannelEpgCachePath(string channelId)
        {
            return Path.Combine(_config.CommonApplicationPaths.CachePath, "embytvepg", channelId + ".json");
        }

        private readonly object _epgLock = new object();
        private void SaveEpgDataForChannel(string channelId, List<ProgramInfo> epgData)
        {
            var path = GetChannelEpgCachePath(channelId);
            _fileSystem.CreateDirectory(Path.GetDirectoryName(path));
            lock (_epgLock)
            {
                _jsonSerializer.SerializeToFile(epgData, path);
            }
        }
        private List<ProgramInfo> GetEpgDataForChannel(string channelId)
        {
            try
            {
                lock (_epgLock)
                {
                    return _jsonSerializer.DeserializeFromFile<List<ProgramInfo>>(GetChannelEpgCachePath(channelId));
                }
            }
            catch
            {
                return new List<ProgramInfo>();
            }
        }
        private List<ProgramInfo> GetEpgDataForChannels(List<string> channelIds)
        {
            return channelIds.SelectMany(GetEpgDataForChannel).ToList();
        }

        public void Dispose()
        {
            foreach (var pair in _activeRecordings.ToList())
            {
                pair.Value.Cancel();
            }
        }

        public Task<MBRegistrationRecord> GetRegistrationInfo(string feature)
        {
            if (string.Equals(feature, "seriesrecordings", StringComparison.OrdinalIgnoreCase))
            {
                return _security.GetRegistrationStatus("embytvseriesrecordings");
            }

            return Task.FromResult(new MBRegistrationRecord
            {
                IsValid = true,
                IsRegistered = true
            });
        }
    }
}