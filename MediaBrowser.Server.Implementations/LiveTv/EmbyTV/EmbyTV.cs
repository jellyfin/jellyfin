using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.FileOrganization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.FileOrganization;
using Microsoft.Win32;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class EmbyTV : ILiveTvService, ISupportsDirectStreamProvider, ISupportsNewTimerIds, IDisposable
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly ItemDataProvider<SeriesTimerInfo> _seriesTimerProvider;
        private readonly TimerManager _timerProvider;

        private readonly LiveTvManager _liveTvManager;
        private readonly IFileSystem _fileSystem;

        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileOrganizationService _organizationService;
        private readonly IMediaEncoder _mediaEncoder;

        public static EmbyTV Current;

        public event EventHandler DataSourceChanged;
        public event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        private readonly ConcurrentDictionary<string, ActiveRecordingInfo> _activeRecordings =
            new ConcurrentDictionary<string, ActiveRecordingInfo>(StringComparer.OrdinalIgnoreCase);

        public EmbyTV(IServerApplicationHost appHost, ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient, IServerConfigurationManager config, ILiveTvManager liveTvManager, IFileSystem fileSystem, ILibraryManager libraryManager, ILibraryMonitor libraryMonitor, IProviderManager providerManager, IFileOrganizationService organizationService, IMediaEncoder mediaEncoder)
        {
            Current = this;

            _appHost = appHost;
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _libraryMonitor = libraryMonitor;
            _providerManager = providerManager;
            _organizationService = organizationService;
            _mediaEncoder = mediaEncoder;
            _liveTvManager = (LiveTvManager)liveTvManager;
            _jsonSerializer = jsonSerializer;

            _seriesTimerProvider = new SeriesTimerManager(fileSystem, jsonSerializer, _logger, Path.Combine(DataPath, "seriestimers"));
            _timerProvider = new TimerManager(fileSystem, jsonSerializer, _logger, Path.Combine(DataPath, "timers"), _logger);
            _timerProvider.TimerFired += _timerProvider_TimerFired;

            _config.NamedConfigurationUpdated += _config_NamedConfigurationUpdated;
        }

        private void _config_NamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "livetv", StringComparison.OrdinalIgnoreCase))
            {
                OnRecordingFoldersChanged();
            }
        }

        public void Start()
        {
            _timerProvider.RestartTimers();

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            CreateRecordingFolders();
        }

        private void OnRecordingFoldersChanged()
        {
            CreateRecordingFolders();
        }

        internal void CreateRecordingFolders()
        {
            try
            {
                CreateRecordingFoldersInternal();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating recording folders", ex);
            }
        }

        internal void CreateRecordingFoldersInternal()
        {
            var recordingFolders = GetRecordingFolders();

            var virtualFolders = _libraryManager.GetVirtualFolders()
                .ToList();

            var allExistingPaths = virtualFolders.SelectMany(i => i.Locations).ToList();

            var pathsAdded = new List<string>();

            foreach (var recordingFolder in recordingFolders)
            {
                var pathsToCreate = recordingFolder.Locations
                    .Where(i => !allExistingPaths.Contains(i, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (pathsToCreate.Count == 0)
                {
                    continue;
                }

                var mediaPathInfos = pathsToCreate.Select(i => new MediaPathInfo { Path = i }).ToArray();

                var libraryOptions = new LibraryOptions
                {
                    PathInfos = mediaPathInfos
                };
                try
                {
                    _libraryManager.AddVirtualFolder(recordingFolder.Name, recordingFolder.CollectionType, libraryOptions, true);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error creating virtual folder", ex);
                }

                pathsAdded.AddRange(pathsToCreate);
            }

            var config = GetConfiguration();

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
                RemovePathFromLibrary(path);
            }
        }

        private void RemovePathFromLibrary(string path)
        {
            _logger.Debug("Removing path from library: {0}", path);

            var requiresRefresh = false;
            var virtualFolders = _libraryManager.GetVirtualFolders()
               .ToList();

            foreach (var virtualFolder in virtualFolders)
            {
                if (!virtualFolder.Locations.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (virtualFolder.Locations.Count == 1)
                {
                    // remove entire virtual folder
                    try
                    {
                        _libraryManager.RemoveVirtualFolder(virtualFolder.Name, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error removing virtual folder", ex);
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
                        _logger.ErrorException("Error removing media path", ex);
                    }
                }
            }

            if (requiresRefresh)
            {
                _libraryManager.ValidateMediaLibrary(new Progress<Double>(), CancellationToken.None);
            }
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            _logger.Info("Power mode changed to {0}", e.Mode);

            if (e.Mode == PowerModes.Resume)
            {
                _timerProvider.RestartTimers();
            }
        }

        public string Name
        {
            get { return "Emby"; }
        }

        public string DataPath
        {
            get { return Path.Combine(_config.CommonApplicationPaths.DataPath, "livetv"); }
        }

        private string DefaultRecordingPath
        {
            get
            {
                return Path.Combine(DataPath, "recordings");
            }
        }

        private string RecordingPath
        {
            get
            {
                var path = GetConfiguration().RecordingPath;

                return string.IsNullOrWhiteSpace(path)
                    ? DefaultRecordingPath
                    : path;
            }
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
            status.Version = _appHost.ApplicationVersion.ToString();
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
                await UpdateTimersForSeriesTimer(epgData, timer, true).ConfigureAwait(false);
            }

            var timers = await GetTimersAsync(cancellationToken).ConfigureAwait(false);

            foreach (var timer in timers.ToList())
            {
                if (DateTime.UtcNow > timer.EndDate && !_activeRecordings.ContainsKey(timer.Id))
                {
                    OnTimerOutOfDate(timer);
                }
            }
        }

        private void OnTimerOutOfDate(TimerInfo timer)
        {
            _timerProvider.Delete(timer);
        }

        private async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(bool enableCache, CancellationToken cancellationToken)
        {
            var list = new List<ChannelInfo>();

            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    var channels = await hostInstance.GetChannels(enableCache, cancellationToken).ConfigureAwait(false);

                    list.AddRange(channels);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channels", ex);
                }
            }

            foreach (var provider in GetListingProviders())
            {
                var enabledChannels = list
                    .Where(i => IsListingProviderEnabledForTuner(provider.Item2, i.TunerHostId))
                    .ToList();

                if (enabledChannels.Count > 0)
                {
                    try
                    {
                        await provider.Item1.AddMetadata(provider.Item2, enabledChannels, cancellationToken).ConfigureAwait(false);
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

            return list;
        }

        public async Task<List<ChannelInfo>> GetChannelsForListingsProvider(ListingsProviderInfo listingsProvider, CancellationToken cancellationToken)
        {
            var list = new List<ChannelInfo>();

            foreach (var hostInstance in _liveTvManager.TunerHosts)
            {
                try
                {
                    var channels = await hostInstance.GetChannels(false, cancellationToken).ConfigureAwait(false);

                    list.AddRange(channels);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channels", ex);
                }
            }

            return list
                .Where(i => IsListingProviderEnabledForTuner(listingsProvider, i.TunerHostId))
                .ToList();
        }

        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            return GetChannelsAsync(false, cancellationToken);
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var timers = _timerProvider
                .GetAll()
                .Where(i => string.Equals(i.SeriesTimerId, timerId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var timer in timers)
            {
                CancelTimerInternal(timer.Id, true);
            }

            var remove = _seriesTimerProvider.GetAll().FirstOrDefault(r => string.Equals(r.Id, timerId, StringComparison.OrdinalIgnoreCase));
            if (remove != null)
            {
                _seriesTimerProvider.Delete(remove);
            }
            return Task.FromResult(true);
        }

        private void CancelTimerInternal(string timerId, bool isSeriesCancelled)
        {
            var timer = _timerProvider.GetTimer(timerId);
            if (timer != null)
            {
                if (string.IsNullOrWhiteSpace(timer.SeriesTimerId) || isSeriesCancelled)
                {
                    _timerProvider.Delete(timer);
                }
                else
                {
                    timer.Status = RecordingStatus.Cancelled;
                    _timerProvider.AddOrUpdate(timer, false);
                }
            }
            ActiveRecordingInfo activeRecordingInfo;

            if (_activeRecordings.TryGetValue(timerId, out activeRecordingInfo))
            {
                activeRecordingInfo.CancellationTokenSource.Cancel();
            }
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            CancelTimerInternal(timerId, false);
            return Task.FromResult(true);
        }

        public Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateTimer(TimerInfo timer, CancellationToken cancellationToken)
        {
            var existingTimer = _timerProvider.GetAll()
                .FirstOrDefault(i => string.Equals(timer.ProgramId, i.ProgramId, StringComparison.OrdinalIgnoreCase));

            if (existingTimer != null)
            {
                if (existingTimer.Status == RecordingStatus.Cancelled ||
                    existingTimer.Status == RecordingStatus.Completed)
                {
                    existingTimer.Status = RecordingStatus.New;
                    _timerProvider.Update(existingTimer);
                    return Task.FromResult(existingTimer.Id);
                }
                else
                {
                    throw new ArgumentException("A scheduled recording already exists for this program.");
                }
            }

            timer.Id = Guid.NewGuid().ToString("N");

            ProgramInfo programInfo = null;

            if (!string.IsNullOrWhiteSpace(timer.ProgramId))
            {
                programInfo = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            }
            if (programInfo == null)
            {
                _logger.Info("Unable to find program with Id {0}. Will search using start date", timer.ProgramId);
                programInfo = GetProgramInfoFromCache(timer.ChannelId, timer.StartDate);
            }

            if (programInfo != null)
            {
                RecordingHelper.CopyProgramInfoToTimerInfo(programInfo, timer);
            }

            _timerProvider.Add(timer);
            return Task.FromResult(timer.Id);
        }

        public async Task<string> CreateSeriesTimer(SeriesTimerInfo info, CancellationToken cancellationToken)
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
            await UpdateTimersForSeriesTimer(epgData, info, false).ConfigureAwait(false);

            return info.Id;
        }

        public async Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            var instance = _seriesTimerProvider.GetAll().FirstOrDefault(i => string.Equals(i.Id, info.Id, StringComparison.OrdinalIgnoreCase));

            if (instance != null)
            {
                instance.ChannelId = info.ChannelId;
                instance.Days = info.Days;
                instance.EndDate = info.EndDate;
                instance.IsPostPaddingRequired = info.IsPostPaddingRequired;
                instance.IsPrePaddingRequired = info.IsPrePaddingRequired;
                instance.PostPaddingSeconds = info.PostPaddingSeconds;
                instance.PrePaddingSeconds = info.PrePaddingSeconds;
                instance.Priority = info.Priority;
                instance.RecordAnyChannel = info.RecordAnyChannel;
                instance.RecordAnyTime = info.RecordAnyTime;
                instance.RecordNewOnly = info.RecordNewOnly;
                instance.SkipEpisodesInLibrary = info.SkipEpisodesInLibrary;
                instance.KeepUpTo = info.KeepUpTo;
                instance.KeepUntil = info.KeepUntil;
                instance.StartDate = info.StartDate;

                _seriesTimerProvider.Update(instance);

                List<ProgramInfo> epgData;
                if (instance.RecordAnyChannel)
                {
                    var channels = await GetChannelsAsync(true, CancellationToken.None).ConfigureAwait(false);
                    var channelIds = channels.Select(i => i.Id).ToList();
                    epgData = GetEpgDataForChannels(channelIds);
                }
                else
                {
                    epgData = GetEpgDataForChannel(instance.ChannelId);
                }

                await UpdateTimersForSeriesTimer(epgData, instance, true).ConfigureAwait(false);
            }
        }

        public Task UpdateTimerAsync(TimerInfo updatedTimer, CancellationToken cancellationToken)
        {
            var existingTimer = _timerProvider.GetTimer(updatedTimer.Id);

            if (existingTimer == null)
            {
                throw new ResourceNotFoundException();
            }

            // Only update if not currently active
            ActiveRecordingInfo activeRecordingInfo;
            if (!_activeRecordings.TryGetValue(updatedTimer.Id, out activeRecordingInfo))
            {
                existingTimer.PrePaddingSeconds = updatedTimer.PrePaddingSeconds;
                existingTimer.PostPaddingSeconds = updatedTimer.PostPaddingSeconds;
                existingTimer.IsPostPaddingRequired = updatedTimer.IsPostPaddingRequired;
                existingTimer.IsPrePaddingRequired = updatedTimer.IsPrePaddingRequired;
            }

            return Task.FromResult(true);
        }

        private void UpdateExistingTimerWithNewMetadata(TimerInfo existingTimer, TimerInfo updatedTimer)
        {
            // Update the program info but retain the status
            existingTimer.ChannelId = updatedTimer.ChannelId;
            existingTimer.CommunityRating = updatedTimer.CommunityRating;
            existingTimer.EndDate = updatedTimer.EndDate;
            existingTimer.EpisodeNumber = updatedTimer.EpisodeNumber;
            existingTimer.EpisodeTitle = updatedTimer.EpisodeTitle;
            existingTimer.Genres = updatedTimer.Genres;
            existingTimer.HomePageUrl = updatedTimer.HomePageUrl;
            existingTimer.IsKids = updatedTimer.IsKids;
            existingTimer.IsNews = updatedTimer.IsNews;
            existingTimer.IsMovie = updatedTimer.IsMovie;
            existingTimer.IsProgramSeries = updatedTimer.IsProgramSeries;
            existingTimer.IsRepeat = updatedTimer.IsRepeat;
            existingTimer.IsSports = updatedTimer.IsSports;
            existingTimer.Name = updatedTimer.Name;
            existingTimer.OfficialRating = updatedTimer.OfficialRating;
            existingTimer.OriginalAirDate = updatedTimer.OriginalAirDate;
            existingTimer.Overview = updatedTimer.Overview;
            existingTimer.ProductionYear = updatedTimer.ProductionYear;
            existingTimer.ProgramId = updatedTimer.ProgramId;
            existingTimer.SeasonNumber = updatedTimer.SeasonNumber;
            existingTimer.ShortOverview = updatedTimer.ShortOverview;
            existingTimer.StartDate = updatedTimer.StartDate;
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
            return _activeRecordings.Values.ToList().Select(GetRecordingInfo).ToList();
        }

        public string GetActiveRecordingPath(string id)
        {
            ActiveRecordingInfo info;

            if (_activeRecordings.TryGetValue(id, out info))
            {
                return info.Path;
            }
            return null;
        }

        private RecordingInfo GetRecordingInfo(ActiveRecordingInfo info)
        {
            var timer = info.Timer;
            var program = info.Program;

            var result = new RecordingInfo
            {
                ChannelId = timer.ChannelId,
                CommunityRating = timer.CommunityRating,
                DateLastUpdated = DateTime.UtcNow,
                EndDate = timer.EndDate,
                EpisodeTitle = timer.EpisodeTitle,
                Genres = timer.Genres,
                Id = "recording" + timer.Id,
                IsKids = timer.IsKids,
                IsMovie = timer.IsMovie,
                IsNews = timer.IsNews,
                IsRepeat = timer.IsRepeat,
                IsSeries = timer.IsProgramSeries,
                IsSports = timer.IsSports,
                Name = timer.Name,
                OfficialRating = timer.OfficialRating,
                OriginalAirDate = timer.OriginalAirDate,
                Overview = timer.Overview,
                ProgramId = timer.ProgramId,
                SeriesTimerId = timer.SeriesTimerId,
                StartDate = timer.StartDate,
                Status = RecordingStatus.InProgress,
                TimerId = timer.Id
            };

            if (program != null)
            {
                result.Audio = program.Audio;
                result.ImagePath = program.ImagePath;
                result.ImageUrl = program.ImageUrl;
                result.IsHD = program.IsHD;
                result.IsLive = program.IsLive;
                result.IsPremiere = program.IsPremiere;
                result.ShowId = program.ShowId;
            }

            return result;
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            var excludeStatues = new List<RecordingStatus>
            {
                RecordingStatus.Completed
            };

            var timers = _timerProvider.GetAll()
                .Where(i => !excludeStatues.Contains(i.Status));

            return Task.FromResult(timers);
        }

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            var config = GetConfiguration();

            var defaults = new SeriesTimerInfo()
            {
                PostPaddingSeconds = Math.Max(config.PostPaddingSeconds, 0),
                PrePaddingSeconds = Math.Max(config.PrePaddingSeconds, 0),
                RecordAnyChannel = false,
                RecordAnyTime = true,
                RecordNewOnly = true,

                Days = new List<DayOfWeek>
                {
                    DayOfWeek.Sunday,
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday,
                    DayOfWeek.Saturday
                }
            };

            if (program != null)
            {
                defaults.SeriesId = program.SeriesId;
                defaults.ProgramId = program.Id;
                defaults.RecordNewOnly = !program.IsRepeat;
            }

            defaults.SkipEpisodesInLibrary = defaults.RecordNewOnly;
            defaults.KeepUntil = KeepUntil.UntilDeleted;

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

        private bool IsListingProviderEnabledForTuner(ListingsProviderInfo info, string tunerHostId)
        {
            if (info.EnableAllTuners)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(tunerHostId))
            {
                throw new ArgumentNullException("tunerHostId");
            }

            return info.EnabledTuners.Contains(tunerHostId, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IEnumerable<ProgramInfo>> GetProgramsAsyncInternal(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var channels = await GetChannelsAsync(true, cancellationToken).ConfigureAwait(false);
            var channel = channels.First(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

            foreach (var provider in GetListingProviders())
            {
                if (!IsListingProviderEnabledForTuner(provider.Item2, channel.TunerHostId))
                {
                    _logger.Debug("Skipping getting programs for channel {0}-{1} from {2}-{3}, because it's not enabled for this tuner.", channel.Number, channel.Name, provider.Item1.Name, provider.Item2.ListingsId ?? string.Empty);
                    continue;
                }

                _logger.Debug("Getting programs for channel {0}-{1} from {2}-{3}", channel.Number, channel.Name, provider.Item1.Name, provider.Item2.ListingsId ?? string.Empty);

                var channelMappings = GetChannelMappings(provider.Item2);
                var channelNumber = channel.Number;
                string mappedChannelNumber;
                if (channelMappings.TryGetValue(channelNumber, out mappedChannelNumber))
                {
                    _logger.Debug("Found mapped channel on provider {0}. Tuner channel number: {1}, Mapped channel number: {2}", provider.Item1.Name, channelNumber, mappedChannelNumber);
                    channelNumber = mappedChannelNumber;
                }

                var programs = await provider.Item1.GetProgramsAsync(provider.Item2, channelNumber, channel.Name, startDateUtc, endDateUtc, cancellationToken)
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

        private Dictionary<string, string> GetChannelMappings(ListingsProviderInfo info)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mapping in info.ChannelMappings)
            {
                dict[mapping.Name] = mapping.Value;
            }

            return dict;
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

        private readonly SemaphoreSlim _liveStreamsSemaphore = new SemaphoreSlim(1, 1);
        private readonly List<LiveStream> _liveStreams = new List<LiveStream>();

        public async Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            var result = await GetChannelStreamWithDirectStreamProvider(channelId, streamId, cancellationToken).ConfigureAwait(false);

            return result.Item1;
        }

        public async Task<Tuple<MediaSourceInfo, IDirectStreamProvider>> GetChannelStreamWithDirectStreamProvider(string channelId, string streamId, CancellationToken cancellationToken)
        {
            var result = await GetChannelStreamInternal(channelId, streamId, cancellationToken).ConfigureAwait(false);

            return new Tuple<MediaSourceInfo, IDirectStreamProvider>(result.Item2, result.Item1 as IDirectStreamProvider);
        }

        private MediaSourceInfo CloneMediaSource(MediaSourceInfo mediaSource, bool enableStreamSharing)
        {
            var json = _jsonSerializer.SerializeToString(mediaSource);
            mediaSource = _jsonSerializer.DeserializeFromString<MediaSourceInfo>(json);

            mediaSource.Id = Guid.NewGuid().ToString("N") + "_" + mediaSource.Id;

            //if (mediaSource.DateLiveStreamOpened.HasValue && enableStreamSharing)
            //{
            //    var ticks = (DateTime.UtcNow - mediaSource.DateLiveStreamOpened.Value).Ticks - TimeSpan.FromSeconds(10).Ticks;
            //    ticks = Math.Max(0, ticks);
            //    mediaSource.Path += "?t=" + ticks.ToString(CultureInfo.InvariantCulture) + "&s=" + mediaSource.DateLiveStreamOpened.Value.Ticks.ToString(CultureInfo.InvariantCulture);
            //}

            return mediaSource;
        }

        public async Task<LiveStream> GetLiveStream(string uniqueId)
        {
            await _liveStreamsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                return _liveStreams
                    .FirstOrDefault(i => string.Equals(i.UniqueId, uniqueId, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _liveStreamsSemaphore.Release();
            }

        }

        private async Task<Tuple<LiveStream, MediaSourceInfo, ITunerHost>> GetChannelStreamInternal(string channelId, string streamId, CancellationToken cancellationToken)
        {
            _logger.Info("Streaming Channel " + channelId);

            await _liveStreamsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            var result = _liveStreams.FirstOrDefault(i => string.Equals(i.OriginalStreamId, streamId, StringComparison.OrdinalIgnoreCase));

            if (result != null && result.EnableStreamSharing)
            {
                var openedMediaSource = CloneMediaSource(result.OpenedMediaSource, result.EnableStreamSharing);
                result.SharedStreamIds.Add(openedMediaSource.Id);
                _liveStreamsSemaphore.Release();

                _logger.Info("Live stream {0} consumer count is now {1}", streamId, result.ConsumerCount);

                return new Tuple<LiveStream, MediaSourceInfo, ITunerHost>(result, openedMediaSource, result.TunerHost);
            }

            try
            {
                foreach (var hostInstance in _liveTvManager.TunerHosts)
                {
                    try
                    {
                        result = await hostInstance.GetChannelStream(channelId, streamId, cancellationToken).ConfigureAwait(false);

                        var openedMediaSource = CloneMediaSource(result.OpenedMediaSource, result.EnableStreamSharing);

                        result.SharedStreamIds.Add(openedMediaSource.Id);
                        _liveStreams.Add(result);

                        result.TunerHost = hostInstance;
                        result.OriginalStreamId = streamId;

                        _logger.Info("Returning mediasource streamId {0}, mediaSource.Id {1}, mediaSource.LiveStreamId {2}",
                            streamId, openedMediaSource.Id, openedMediaSource.LiveStreamId);

                        return new Tuple<LiveStream, MediaSourceInfo, ITunerHost>(result, openedMediaSource, hostInstance);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            finally
            {
                _liveStreamsSemaphore.Release();
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

        public async Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken)
        {
            ActiveRecordingInfo info;

            recordingId = recordingId.Replace("recording", string.Empty);

            if (_activeRecordings.TryGetValue(recordingId, out info))
            {
                var stream = new MediaSourceInfo
                {
                    Path = _appHost.GetLocalApiUrl("127.0.0.1") + "/LiveTv/LiveRecordings/" + recordingId + "/stream",
                    Id = recordingId,
                    SupportsDirectPlay = false,
                    SupportsDirectStream = true,
                    SupportsTranscoding = true,
                    IsInfiniteStream = true,
                    RequiresOpening = false,
                    RequiresClosing = false,
                    Protocol = Model.MediaInfo.MediaProtocol.Http,
                    BufferMs = 0
                };

                var isAudio = false;
                await new LiveStreamHelper(_mediaEncoder, _logger).AddMediaInfoWithProbe(stream, isAudio, cancellationToken).ConfigureAwait(false);

                return new List<MediaSourceInfo>
                {
                    stream
                };
            }

            throw new FileNotFoundException();
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            // Ignore the consumer id
            //id = id.Substring(id.IndexOf('_') + 1);

            await _liveStreamsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var stream = _liveStreams.FirstOrDefault(i => i.SharedStreamIds.Contains(id));
                if (stream != null)
                {
                    stream.SharedStreamIds.Remove(id);

                    _logger.Info("Live stream {0} consumer count is now {1}", id, stream.ConsumerCount);

                    if (stream.ConsumerCount <= 0)
                    {
                        _liveStreams.Remove(stream);

                        _logger.Info("Closing live stream {0}", id);

                        await stream.Close().ConfigureAwait(false);
                        _logger.Info("Live stream {0} closed successfully", id);
                    }
                }
                else
                {
                    _logger.Warn("Live stream not found: {0}, unable to close", id);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error closing live stream", ex);
            }
            finally
            {
                _liveStreamsSemaphore.Release();
            }
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
                    _logger.Warn("Recording timer fired for updatedTimer {0}, Id: {1}, but the program has already ended.", timer.Name, timer.Id);
                    OnTimerOutOfDate(timer);
                    return;
                }

                var activeRecordingInfo = new ActiveRecordingInfo
                {
                    CancellationTokenSource = new CancellationTokenSource(),
                    Timer = timer
                };

                if (_activeRecordings.TryAdd(timer.Id, activeRecordingInfo))
                {
                    await RecordStream(timer, recordingEndDate, activeRecordingInfo, activeRecordingInfo.CancellationTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    _logger.Info("Skipping RecordStream because it's already in progress.");
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

        private string GetRecordingPath(TimerInfo timer, out string seriesPath)
        {
            var recordPath = RecordingPath;
            var config = GetConfiguration();
            seriesPath = null;

            if (timer.IsProgramSeries)
            {
                var customRecordingPath = config.SeriesRecordingPath;
                var allowSubfolder = true;
                if (!string.IsNullOrWhiteSpace(customRecordingPath))
                {
                    allowSubfolder = string.Equals(customRecordingPath, recordPath, StringComparison.OrdinalIgnoreCase);
                    recordPath = customRecordingPath;
                }

                if (allowSubfolder && config.EnableRecordingSubfolders)
                {
                    recordPath = Path.Combine(recordPath, "Series");
                }

                var folderName = _fileSystem.GetValidFilename(timer.Name).Trim();

                // Can't use the year here in the folder name because it is the year of the episode, not the series.
                recordPath = Path.Combine(recordPath, folderName);

                seriesPath = recordPath;

                if (timer.SeasonNumber.HasValue)
                {
                    folderName = string.Format("Season {0}", timer.SeasonNumber.Value.ToString(CultureInfo.InvariantCulture));
                    recordPath = Path.Combine(recordPath, folderName);
                }
            }
            else if (timer.IsMovie)
            {
                var customRecordingPath = config.MovieRecordingPath;
                var allowSubfolder = true;
                if (!string.IsNullOrWhiteSpace(customRecordingPath))
                {
                    allowSubfolder = string.Equals(customRecordingPath, recordPath, StringComparison.OrdinalIgnoreCase);
                    recordPath = customRecordingPath;
                }

                if (allowSubfolder && config.EnableRecordingSubfolders)
                {
                    recordPath = Path.Combine(recordPath, "Movies");
                }

                var folderName = _fileSystem.GetValidFilename(timer.Name).Trim();
                if (timer.ProductionYear.HasValue)
                {
                    folderName += " (" + timer.ProductionYear.Value.ToString(CultureInfo.InvariantCulture) + ")";
                }
                recordPath = Path.Combine(recordPath, folderName);
            }
            else if (timer.IsKids)
            {
                if (config.EnableRecordingSubfolders)
                {
                    recordPath = Path.Combine(recordPath, "Kids");
                }

                var folderName = _fileSystem.GetValidFilename(timer.Name).Trim();
                if (timer.ProductionYear.HasValue)
                {
                    folderName += " (" + timer.ProductionYear.Value.ToString(CultureInfo.InvariantCulture) + ")";
                }
                recordPath = Path.Combine(recordPath, folderName);
            }
            else if (timer.IsSports)
            {
                if (config.EnableRecordingSubfolders)
                {
                    recordPath = Path.Combine(recordPath, "Sports");
                }
                recordPath = Path.Combine(recordPath, _fileSystem.GetValidFilename(timer.Name).Trim());
            }
            else
            {
                if (config.EnableRecordingSubfolders)
                {
                    recordPath = Path.Combine(recordPath, "Other");
                }
                recordPath = Path.Combine(recordPath, _fileSystem.GetValidFilename(timer.Name).Trim());
            }

            var recordingFileName = _fileSystem.GetValidFilename(RecordingHelper.GetRecordingName(timer)).Trim() + ".ts";

            return Path.Combine(recordPath, recordingFileName);
        }

        private async Task RecordStream(TimerInfo timer, DateTime recordingEndDate,
            ActiveRecordingInfo activeRecordingInfo, CancellationToken cancellationToken)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            ProgramInfo programInfo = null;

            if (!string.IsNullOrWhiteSpace(timer.ProgramId))
            {
                programInfo = GetProgramInfoFromCache(timer.ChannelId, timer.ProgramId);
            }
            if (programInfo == null)
            {
                _logger.Info("Unable to find program with Id {0}. Will search using start date", timer.ProgramId);
                programInfo = GetProgramInfoFromCache(timer.ChannelId, timer.StartDate);
            }

            if (programInfo != null)
            {
                RecordingHelper.CopyProgramInfoToTimerInfo(programInfo, timer);
                activeRecordingInfo.Program = programInfo;
            }

            string seriesPath = null;
            var recordPath = GetRecordingPath(timer, out seriesPath);
            var recordingStatus = RecordingStatus.New;

            string liveStreamId = null;

            OnRecordingStatusChanged();

            try
            {
                var allMediaSources = await GetChannelStreamMediaSources(timer.ChannelId, CancellationToken.None).ConfigureAwait(false);

                var liveStreamInfo = await GetChannelStreamInternal(timer.ChannelId, allMediaSources[0].Id, CancellationToken.None)
                            .ConfigureAwait(false);

                var mediaStreamInfo = liveStreamInfo.Item2;
                liveStreamId = mediaStreamInfo.Id;

                // HDHR doesn't seem to release the tuner right away after first probing with ffmpeg
                //await Task.Delay(3000, cancellationToken).ConfigureAwait(false);

                var recorder = await GetRecorder().ConfigureAwait(false);

                recordPath = recorder.GetOutputPath(mediaStreamInfo, recordPath);
                recordPath = EnsureFileUnique(recordPath, timer.Id);

                _libraryManager.RegisterIgnoredPath(recordPath);
                _libraryMonitor.ReportFileSystemChangeBeginning(recordPath);
                _fileSystem.CreateDirectory(Path.GetDirectoryName(recordPath));
                activeRecordingInfo.Path = recordPath;

                var duration = recordingEndDate - DateTime.UtcNow;

                _logger.Info("Beginning recording. Will record for {0} minutes.",
                    duration.TotalMinutes.ToString(CultureInfo.InvariantCulture));

                _logger.Info("Writing file to path: " + recordPath);
                _logger.Info("Opening recording stream from tuner provider");

                Action onStarted = () =>
                {
                    timer.Status = RecordingStatus.InProgress;
                    _timerProvider.AddOrUpdate(timer, false);

                    SaveNfo(timer, recordPath, seriesPath);
                    EnforceKeepUpTo(timer);
                };

                await recorder.Record(mediaStreamInfo, recordPath, duration, onStarted, cancellationToken)
                        .ConfigureAwait(false);

                recordingStatus = RecordingStatus.Completed;
                _logger.Info("Recording completed: {0}", recordPath);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Recording stopped: {0}", recordPath);
                recordingStatus = RecordingStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error recording to {0}", ex, recordPath);
                recordingStatus = RecordingStatus.Error;
            }

            if (!string.IsNullOrWhiteSpace(liveStreamId))
            {
                try
                {
                    await CloseLiveStream(liveStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing live stream", ex);
                }
            }

            _libraryManager.UnRegisterIgnoredPath(recordPath);
            _libraryMonitor.ReportFileSystemChangeComplete(recordPath, true);

            ActiveRecordingInfo removed;
            _activeRecordings.TryRemove(timer.Id, out removed);

            if (recordingStatus != RecordingStatus.Completed && DateTime.UtcNow < timer.EndDate)
            {
                const int retryIntervalSeconds = 60;
                _logger.Info("Retrying recording in {0} seconds.", retryIntervalSeconds);

                timer.Status = RecordingStatus.New;
                timer.StartDate = DateTime.UtcNow.AddSeconds(retryIntervalSeconds);
                _timerProvider.AddOrUpdate(timer);
            }
            else if (File.Exists(recordPath))
            {
                timer.RecordingPath = recordPath;
                timer.Status = RecordingStatus.Completed;
                _timerProvider.AddOrUpdate(timer, false);
                OnSuccessfulRecording(timer, recordPath);
            }
            else
            {
                _timerProvider.Delete(timer);
            }

            OnRecordingStatusChanged();
        }

        private void OnRecordingStatusChanged()
        {
            EventHelper.FireEventIfNotNull(RecordingStatusChanged, this, new RecordingStatusChangedEventArgs
            {

            }, _logger);
        }

        private async void EnforceKeepUpTo(TimerInfo timer)
        {
            if (string.IsNullOrWhiteSpace(timer.SeriesTimerId))
            {
                return;
            }

            var seriesTimerId = timer.SeriesTimerId;
            var seriesTimer = _seriesTimerProvider.GetAll().FirstOrDefault(i => string.Equals(i.Id, seriesTimerId, StringComparison.OrdinalIgnoreCase));

            if (seriesTimer == null || seriesTimer.KeepUpTo <= 1)
            {
                return;
            }

            if (_disposed)
            {
                return;
            }

            await _recordingDeleteSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_disposed)
                {
                    return;
                }

                var timersToDelete = _timerProvider.GetAll()
                    .Where(i => i.Status == RecordingStatus.Completed && !string.IsNullOrWhiteSpace(i.RecordingPath))
                    .Where(i => string.Equals(i.SeriesTimerId, seriesTimerId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(i => i.EndDate)
                    .Where(i => File.Exists(i.RecordingPath))
                    .Skip(seriesTimer.KeepUpTo - 1)
                    .ToList();

                await DeleteLibraryItemsForTimers(timersToDelete).ConfigureAwait(false);
            }
            finally
            {
                _recordingDeleteSemaphore.Release();
            }
        }

        private readonly SemaphoreSlim _recordingDeleteSemaphore = new SemaphoreSlim(1, 1);
        private async Task DeleteLibraryItemsForTimers(List<TimerInfo> timers)
        {
            foreach (var timer in timers)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    await DeleteLibraryItemForTimer(timer).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting recording", ex);
                }
            }
        }

        private async Task DeleteLibraryItemForTimer(TimerInfo timer)
        {
            var libraryItem = _libraryManager.FindByPath(timer.RecordingPath, false);

            if (libraryItem != null)
            {
                await _libraryManager.DeleteItem(libraryItem, new DeleteOptions
                {
                    DeleteFileLocation = true
                });
            }
            else
            {
                try
                {
                    File.Delete(timer.RecordingPath);
                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (FileNotFoundException)
                {

                }
            }

            _timerProvider.Delete(timer);
        }

        private string EnsureFileUnique(string path, string timerId)
        {
            var originalPath = path;
            var index = 1;

            while (FileExists(path, timerId))
            {
                var parent = Path.GetDirectoryName(originalPath);
                var name = Path.GetFileNameWithoutExtension(originalPath);
                name += "-" + index.ToString(CultureInfo.InvariantCulture);

                path = Path.ChangeExtension(Path.Combine(parent, name), Path.GetExtension(originalPath));
                index++;
            }

            return path;
        }

        private bool FileExists(string path, string timerId)
        {
            if (_fileSystem.FileExists(path))
            {
                return true;
            }

            var hasRecordingAtPath = _activeRecordings
                .Values
                .ToList()
                .Any(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase) && !string.Equals(i.Timer.Id, timerId, StringComparison.OrdinalIgnoreCase));

            if (hasRecordingAtPath)
            {
                return true;
            }
            return false;
        }

        private async Task<IRecorder> GetRecorder()
        {
            var config = GetConfiguration();

            if (config.EnableRecordingEncoding)
            {
                var regInfo = await _liveTvManager.GetRegistrationInfo("embytvrecordingconversion").ConfigureAwait(false);

                if (regInfo.IsValid)
                {
                    return new EncodedRecorder(_logger, _fileSystem, _mediaEncoder, _config.ApplicationPaths, _jsonSerializer, config, _httpClient);
                }
            }

            return new DirectRecorder(_logger, _httpClient, _fileSystem);
        }

        private async void OnSuccessfulRecording(TimerInfo timer, string path)
        {
            if (timer.IsProgramSeries && GetConfiguration().EnableAutoOrganize)
            {
                try
                {
                    // this is to account for the library monitor holding a lock for additional time after the change is complete.
                    // ideally this shouldn't be hard-coded
                    await Task.Delay(30000).ConfigureAwait(false);

                    var organize = new EpisodeFileOrganizer(_organizationService, _config, _fileSystem, _logger, _libraryManager, _libraryMonitor, _providerManager);

                    var result = await organize.OrganizeEpisodeFile(path, _config.GetAutoOrganizeOptions(), false, CancellationToken.None).ConfigureAwait(false);

                    if (result.Status == FileSortingStatus.Success)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error processing new recording", ex);
                }
            }
        }

        private void SaveNfo(TimerInfo timer, string recordingPath, string seriesPath)
        {
            try
            {
                if (timer.IsProgramSeries)
                {
                    SaveSeriesNfo(timer, recordingPath, seriesPath);
                }
                else if (!timer.IsMovie || timer.IsSports || timer.IsNews)
                {
                    SaveVideoNfo(timer, recordingPath);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving nfo", ex);
            }
        }

        private void SaveSeriesNfo(TimerInfo timer, string recordingPath, string seriesPath)
        {
            var nfoPath = Path.Combine(seriesPath, "tvshow.nfo");

            if (File.Exists(nfoPath))
            {
                return;
            }

            using (var stream = _fileSystem.GetFileStream(nfoPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    CloseOutput = false
                };

                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    writer.WriteStartDocument(true);
                    writer.WriteStartElement("tvshow");

                    if (!string.IsNullOrWhiteSpace(timer.Name))
                    {
                        writer.WriteElementString("title", timer.Name);
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";
        private void SaveVideoNfo(TimerInfo timer, string recordingPath)
        {
            var nfoPath = Path.ChangeExtension(recordingPath, ".nfo");

            if (File.Exists(nfoPath))
            {
                return;
            }

            using (var stream = _fileSystem.GetFileStream(nfoPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    CloseOutput = false
                };

                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    writer.WriteStartDocument(true);
                    writer.WriteStartElement("movie");

                    if (!string.IsNullOrWhiteSpace(timer.Name))
                    {
                        writer.WriteElementString("title", timer.Name);
                    }

                    writer.WriteElementString("dateadded", DateTime.UtcNow.ToLocalTime().ToString(DateAddedFormat));

                    if (timer.ProductionYear.HasValue)
                    {
                        writer.WriteElementString("year", timer.ProductionYear.Value.ToString(CultureInfo.InvariantCulture));
                    }
                    if (!string.IsNullOrEmpty(timer.OfficialRating))
                    {
                        writer.WriteElementString("mpaa", timer.OfficialRating);
                    }

                    var overview = (timer.Overview ?? string.Empty)
                        .StripHtml()
                        .Replace("&quot;", "'");

                    writer.WriteElementString("plot", overview);
                    writer.WriteElementString("lockdata", true.ToString().ToLower());

                    if (timer.CommunityRating.HasValue)
                    {
                        writer.WriteElementString("rating", timer.CommunityRating.Value.ToString(CultureInfo.InvariantCulture));
                    }

                    if (timer.IsSports)
                    {
                        AddGenre(timer.Genres, "Sports");
                    }
                    if (timer.IsKids)
                    {
                        AddGenre(timer.Genres, "Kids");
                        AddGenre(timer.Genres, "Children");
                    }
                    if (timer.IsNews)
                    {
                        AddGenre(timer.Genres, "News");
                    }

                    foreach (var genre in timer.Genres)
                    {
                        writer.WriteElementString("genre", genre);
                    }

                    if (!string.IsNullOrWhiteSpace(timer.ShortOverview))
                    {
                        writer.WriteElementString("outline", timer.ShortOverview);
                    }

                    if (!string.IsNullOrWhiteSpace(timer.HomePageUrl))
                    {
                        writer.WriteElementString("website", timer.HomePageUrl);
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        private void AddGenre(List<string> genres, string genre)
        {
            if (!genres.Contains(genre, StringComparer.OrdinalIgnoreCase))
            {
                genres.Add(genre);
            }
        }

        private ProgramInfo GetProgramInfoFromCache(string channelId, string programId)
        {
            var epgData = GetEpgDataForChannel(channelId);
            return epgData.FirstOrDefault(p => string.Equals(p.Id, programId, StringComparison.OrdinalIgnoreCase));
        }

        private ProgramInfo GetProgramInfoFromCache(string channelId, DateTime startDateUtc)
        {
            var epgData = GetEpgDataForChannel(channelId);
            var startDateTicks = startDateUtc.Ticks;
            // Find the first program that starts within 3 minutes
            return epgData.FirstOrDefault(p => Math.Abs(startDateTicks - p.StartDate.Ticks) <= TimeSpan.FromMinutes(3).Ticks);
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        private bool ShouldCancelTimerForSeriesTimer(SeriesTimerInfo seriesTimer, TimerInfo timer)
        {
            if (!seriesTimer.RecordAnyTime)
            {
                if (Math.Abs(seriesTimer.StartDate.TimeOfDay.Ticks - timer.StartDate.TimeOfDay.Ticks) >= TimeSpan.FromMinutes(5).Ticks)
                {
                    return true;
                }

                if (!seriesTimer.Days.Contains(timer.StartDate.ToLocalTime().DayOfWeek))
                {
                    return true;
                }
            }

            if (seriesTimer.RecordNewOnly && timer.IsRepeat)
            {
                return true;
            }

            if (!seriesTimer.RecordAnyChannel && !string.Equals(timer.ChannelId, seriesTimer.ChannelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return seriesTimer.SkipEpisodesInLibrary && IsProgramAlreadyInLibrary(timer);
        }

        private async Task UpdateTimersForSeriesTimer(List<ProgramInfo> epgData, SeriesTimerInfo seriesTimer, bool deleteInvalidTimers)
        {
            var allTimers = GetTimersForSeries(seriesTimer, epgData)
                .ToList();

            var registration = await _liveTvManager.GetRegistrationInfo("seriesrecordings").ConfigureAwait(false);

            if (registration.IsValid)
            {
                foreach (var timer in allTimers)
                {
                    var existingTimer = _timerProvider.GetTimer(timer.Id);

                    if (existingTimer == null)
                    {
                        if (ShouldCancelTimerForSeriesTimer(seriesTimer, timer))
                        {
                            timer.Status = RecordingStatus.Cancelled;
                        }
                        _timerProvider.Add(timer);
                    }
                    else
                    {
                        // Only update if not currently active
                        ActiveRecordingInfo activeRecordingInfo;
                        if (!_activeRecordings.TryGetValue(timer.Id, out activeRecordingInfo))
                        {
                            UpdateExistingTimerWithNewMetadata(existingTimer, timer);

                            if (ShouldCancelTimerForSeriesTimer(seriesTimer, timer))
                            {
                                existingTimer.Status = RecordingStatus.Cancelled;
                            }

                            existingTimer.SeriesTimerId = seriesTimer.Id;
                            _timerProvider.Update(existingTimer);
                        }
                    }
                }
            }

            if (deleteInvalidTimers)
            {
                var allTimerIds = allTimers
                    .Select(i => i.Id)
                    .ToList();

                var deleteStatuses = new List<RecordingStatus>
                {
                    RecordingStatus.New
                };

                var deletes = _timerProvider.GetAll()
                    .Where(i => string.Equals(i.SeriesTimerId, seriesTimer.Id, StringComparison.OrdinalIgnoreCase))
                    .Where(i => !allTimerIds.Contains(i.Id, StringComparer.OrdinalIgnoreCase) && i.StartDate > DateTime.UtcNow)
                    .Where(i => deleteStatuses.Contains(i.Status))
                    .ToList();

                foreach (var timer in deletes)
                {
                    CancelTimerInternal(timer.Id, false);
                }
            }
        }

        private IEnumerable<TimerInfo> GetTimersForSeries(SeriesTimerInfo seriesTimer,
            IEnumerable<ProgramInfo> allPrograms)
        {
            if (seriesTimer == null)
            {
                throw new ArgumentNullException("seriesTimer");
            }
            if (allPrograms == null)
            {
                throw new ArgumentNullException("allPrograms");
            }

            // Exclude programs that have already ended
            allPrograms = allPrograms.Where(i => i.EndDate > DateTime.UtcNow);

            allPrograms = GetProgramsForSeries(seriesTimer, allPrograms);

            return allPrograms.Select(i => RecordingHelper.CreateTimer(i, seriesTimer));
        }

        private bool IsProgramAlreadyInLibrary(TimerInfo program)
        {
            if ((program.EpisodeNumber.HasValue && program.SeasonNumber.HasValue) || !string.IsNullOrWhiteSpace(program.EpisodeTitle))
            {
                var seriesIds = _libraryManager.GetItemIds(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(Series).Name },
                    Name = program.Name

                }).Select(i => i.ToString("N")).ToArray();

                if (seriesIds.Length == 0)
                {
                    return false;
                }

                if (program.EpisodeNumber.HasValue && program.SeasonNumber.HasValue)
                {
                    var result = _libraryManager.GetItemsResult(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Episode).Name },
                        ParentIndexNumber = program.SeasonNumber.Value,
                        IndexNumber = program.EpisodeNumber.Value,
                        AncestorIds = seriesIds,
                        IsVirtualItem = false
                    });

                    if (result.TotalRecordCount > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerable<ProgramInfo> GetProgramsForSeries(SeriesTimerInfo seriesTimer, IEnumerable<ProgramInfo> allPrograms)
        {
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

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            foreach (var pair in _activeRecordings.ToList())
            {
                pair.Value.CancellationTokenSource.Cancel();
            }
        }

        public List<VirtualFolderInfo> GetRecordingFolders()
        {
            var list = new List<VirtualFolderInfo>();

            var defaultFolder = RecordingPath;
            var defaultName = "Recordings";

            if (Directory.Exists(defaultFolder))
            {
                list.Add(new VirtualFolderInfo
                {
                    Locations = new List<string> { defaultFolder },
                    Name = defaultName
                });
            }

            var customPath = GetConfiguration().MovieRecordingPath;
            if ((!string.IsNullOrWhiteSpace(customPath) && !string.Equals(customPath, defaultFolder, StringComparison.OrdinalIgnoreCase)) && Directory.Exists(customPath))
            {
                list.Add(new VirtualFolderInfo
                {
                    Locations = new List<string> { customPath },
                    Name = "Recorded Movies",
                    CollectionType = CollectionType.Movies
                });
            }

            customPath = GetConfiguration().SeriesRecordingPath;
            if ((!string.IsNullOrWhiteSpace(customPath) && !string.Equals(customPath, defaultFolder, StringComparison.OrdinalIgnoreCase)) && Directory.Exists(customPath))
            {
                list.Add(new VirtualFolderInfo
                {
                    Locations = new List<string> { customPath },
                    Name = "Recorded Series",
                    CollectionType = CollectionType.TvShows
                });
            }

            return list;
        }

        class ActiveRecordingInfo
        {
            public string Path { get; set; }
            public TimerInfo Timer { get; set; }
            public ProgramInfo Program { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
    }
}