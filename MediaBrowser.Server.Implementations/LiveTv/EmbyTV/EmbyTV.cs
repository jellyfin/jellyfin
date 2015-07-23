using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class EmbyTV : ILiveTvService, IDisposable
    {
        private readonly IApplicationHost _appHpst;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly ItemDataProvider<RecordingInfo> _recordingProvider;
        private readonly ItemDataProvider<SeriesTimerInfo> _seriesTimerProvider;
        private readonly TimerManager _timerProvider;

        private readonly LiveTvManager _liveTvManager;

        public EmbyTV(IApplicationHost appHost, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient, IConfigurationManager config, ILiveTvManager liveTvManager)
        {
            _appHpst = appHost;
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
            _liveTvManager = (LiveTvManager)liveTvManager;
            _jsonSerializer = jsonSerializer;

            _recordingProvider = new ItemDataProvider<RecordingInfo>(jsonSerializer, _logger, Path.Combine(DataPath, "recordings"), (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase));
            _seriesTimerProvider = new SeriesTimerManager(jsonSerializer, _logger, Path.Combine(DataPath, "seriestimers"));
            _timerProvider = new TimerManager(jsonSerializer, _logger, Path.Combine(DataPath, "timers"));
            _timerProvider.TimerFired += _timerProvider_TimerFired;
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

            foreach (var host in _liveTvManager.TunerHosts)
            {
                foreach (var hostInstance in host.GetTunerHosts())
                {
                    try
                    {
                        var tuners = await host.GetTunerInfos(hostInstance, cancellationToken).ConfigureAwait(false);

                        list.AddRange(tuners);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting tuners", ex);
                    }
                }
            }

            status.Tuners = list;
            status.Status = LiveTvServiceStatus.Ok;
            status.Version = _appHpst.ApplicationVersion.ToString();
            status.IsVisible = false;
            return status;
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            var list = new List<ChannelInfo>();

            foreach (var host in _liveTvManager.TunerHosts)
            {
                foreach (var hostInstance in host.GetTunerHosts())
                {
                    try
                    {
                        var channels = await host.GetChannels(hostInstance, cancellationToken).ConfigureAwait(false);

                        list.AddRange(channels);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting channels", ex);
                    }
                }
            }

            if (list.Count > 0)
            {
                foreach (var provider in GetListingProviders())
                {
                    await provider.Item1.AddMetadata(provider.Item2, list, cancellationToken).ConfigureAwait(false);
                }
            }

            return list;
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var remove = _seriesTimerProvider.GetAll().SingleOrDefault(r => r.Id == timerId);
            if (remove != null)
            {
                _seriesTimerProvider.Delete(remove);
            }
            return Task.FromResult(true);
        }

        private void CancelTimerInternal(string timerId)
        {
            var remove = _timerProvider.GetAll().SingleOrDefault(r => r.Id == timerId);
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

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            var remove = _recordingProvider.GetAll().FirstOrDefault(i => string.Equals(i.Id, recordingId, StringComparison.OrdinalIgnoreCase));
            if (remove != null)
            {
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
            return Task.FromResult(true);
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            info.Id = Guid.NewGuid().ToString("N");
            _timerProvider.Add(info);
            return Task.FromResult(0);
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            info.Id = info.ProgramId.Substring(0, 10);

            UpdateTimersForSeriesTimer(info);
            _seriesTimerProvider.Add(info);
            return Task.FromResult(true);
        }

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            _seriesTimerProvider.Update(info);
            UpdateTimersForSeriesTimer(info);
            return Task.FromResult(true);
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
            var defaults = new SeriesTimerInfo()
            {
                PostPaddingSeconds = 60,
                PrePaddingSeconds = 60,
                RecordAnyChannel = false,
                RecordAnyTime = false,
                RecordNewOnly = false
            };
            return Task.FromResult(defaults);
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<SeriesTimerInfo>)_seriesTimerProvider.GetAll());
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var allChannels = await GetChannelsAsync(cancellationToken).ConfigureAwait(false);
            var channelInfo = allChannels.FirstOrDefault(i => string.Equals(channelId, i.Id, StringComparison.OrdinalIgnoreCase));

            if (channelInfo == null)
            {
                _logger.Debug("Returning empty program list because channel was not found.");
                return new List<ProgramInfo>();
            }

            foreach (var provider in GetListingProviders())
            {
                var programs = await provider.Item1.GetProgramsAsync(provider.Item2, channelInfo, startDateUtc, endDateUtc, cancellationToken)
                        .ConfigureAwait(false);
                var list = programs.ToList();

                if (list.Count > 0)
                {
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

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
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
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                if (_activeRecordings.TryAdd(e.Argument.Id, cancellationTokenSource))
                {
                    await RecordStream(e.Argument, cancellationTokenSource.Token).ConfigureAwait(false);
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

        private async Task RecordStream(TimerInfo timer, CancellationToken cancellationToken)
        {
            var mediaStreamInfo = await GetChannelStream(timer.ChannelId, "none", CancellationToken.None);
            var duration = (timer.EndDate - RecordingHelper.GetStartTime(timer)).TotalSeconds + timer.PrePaddingSeconds;

            HttpRequestOptions httpRequestOptions = new HttpRequestOptions()
            {
                Url = mediaStreamInfo.Path + "?duration=" + duration
            };

            var info = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            var recordPath = RecordingPath;
            if (info.IsMovie)
            {
                recordPath = Path.Combine(recordPath, "Movies", RecordingHelper.RemoveSpecialCharacters(info.Name));
            }
            else
            {
                recordPath = Path.Combine(recordPath, "TV", RecordingHelper.RemoveSpecialCharacters(info.Name));
            }

            recordPath = Path.Combine(recordPath, RecordingHelper.GetRecordingName(timer, info));
            Directory.CreateDirectory(Path.GetDirectoryName(recordPath));

            var recording = _recordingProvider.GetAll().FirstOrDefault(x => string.Equals(x.Id, info.Id, StringComparison.OrdinalIgnoreCase));

            if (recording == null)
            {
                recording = new RecordingInfo()
                {
                    ChannelId = info.ChannelId,
                    Id = info.Id,
                    StartDate = info.StartDate,
                    EndDate = info.EndDate,
                    Genres = info.Genres ?? null,
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
                    EpisodeTitle = info.EpisodeTitle ?? "",
                    ProgramId = info.Id,
                    HasImage = info.HasImage ?? false,
                    ImagePath = info.ImagePath ?? null,
                    ImageUrl = info.ImageUrl,
                    OriginalAirDate = info.OriginalAirDate,
                    Status = RecordingStatus.Scheduled,
                    Overview = info.Overview,
                    SeriesTimerId = info.Id.Substring(0, 10)
                };
                _recordingProvider.Add(recording);
            }

            recording.Path = recordPath;
            recording.Status = RecordingStatus.InProgress;
            _recordingProvider.Update(recording);

            try
            {
                httpRequestOptions.BufferContent = false;
                httpRequestOptions.CancellationToken = cancellationToken;
                _logger.Info("Writing file to path: " + recordPath);
                using (var response = await _httpClient.SendAsync(httpRequestOptions, "GET"))
                {
                    using (var output = File.Open(recordPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await response.Content.CopyToAsync(output, 4096, cancellationToken);
                    }
                }

                recording.Status = RecordingStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                recording.Status = RecordingStatus.Cancelled;
            }
            catch
            {
                recording.Status = RecordingStatus.Error;
            }

            _recordingProvider.Update(recording);
            _timerProvider.Delete(timer);
            _logger.Info("Recording was a success");
        }

        private ProgramInfo GetProgramInfoFromCache(string channelId, string programId)
        {
            var epgData = GetEpgDataForChannel(channelId);
            if (epgData.Any())
            {
                return epgData.FirstOrDefault(p => p.Id == programId);
            }
            return null;
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

        private void UpdateTimersForSeriesTimer(SeriesTimerInfo seriesTimer)
        {
            List<ProgramInfo> epgData;
            if (seriesTimer.RecordAnyChannel)
            {
                epgData = GetEpgDataForAllChannels();
            }
            else
            {
                epgData = GetEpgDataForChannel(seriesTimer.ChannelId);
            }

            var newTimers = RecordingHelper.GetTimersForSeries(seriesTimer, epgData, _recordingProvider.GetAll(), _logger);

            var existingTimers = _timerProvider.GetAll()
                .Where(i => string.Equals(i.SeriesTimerId, seriesTimer.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var timer in newTimers)
            {
                _timerProvider.AddOrUpdate(timer);
            }

            var newTimerIds = newTimers.Select(i => i.Id).ToList();

            foreach (var timer in existingTimers)
            {
                if (!newTimerIds.Contains(timer.Id, StringComparer.OrdinalIgnoreCase))
                {
                    CancelTimerInternal(timer.Id);
                }
            }
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
        private List<ProgramInfo> GetEpgDataForAllChannels()
        {
            List<ProgramInfo> channelEpg = new List<ProgramInfo>();
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(DataPath, "epg"));
            List<string> channels = dir.GetFiles("*").Where(i => string.Equals(i.Extension, ".json", StringComparison.OrdinalIgnoreCase)).Select(f => f.Name).ToList();
            foreach (var channel in channels)
            {
                channelEpg.AddRange(GetEpgDataForChannel(channel));
            }
            return channelEpg;
        }

        public void Dispose()
        {
            foreach (var pair in _activeRecordings.ToList())
            {
                pair.Value.Cancel();
            }
        }
    }
}
