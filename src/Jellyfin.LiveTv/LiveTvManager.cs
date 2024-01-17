#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Guide;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv
{
    /// <summary>
    /// Class LiveTvManager.
    /// </summary>
    public class LiveTvManager : ILiveTvManager
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger<LiveTvManager> _logger;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ITaskManager _taskManager;
        private readonly ILocalizationManager _localization;
        private readonly IChannelManager _channelManager;
        private readonly LiveTvDtoService _tvDtoService;
        private readonly IListingsProvider[] _listingProviders;

        private ILiveTvService[] _services = Array.Empty<ILiveTvService>();

        public LiveTvManager(
            IServerConfigurationManager config,
            ILogger<LiveTvManager> logger,
            IUserDataManager userDataManager,
            IDtoService dtoService,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ITaskManager taskManager,
            ILocalizationManager localization,
            IChannelManager channelManager,
            LiveTvDtoService liveTvDtoService,
            IEnumerable<IListingsProvider> listingProviders)
        {
            _config = config;
            _logger = logger;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _taskManager = taskManager;
            _localization = localization;
            _dtoService = dtoService;
            _userDataManager = userDataManager;
            _channelManager = channelManager;
            _tvDtoService = liveTvDtoService;
            _listingProviders = listingProviders.ToArray();
        }

        public event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCancelled;

        public event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCancelled;

        public event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCreated;

        public event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCreated;

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IReadOnlyList<ILiveTvService> Services => _services;

        public IReadOnlyList<IListingsProvider> ListingProviders => _listingProviders;

        public string GetEmbyTvActiveRecordingPath(string id)
        {
            return EmbyTV.EmbyTV.Current.GetActiveRecordingPath(id);
        }

        /// <inheritdoc />
        public void AddParts(IEnumerable<ILiveTvService> services)
        {
            _services = services.ToArray();

            foreach (var service in _services)
            {
                if (service is EmbyTV.EmbyTV embyTv)
                {
                    embyTv.TimerCreated += OnEmbyTvTimerCreated;
                    embyTv.TimerCancelled += OnEmbyTvTimerCancelled;
                }
            }
        }

        private void OnEmbyTvTimerCancelled(object sender, GenericEventArgs<string> e)
        {
            var timerId = e.Argument;

            TimerCancelled?.Invoke(this, new GenericEventArgs<TimerEventInfo>(new TimerEventInfo(timerId)));
        }

        private void OnEmbyTvTimerCreated(object sender, GenericEventArgs<TimerInfo> e)
        {
            var timer = e.Argument;

            TimerCreated?.Invoke(this, new GenericEventArgs<TimerEventInfo>(
                new TimerEventInfo(timer.Id)
                {
                    ProgramId = _tvDtoService.GetInternalProgramId(timer.ProgramId)
                }));
        }

        public QueryResult<BaseItem> GetInternalChannels(LiveTvChannelQuery query, DtoOptions dtoOptions, CancellationToken cancellationToken)
        {
            var user = query.UserId.Equals(default)
                ? null
                : _userManager.GetUserById(query.UserId);

            var topFolder = GetInternalLiveTvFolder(cancellationToken);

            var internalQuery = new InternalItemsQuery(user)
            {
                IsMovie = query.IsMovie,
                IsNews = query.IsNews,
                IsKids = query.IsKids,
                IsSports = query.IsSports,
                IsSeries = query.IsSeries,
                IncludeItemTypes = new[] { BaseItemKind.LiveTvChannel },
                TopParentIds = new[] { topFolder.Id },
                IsFavorite = query.IsFavorite,
                IsLiked = query.IsLiked,
                StartIndex = query.StartIndex,
                Limit = query.Limit,
                DtoOptions = dtoOptions
            };

            var orderBy = internalQuery.OrderBy.ToList();

            orderBy.AddRange(query.SortBy.Select(i => (i, query.SortOrder ?? SortOrder.Ascending)));

            if (query.EnableFavoriteSorting)
            {
                orderBy.Insert(0, (ItemSortBy.IsFavoriteOrLiked, SortOrder.Descending));
            }

            if (internalQuery.OrderBy.All(i => i.OrderBy != ItemSortBy.SortName))
            {
                orderBy.Add((ItemSortBy.SortName, SortOrder.Ascending));
            }

            internalQuery.OrderBy = orderBy.ToArray();

            return _libraryManager.GetItemsResult(internalQuery);
        }

        public async Task<Tuple<MediaSourceInfo, ILiveStream>> GetChannelStream(string id, string mediaSourceId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            if (string.Equals(id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
            {
                mediaSourceId = null;
            }

            var channel = (LiveTvChannel)_libraryManager.GetItemById(id);

            bool isVideo = channel.ChannelType == ChannelType.TV;
            var service = GetService(channel);
            _logger.LogInformation("Opening channel stream from {0}, external channel Id: {1}", service.Name, channel.ExternalId);

            MediaSourceInfo info;
#pragma warning disable CA1859 // TODO: Analyzer bug?
            ILiveStream liveStream;
#pragma warning restore CA1859
            if (service is ISupportsDirectStreamProvider supportsManagedStream)
            {
                liveStream = await supportsManagedStream.GetChannelStreamWithDirectStreamProvider(channel.ExternalId, mediaSourceId, currentLiveStreams, cancellationToken).ConfigureAwait(false);
                info = liveStream.MediaSource;
            }
            else
            {
                info = await service.GetChannelStream(channel.ExternalId, mediaSourceId, cancellationToken).ConfigureAwait(false);
                var openedId = info.Id;
                Func<Task> closeFn = () => service.CloseLiveStream(openedId, CancellationToken.None);

                liveStream = new ExclusiveLiveStream(info, closeFn);

                var startTime = DateTime.UtcNow;
                await liveStream.Open(cancellationToken).ConfigureAwait(false);
                var endTime = DateTime.UtcNow;
                _logger.LogInformation("Live stream opened after {0}ms", (endTime - startTime).TotalMilliseconds);
            }

            info.RequiresClosing = true;

            var idPrefix = service.GetType().FullName.GetMD5().ToString("N", CultureInfo.InvariantCulture) + "_";

            info.LiveStreamId = idPrefix + info.Id;

            Normalize(info, service, isVideo);

            return new Tuple<MediaSourceInfo, ILiveStream>(info, liveStream);
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetChannelMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            var baseItem = (LiveTvChannel)item;
            var service = GetService(baseItem);

            var sources = await service.GetChannelStreamMediaSources(baseItem.ExternalId, cancellationToken).ConfigureAwait(false);

            if (sources.Count == 0)
            {
                throw new NotImplementedException();
            }

            foreach (var source in sources)
            {
                Normalize(source, service, baseItem.ChannelType == ChannelType.TV);
            }

            return sources;
        }

        private ILiveTvService GetService(LiveTvChannel item)
        {
            var name = item.ServiceName;
            return GetService(name);
        }

        private ILiveTvService GetService(LiveTvProgram item)
        {
            var channel = _libraryManager.GetItemById(item.ChannelId) as LiveTvChannel;

            return GetService(channel);
        }

        private ILiveTvService GetService(string name)
            => Array.Find(_services, x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No service with the name '{0}' can be found.",
                        name));

        private static void Normalize(MediaSourceInfo mediaSource, ILiveTvService service, bool isVideo)
        {
            // Not all of the plugins are setting this
            mediaSource.IsInfiniteStream = true;

            if (mediaSource.MediaStreams.Count == 0)
            {
                if (isVideo)
                {
                    mediaSource.MediaStreams = new MediaStream[]
                    {
                        new MediaStream
                        {
                            Type = MediaStreamType.Video,
                            // Set the index to -1 because we don't know the exact index of the video stream within the container
                            Index = -1,

                            // Set to true if unknown to enable deinterlacing
                            IsInterlaced = true
                        },
                        new MediaStream
                        {
                            Type = MediaStreamType.Audio,
                            // Set the index to -1 because we don't know the exact index of the audio stream within the container
                            Index = -1
                        }
                    };
                }
                else
                {
                    mediaSource.MediaStreams = new MediaStream[]
                    {
                        new MediaStream
                        {
                            Type = MediaStreamType.Audio,
                            // Set the index to -1 because we don't know the exact index of the audio stream within the container
                            Index = -1
                        }
                    };
                }
            }

            // Clean some bad data coming from providers
            foreach (var stream in mediaSource.MediaStreams)
            {
                if (stream.BitRate.HasValue && stream.BitRate <= 0)
                {
                    stream.BitRate = null;
                }

                if (stream.Channels.HasValue && stream.Channels <= 0)
                {
                    stream.Channels = null;
                }

                if (stream.AverageFrameRate.HasValue && stream.AverageFrameRate <= 0)
                {
                    stream.AverageFrameRate = null;
                }

                if (stream.RealFrameRate.HasValue && stream.RealFrameRate <= 0)
                {
                    stream.RealFrameRate = null;
                }

                if (stream.Width.HasValue && stream.Width <= 0)
                {
                    stream.Width = null;
                }

                if (stream.Height.HasValue && stream.Height <= 0)
                {
                    stream.Height = null;
                }

                if (stream.SampleRate.HasValue && stream.SampleRate <= 0)
                {
                    stream.SampleRate = null;
                }

                if (stream.Level.HasValue && stream.Level <= 0)
                {
                    stream.Level = null;
                }
            }

            var indexes = mediaSource.MediaStreams.Select(i => i.Index).Distinct().ToList();

            // If there are duplicate stream indexes, set them all to unknown
            if (indexes.Count != mediaSource.MediaStreams.Count)
            {
                foreach (var stream in mediaSource.MediaStreams)
                {
                    stream.Index = -1;
                }
            }

            // Set the total bitrate if not already supplied
            mediaSource.InferTotalBitrate();

            if (service is not EmbyTV.EmbyTV)
            {
                // We can't trust that we'll be able to direct stream it through emby server, no matter what the provider says
                // mediaSource.SupportsDirectPlay = false;
                // mediaSource.SupportsDirectStream = false;
                mediaSource.SupportsTranscoding = true;
                foreach (var stream in mediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Video && string.IsNullOrWhiteSpace(stream.NalLengthSize))
                    {
                        stream.NalLengthSize = "0";
                    }

                    if (stream.Type == MediaStreamType.Video)
                    {
                        stream.IsInterlaced = true;
                    }
                }
            }
        }

        public async Task<BaseItemDto> GetProgram(string id, CancellationToken cancellationToken, User user = null)
        {
            var program = _libraryManager.GetItemById(id);

            var dto = _dtoService.GetBaseItemDto(program, new DtoOptions(), user);

            var list = new List<(BaseItemDto ItemDto, string ExternalId, string ExternalSeriesId)>
            {
                (dto, program.ExternalId, program.ExternalSeriesId)
            };

            await AddRecordingInfo(list, cancellationToken).ConfigureAwait(false);

            return dto;
        }

        public async Task<QueryResult<BaseItemDto>> GetPrograms(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            var user = query.User;

            var topFolder = GetInternalLiveTvFolder(cancellationToken);

            if (query.OrderBy.Count == 0)
            {
                // Unless something else was specified, order by start date to take advantage of a specialized index
                query.OrderBy = new[]
                {
                    (ItemSortBy.StartDate, SortOrder.Ascending)
                };
            }

            RemoveFields(options);

            var internalQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.LiveTvProgram },
                MinEndDate = query.MinEndDate,
                MinStartDate = query.MinStartDate,
                MaxEndDate = query.MaxEndDate,
                MaxStartDate = query.MaxStartDate,
                ChannelIds = query.ChannelIds,
                IsMovie = query.IsMovie,
                IsSeries = query.IsSeries,
                IsSports = query.IsSports,
                IsKids = query.IsKids,
                IsNews = query.IsNews,
                Genres = query.Genres,
                GenreIds = query.GenreIds,
                StartIndex = query.StartIndex,
                Limit = query.Limit,
                OrderBy = query.OrderBy,
                EnableTotalRecordCount = query.EnableTotalRecordCount,
                TopParentIds = new[] { topFolder.Id },
                Name = query.Name,
                DtoOptions = options,
                HasAired = query.HasAired,
                IsAiring = query.IsAiring
            };

            if (!string.IsNullOrWhiteSpace(query.SeriesTimerId))
            {
                var seriesTimers = await GetSeriesTimersInternal(new SeriesTimerQuery(), cancellationToken).ConfigureAwait(false);
                var seriesTimer = seriesTimers.Items.FirstOrDefault(i => string.Equals(_tvDtoService.GetInternalSeriesTimerId(i.Id).ToString("N", CultureInfo.InvariantCulture), query.SeriesTimerId, StringComparison.OrdinalIgnoreCase));
                if (seriesTimer is not null)
                {
                    internalQuery.ExternalSeriesId = seriesTimer.SeriesId;

                    if (string.IsNullOrWhiteSpace(seriesTimer.SeriesId))
                    {
                        // Better to return nothing than every program in the database
                        return new QueryResult<BaseItemDto>();
                    }
                }
                else
                {
                    // Better to return nothing than every program in the database
                    return new QueryResult<BaseItemDto>();
                }
            }

            var queryResult = _libraryManager.QueryItems(internalQuery);

            var returnArray = _dtoService.GetBaseItemDtos(queryResult.Items, options, user);

            return new QueryResult<BaseItemDto>(
                query.StartIndex,
                queryResult.TotalRecordCount,
                returnArray);
        }

        public QueryResult<BaseItem> GetRecommendedProgramsInternal(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            var user = query.User;

            var topFolder = GetInternalLiveTvFolder(cancellationToken);

            var internalQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.LiveTvProgram },
                IsAiring = query.IsAiring,
                HasAired = query.HasAired,
                IsNews = query.IsNews,
                IsMovie = query.IsMovie,
                IsSeries = query.IsSeries,
                IsSports = query.IsSports,
                IsKids = query.IsKids,
                EnableTotalRecordCount = query.EnableTotalRecordCount,
                OrderBy = new[] { (ItemSortBy.StartDate, SortOrder.Ascending) },
                TopParentIds = new[] { topFolder.Id },
                DtoOptions = options,
                GenreIds = query.GenreIds
            };

            if (query.Limit.HasValue)
            {
                internalQuery.Limit = Math.Max(query.Limit.Value * 4, 200);
            }

            var programList = _libraryManager.QueryItems(internalQuery).Items;
            var totalCount = programList.Count;

            var orderedPrograms = programList.Cast<LiveTvProgram>().OrderBy(i => i.StartDate.Date);

            if (query.IsAiring ?? false)
            {
                orderedPrograms = orderedPrograms
                    .ThenByDescending(i => GetRecommendationScore(i, user, true));
            }

            IEnumerable<BaseItem> programs = orderedPrograms;

            if (query.Limit.HasValue)
            {
                programs = programs.Take(query.Limit.Value);
            }

            return new QueryResult<BaseItem>(
                query.StartIndex,
                totalCount,
                programs.ToArray());
        }

        public Task<QueryResult<BaseItemDto>> GetRecommendedProgramsAsync(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            if (!(query.IsAiring ?? false))
            {
                return GetPrograms(query, options, cancellationToken);
            }

            RemoveFields(options);

            var internalResult = GetRecommendedProgramsInternal(query, options, cancellationToken);

            return Task.FromResult(new QueryResult<BaseItemDto>(
                query.StartIndex,
                internalResult.TotalRecordCount,
                _dtoService.GetBaseItemDtos(internalResult.Items, options, query.User)));
        }

        private int GetRecommendationScore(LiveTvProgram program, User user, bool factorChannelWatchCount)
        {
            var score = 0;

            if (program.IsLive)
            {
                score++;
            }

            if (program.IsSeries && !program.IsRepeat)
            {
                score++;
            }

            var channel = _libraryManager.GetItemById(program.ChannelId);

            if (channel is null)
            {
                return score;
            }

            var channelUserdata = _userDataManager.GetUserData(user, channel);

            if (channelUserdata.Likes.HasValue)
            {
                score += channelUserdata.Likes.Value ? 2 : -2;
            }

            if (channelUserdata.IsFavorite)
            {
                score += 3;
            }

            if (factorChannelWatchCount)
            {
                score += channelUserdata.PlayCount;
            }

            return score;
        }

        private async Task AddRecordingInfo(IEnumerable<(BaseItemDto ItemDto, string ExternalId, string ExternalSeriesId)> programs, CancellationToken cancellationToken)
        {
            IReadOnlyList<TimerInfo> timerList = null;
            IReadOnlyList<SeriesTimerInfo> seriesTimerList = null;

            foreach (var programTuple in programs)
            {
                var program = programTuple.ItemDto;
                var externalProgramId = programTuple.ExternalId;
                string externalSeriesId = programTuple.ExternalSeriesId;

                timerList ??= (await GetTimersInternal(new TimerQuery(), cancellationToken).ConfigureAwait(false)).Items;

                var timer = timerList.FirstOrDefault(i => string.Equals(i.ProgramId, externalProgramId, StringComparison.OrdinalIgnoreCase));
                var foundSeriesTimer = false;

                if (timer is not null)
                {
                    if (timer.Status != RecordingStatus.Cancelled && timer.Status != RecordingStatus.Error)
                    {
                        program.TimerId = _tvDtoService.GetInternalTimerId(timer.Id);

                        program.Status = timer.Status.ToString();
                    }

                    if (!string.IsNullOrEmpty(timer.SeriesTimerId))
                    {
                        program.SeriesTimerId = _tvDtoService.GetInternalSeriesTimerId(timer.SeriesTimerId)
                            .ToString("N", CultureInfo.InvariantCulture);

                        foundSeriesTimer = true;
                    }
                }

                if (foundSeriesTimer || string.IsNullOrWhiteSpace(externalSeriesId))
                {
                    continue;
                }

                seriesTimerList ??= (await GetSeriesTimersInternal(new SeriesTimerQuery(), cancellationToken).ConfigureAwait(false)).Items;

                var seriesTimer = seriesTimerList.FirstOrDefault(i => string.Equals(i.SeriesId, externalSeriesId, StringComparison.OrdinalIgnoreCase));

                if (seriesTimer is not null)
                {
                    program.SeriesTimerId = _tvDtoService.GetInternalSeriesTimerId(seriesTimer.Id)
                        .ToString("N", CultureInfo.InvariantCulture);
                }
            }
        }

        private async Task<QueryResult<BaseItem>> GetEmbyRecordingsAsync(RecordingQuery query, DtoOptions dtoOptions, User user)
        {
            if (user is null)
            {
                return new QueryResult<BaseItem>();
            }

            var folders = await GetRecordingFoldersAsync(user, true).ConfigureAwait(false);
            var folderIds = Array.ConvertAll(folders, x => x.Id);

            var excludeItemTypes = new List<BaseItemKind>();

            if (folderIds.Length == 0)
            {
                return new QueryResult<BaseItem>();
            }

            var includeItemTypes = new List<BaseItemKind>();
            var genres = new List<string>();

            if (query.IsMovie.HasValue)
            {
                if (query.IsMovie.Value)
                {
                    includeItemTypes.Add(BaseItemKind.Movie);
                }
                else
                {
                    excludeItemTypes.Add(BaseItemKind.Movie);
                }
            }

            if (query.IsSeries.HasValue)
            {
                if (query.IsSeries.Value)
                {
                    includeItemTypes.Add(BaseItemKind.Episode);
                }
                else
                {
                    excludeItemTypes.Add(BaseItemKind.Episode);
                }
            }

            if (query.IsSports ?? false)
            {
                genres.Add("Sports");
            }

            if (query.IsKids ?? false)
            {
                genres.Add("Kids");
                genres.Add("Children");
                genres.Add("Family");
            }

            var limit = query.Limit;

            if (query.IsInProgress ?? false)
            {
                // limit = (query.Limit ?? 10) * 2;
                limit = null;

                // var allActivePaths = EmbyTV.EmbyTV.Current.GetAllActiveRecordings().Select(i => i.Path).ToArray();
                // var items = allActivePaths.Select(i => _libraryManager.FindByPath(i, false)).Where(i => i is not null).ToArray();

                // return new QueryResult<BaseItem>
                // {
                //    Items = items,
                //    TotalRecordCount = items.Length
                // };

                dtoOptions.Fields = dtoOptions.Fields.Concat(new[] { ItemFields.Tags }).Distinct().ToArray();
            }

            var result = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                MediaTypes = new[] { MediaType.Video },
                Recursive = true,
                AncestorIds = folderIds,
                IsFolder = false,
                IsVirtualItem = false,
                Limit = limit,
                StartIndex = query.StartIndex,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                EnableTotalRecordCount = query.EnableTotalRecordCount,
                IncludeItemTypes = includeItemTypes.ToArray(),
                ExcludeItemTypes = excludeItemTypes.ToArray(),
                Genres = genres.ToArray(),
                DtoOptions = dtoOptions
            });

            if (query.IsInProgress ?? false)
            {
                // TODO: Fix The co-variant conversion between Video[] and BaseItem[], this can generate runtime issues.
                result.Items = result
                    .Items
                    .OfType<Video>()
                    .Where(i => !i.IsCompleteMedia)
                    .ToArray();

                result.TotalRecordCount = result.Items.Count;
            }

            return result;
        }

        public Task AddInfoToProgramDto(IReadOnlyCollection<(BaseItem Item, BaseItemDto ItemDto)> programs, IReadOnlyList<ItemFields> fields, User user = null)
        {
            var programTuples = new List<(BaseItemDto Dto, string ExternalId, string ExternalSeriesId)>();
            var hasChannelImage = fields.Contains(ItemFields.ChannelImage);
            var hasChannelInfo = fields.Contains(ItemFields.ChannelInfo);

            foreach (var (item, dto) in programs)
            {
                var program = (LiveTvProgram)item;

                dto.StartDate = program.StartDate;
                dto.EpisodeTitle = program.EpisodeTitle;
                dto.IsRepeat |= program.IsRepeat;
                dto.IsMovie |= program.IsMovie;
                dto.IsSeries |= program.IsSeries;
                dto.IsSports |= program.IsSports;
                dto.IsLive |= program.IsLive;
                dto.IsNews |= program.IsNews;
                dto.IsKids |= program.IsKids;
                dto.IsPremiere |= program.IsPremiere;

                if (hasChannelInfo || hasChannelImage)
                {
                    var channel = _libraryManager.GetItemById(program.ChannelId);

                    if (channel is LiveTvChannel liveChannel)
                    {
                        dto.ChannelName = liveChannel.Name;
                        dto.MediaType = liveChannel.MediaType;
                        dto.ChannelNumber = liveChannel.Number;

                        if (hasChannelImage && liveChannel.HasImage(ImageType.Primary))
                        {
                            dto.ChannelPrimaryImageTag = _tvDtoService.GetImageTag(liveChannel);
                        }
                    }
                }

                programTuples.Add((dto, program.ExternalId, program.ExternalSeriesId));
            }

            return AddRecordingInfo(programTuples, CancellationToken.None);
        }

        public ActiveRecordingInfo GetActiveRecordingInfo(string path)
        {
            return EmbyTV.EmbyTV.Current.GetActiveRecordingInfo(path);
        }

        public void AddInfoToRecordingDto(BaseItem item, BaseItemDto dto, ActiveRecordingInfo activeRecordingInfo, User user = null)
        {
            var service = EmbyTV.EmbyTV.Current;

            var info = activeRecordingInfo.Timer;

            var channel = string.IsNullOrWhiteSpace(info.ChannelId) ? null : _libraryManager.GetItemById(_tvDtoService.GetInternalChannelId(service.Name, info.ChannelId));

            dto.SeriesTimerId = string.IsNullOrEmpty(info.SeriesTimerId)
                ? null
                : _tvDtoService.GetInternalSeriesTimerId(info.SeriesTimerId).ToString("N", CultureInfo.InvariantCulture);

            dto.TimerId = string.IsNullOrEmpty(info.Id)
                ? null
                : _tvDtoService.GetInternalTimerId(info.Id);

            var startDate = info.StartDate;
            var endDate = info.EndDate;

            dto.StartDate = startDate;
            dto.EndDate = endDate;
            dto.Status = info.Status.ToString();
            dto.IsRepeat = info.IsRepeat;
            dto.EpisodeTitle = info.EpisodeTitle;
            dto.IsMovie = info.IsMovie;
            dto.IsSeries = info.IsSeries;
            dto.IsSports = info.IsSports;
            dto.IsLive = info.IsLive;
            dto.IsNews = info.IsNews;
            dto.IsKids = info.IsKids;
            dto.IsPremiere = info.IsPremiere;

            if (info.Status == RecordingStatus.InProgress)
            {
                startDate = info.StartDate.AddSeconds(0 - info.PrePaddingSeconds);
                endDate = info.EndDate.AddSeconds(info.PostPaddingSeconds);

                var now = DateTime.UtcNow.Ticks;
                var start = startDate.Ticks;
                var end = endDate.Ticks;

                var pct = now - start;

                pct /= end;
                pct *= 100;
                dto.CompletionPercentage = pct;
            }

            if (channel is not null)
            {
                dto.ChannelName = channel.Name;

                if (channel.HasImage(ImageType.Primary))
                {
                    dto.ChannelPrimaryImageTag = _tvDtoService.GetImageTag(channel);
                }
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetRecordingsAsync(RecordingQuery query, DtoOptions options)
        {
            var user = query.UserId.Equals(default)
                ? null
                : _userManager.GetUserById(query.UserId);

            RemoveFields(options);

            var internalResult = await GetEmbyRecordingsAsync(query, options, user).ConfigureAwait(false);

            var returnArray = _dtoService.GetBaseItemDtos(internalResult.Items, options, user);

            return new QueryResult<BaseItemDto>(
                query.StartIndex,
                internalResult.TotalRecordCount,
                returnArray);
        }

        private async Task<QueryResult<TimerInfo>> GetTimersInternal(TimerQuery query, CancellationToken cancellationToken)
        {
            var tasks = _services.Select(async i =>
            {
                try
                {
                    var recs = await i.GetTimersAsync(cancellationToken).ConfigureAwait(false);
                    return recs.Select(r => new Tuple<TimerInfo, ILiveTvService>(r, i));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting recordings");
                    return new List<Tuple<TimerInfo, ILiveTvService>>();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var timers = results.SelectMany(i => i.ToList());

            if (query.IsActive.HasValue)
            {
                if (query.IsActive.Value)
                {
                    timers = timers.Where(i => i.Item1.Status == RecordingStatus.InProgress);
                }
                else
                {
                    timers = timers.Where(i => i.Item1.Status != RecordingStatus.InProgress);
                }
            }

            if (query.IsScheduled.HasValue)
            {
                if (query.IsScheduled.Value)
                {
                    timers = timers.Where(i => i.Item1.Status == RecordingStatus.New);
                }
                else
                {
                    timers = timers.Where(i => i.Item1.Status != RecordingStatus.New);
                }
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);
                timers = timers.Where(i => _tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId).Equals(guid));
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                timers = timers
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(i.Item1.SeriesTimerId).Equals(guid));
            }

            if (!string.IsNullOrEmpty(query.Id))
            {
                timers = timers
                    .Where(i => string.Equals(_tvDtoService.GetInternalTimerId(i.Item1.Id), query.Id, StringComparison.OrdinalIgnoreCase));
            }

            var returnArray = timers
                .Select(i => i.Item1)
                .OrderBy(i => i.StartDate)
                .ToArray();

            return new QueryResult<TimerInfo>(returnArray);
        }

        public async Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken)
        {
            var tasks = _services.Select(async i =>
            {
                try
                {
                    var recs = await i.GetTimersAsync(cancellationToken).ConfigureAwait(false);
                    return recs.Select(r => new Tuple<TimerInfo, ILiveTvService>(r, i));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting recordings");
                    return new List<Tuple<TimerInfo, ILiveTvService>>();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var timers = results.SelectMany(i => i.ToList());

            if (query.IsActive.HasValue)
            {
                if (query.IsActive.Value)
                {
                    timers = timers.Where(i => i.Item1.Status == RecordingStatus.InProgress);
                }
                else
                {
                    timers = timers.Where(i => i.Item1.Status != RecordingStatus.InProgress);
                }
            }

            if (query.IsScheduled.HasValue)
            {
                if (query.IsScheduled.Value)
                {
                    timers = timers.Where(i => i.Item1.Status == RecordingStatus.New);
                }
                else
                {
                    timers = timers.Where(i => i.Item1.Status != RecordingStatus.New);
                }
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);
                timers = timers.Where(i => _tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId).Equals(guid));
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                timers = timers
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(i.Item1.SeriesTimerId).Equals(guid));
            }

            if (!string.IsNullOrEmpty(query.Id))
            {
                timers = timers
                    .Where(i => string.Equals(_tvDtoService.GetInternalTimerId(i.Item1.Id), query.Id, StringComparison.OrdinalIgnoreCase));
            }

            var returnList = new List<TimerInfoDto>();

            foreach (var i in timers)
            {
                var program = string.IsNullOrEmpty(i.Item1.ProgramId) ?
                    null :
                    _libraryManager.GetItemById(_tvDtoService.GetInternalProgramId(i.Item1.ProgramId)) as LiveTvProgram;

                var channel = string.IsNullOrEmpty(i.Item1.ChannelId) ? null : _libraryManager.GetItemById(_tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId));

                returnList.Add(_tvDtoService.GetTimerInfoDto(i.Item1, i.Item2, program, channel));
            }

            var returnArray = returnList
                .OrderBy(i => i.StartDate)
                .ToArray();

            return new QueryResult<TimerInfoDto>(returnArray);
        }

        public async Task CancelTimer(string id)
        {
            var timer = await GetTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer is null)
            {
                throw new ResourceNotFoundException(string.Format(CultureInfo.InvariantCulture, "Timer with Id {0} not found", id));
            }

            var service = GetService(timer.ServiceName);

            await service.CancelTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);

            if (service is not EmbyTV.EmbyTV)
            {
                TimerCancelled?.Invoke(this, new GenericEventArgs<TimerEventInfo>(new TimerEventInfo(id)));
            }
        }

        public async Task CancelSeriesTimer(string id)
        {
            var timer = await GetSeriesTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer is null)
            {
                throw new ResourceNotFoundException(string.Format(CultureInfo.InvariantCulture, "SeriesTimer with Id {0} not found", id));
            }

            var service = GetService(timer.ServiceName);

            await service.CancelSeriesTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);

            SeriesTimerCancelled?.Invoke(this, new GenericEventArgs<TimerEventInfo>(new TimerEventInfo(id)));
        }

        public async Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetTimers(
                new TimerQuery
                {
                    Id = id
                },
                cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetSeriesTimers(new SeriesTimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<QueryResult<SeriesTimerInfo>> GetSeriesTimersInternal(SeriesTimerQuery query, CancellationToken cancellationToken)
        {
            var tasks = _services.Select(async i =>
            {
                try
                {
                    var recs = await i.GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);
                    return recs.Select(r =>
                    {
                        r.ServiceName = i.Name;
                        return new Tuple<SeriesTimerInfo, ILiveTvService>(r, i);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting recordings");
                    return new List<Tuple<SeriesTimerInfo, ILiveTvService>>();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var timers = results.SelectMany(i => i.ToList());

            if (string.Equals(query.SortBy, "Priority", StringComparison.OrdinalIgnoreCase))
            {
                timers = query.SortOrder == SortOrder.Descending ?
                    timers.OrderBy(i => i.Item1.Priority).ThenByStringDescending(i => i.Item1.Name) :
                    timers.OrderByDescending(i => i.Item1.Priority).ThenByString(i => i.Item1.Name);
            }
            else
            {
                timers = query.SortOrder == SortOrder.Descending ?
                    timers.OrderByStringDescending(i => i.Item1.Name) :
                    timers.OrderByString(i => i.Item1.Name);
            }

            var returnArray = timers
                .Select(i => i.Item1)
                .ToArray();

            return new QueryResult<SeriesTimerInfo>(returnArray);
        }

        public async Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken)
        {
            var tasks = _services.Select(async i =>
            {
                try
                {
                    var recs = await i.GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);
                    return recs.Select(r => new Tuple<SeriesTimerInfo, ILiveTvService>(r, i));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting recordings");
                    return new List<Tuple<SeriesTimerInfo, ILiveTvService>>();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var timers = results.SelectMany(i => i.ToList());

            if (string.Equals(query.SortBy, "Priority", StringComparison.OrdinalIgnoreCase))
            {
                timers = query.SortOrder == SortOrder.Descending ?
                    timers.OrderBy(i => i.Item1.Priority).ThenByStringDescending(i => i.Item1.Name) :
                    timers.OrderByDescending(i => i.Item1.Priority).ThenByString(i => i.Item1.Name);
            }
            else
            {
                timers = query.SortOrder == SortOrder.Descending ?
                    timers.OrderByStringDescending(i => i.Item1.Name) :
                    timers.OrderByString(i => i.Item1.Name);
            }

            var returnArray = timers
                .Select(i =>
                {
                    string channelName = null;

                    if (!string.IsNullOrEmpty(i.Item1.ChannelId))
                    {
                        var internalChannelId = _tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId);
                        var channel = _libraryManager.GetItemById(internalChannelId);
                        channelName = channel is null ? null : channel.Name;
                    }

                    return _tvDtoService.GetSeriesTimerInfoDto(i.Item1, i.Item2, channelName);
                })
                .ToArray();

            return new QueryResult<SeriesTimerInfoDto>(returnArray);
        }

        public BaseItem GetLiveTvChannel(TimerInfo timer, ILiveTvService service)
        {
            var internalChannelId = _tvDtoService.GetInternalChannelId(service.Name, timer.ChannelId);
            return _libraryManager.GetItemById(internalChannelId);
        }

        public void AddChannelInfo(IReadOnlyCollection<(BaseItemDto ItemDto, LiveTvChannel Channel)> items, DtoOptions options, User user)
        {
            var now = DateTime.UtcNow;

            var channelIds = items.Select(i => i.Channel.Id).Distinct().ToArray();

            var programs = options.AddCurrentProgram ? _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.LiveTvProgram },
                ChannelIds = channelIds,
                MaxStartDate = now,
                MinEndDate = now,
                Limit = channelIds.Length,
                OrderBy = new[] { (ItemSortBy.StartDate, SortOrder.Ascending) },
                TopParentIds = new[] { GetInternalLiveTvFolder(CancellationToken.None).Id },
                DtoOptions = options
            }) : new List<BaseItem>();

            RemoveFields(options);

            var currentProgramsList = new List<BaseItem>();
            var currentChannelsDict = new Dictionary<Guid, BaseItemDto>();

            var addCurrentProgram = options.AddCurrentProgram;

            foreach (var (dto, channel) in items)
            {
                dto.Number = channel.Number;
                dto.ChannelNumber = channel.Number;
                dto.ChannelType = channel.ChannelType;

                currentChannelsDict[dto.Id] = dto;

                if (addCurrentProgram)
                {
                    var currentProgram = programs.FirstOrDefault(i => channel.Id.Equals(i.ChannelId));

                    if (currentProgram is not null)
                    {
                        currentProgramsList.Add(currentProgram);
                    }
                }
            }

            if (addCurrentProgram)
            {
                var currentProgramDtos = _dtoService.GetBaseItemDtos(currentProgramsList, options, user);

                foreach (var programDto in currentProgramDtos)
                {
                    if (programDto.ChannelId.HasValue && currentChannelsDict.TryGetValue(programDto.ChannelId.Value, out BaseItemDto channelDto))
                    {
                        channelDto.CurrentProgram = programDto;
                    }
                }
            }
        }

        private async Task<Tuple<SeriesTimerInfo, ILiveTvService>> GetNewTimerDefaultsInternal(CancellationToken cancellationToken, LiveTvProgram program = null)
        {
            ILiveTvService service = null;
            ProgramInfo programInfo = null;

            if (program is not null)
            {
                service = GetService(program);

                var channel = _libraryManager.GetItemById(program.ChannelId);

                programInfo = new ProgramInfo
                {
                    Audio = program.Audio,
                    ChannelId = channel.ExternalId,
                    CommunityRating = program.CommunityRating,
                    EndDate = program.EndDate ?? DateTime.MinValue,
                    EpisodeTitle = program.EpisodeTitle,
                    Genres = program.Genres.ToList(),
                    Id = program.ExternalId,
                    IsHD = program.IsHD,
                    IsKids = program.IsKids,
                    IsLive = program.IsLive,
                    IsMovie = program.IsMovie,
                    IsNews = program.IsNews,
                    IsPremiere = program.IsPremiere,
                    IsRepeat = program.IsRepeat,
                    IsSeries = program.IsSeries,
                    IsSports = program.IsSports,
                    OriginalAirDate = program.PremiereDate,
                    Overview = program.Overview,
                    StartDate = program.StartDate,
                    // ImagePath = program.ExternalImagePath,
                    Name = program.Name,
                    OfficialRating = program.OfficialRating
                };
            }

            service ??= _services[0];

            var info = await service.GetNewTimerDefaultsAsync(cancellationToken, programInfo).ConfigureAwait(false);

            info.RecordAnyTime = true;
            info.Days = new List<DayOfWeek>
            {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            info.Id = null;

            return new Tuple<SeriesTimerInfo, ILiveTvService>(info, service);
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken)
        {
            var info = await GetNewTimerDefaultsInternal(cancellationToken).ConfigureAwait(false);

            return _tvDtoService.GetSeriesTimerInfoDto(info.Item1, info.Item2, null);
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken)
        {
            var program = (LiveTvProgram)_libraryManager.GetItemById(programId);
            var programDto = await GetProgram(programId, cancellationToken).ConfigureAwait(false);

            var defaults = await GetNewTimerDefaultsInternal(cancellationToken, program).ConfigureAwait(false);
            var info = _tvDtoService.GetSeriesTimerInfoDto(defaults.Item1, defaults.Item2, null);

            info.Days = defaults.Item1.Days.ToArray();

            info.DayPattern = _tvDtoService.GetDayPattern(info.Days);

            info.Name = program.Name;
            info.ChannelId = programDto.ChannelId ?? Guid.Empty;
            info.ChannelName = programDto.ChannelName;
            info.StartDate = program.StartDate;
            info.Name = program.Name;
            info.Overview = program.Overview;
            info.ProgramId = programDto.Id.ToString("N", CultureInfo.InvariantCulture);
            info.ExternalProgramId = program.ExternalId;

            if (program.EndDate.HasValue)
            {
                info.EndDate = program.EndDate.Value;
            }

            return info;
        }

        public async Task CreateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = GetService(timer.ServiceName);

            var info = await _tvDtoService.GetTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            string newTimerId = null;
            if (service is ISupportsNewTimerIds supportsNewTimerIds)
            {
                newTimerId = await supportsNewTimerIds.CreateTimer(info, cancellationToken).ConfigureAwait(false);
                newTimerId = _tvDtoService.GetInternalTimerId(newTimerId);
            }
            else
            {
                await service.CreateTimerAsync(info, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("New recording scheduled");

            if (service is not EmbyTV.EmbyTV)
            {
                TimerCreated?.Invoke(this, new GenericEventArgs<TimerEventInfo>(
                    new TimerEventInfo(newTimerId)
                    {
                        ProgramId = _tvDtoService.GetInternalProgramId(info.ProgramId)
                    }));
            }
        }

        public async Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = GetService(timer.ServiceName);

            var info = await _tvDtoService.GetSeriesTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            string newTimerId = null;
            if (service is ISupportsNewTimerIds supportsNewTimerIds)
            {
                newTimerId = await supportsNewTimerIds.CreateSeriesTimer(info, cancellationToken).ConfigureAwait(false);
                newTimerId = _tvDtoService.GetInternalSeriesTimerId(newTimerId).ToString("N", CultureInfo.InvariantCulture);
            }
            else
            {
                await service.CreateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
            }

            SeriesTimerCreated?.Invoke(this, new GenericEventArgs<TimerEventInfo>(
                new TimerEventInfo(newTimerId)
                {
                    ProgramId = _tvDtoService.GetInternalProgramId(info.ProgramId)
                }));
        }

        public async Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = GetService(timer.ServiceName);

            await service.UpdateTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetSeriesTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = GetService(timer.ServiceName);

            await service.UpdateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        private LiveTvServiceInfo[] GetServiceInfos()
        {
            return Services.Select(GetServiceInfo).ToArray();
        }

        private static LiveTvServiceInfo GetServiceInfo(ILiveTvService service)
        {
            return new LiveTvServiceInfo
            {
                Name = service.Name
            };
        }

        public LiveTvInfo GetLiveTvInfo(CancellationToken cancellationToken)
        {
            var services = GetServiceInfos();

            var info = new LiveTvInfo
            {
                Services = services,
                IsEnabled = services.Length > 0,
                EnabledUsers = _userManager.Users
                    .Where(IsLiveTvEnabled)
                    .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                    .ToArray()
            };

            return info;
        }

        private bool IsLiveTvEnabled(User user)
        {
            return user.HasPermission(PermissionKind.EnableLiveTvAccess) && (Services.Count > 1 || _config.GetLiveTvConfiguration().TunerHosts.Length > 0);
        }

        public IEnumerable<User> GetEnabledUsers()
        {
            return _userManager.Users
                .Where(IsLiveTvEnabled);
        }

        /// <summary>
        /// Resets the tuner.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            var parts = id.Split('_', 2);

            var service = _services.FirstOrDefault(i => string.Equals(i.GetType().FullName.GetMD5().ToString("N", CultureInfo.InvariantCulture), parts[0], StringComparison.OrdinalIgnoreCase));

            if (service is null)
            {
                throw new ArgumentException("Service not found.");
            }

            return service.ResetTuner(parts[1], cancellationToken);
        }

        private static void RemoveFields(DtoOptions options)
        {
            var fields = options.Fields.ToList();

            fields.Remove(ItemFields.CanDelete);
            fields.Remove(ItemFields.CanDownload);
            fields.Remove(ItemFields.DisplayPreferencesId);
            fields.Remove(ItemFields.Etag);
            options.Fields = fields.ToArray();
        }

        public Folder GetInternalLiveTvFolder(CancellationToken cancellationToken)
        {
            var name = _localization.GetLocalizedString("HeaderLiveTV");
            return _libraryManager.GetNamedView(name, CollectionType.livetv, name);
        }

        public async Task<ListingsProviderInfo> SaveListingProvider(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            // Hack to make the object a pure ListingsProviderInfo instead of an AddListingProvider
            // ServerConfiguration.SaveConfiguration crashes during xml serialization for AddListingProvider
            info = JsonSerializer.Deserialize<ListingsProviderInfo>(JsonSerializer.SerializeToUtf8Bytes(info));

            var provider = _listingProviders.FirstOrDefault(i => string.Equals(info.Type, i.Type, StringComparison.OrdinalIgnoreCase));

            if (provider is null)
            {
                throw new ResourceNotFoundException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Couldn't find provider of type: '{0}'",
                        info.Type));
            }

            await provider.Validate(info, validateLogin, validateListings).ConfigureAwait(false);

            var config = _config.GetLiveTvConfiguration();

            var list = config.ListingProviders.ToList();
            int index = list.FindIndex(i => string.Equals(i.Id, info.Id, StringComparison.OrdinalIgnoreCase));

            if (index == -1 || string.IsNullOrWhiteSpace(info.Id))
            {
                info.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
                list.Add(info);
                config.ListingProviders = list.ToArray();
            }
            else
            {
                config.ListingProviders[index] = info;
            }

            _config.SaveConfiguration("livetv", config);

            _taskManager.CancelIfRunningAndQueue<RefreshGuideScheduledTask>();

            return info;
        }

        public void DeleteListingsProvider(string id)
        {
            var config = _config.GetLiveTvConfiguration();

            config.ListingProviders = config.ListingProviders.Where(i => !string.Equals(id, i.Id, StringComparison.OrdinalIgnoreCase)).ToArray();

            _config.SaveConfiguration("livetv", config);
            _taskManager.CancelIfRunningAndQueue<RefreshGuideScheduledTask>();
        }

        public async Task<TunerChannelMapping> SetChannelMapping(string providerId, string tunerChannelNumber, string providerChannelNumber)
        {
            var config = _config.GetLiveTvConfiguration();

            var listingsProviderInfo = config.ListingProviders.First(i => string.Equals(providerId, i.Id, StringComparison.OrdinalIgnoreCase));
            listingsProviderInfo.ChannelMappings = listingsProviderInfo.ChannelMappings.Where(i => !string.Equals(i.Name, tunerChannelNumber, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (!string.Equals(tunerChannelNumber, providerChannelNumber, StringComparison.OrdinalIgnoreCase))
            {
                var list = listingsProviderInfo.ChannelMappings.ToList();
                list.Add(new NameValuePair
                {
                    Name = tunerChannelNumber,
                    Value = providerChannelNumber
                });
                listingsProviderInfo.ChannelMappings = list.ToArray();
            }

            _config.SaveConfiguration("livetv", config);

            var tunerChannels = await GetChannelsForListingsProvider(providerId, CancellationToken.None)
                        .ConfigureAwait(false);

            var providerChannels = await GetChannelsFromListingsProviderData(providerId, CancellationToken.None)
                     .ConfigureAwait(false);

            var mappings = listingsProviderInfo.ChannelMappings;

            var tunerChannelMappings =
                tunerChannels.Select(i => GetTunerChannelMapping(i, mappings, providerChannels)).ToList();

            _taskManager.CancelIfRunningAndQueue<RefreshGuideScheduledTask>();

            return tunerChannelMappings.First(i => string.Equals(i.Id, tunerChannelNumber, StringComparison.OrdinalIgnoreCase));
        }

        public TunerChannelMapping GetTunerChannelMapping(ChannelInfo tunerChannel, NameValuePair[] mappings, List<ChannelInfo> providerChannels)
        {
            var result = new TunerChannelMapping
            {
                Name = tunerChannel.Name,
                Id = tunerChannel.Id
            };

            if (!string.IsNullOrWhiteSpace(tunerChannel.Number))
            {
                result.Name = tunerChannel.Number + " " + result.Name;
            }

            var providerChannel = EmbyTV.EmbyTV.Current.GetEpgChannelFromTunerChannel(mappings, tunerChannel, providerChannels);

            if (providerChannel is not null)
            {
                result.ProviderChannelName = providerChannel.Name;
                result.ProviderChannelId = providerChannel.Id;
            }

            return result;
        }

        public Task<List<NameIdPair>> GetLineups(string providerType, string providerId, string country, string location)
        {
            var config = _config.GetLiveTvConfiguration();

            if (string.IsNullOrWhiteSpace(providerId))
            {
                var provider = _listingProviders.FirstOrDefault(i => string.Equals(providerType, i.Type, StringComparison.OrdinalIgnoreCase));

                if (provider is null)
                {
                    throw new ResourceNotFoundException();
                }

                return provider.GetLineups(null, country, location);
            }
            else
            {
                var info = config.ListingProviders.FirstOrDefault(i => string.Equals(i.Id, providerId, StringComparison.OrdinalIgnoreCase));

                var provider = _listingProviders.FirstOrDefault(i => string.Equals(info.Type, i.Type, StringComparison.OrdinalIgnoreCase));

                if (provider is null)
                {
                    throw new ResourceNotFoundException();
                }

                return provider.GetLineups(info, country, location);
            }
        }

        public Task<List<ChannelInfo>> GetChannelsForListingsProvider(string id, CancellationToken cancellationToken)
        {
            var info = _config.GetLiveTvConfiguration().ListingProviders.First(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            return EmbyTV.EmbyTV.Current.GetChannelsForListingsProvider(info, cancellationToken);
        }

        public Task<List<ChannelInfo>> GetChannelsFromListingsProviderData(string id, CancellationToken cancellationToken)
        {
            var info = _config.GetLiveTvConfiguration().ListingProviders.First(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            var provider = _listingProviders.First(i => string.Equals(i.Type, info.Type, StringComparison.OrdinalIgnoreCase));
            return provider.GetChannels(info, cancellationToken);
        }

        public Guid GetInternalChannelId(string serviceName, string externalId)
        {
            return _tvDtoService.GetInternalChannelId(serviceName, externalId);
        }

        public Guid GetInternalProgramId(string externalId)
        {
            return _tvDtoService.GetInternalProgramId(externalId);
        }

        /// <inheritdoc />
        public Task<BaseItem[]> GetRecordingFoldersAsync(User user)
            => GetRecordingFoldersAsync(user, false);

        private async Task<BaseItem[]> GetRecordingFoldersAsync(User user, bool refreshChannels)
        {
            var folders = EmbyTV.EmbyTV.Current.GetRecordingFolders()
                .SelectMany(i => i.Locations)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => _libraryManager.FindByPath(i, true))
                .Where(i => i is not null && i.IsVisibleStandalone(user))
                .SelectMany(i => _libraryManager.GetCollectionFolders(i))
                .DistinctBy(x => x.Id)
                .OrderBy(i => i.SortName)
                .ToList();

            var channels = await _channelManager.GetChannelsInternalAsync(new MediaBrowser.Model.Channels.ChannelQuery
            {
                UserId = user.Id,
                IsRecordingsFolder = true,
                RefreshLatestChannelItems = refreshChannels
            }).ConfigureAwait(false);

            folders.AddRange(channels.Items);

            return folders.Cast<BaseItem>().ToArray();
        }
    }
}
