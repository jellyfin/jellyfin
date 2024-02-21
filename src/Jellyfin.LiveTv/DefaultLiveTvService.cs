#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.Extensions;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Timers;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv
{
    public sealed class DefaultLiveTvService : ILiveTvService, ISupportsDirectStreamProvider, ISupportsNewTimerIds
    {
        public const string ServiceName = "Emby";

        private readonly ILogger<DefaultLiveTvService> _logger;
        private readonly IServerConfigurationManager _config;
        private readonly ITunerHostManager _tunerHostManager;
        private readonly IListingsManager _listingsManager;
        private readonly IRecordingsManager _recordingsManager;
        private readonly ILibraryManager _libraryManager;
        private readonly LiveTvDtoService _tvDtoService;
        private readonly TimerManager _timerManager;
        private readonly SeriesTimerManager _seriesTimerManager;

        public DefaultLiveTvService(
            ILogger<DefaultLiveTvService> logger,
            IServerConfigurationManager config,
            ITunerHostManager tunerHostManager,
            IListingsManager listingsManager,
            IRecordingsManager recordingsManager,
            ILibraryManager libraryManager,
            LiveTvDtoService tvDtoService,
            TimerManager timerManager,
            SeriesTimerManager seriesTimerManager)
        {
            _logger = logger;
            _config = config;
            _libraryManager = libraryManager;
            _tunerHostManager = tunerHostManager;
            _listingsManager = listingsManager;
            _recordingsManager = recordingsManager;
            _tvDtoService = tvDtoService;
            _timerManager = timerManager;
            _seriesTimerManager = seriesTimerManager;

            _timerManager.TimerFired += OnTimerManagerTimerFired;
        }

        public event EventHandler<GenericEventArgs<TimerInfo>> TimerCreated;

        public event EventHandler<GenericEventArgs<string>> TimerCancelled;

        /// <inheritdoc />
        public string Name => ServiceName;

        /// <inheritdoc />
        public string HomePageUrl => "https://github.com/jellyfin/jellyfin";

        public async Task RefreshSeriesTimers(CancellationToken cancellationToken)
        {
            var seriesTimers = await GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);

            foreach (var timer in seriesTimers)
            {
                UpdateTimersForSeriesTimer(timer, false, true);
            }
        }

        public async Task RefreshTimers(CancellationToken cancellationToken)
        {
            var timers = await GetTimersAsync(cancellationToken).ConfigureAwait(false);

            var tempChannelCache = new Dictionary<Guid, LiveTvChannel>();

            foreach (var timer in timers)
            {
                if (DateTime.UtcNow > timer.EndDate && _recordingsManager.GetActiveRecordingPath(timer.Id) is null)
                {
                    _timerManager.Delete(timer);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(timer.ProgramId) || string.IsNullOrWhiteSpace(timer.ChannelId))
                {
                    continue;
                }

                var program = GetProgramInfoFromCache(timer);
                if (program is null)
                {
                    _timerManager.Delete(timer);
                    continue;
                }

                CopyProgramInfoToTimerInfo(program, timer, tempChannelCache);
                _timerManager.Update(timer);
            }
        }

        private async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(bool enableCache, CancellationToken cancellationToken)
        {
            var channels = new List<ChannelInfo>();

            foreach (var hostInstance in _tunerHostManager.TunerHosts)
            {
                try
                {
                    var tunerChannels = await hostInstance.GetChannels(enableCache, cancellationToken).ConfigureAwait(false);

                    channels.AddRange(tunerChannels);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting channels");
                }
            }

            await _listingsManager.AddProviderMetadata(channels, enableCache, cancellationToken).ConfigureAwait(false);

            return channels;
        }

        public Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken)
        {
            return GetChannelsAsync(false, cancellationToken);
        }

        public Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            var timers = _timerManager
                .GetAll()
                .Where(i => string.Equals(i.SeriesTimerId, timerId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var timer in timers)
            {
                CancelTimerInternal(timer.Id, true, true);
            }

            var remove = _seriesTimerManager.GetAll().FirstOrDefault(r => string.Equals(r.Id, timerId, StringComparison.OrdinalIgnoreCase));
            if (remove is not null)
            {
                _seriesTimerManager.Delete(remove);
            }

            return Task.CompletedTask;
        }

        private void CancelTimerInternal(string timerId, bool isSeriesCancelled, bool isManualCancellation)
        {
            var timer = _timerManager.GetTimer(timerId);
            if (timer is not null)
            {
                var statusChanging = timer.Status != RecordingStatus.Cancelled;
                timer.Status = RecordingStatus.Cancelled;

                if (isManualCancellation)
                {
                    timer.IsManual = true;
                }

                if (string.IsNullOrWhiteSpace(timer.SeriesTimerId) || isSeriesCancelled)
                {
                    _timerManager.Delete(timer);
                }
                else
                {
                    _timerManager.AddOrUpdate(timer, false);
                }

                if (statusChanging && TimerCancelled is not null)
                {
                    TimerCancelled(this, new GenericEventArgs<string>(timerId));
                }
            }

            _recordingsManager.CancelRecording(timerId, timer);
        }

        public Task CancelTimerAsync(string timerId, CancellationToken cancellationToken)
        {
            CancelTimerInternal(timerId, false, true);
            return Task.CompletedTask;
        }

        public Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateTimer(TimerInfo info, CancellationToken cancellationToken)
        {
            var existingTimer = string.IsNullOrWhiteSpace(info.ProgramId) ?
                null :
                _timerManager.GetTimerByProgramId(info.ProgramId);

            if (existingTimer is not null)
            {
                if (existingTimer.Status == RecordingStatus.Cancelled
                    || existingTimer.Status == RecordingStatus.Completed)
                {
                    existingTimer.Status = RecordingStatus.New;
                    existingTimer.IsManual = true;
                    _timerManager.Update(existingTimer);
                    return Task.FromResult(existingTimer.Id);
                }

                throw new ArgumentException("A scheduled recording already exists for this program.");
            }

            info.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            LiveTvProgram programInfo = null;

            if (!string.IsNullOrWhiteSpace(info.ProgramId))
            {
                programInfo = GetProgramInfoFromCache(info);
            }

            if (programInfo is null)
            {
                _logger.LogInformation("Unable to find program with Id {0}. Will search using start date", info.ProgramId);
                programInfo = GetProgramInfoFromCache(info.ChannelId, info.StartDate);
            }

            if (programInfo is not null)
            {
                CopyProgramInfoToTimerInfo(programInfo, info);
            }

            info.IsManual = true;
            _timerManager.Add(info);

            TimerCreated?.Invoke(this, new GenericEventArgs<TimerInfo>(info));

            return Task.FromResult(info.Id);
        }

        public async Task<string> CreateSeriesTimer(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            info.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // populate info.seriesID
            var program = GetProgramInfoFromCache(info.ProgramId);

            if (program is not null)
            {
                info.SeriesId = program.ExternalSeriesId;
            }
            else
            {
                throw new InvalidOperationException("SeriesId for program not found");
            }

            // If any timers have already been manually created, make sure they don't get cancelled
            var existingTimers = (await GetTimersAsync(CancellationToken.None).ConfigureAwait(false))
                .Where(i =>
                {
                    if (string.Equals(i.ProgramId, info.ProgramId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(info.ProgramId))
                    {
                        return true;
                    }

                    if (string.Equals(i.SeriesId, info.SeriesId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(info.SeriesId))
                    {
                        return true;
                    }

                    return false;
                })
                .ToList();

            _seriesTimerManager.Add(info);

            foreach (var timer in existingTimers)
            {
                timer.SeriesTimerId = info.Id;
                timer.IsManual = true;

                _timerManager.AddOrUpdate(timer, false);
            }

            UpdateTimersForSeriesTimer(info, true, false);

            return info.Id;
        }

        public Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken)
        {
            var instance = _seriesTimerManager.GetAll().FirstOrDefault(i => string.Equals(i.Id, info.Id, StringComparison.OrdinalIgnoreCase));

            if (instance is not null)
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

                _seriesTimerManager.Update(instance);

                UpdateTimersForSeriesTimer(instance, true, true);
            }

            return Task.CompletedTask;
        }

        public Task UpdateTimerAsync(TimerInfo updatedTimer, CancellationToken cancellationToken)
        {
            var existingTimer = _timerManager.GetTimer(updatedTimer.Id);

            if (existingTimer is null)
            {
                throw new ResourceNotFoundException();
            }

            // Only update if not currently active
            if (_recordingsManager.GetActiveRecordingPath(updatedTimer.Id) is null)
            {
                existingTimer.PrePaddingSeconds = updatedTimer.PrePaddingSeconds;
                existingTimer.PostPaddingSeconds = updatedTimer.PostPaddingSeconds;
                existingTimer.IsPostPaddingRequired = updatedTimer.IsPostPaddingRequired;
                existingTimer.IsPrePaddingRequired = updatedTimer.IsPrePaddingRequired;

                _timerManager.Update(existingTimer);
            }

            return Task.CompletedTask;
        }

        private static void UpdateExistingTimerWithNewMetadata(TimerInfo existingTimer, TimerInfo updatedTimer)
        {
            // Update the program info but retain the status
            existingTimer.ChannelId = updatedTimer.ChannelId;
            existingTimer.CommunityRating = updatedTimer.CommunityRating;
            existingTimer.EndDate = updatedTimer.EndDate;
            existingTimer.EpisodeNumber = updatedTimer.EpisodeNumber;
            existingTimer.EpisodeTitle = updatedTimer.EpisodeTitle;
            existingTimer.Genres = updatedTimer.Genres;
            existingTimer.IsMovie = updatedTimer.IsMovie;
            existingTimer.IsSeries = updatedTimer.IsSeries;
            existingTimer.Tags = updatedTimer.Tags;
            existingTimer.IsProgramSeries = updatedTimer.IsProgramSeries;
            existingTimer.IsRepeat = updatedTimer.IsRepeat;
            existingTimer.Name = updatedTimer.Name;
            existingTimer.OfficialRating = updatedTimer.OfficialRating;
            existingTimer.OriginalAirDate = updatedTimer.OriginalAirDate;
            existingTimer.Overview = updatedTimer.Overview;
            existingTimer.ProductionYear = updatedTimer.ProductionYear;
            existingTimer.ProgramId = updatedTimer.ProgramId;
            existingTimer.SeasonNumber = updatedTimer.SeasonNumber;
            existingTimer.StartDate = updatedTimer.StartDate;
            existingTimer.ShowId = updatedTimer.ShowId;
            existingTimer.ProviderIds = updatedTimer.ProviderIds;
            existingTimer.SeriesProviderIds = updatedTimer.SeriesProviderIds;
        }

        public Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken)
        {
            var excludeStatues = new List<RecordingStatus>
            {
                RecordingStatus.Completed
            };

            var timers = _timerManager.GetAll()
                .Where(i => !excludeStatues.Contains(i.Status));

            return Task.FromResult(timers);
        }

        public Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null)
        {
            var config = _config.GetLiveTvConfiguration();

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

            if (program is not null)
            {
                defaults.SeriesId = program.SeriesId;
                defaults.ProgramId = program.Id;
                defaults.RecordNewOnly = !program.IsRepeat;
                defaults.Name = program.Name;
            }

            defaults.SkipEpisodesInLibrary = defaults.RecordNewOnly;
            defaults.KeepUntil = KeepUntil.UntilDeleted;

            return Task.FromResult(defaults);
        }

        public Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<SeriesTimerInfo>)_seriesTimerManager.GetAll());
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var channels = await GetChannelsAsync(true, cancellationToken).ConfigureAwait(false);
            var channel = channels.First(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

            return await _listingsManager.GetProgramsAsync(channel, startDateUtc, endDateUtc, cancellationToken)
                .ConfigureAwait(false);
        }

        public Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ILiveStream> GetChannelStreamWithDirectStreamProvider(string channelId, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Streaming Channel {Id}", channelId);

            var result = string.IsNullOrEmpty(streamId) ?
                null :
                currentLiveStreams.FirstOrDefault(i => string.Equals(i.OriginalStreamId, streamId, StringComparison.OrdinalIgnoreCase));

            if (result is not null && result.EnableStreamSharing)
            {
                result.ConsumerCount++;

                _logger.LogInformation("Live stream {0} consumer count is now {1}", streamId, result.ConsumerCount);

                return result;
            }

            foreach (var hostInstance in _tunerHostManager.TunerHosts)
            {
                try
                {
                    result = await hostInstance.GetChannelStream(channelId, streamId, currentLiveStreams, cancellationToken).ConfigureAwait(false);

                    var openedMediaSource = result.MediaSource;

                    result.OriginalStreamId = streamId;

                    _logger.LogInformation("Returning mediasource streamId {0}, mediaSource.Id {1}, mediaSource.LiveStreamId {2}", streamId, openedMediaSource.Id, openedMediaSource.LiveStreamId);

                    return result;
                }
                catch (FileNotFoundException)
                {
                }
                catch (OperationCanceledException)
                {
                }
            }

            throw new ResourceNotFoundException("Tuner not found.");
        }

        public async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            foreach (var hostInstance in _tunerHostManager.TunerHosts)
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

        public Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void OnTimerManagerTimerFired(object sender, GenericEventArgs<TimerInfo> e)
        {
            var timer = e.Argument;

            _logger.LogInformation("Recording timer fired for {0}.", timer.Name);

            try
            {
                var recordingEndDate = timer.EndDate.AddSeconds(timer.PostPaddingSeconds);
                if (recordingEndDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Recording timer fired for updatedTimer {0}, Id: {1}, but the program has already ended.", timer.Name, timer.Id);
                    _timerManager.Delete(timer);
                    return;
                }

                var activeRecordingInfo = new ActiveRecordingInfo
                {
                    CancellationTokenSource = new CancellationTokenSource(),
                    Timer = timer,
                    Id = timer.Id
                };

                if (_recordingsManager.GetActiveRecordingPath(timer.Id) is not null)
                {
                    _logger.LogInformation("Skipping RecordStream because it's already in progress.");
                    return;
                }

                LiveTvProgram programInfo = null;
                if (!string.IsNullOrWhiteSpace(timer.ProgramId))
                {
                    programInfo = GetProgramInfoFromCache(timer);
                }

                if (programInfo is null)
                {
                    _logger.LogInformation("Unable to find program with Id {0}. Will search using start date", timer.ProgramId);
                    programInfo = GetProgramInfoFromCache(timer.ChannelId, timer.StartDate);
                }

                if (programInfo is not null)
                {
                    CopyProgramInfoToTimerInfo(programInfo, timer);
                }

                await _recordingsManager.RecordStream(activeRecordingInfo, GetLiveTvChannel(timer), recordingEndDate)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording stream");
            }
        }

        private BaseItem GetLiveTvChannel(TimerInfo timer)
        {
            var internalChannelId = _tvDtoService.GetInternalChannelId(Name, timer.ChannelId);
            return _libraryManager.GetItemById(internalChannelId);
        }

        private LiveTvProgram GetProgramInfoFromCache(string programId)
        {
            var query = new InternalItemsQuery
            {
                ItemIds = [_tvDtoService.GetInternalProgramId(programId)],
                Limit = 1,
                DtoOptions = new DtoOptions()
            };

            return _libraryManager.GetItemList(query).Cast<LiveTvProgram>().FirstOrDefault();
        }

        private LiveTvProgram GetProgramInfoFromCache(TimerInfo timer)
        {
            return GetProgramInfoFromCache(timer.ProgramId);
        }

        private LiveTvProgram GetProgramInfoFromCache(string channelId, DateTime startDateUtc)
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.LiveTvProgram },
                Limit = 1,
                DtoOptions = new DtoOptions(true)
                {
                    EnableImages = false
                },
                MinStartDate = startDateUtc.AddMinutes(-3),
                MaxStartDate = startDateUtc.AddMinutes(3),
                OrderBy = new[] { (ItemSortBy.StartDate, SortOrder.Ascending) }
            };

            if (!string.IsNullOrWhiteSpace(channelId))
            {
                query.ChannelIds = [_tvDtoService.GetInternalChannelId(Name, channelId)];
            }

            return _libraryManager.GetItemList(query).Cast<LiveTvProgram>().FirstOrDefault();
        }

        private bool ShouldCancelTimerForSeriesTimer(SeriesTimerInfo seriesTimer, TimerInfo timer)
        {
            if (timer.IsManual)
            {
                return false;
            }

            if (!seriesTimer.RecordAnyTime
                && Math.Abs(seriesTimer.StartDate.TimeOfDay.Ticks - timer.StartDate.TimeOfDay.Ticks) >= TimeSpan.FromMinutes(10).Ticks)
            {
                return true;
            }

            if (seriesTimer.RecordNewOnly && timer.IsRepeat)
            {
                return true;
            }

            if (!seriesTimer.RecordAnyChannel
                && !string.Equals(timer.ChannelId, seriesTimer.ChannelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return seriesTimer.SkipEpisodesInLibrary && IsProgramAlreadyInLibrary(timer);
        }

        private void HandleDuplicateShowIds(List<TimerInfo> timers)
        {
            // sort showings by HD channels first, then by startDate, record earliest showing possible
            foreach (var timer in timers.OrderByDescending(t => GetLiveTvChannel(t).IsHD).ThenBy(t => t.StartDate).Skip(1))
            {
                timer.Status = RecordingStatus.Cancelled;
                _timerManager.Update(timer);
            }
        }

        private void SearchForDuplicateShowIds(IEnumerable<TimerInfo> timers)
        {
            var groups = timers.ToLookup(i => i.ShowId ?? string.Empty).ToList();

            foreach (var group in groups)
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                {
                    continue;
                }

                var groupTimers = group.ToList();

                if (groupTimers.Count < 2)
                {
                    continue;
                }

                // Skip ShowId without SubKey from duplicate removal actions - https://github.com/jellyfin/jellyfin/issues/5856
                if (group.Key.EndsWith("0000", StringComparison.Ordinal))
                {
                    continue;
                }

                HandleDuplicateShowIds(groupTimers);
            }
        }

        private void UpdateTimersForSeriesTimer(SeriesTimerInfo seriesTimer, bool updateTimerSettings, bool deleteInvalidTimers)
        {
            var allTimers = GetTimersForSeries(seriesTimer).ToList();

            var enabledTimersForSeries = new List<TimerInfo>();
            foreach (var timer in allTimers)
            {
                var existingTimer = _timerManager.GetTimer(timer.Id)
                    ?? (string.IsNullOrWhiteSpace(timer.ProgramId)
                        ? null
                        : _timerManager.GetTimerByProgramId(timer.ProgramId));

                if (existingTimer is null)
                {
                    if (ShouldCancelTimerForSeriesTimer(seriesTimer, timer))
                    {
                        timer.Status = RecordingStatus.Cancelled;
                    }
                    else
                    {
                        enabledTimersForSeries.Add(timer);
                    }

                    _timerManager.Add(timer);

                    TimerCreated?.Invoke(this, new GenericEventArgs<TimerInfo>(timer));
                }

                // Only update if not currently active - test both new timer and existing in case Id's are different
                // Id's could be different if the timer was created manually prior to series timer creation
                else if (_recordingsManager.GetActiveRecordingPath(timer.Id) is null
                         && _recordingsManager.GetActiveRecordingPath(existingTimer.Id) is null)
                {
                    UpdateExistingTimerWithNewMetadata(existingTimer, timer);

                    // Needed by ShouldCancelTimerForSeriesTimer
                    timer.IsManual = existingTimer.IsManual;

                    if (ShouldCancelTimerForSeriesTimer(seriesTimer, timer))
                    {
                        existingTimer.Status = RecordingStatus.Cancelled;
                    }
                    else if (!existingTimer.IsManual)
                    {
                        existingTimer.Status = RecordingStatus.New;
                    }

                    if (existingTimer.Status != RecordingStatus.Cancelled)
                    {
                        enabledTimersForSeries.Add(existingTimer);
                    }

                    if (updateTimerSettings)
                    {
                        existingTimer.KeepUntil = seriesTimer.KeepUntil;
                        existingTimer.IsPostPaddingRequired = seriesTimer.IsPostPaddingRequired;
                        existingTimer.IsPrePaddingRequired = seriesTimer.IsPrePaddingRequired;
                        existingTimer.PostPaddingSeconds = seriesTimer.PostPaddingSeconds;
                        existingTimer.PrePaddingSeconds = seriesTimer.PrePaddingSeconds;
                        existingTimer.Priority = seriesTimer.Priority;
                        existingTimer.SeriesTimerId = seriesTimer.Id;
                    }

                    existingTimer.SeriesTimerId = seriesTimer.Id;
                    _timerManager.Update(existingTimer);
                }
            }

            SearchForDuplicateShowIds(enabledTimersForSeries);

            if (deleteInvalidTimers)
            {
                var allTimerIds = allTimers
                    .Select(i => i.Id)
                    .ToList();

                var deleteStatuses = new[]
                {
                    RecordingStatus.New
                };

                var deletes = _timerManager.GetAll()
                    .Where(i => string.Equals(i.SeriesTimerId, seriesTimer.Id, StringComparison.OrdinalIgnoreCase))
                    .Where(i => !allTimerIds.Contains(i.Id, StringComparison.OrdinalIgnoreCase) && i.StartDate > DateTime.UtcNow)
                    .Where(i => deleteStatuses.Contains(i.Status))
                    .ToList();

                foreach (var timer in deletes)
                {
                    CancelTimerInternal(timer.Id, false, false);
                }
            }
        }

        private IEnumerable<TimerInfo> GetTimersForSeries(SeriesTimerInfo seriesTimer)
        {
            ArgumentNullException.ThrowIfNull(seriesTimer);

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.LiveTvProgram },
                ExternalSeriesId = seriesTimer.SeriesId,
                DtoOptions = new DtoOptions(true)
                {
                    EnableImages = false
                },
                MinEndDate = DateTime.UtcNow
            };

            if (string.IsNullOrEmpty(seriesTimer.SeriesId))
            {
                query.Name = seriesTimer.Name;
            }

            if (!seriesTimer.RecordAnyChannel)
            {
                query.ChannelIds = [_tvDtoService.GetInternalChannelId(Name, seriesTimer.ChannelId)];
            }

            var tempChannelCache = new Dictionary<Guid, LiveTvChannel>();

            return _libraryManager.GetItemList(query).Cast<LiveTvProgram>().Select(i => CreateTimer(i, seriesTimer, tempChannelCache));
        }

        private TimerInfo CreateTimer(LiveTvProgram parent, SeriesTimerInfo seriesTimer, Dictionary<Guid, LiveTvChannel> tempChannelCache)
        {
            string channelId = seriesTimer.RecordAnyChannel ? null : seriesTimer.ChannelId;

            if (string.IsNullOrWhiteSpace(channelId) && !parent.ChannelId.IsEmpty())
            {
                if (!tempChannelCache.TryGetValue(parent.ChannelId, out LiveTvChannel channel))
                {
                    channel = _libraryManager.GetItemList(
                        new InternalItemsQuery
                        {
                            IncludeItemTypes = new[] { BaseItemKind.LiveTvChannel },
                            ItemIds = new[] { parent.ChannelId },
                            DtoOptions = new DtoOptions()
                        }).FirstOrDefault() as LiveTvChannel;

                    if (channel is not null && !string.IsNullOrWhiteSpace(channel.ExternalId))
                    {
                        tempChannelCache[parent.ChannelId] = channel;
                    }
                }

                if (channel is not null || tempChannelCache.TryGetValue(parent.ChannelId, out channel))
                {
                    channelId = channel.ExternalId;
                }
            }

            var timer = new TimerInfo
            {
                ChannelId = channelId,
                Id = (seriesTimer.Id + parent.ExternalId).GetMD5().ToString("N", CultureInfo.InvariantCulture),
                StartDate = parent.StartDate,
                EndDate = parent.EndDate.Value,
                ProgramId = parent.ExternalId,
                PrePaddingSeconds = seriesTimer.PrePaddingSeconds,
                PostPaddingSeconds = seriesTimer.PostPaddingSeconds,
                IsPostPaddingRequired = seriesTimer.IsPostPaddingRequired,
                IsPrePaddingRequired = seriesTimer.IsPrePaddingRequired,
                KeepUntil = seriesTimer.KeepUntil,
                Priority = seriesTimer.Priority,
                Name = parent.Name,
                Overview = parent.Overview,
                SeriesId = parent.ExternalSeriesId,
                SeriesTimerId = seriesTimer.Id,
                ShowId = parent.ShowId
            };

            CopyProgramInfoToTimerInfo(parent, timer, tempChannelCache);

            return timer;
        }

        private void CopyProgramInfoToTimerInfo(LiveTvProgram programInfo, TimerInfo timerInfo)
        {
            var tempChannelCache = new Dictionary<Guid, LiveTvChannel>();
            CopyProgramInfoToTimerInfo(programInfo, timerInfo, tempChannelCache);
        }

        private void CopyProgramInfoToTimerInfo(LiveTvProgram programInfo, TimerInfo timerInfo, Dictionary<Guid, LiveTvChannel> tempChannelCache)
        {
            string channelId = null;

            if (!programInfo.ChannelId.IsEmpty())
            {
                if (!tempChannelCache.TryGetValue(programInfo.ChannelId, out LiveTvChannel channel))
                {
                    channel = _libraryManager.GetItemList(
                        new InternalItemsQuery
                        {
                            IncludeItemTypes = new[] { BaseItemKind.LiveTvChannel },
                            ItemIds = new[] { programInfo.ChannelId },
                            DtoOptions = new DtoOptions()
                        }).FirstOrDefault() as LiveTvChannel;

                    if (channel is not null && !string.IsNullOrWhiteSpace(channel.ExternalId))
                    {
                        tempChannelCache[programInfo.ChannelId] = channel;
                    }
                }

                if (channel is not null || tempChannelCache.TryGetValue(programInfo.ChannelId, out channel))
                {
                    channelId = channel.ExternalId;
                }
            }

            timerInfo.Name = programInfo.Name;
            timerInfo.StartDate = programInfo.StartDate;
            timerInfo.EndDate = programInfo.EndDate.Value;

            if (!string.IsNullOrWhiteSpace(channelId))
            {
                timerInfo.ChannelId = channelId;
            }

            timerInfo.SeasonNumber = programInfo.ParentIndexNumber;
            timerInfo.EpisodeNumber = programInfo.IndexNumber;
            timerInfo.IsMovie = programInfo.IsMovie;
            timerInfo.ProductionYear = programInfo.ProductionYear;
            timerInfo.EpisodeTitle = programInfo.EpisodeTitle;
            timerInfo.OriginalAirDate = programInfo.PremiereDate;
            timerInfo.IsProgramSeries = programInfo.IsSeries;

            timerInfo.IsSeries = programInfo.IsSeries;

            timerInfo.CommunityRating = programInfo.CommunityRating;
            timerInfo.Overview = programInfo.Overview;
            timerInfo.OfficialRating = programInfo.OfficialRating;
            timerInfo.IsRepeat = programInfo.IsRepeat;
            timerInfo.SeriesId = programInfo.ExternalSeriesId;
            timerInfo.ProviderIds = programInfo.ProviderIds;
            timerInfo.Tags = programInfo.Tags;

            var seriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var providerId in timerInfo.ProviderIds)
            {
                const string Search = "Series";
                if (providerId.Key.StartsWith(Search, StringComparison.OrdinalIgnoreCase))
                {
                    seriesProviderIds[providerId.Key.Substring(Search.Length)] = providerId.Value;
                }
            }

            timerInfo.SeriesProviderIds = seriesProviderIds;
        }

        private bool IsProgramAlreadyInLibrary(TimerInfo program)
        {
            if ((program.EpisodeNumber.HasValue && program.SeasonNumber.HasValue) || !string.IsNullOrWhiteSpace(program.EpisodeTitle))
            {
                var seriesIds = _libraryManager.GetItemIds(
                    new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Series },
                        Name = program.Name
                    }).ToArray();

                if (seriesIds.Length == 0)
                {
                    return false;
                }

                if (program.EpisodeNumber.HasValue && program.SeasonNumber.HasValue)
                {
                    var result = _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        ParentIndexNumber = program.SeasonNumber.Value,
                        IndexNumber = program.EpisodeNumber.Value,
                        AncestorIds = seriesIds,
                        IsVirtualItem = false,
                        Limit = 1
                    });

                    if (result.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
