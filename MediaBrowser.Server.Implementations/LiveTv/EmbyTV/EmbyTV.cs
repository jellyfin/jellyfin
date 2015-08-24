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
using MediaBrowser.Controller.Providers;
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

        public static EmbyTV Current;

        public EmbyTV(IApplicationHost appHost, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient, IServerConfigurationManager config, ILiveTvManager liveTvManager, IFileSystem fileSystem, ISecurityManager security, ILibraryManager libraryManager, ILibraryMonitor libraryMonitor, IProviderManager providerManager, IFileOrganizationService organizationService)
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
            _liveTvManager = (LiveTvManager)liveTvManager;
            _jsonSerializer = jsonSerializer;

            _recordingProvider = new ItemDataProvider<RecordingInfo>(jsonSerializer, _logger, Path.Combine(DataPath, "recordings"), (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase));
            _seriesTimerProvider = new SeriesTimerManager(jsonSerializer, _logger, Path.Combine(DataPath, "seriestimers"));
            _timerProvider = new TimerManager(jsonSerializer, _logger, Path.Combine(DataPath, "timers"));
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
                    File.Delete(remove.Path);
                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (FileNotFoundException)
                {

                }
                _recordingProvider.Delete(remove);
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

        public Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<RecordingInfo>)_recordingProvider.GetAll());
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
            var channels = await GetChannelsAsync(true, cancellationToken).ConfigureAwait(false);
            var channel = channels.First(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

            foreach (var provider in GetListingProviders())
            {
                var programs = await provider.Item1.GetProgramsAsync(provider.Item2, channel.Number, startDateUtc, endDateUtc, cancellationToken)
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
                MediaSourceInfo mediaSourceInfo = null;
                try
                {
                    mediaSourceInfo = await hostInstance.GetChannelStream(channelId, streamId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error getting channel stream", e);
                }

                if (mediaSourceInfo != null)
                {
                    mediaSourceInfo.Id = Guid.NewGuid().ToString("N");
                    return mediaSourceInfo;
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

            throw new ApplicationException("Tuner not found.");
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

            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                if (_activeRecordings.TryAdd(timer.Id, cancellationTokenSource))
                {
                    await RecordStream(timer, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error recording stream", ex);

                if (DateTime.UtcNow < timer.EndDate)
                {
                    const int retryIntervalSeconds = 60;
                    _logger.Debug("Retrying recording in {0} seconds.", retryIntervalSeconds);

                    _timerProvider.StartTimer(timer, TimeSpan.FromSeconds(retryIntervalSeconds));
                }
            }
        }

        private async Task RecordStream(TimerInfo timer, CancellationToken cancellationToken)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            var mediaStreamInfo = await GetChannelStream(timer.ChannelId, null, CancellationToken.None);
            var duration = (timer.EndDate - DateTime.UtcNow).Add(TimeSpan.FromSeconds(timer.PostPaddingSeconds));

            HttpRequestOptions httpRequestOptions = new HttpRequestOptions()
            {
                Url = mediaStreamInfo.Path
            };

            var info = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            var recordPath = RecordingPath;

            if (info.IsMovie)
            {
                recordPath = Path.Combine(recordPath, "Movies", _fileSystem.GetValidFilename(info.Name));
            }
            else if (info.IsSeries)
            {
                recordPath = Path.Combine(recordPath, "Series", _fileSystem.GetValidFilename(info.Name));
            }
            else if (info.IsKids)
            {
                recordPath = Path.Combine(recordPath, "Kids", _fileSystem.GetValidFilename(info.Name));
            }
            else if (info.IsSports)
            {
                recordPath = Path.Combine(recordPath, "Sports", _fileSystem.GetValidFilename(info.Name));
            }
            else
            {
                recordPath = Path.Combine(recordPath, "Other", _fileSystem.GetValidFilename(info.Name));
            }

            var recordingFileName = _fileSystem.GetValidFilename(RecordingHelper.GetRecordingName(timer, info)) + ".ts";

            recordPath = Path.Combine(recordPath, recordingFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(recordPath));

            var recording = _recordingProvider.GetAll().FirstOrDefault(x => string.Equals(x.ProgramId, info.Id, StringComparison.OrdinalIgnoreCase));

            if (recording == null)
            {
                recording = new RecordingInfo
                {
                    ChannelId = info.ChannelId,
                    Id = Guid.NewGuid().ToString("N"),
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
                    HasImage = info.HasImage,
                    ImagePath = info.ImagePath,
                    ImageUrl = info.ImageUrl,
                    OriginalAirDate = info.OriginalAirDate,
                    Status = RecordingStatus.Scheduled,
                    Overview = info.Overview,
                    SeriesTimerId = timer.SeriesTimerId,
                    TimerId = timer.Id,
                    ShowId = info.ShowId
                };
                _recordingProvider.Add(recording);
            }

            recording.Path = recordPath;
            recording.Status = RecordingStatus.InProgress;
            recording.DateLastUpdated = DateTime.UtcNow;
            _recordingProvider.Update(recording);

            try
            {
                httpRequestOptions.BufferContent = false;
                var durationToken = new CancellationTokenSource(duration);
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;
                httpRequestOptions.CancellationToken = linkedToken;
                _logger.Info("Writing file to path: " + recordPath);
                using (var response = await _httpClient.SendAsync(httpRequestOptions, "GET"))
                {
                    using (var output = File.Open(recordPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await response.Content.CopyToAsync(output, StreamDefaults.DefaultCopyToBufferSize, linkedToken);
                    }
                }

                recording.Status = RecordingStatus.Completed;
                _logger.Info("Recording completed");
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

            recording.DateLastUpdated = DateTime.UtcNow;
            _recordingProvider.Update(recording);
            _timerProvider.Delete(timer);
            _logger.Info("Recording was a success");

            if (recording.Status == RecordingStatus.Completed)
            {
                OnSuccessfulRecording(recording);
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
            var registration = await GetRegistrationInfo("seriesrecordings").ConfigureAwait(false);

            if (registration.IsValid)
            {
                var newTimers = GetTimersForSeries(seriesTimer, epgData, _recordingProvider.GetAll()).ToList();

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
                allPrograms = allPrograms.Where(epg => (seriesTimer.StartDate.TimeOfDay == epg.StartDate.TimeOfDay));
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
            return Path.Combine(DataPath, "epg", channelId + ".json");
        }

        private readonly object _epgLock = new object();
        private void SaveEpgDataForChannel(string channelId, List<ProgramInfo> epgData)
        {
            var path = GetChannelEpgCachePath(channelId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
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
