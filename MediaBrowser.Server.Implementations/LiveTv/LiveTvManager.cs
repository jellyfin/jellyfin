using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    /// <summary>
    /// Class LiveTvManager
    /// </summary>
    public class LiveTvManager : ILiveTvManager, IDisposable
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ITaskManager _taskManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IProviderManager _providerManager;

        private readonly IDtoService _dtoService;
        private readonly ILocalizationManager _localization;

        private readonly LiveTvDtoService _tvDtoService;

        private readonly List<ILiveTvService> _services = new List<ILiveTvService>();

        private readonly ConcurrentDictionary<string, LiveStreamData> _openStreams =
            new ConcurrentDictionary<string, LiveStreamData>();

        private List<Guid> _channelIdList = new List<Guid>();
        private Dictionary<Guid, LiveTvProgram> _programs;
        private readonly ConcurrentDictionary<Guid, bool> _refreshedPrograms = new ConcurrentDictionary<Guid, bool>();

        public LiveTvManager(IApplicationHost appHost, IServerConfigurationManager config, ILogger logger, IItemRepository itemRepo, IImageProcessor imageProcessor, IUserDataManager userDataManager, IDtoService dtoService, IUserManager userManager, ILibraryManager libraryManager, ITaskManager taskManager, ILocalizationManager localization, IJsonSerializer jsonSerializer, IProviderManager providerManager)
        {
            _config = config;
            _logger = logger;
            _itemRepo = itemRepo;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _taskManager = taskManager;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
            _providerManager = providerManager;
            _dtoService = dtoService;
            _userDataManager = userDataManager;

            _tvDtoService = new LiveTvDtoService(dtoService, userDataManager, imageProcessor, logger, appHost);
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IReadOnlyList<ILiveTvService> Services
        {
            get { return _services; }
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        public void AddParts(IEnumerable<ILiveTvService> services)
        {
            _services.AddRange(services);

            foreach (var service in _services)
            {
                service.DataSourceChanged += service_DataSourceChanged;
            }
        }

        void service_DataSourceChanged(object sender, EventArgs e)
        {
            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();
        }

        private readonly object _programsDataLock = new object();
        private Dictionary<Guid, LiveTvProgram> GetProgramsDictionary()
        {
            if (_programs == null)
            {
                lock (_programsDataLock)
                {
                    if (_programs == null)
                    {
                        var dict = new Dictionary<Guid, LiveTvProgram>();

                        foreach (var item in _itemRepo.GetItemsOfType(typeof (LiveTvProgram))
                            .Cast<LiveTvProgram>()
                            .ToList())
                        {
                            dict[item.Id] = item;    
                        }

                        _programs = dict;
                    }
                }
            }

            return _programs;
        }

        private IEnumerable<LiveTvProgram> GetPrograms()
        {
            return GetProgramsDictionary().Values;
        }

        public async Task<QueryResult<LiveTvChannel>> GetInternalChannels(LiveTvChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var channels = _channelIdList.Select(_libraryManager.GetItemById)
                .Where(i => i != null)
                .OfType<LiveTvChannel>();

            if (user != null)
            {
                // Avoid implicitly captured closure
                var currentUser = user;

                channels = channels
                    .Where(i => i.IsVisible(currentUser))
                    .OrderBy(i =>
                    {
                        double number = 0;

                        if (!string.IsNullOrEmpty(i.Number))
                        {
                            double.TryParse(i.Number, out number);
                        }

                        return number;

                    });

                if (query.IsFavorite.HasValue)
                {
                    var val = query.IsFavorite.Value;

                    channels = channels
                        .Where(i => _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).IsFavorite == val);
                }

                if (query.IsLiked.HasValue)
                {
                    var val = query.IsLiked.Value;

                    channels = channels
                        .Where(i =>
                        {
                            var likes = _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).Likes;

                            return likes.HasValue && likes.Value == val;
                        });
                }

                if (query.IsDisliked.HasValue)
                {
                    var val = query.IsDisliked.Value;

                    channels = channels
                        .Where(i =>
                        {
                            var likes = _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).Likes;

                            return likes.HasValue && likes.Value != val;
                        });
                }
            }

            var enableFavoriteSorting = query.EnableFavoriteSorting;

            channels = channels.OrderBy(i =>
            {
                if (enableFavoriteSorting)
                {
                    var userData = _userDataManager.GetUserData(user.Id, i.GetUserDataKey());

                    if (userData.IsFavorite)
                    {
                        return 0;
                    }
                    if (userData.Likes.HasValue)
                    {
                        if (!userData.Likes.Value)
                        {
                            return 3;
                        }

                        return 1;
                    }
                }

                return 2;
            });

            var allChannels = channels.ToList();
            IEnumerable<LiveTvChannel> allEnumerable = allChannels;

            if (query.StartIndex.HasValue)
            {
                allEnumerable = allEnumerable.Skip(query.StartIndex.Value);
            }

            if (query.Limit.HasValue)
            {
                allEnumerable = allEnumerable.Take(query.Limit.Value);
            }

            var result = new QueryResult<LiveTvChannel>
            {
                Items = allEnumerable.ToArray(),
                TotalRecordCount = allChannels.Count
            };

            return result;
        }

        public async Task<QueryResult<ChannelInfoDto>> GetChannels(LiveTvChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var internalResult = await GetInternalChannels(query, cancellationToken).ConfigureAwait(false);

            var returnList = new List<ChannelInfoDto>();

            foreach (var channel in internalResult.Items)
            {
                var currentProgram = GetCurrentProgram(channel.ExternalId);

                returnList.Add(_tvDtoService.GetChannelInfoDto(channel, currentProgram, user));
            }

            var result = new QueryResult<ChannelInfoDto>
            {
                Items = returnList.ToArray(),
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        public LiveTvChannel GetInternalChannel(string id)
        {
            return GetInternalChannel(new Guid(id));
        }

        private LiveTvChannel GetInternalChannel(Guid id)
        {
            return _libraryManager.GetItemById(id) as LiveTvChannel;
        }

        private LiveTvProgram GetInternalProgram(string id)
        {
            var guid = new Guid(id);

            LiveTvProgram obj = null;

            GetProgramsDictionary().TryGetValue(guid, out obj);

            if (obj != null)
            {
                RefreshIfNeeded(obj);
            }
            return obj;
        }

        private void RefreshIfNeeded(LiveTvProgram program)
        {
            if (!_refreshedPrograms.ContainsKey(program.Id))
            {
                _refreshedPrograms.TryAdd(program.Id, true);
                _providerManager.QueueRefresh(program.Id, new MetadataRefreshOptions());
            }
        }

        private void RefreshIfNeeded(IEnumerable<LiveTvProgram> programs)
        {
            foreach (var program in programs)
            {
                RefreshIfNeeded(program);
            }
        }

        public async Task<ILiveTvRecording> GetInternalRecording(string id, CancellationToken cancellationToken)
        {
            var result = await GetInternalRecordings(new RecordingQuery
            {
                Id = id

            }, cancellationToken).ConfigureAwait(false);

            return result.Items.FirstOrDefault() as ILiveTvRecording;
        }

        private readonly SemaphoreSlim _liveStreamSemaphore = new SemaphoreSlim(1, 1);

        public async Task<MediaSourceInfo> GetRecordingStream(string id, CancellationToken cancellationToken)
        {
            return await GetLiveStream(id, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MediaSourceInfo> GetChannelStream(string id, CancellationToken cancellationToken)
        {
            return await GetLiveStream(id, true, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetRecordingMediaSources(string id, CancellationToken cancellationToken)
        {
            var item = await GetInternalRecording(id, cancellationToken).ConfigureAwait(false);
            var service = GetService(item);

            return await service.GetRecordingStreamMediaSources(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetChannelMediaSources(string id, CancellationToken cancellationToken)
        {
            var item = GetInternalChannel(id);
            var service = GetService(item);

            return await service.GetChannelStreamMediaSources(item.ExternalId, cancellationToken).ConfigureAwait(false);
        }

        private ILiveTvService GetService(ILiveTvItem item)
        {
            return GetService(item.ServiceName);
        }

        private ILiveTvService GetService(string name)
        {
            return _services.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<MediaSourceInfo> GetLiveStream(string id, bool isChannel, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                MediaSourceInfo info;
                bool isVideo;

                if (isChannel)
                {
                    var channel = GetInternalChannel(id);
                    isVideo = channel.ChannelType == ChannelType.TV;
                    var service = GetService(channel);
                    _logger.Info("Opening channel stream from {0}, external channel Id: {1}", service.Name, channel.ExternalId);
                    info = await service.GetChannelStream(channel.ExternalId, null, cancellationToken).ConfigureAwait(false);
                    info.RequiresClosing = true;

                    if (info.RequiresClosing)
                    {
                        var idPrefix = service.GetType().FullName.GetMD5().ToString("N") + "_";

                        info.LiveStreamId = idPrefix + info.Id;
                    }
                }
                else
                {
                    var recording = await GetInternalRecording(id, cancellationToken).ConfigureAwait(false);
                    isVideo = !string.Equals(recording.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase);
                    var service = GetService(recording);

                    _logger.Info("Opening recording stream from {0}, external recording Id: {1}", service.Name, recording.RecordingInfo.Id);
                    info = await service.GetRecordingStream(recording.RecordingInfo.Id, null, cancellationToken).ConfigureAwait(false);
                    info.RequiresClosing = true;

                    if (info.RequiresClosing)
                    {
                        var idPrefix = service.GetType().FullName.GetMD5().ToString("N") + "_";

                        info.LiveStreamId = idPrefix + info.Id;
                    }
                }

                _logger.Info("Live stream info: {0}", _jsonSerializer.SerializeToString(info));
                Normalize(info, isVideo);

                var data = new LiveStreamData
                {
                    Info = info,
                    IsChannel = isChannel,
                    ItemId = id
                };

                _openStreams.AddOrUpdate(info.Id, data, (key, i) => data);

                return info;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting channel stream", ex);

                throw;
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        private void Normalize(MediaSourceInfo mediaSource, bool isVideo)
        {
            if (mediaSource.MediaStreams.Count == 0)
            {
                if (isVideo)
                {
                    mediaSource.MediaStreams.AddRange(new List<MediaStream>
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
                    });
                }
                else
                {
                    mediaSource.MediaStreams.AddRange(new List<MediaStream>
                    {
                        new MediaStream
                        {
                            Type = MediaStreamType.Audio,
                            // Set the index to -1 because we don't know the exact index of the audio stream within the container
                            Index = -1
                        }
                    });
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
        }

        private async Task<LiveTvChannel> GetChannel(ChannelInfo channelInfo, string serviceName, CancellationToken cancellationToken)
        {
            var isNew = false;

            var id = _tvDtoService.GetInternalChannelId(serviceName, channelInfo.Id);

            var item = _itemRepo.RetrieveItem(id) as LiveTvChannel;

            if (item == null)
            {
                item = new LiveTvChannel
                {
                    Name = channelInfo.Name,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                };

                isNew = true;
            }

            item.ChannelType = channelInfo.ChannelType;
            item.ExternalId = channelInfo.Id;
            item.ServiceName = serviceName;
            item.Number = channelInfo.Number;

            var replaceImages = new List<ImageType>();

            if (!string.Equals(item.ProviderImageUrl, channelInfo.ImageUrl, StringComparison.OrdinalIgnoreCase))
            {
                isNew = true;
                replaceImages.Add(ImageType.Primary);
            }
            if (!string.Equals(item.ProviderImagePath, channelInfo.ImagePath, StringComparison.OrdinalIgnoreCase))
            {
                isNew = true;
                replaceImages.Add(ImageType.Primary);
            }

            item.ProviderImageUrl = channelInfo.ImageUrl;
            item.HasProviderImage = channelInfo.HasImage;
            item.ProviderImagePath = channelInfo.ImagePath;

            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = channelInfo.Name;
            }

            await item.RefreshMetadata(new MetadataRefreshOptions
            {
                ForceSave = isNew,
                ReplaceImages = replaceImages.Distinct().ToList()

            }, cancellationToken);

            return item;
        }

        private async Task<LiveTvProgram> GetProgram(ProgramInfo info, ChannelType channelType, string serviceName, CancellationToken cancellationToken)
        {
            var id = _tvDtoService.GetInternalProgramId(serviceName, info.Id);

            var item = _libraryManager.GetItemById(id) as LiveTvProgram;

            if (item == null)
            {
                item = new LiveTvProgram
                {
                    Name = info.Name,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow
                };
            }

            item.ChannelType = channelType;
            item.ServiceName = serviceName;

            item.Audio = info.Audio;
            item.ExternalChannelId = info.ChannelId;
            item.CommunityRating = info.CommunityRating;
            item.EndDate = info.EndDate;
            item.EpisodeTitle = info.EpisodeTitle;
            item.ExternalId = info.Id;
            item.Genres = info.Genres;
            item.HasProviderImage = info.HasImage;
            item.IsHD = info.IsHD;
            item.IsKids = info.IsKids;
            item.IsLive = info.IsLive;
            item.IsMovie = info.IsMovie;
            item.IsNews = info.IsNews;
            item.IsPremiere = info.IsPremiere;
            item.IsRepeat = info.IsRepeat;
            item.IsSeries = info.IsSeries;
            item.IsSports = info.IsSports;
            item.Name = info.Name;
            item.OfficialRating = info.OfficialRating;
            item.Overview = info.Overview;
            item.OriginalAirDate = info.OriginalAirDate;
            item.ProviderImagePath = info.ImagePath;
            item.ProviderImageUrl = info.ImageUrl;
            item.RunTimeTicks = (info.EndDate - info.StartDate).Ticks;
            item.StartDate = info.StartDate;

            item.ProductionYear = info.ProductionYear;
            item.PremiereDate = item.PremiereDate ?? info.OriginalAirDate;

            await item.UpdateToRepository(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);

            return item;
        }

        private async Task<ILiveTvRecording> GetRecording(RecordingInfo info, string serviceName, CancellationToken cancellationToken)
        {
            var isNew = false;

            var id = _tvDtoService.GetInternalRecordingId(serviceName, info.Id);

            var item = _itemRepo.RetrieveItem(id) as ILiveTvRecording;

            if (item == null)
            {
                if (info.ChannelType == ChannelType.TV)
                {
                    item = new LiveTvVideoRecording
                    {
                        Name = info.Name,
                        Id = id,
                        DateCreated = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        VideoType = VideoType.VideoFile
                    };
                }
                else
                {
                    item = new LiveTvAudioRecording
                    {
                        Name = info.Name,
                        Id = id,
                        DateCreated = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow
                    };
                }

                isNew = true;
            }

            item.RecordingInfo = info;
            item.ServiceName = serviceName;

            var originalPath = item.Path;

            if (!string.IsNullOrEmpty(info.Path))
            {
                item.Path = info.Path;
            }
            else if (!string.IsNullOrEmpty(info.Url))
            {
                item.Path = info.Url;
            }

            var pathChanged = !string.Equals(originalPath, item.Path);

            await item.RefreshMetadata(new MetadataRefreshOptions
            {
                ForceSave = isNew || pathChanged

            }, cancellationToken);

            _libraryManager.RegisterItem((BaseItem)item);

            return item;
        }

        private LiveTvChannel GetChannel(LiveTvProgram program)
        {
            var programChannelId = program.ExternalChannelId;

            if (string.IsNullOrWhiteSpace(programChannelId)) return null;

            var internalProgramChannelId = _tvDtoService.GetInternalChannelId(program.ServiceName, programChannelId);

            return GetInternalChannel(internalProgramChannelId);
        }

        public async Task<ProgramInfoDto> GetProgram(string id, CancellationToken cancellationToken, User user = null)
        {
            var program = GetInternalProgram(id);

            var channel = GetChannel(program);

            var dto = _tvDtoService.GetProgramInfoDto(program, channel, user);

            await AddRecordingInfo(new[] { dto }, cancellationToken).ConfigureAwait(false);

            return dto;
        }

        public async Task<QueryResult<ProgramInfoDto>> GetPrograms(ProgramQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<LiveTvProgram> programs = GetPrograms();

            if (query.MinEndDate.HasValue)
            {
                var val = query.MinEndDate.Value;

                programs = programs.Where(i => i.EndDate.HasValue && i.EndDate.Value >= val);
            }

            if (query.MinStartDate.HasValue)
            {
                var val = query.MinStartDate.Value;

                programs = programs.Where(i => i.StartDate >= val);
            }

            if (query.MaxEndDate.HasValue)
            {
                var val = query.MaxEndDate.Value;

                programs = programs.Where(i => i.EndDate.HasValue && i.EndDate.Value <= val);
            }

            if (query.MaxStartDate.HasValue)
            {
                var val = query.MaxStartDate.Value;

                programs = programs.Where(i => i.StartDate <= val);
            }

            if (query.HasAired.HasValue)
            {
                var val = query.HasAired.Value;
                programs = programs.Where(i => i.HasAired == val);
            }

            if (query.ChannelIds.Length > 0)
            {
                var guids = query.ChannelIds.Select(i => new Guid(i)).ToList();

                programs = programs.Where(i =>
                {
                    var programChannelId = i.ExternalChannelId;

                    var service = GetService(i);
                    var internalProgramChannelId = _tvDtoService.GetInternalChannelId(service.Name, programChannelId);

                    return guids.Contains(internalProgramChannelId);
                });
            }

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);
            if (user != null)
            {
                // Avoid implicitly captured closure
                var currentUser = user;
                programs = programs.Where(i => i.IsVisible(currentUser));
            }

            // Apply genre filter
            if (query.Genres.Length > 0)
            {
                programs = programs.Where(p => p.Genres.Any(g => query.Genres.Contains(g, StringComparer.OrdinalIgnoreCase)));
            }

            if (query.IsMovie.HasValue)
            {
                programs = programs.Where(p => p.IsMovie == query.IsMovie);
            }

            if (query.IsSports.HasValue)
            {
                programs = programs.Where(p => p.IsSports == query.IsSports);
            }

            programs = _libraryManager.Sort(programs, user, query.SortBy, query.SortOrder ?? SortOrder.Ascending)
                .Cast<LiveTvProgram>();

            var programList = programs.ToList();
            IEnumerable<LiveTvProgram> returnPrograms = programList;

            if (query.StartIndex.HasValue)
            {
                returnPrograms = returnPrograms.Skip(query.StartIndex.Value);
            }

            if (query.Limit.HasValue)
            {
                returnPrograms = returnPrograms.Take(query.Limit.Value);
            }

            var returnArray = returnPrograms
                .Select(i =>
                {
                    var channel = GetChannel(i);

                    return _tvDtoService.GetProgramInfoDto(i, channel, user);
                })
                .ToArray();

            RefreshIfNeeded(programList);

            await AddRecordingInfo(returnArray, cancellationToken).ConfigureAwait(false);

            var result = new QueryResult<ProgramInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = programList.Count
            };

            return result;
        }

        public async Task<QueryResult<LiveTvProgram>> GetRecommendedProgramsInternal(RecommendedProgramQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<LiveTvProgram> programs = GetPrograms();

            var user = _userManager.GetUserById(query.UserId);

            // Avoid implicitly captured closure
            var currentUser = user;
            programs = programs.Where(i => i.IsVisible(currentUser));

            if (query.IsAiring.HasValue)
            {
                var val = query.IsAiring.Value;
                programs = programs.Where(i => i.IsAiring == val);
            }

            if (query.HasAired.HasValue)
            {
                var val = query.HasAired.Value;
                programs = programs.Where(i => i.HasAired == val);
            }

            if (query.IsMovie.HasValue)
            {
                programs = programs.Where(p => p.IsMovie == query.IsMovie.Value);
            }

            if (query.IsSports.HasValue)
            {
                programs = programs.Where(p => p.IsSports == query.IsSports.Value);
            }

            var programList = programs.ToList();

            var genres = programList.SelectMany(i => i.Genres)
                .DistinctNames()
                .Select(i => _libraryManager.GetGenre(i))
                .ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

            programs = programList.OrderBy(i => i.HasImage(ImageType.Primary) ? 0 : 1)
                .ThenByDescending(i => GetRecommendationScore(i, user.Id, genres))
                .ThenBy(i => i.StartDate);

            if (query.Limit.HasValue)
            {
                programs = programs.Take(query.Limit.Value)
                    .OrderBy(i => i.StartDate);
            }

            programList = programs.ToList();

            RefreshIfNeeded(programList);

            var returnArray = programList.ToArray();

            var result = new QueryResult<LiveTvProgram>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };

            return result;
        }

        public async Task<QueryResult<ProgramInfoDto>> GetRecommendedPrograms(RecommendedProgramQuery query, CancellationToken cancellationToken)
        {
            var internalResult = await GetRecommendedProgramsInternal(query, cancellationToken).ConfigureAwait(false);

            var user = _userManager.GetUserById(query.UserId);

            var returnArray = internalResult.Items
                .Select(i =>
                {
                    var channel = GetChannel(i);

                    return _tvDtoService.GetProgramInfoDto(i, channel, user);
                })
                .ToArray();

            await AddRecordingInfo(returnArray, cancellationToken).ConfigureAwait(false);

            var result = new QueryResult<ProgramInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        private int GetRecommendationScore(LiveTvProgram program, Guid userId, Dictionary<string, Genre> genres)
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

            var internalChannelId = _tvDtoService.GetInternalChannelId(program.ServiceName, program.ExternalChannelId);
            var channel = GetInternalChannel(internalChannelId);

            var channelUserdata = _userDataManager.GetUserData(userId, channel.GetUserDataKey());

            if ((channelUserdata.Likes ?? false))
            {
                score += 2;
            }
            else if (!(channelUserdata.Likes ?? true))
            {
                score -= 2;
            }

            if (channelUserdata.IsFavorite)
            {
                score += 3;
            }

            score += GetGenreScore(program.Genres, userId, genres);

            return score;
        }

        private int GetGenreScore(IEnumerable<string> programGenres, Guid userId, Dictionary<string, Genre> genres)
        {
            return programGenres.Select(i =>
            {
                var score = 0;

                Genre genre;

                if (genres.TryGetValue(i, out genre))
                {
                    var genreUserdata = _userDataManager.GetUserData(userId, genre.GetUserDataKey());

                    if ((genreUserdata.Likes ?? false))
                    {
                        score++;
                    }
                    else if (!(genreUserdata.Likes ?? true))
                    {
                        score--;
                    }

                    if (genreUserdata.IsFavorite)
                    {
                        score += 2;
                    }
                }

                return score;

            }).Sum();
        }

        private async Task AddRecordingInfo(IEnumerable<ProgramInfoDto> programs, CancellationToken cancellationToken)
        {
            var timers = new Dictionary<string, List<TimerInfo>>();

            foreach (var program in programs)
            {
                List<TimerInfo> timerList;
                if (!timers.TryGetValue(program.ServiceName, out timerList))
                {
                    try
                    {
                        var tempTimers = await GetService(program.ServiceName).GetTimersAsync(cancellationToken).ConfigureAwait(false);
                        timers[program.ServiceName] = timerList = tempTimers.ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting timer infos", ex);
                        timers[program.ServiceName] = timerList = new List<TimerInfo>();
                    }
                }

                var timer = timerList.FirstOrDefault(i => string.Equals(i.ProgramId, program.ExternalId, StringComparison.OrdinalIgnoreCase));

                if (timer != null)
                {
                    program.TimerId = _tvDtoService.GetInternalTimerId(program.ServiceName, timer.Id)
                        .ToString("N");

                    if (!string.IsNullOrEmpty(timer.SeriesTimerId))
                    {
                        program.SeriesTimerId = _tvDtoService.GetInternalSeriesTimerId(program.ServiceName, timer.SeriesTimerId)
                            .ToString("N");
                    }
                }
            }
        }

        internal async Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(p * .9));
            await RefreshChannelsInternal(innerProgress, cancellationToken).ConfigureAwait(false);

            innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(90 + (p * .1)));
            await CleanDatabaseInternal(progress, cancellationToken).ConfigureAwait(false);

            RefreshIfNeeded(GetPrograms().ToList());
        }

        private async Task RefreshChannelsInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var numComplete = 0;
            double progressPerService = _services.Count == 0
                ? 0
                : 1 / _services.Count;

            foreach (var service in _services)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var innerProgress = new ActionableProgress<double>();
                    innerProgress.RegisterAction(p => progress.Report(p * progressPerService));

                    await RefreshChannelsInternal(service, innerProgress, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error refreshing channels for service", ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= _services.Count;

                progress.Report(100 * percent);
            }

            progress.Report(100);
        }

        private async Task RefreshChannelsInternal(ILiveTvService service, IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(10);

            var allChannels = await GetChannels(service, cancellationToken).ConfigureAwait(false);
            var allChannelsList = allChannels.ToList();

            var list = new List<LiveTvChannel>();

            var numComplete = 0;

            foreach (var channelInfo in allChannelsList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = await GetChannel(channelInfo.Item2, channelInfo.Item1, cancellationToken).ConfigureAwait(false);

                    list.Add(item);

                    _libraryManager.RegisterItem(item);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channel information for {0}", ex, channelInfo.Item2.Name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= allChannelsList.Count;

                progress.Report(5 * percent + 10);
            }

            _channelIdList = list.Select(i => i.Id).ToList();
            progress.Report(15);

            numComplete = 0;
            var programs = new List<LiveTvProgram>();

            var guideDays = GetGuideDays(list.Count);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var item in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Avoid implicitly captured closure
                var currentChannel = item;

                try
                {
                    var start = DateTime.UtcNow.AddHours(-1);
                    var end = start.AddDays(guideDays);

                    var channelPrograms = await service.GetProgramsAsync(currentChannel.ExternalId, start, end, cancellationToken).ConfigureAwait(false);

                    foreach (var program in channelPrograms)
                    {
                        programs.Add(await GetProgram(program, currentChannel.ChannelType, service.Name, cancellationToken).ConfigureAwait(false));
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting programs for channel {0}", ex, currentChannel.Name);
                }

                numComplete++;
                double percent = numComplete;
                percent /= allChannelsList.Count;

                progress.Report(80 * percent + 10);
            }

            lock (_programsDataLock)
            {
                _programs = programs.ToDictionary(i => i.Id);
            }

            _refreshedPrograms.Clear();
            progress.Report(90);

            // Load these now which will prefetch metadata
            var dtoOptions = new DtoOptions();
            dtoOptions.Fields.Remove(ItemFields.SyncInfo);
            await GetRecordings(new RecordingQuery(), dtoOptions, cancellationToken).ConfigureAwait(false);
            progress.Report(100);
        }

        private Task CleanDatabaseInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return DeleteOldPrograms(GetProgramsDictionary().Keys.ToList(), progress, cancellationToken);
        }

        private async Task DeleteOldPrograms(List<Guid> currentIdList, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = _itemRepo.GetItemIdsOfType(typeof(LiveTvProgram)).ToList();

            var numComplete = 0;

            foreach (var programId in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!currentIdList.Contains(programId))
                {
                    var program = _libraryManager.GetItemById(programId);

                    if (program != null)
                    {
                        await _libraryManager.DeleteItem(program).ConfigureAwait(false);
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= list.Count;

                progress.Report(100 * percent);
            }
        }

        private double GetGuideDays(int channelCount)
        {
            var config = GetConfiguration();

            if (config.GuideDays.HasValue)
            {
                return config.GuideDays.Value;
            }

            var programsPerDay = channelCount * 48;

            const int maxPrograms = 24000;

            var days = Math.Round(((double)maxPrograms) / programsPerDay);

            return Math.Max(3, Math.Min(days, 14));
        }

        private async Task<IEnumerable<Tuple<string, ChannelInfo>>> GetChannels(ILiveTvService service, CancellationToken cancellationToken)
        {
            var channels = await service.GetChannelsAsync(cancellationToken).ConfigureAwait(false);

            return channels.Select(i => new Tuple<string, ChannelInfo>(service.Name, i));
        }

        public async Task<QueryResult<BaseItem>> GetInternalRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var tasks = _services.Select(async i =>
            {
                try
                {
                    var recs = await i.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
                    return recs.Select(r => new Tuple<RecordingInfo, ILiveTvService>(r, i));
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting recordings", ex);
                    return new List<Tuple<RecordingInfo, ILiveTvService>>();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var recordings = results.SelectMany(i => i.ToList());

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            if (user != null && !IsLiveTvEnabled(user))
            {
                recordings = new List<Tuple<RecordingInfo, ILiveTvService>>();
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId) == guid);
            }

            if (!string.IsNullOrEmpty(query.Id))
            {
                var guid = new Guid(query.Id);

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalRecordingId(i.Item2.Name, i.Item1.Id) == guid);
            }

            if (!string.IsNullOrEmpty(query.GroupId))
            {
                var guid = new Guid(query.GroupId);

                recordings = recordings.Where(i => GetRecordingGroupIds(i.Item1).Contains(guid));
            }

            if (query.IsInProgress.HasValue)
            {
                var val = query.IsInProgress.Value;
                recordings = recordings.Where(i => (i.Item1.Status == RecordingStatus.InProgress) == val);
            }

            if (query.Status.HasValue)
            {
                var val = query.Status.Value;
                recordings = recordings.Where(i => (i.Item1.Status == val));
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(i.Item2.Name, i.Item1.SeriesTimerId) == guid);
            }

            recordings = recordings.OrderByDescending(i => i.Item1.StartDate);

            IEnumerable<ILiveTvRecording> entities = await GetEntities(recordings, cancellationToken).ConfigureAwait(false);

            if (user != null)
            {
                var currentUser = user;
                entities = entities.Where(i => i.IsParentalAllowed(currentUser));
            }

            var entityList = entities.ToList();
            entities = entityList;

            if (query.StartIndex.HasValue)
            {
                entities = entities.Skip(query.StartIndex.Value);
            }

            if (query.Limit.HasValue)
            {
                entities = entities.Take(query.Limit.Value);
            }

            return new QueryResult<BaseItem>
            {
                Items = entities.Cast<BaseItem>().ToArray(),
                TotalRecordCount = entityList.Count
            };
        }

        public async Task<QueryResult<RecordingInfoDto>> GetRecordings(RecordingQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var internalResult = await GetInternalRecordings(query, cancellationToken).ConfigureAwait(false);

            var returnArray = internalResult.Items.Cast<ILiveTvRecording>()
                .Select(i =>
                {
                    var service = GetService(i);

                    var channel = string.IsNullOrEmpty(i.RecordingInfo.ChannelId) ? null : GetInternalChannel(_tvDtoService.GetInternalChannelId(service.Name, i.RecordingInfo.ChannelId));
                    return _tvDtoService.GetRecordingInfoDto(i, channel, service, user);
                })
                .ToArray();

            if (user != null)
            {
                _dtoService.FillSyncInfo(returnArray, new DtoOptions(), user);
            }

            return new QueryResult<RecordingInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = internalResult.TotalRecordCount
            };
        }

        private Task<ILiveTvRecording[]> GetEntities(IEnumerable<Tuple<RecordingInfo, ILiveTvService>> recordings, CancellationToken cancellationToken)
        {
            var tasks = recordings.Select(i => GetRecording(i.Item1, i.Item2.Name, cancellationToken));

            return Task.WhenAll(tasks);
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
                    _logger.ErrorException("Error getting recordings", ex);
                    return new List<Tuple<TimerInfo, ILiveTvService>>();
                }
            });
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var timers = results.SelectMany(i => i.ToList());

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);
                timers = timers.Where(i => guid == _tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId));
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                timers = timers
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(i.Item2.Name, i.Item1.SeriesTimerId) == guid);
            }

            var returnList = new List<TimerInfoDto>();

            foreach (var i in timers)
            {
                var program = string.IsNullOrEmpty(i.Item1.ProgramId) ?
                    null :
                    GetInternalProgram(_tvDtoService.GetInternalProgramId(i.Item2.Name, i.Item1.ProgramId).ToString("N"));

                var channel = string.IsNullOrEmpty(i.Item1.ChannelId) ? null : GetInternalChannel(_tvDtoService.GetInternalChannelId(i.Item2.Name, i.Item1.ChannelId));

                returnList.Add(_tvDtoService.GetTimerInfoDto(i.Item1, i.Item2, program, channel));
            }

            var returnArray = returnList
                .OrderBy(i => i.StartDate)
                .ToArray();

            return new QueryResult<TimerInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        public async Task DeleteRecording(string recordingId)
        {
            var dtoOptions = new DtoOptions();
            dtoOptions.Fields.Remove(ItemFields.SyncInfo);

            var recording = await GetRecording(recordingId, dtoOptions, CancellationToken.None).ConfigureAwait(false);

            if (recording == null)
            {
                throw new ResourceNotFoundException(string.Format("Recording with Id {0} not found", recordingId));
            }

            var service = GetService(recording.ServiceName);

            await service.DeleteRecordingAsync(recording.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CancelTimer(string id)
        {
            var timer = await GetTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer == null)
            {
                throw new ResourceNotFoundException(string.Format("Timer with Id {0} not found", id));
            }

            var service = GetService(timer.ServiceName);

            await service.CancelTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CancelSeriesTimer(string id)
        {
            var timer = await GetSeriesTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer == null)
            {
                throw new ResourceNotFoundException(string.Format("Timer with Id {0} not found", id));
            }

            var service = GetService(timer.ServiceName);

            await service.CancelSeriesTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<RecordingInfoDto> GetRecording(string id, DtoOptions options, CancellationToken cancellationToken, User user = null)
        {
            var results = await GetRecordings(new RecordingQuery
            {
                UserId = user == null ? null : user.Id.ToString("N"),
                Id = id

            }, options, cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault();
        }

        public async Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetTimers(new TimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetSeriesTimers(new SeriesTimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
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
                    _logger.ErrorException("Error getting recordings", ex);
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
                        var channel = GetInternalChannel(internalChannelId);
                        channelName = channel == null ? null : channel.Name;
                    }

                    return _tvDtoService.GetSeriesTimerInfoDto(i.Item1, i.Item2, channelName);

                })
                .ToArray();

            return new QueryResult<SeriesTimerInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };
        }

        public async Task<ChannelInfoDto> GetChannel(string id, CancellationToken cancellationToken, User user = null)
        {
            var channel = GetInternalChannel(id);

            var currentProgram = GetCurrentProgram(channel.ExternalId);

            var dto = _tvDtoService.GetChannelInfoDto(channel, currentProgram, user);

            return dto;
        }

        private LiveTvProgram GetCurrentProgram(string externalChannelId)
        {
            var now = DateTime.UtcNow;

            var program = GetPrograms()
                .Where(i => string.Equals(externalChannelId, i.ExternalChannelId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.StartDate)
                .SkipWhile(i => now >= (i.EndDate ?? DateTime.MinValue))
                .FirstOrDefault();

            if (program != null)
            {
                RefreshIfNeeded(program);
            }

            return program;
        }

        private async Task<Tuple<SeriesTimerInfo, ILiveTvService>> GetNewTimerDefaultsInternal(CancellationToken cancellationToken, LiveTvProgram program = null)
        {
            var service = program != null && !string.IsNullOrWhiteSpace(program.ServiceName) ?
                GetService(program) :
                _services.FirstOrDefault();

            ProgramInfo programInfo = null;

            if (program != null)
            {
                programInfo = new ProgramInfo
                {
                    Audio = program.Audio,
                    ChannelId = program.ExternalChannelId,
                    CommunityRating = program.CommunityRating,
                    EndDate = program.EndDate ?? DateTime.MinValue,
                    EpisodeTitle = program.EpisodeTitle,
                    Genres = program.Genres,
                    HasImage = program.HasProviderImage,
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
                    ImagePath = program.ProviderImagePath,
                    ImageUrl = program.ProviderImageUrl,
                    Name = program.Name,
                    OfficialRating = program.OfficialRating
                };
            }

            var info = await service.GetNewTimerDefaultsAsync(cancellationToken, programInfo).ConfigureAwait(false);

            info.Id = null;

            return new Tuple<SeriesTimerInfo, ILiveTvService>(info, service);
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken)
        {
            var info = await GetNewTimerDefaultsInternal(cancellationToken).ConfigureAwait(false);

            var obj = _tvDtoService.GetSeriesTimerInfoDto(info.Item1, info.Item2, null);

            return obj;
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken)
        {
            var program = GetInternalProgram(programId);
            var programDto = await GetProgram(programId, cancellationToken).ConfigureAwait(false);

            var defaults = await GetNewTimerDefaultsInternal(cancellationToken, program).ConfigureAwait(false);
            var info = _tvDtoService.GetSeriesTimerInfoDto(defaults.Item1, defaults.Item2, null);

            info.Days = new List<DayOfWeek>
            {
                program.StartDate.ToLocalTime().DayOfWeek
            };

            info.DayPattern = _tvDtoService.GetDayPattern(info.Days);

            info.Name = program.Name;
            info.ChannelId = programDto.ChannelId;
            info.ChannelName = programDto.ChannelName;
            info.StartDate = program.StartDate;
            info.Name = program.Name;
            info.Overview = program.Overview;
            info.ProgramId = programDto.Id;
            info.ExternalProgramId = programDto.ExternalId;

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

            await service.CreateTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = GetService(timer.ServiceName);

            var info = await _tvDtoService.GetSeriesTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            await service.CreateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
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

        private IEnumerable<string> GetRecordingGroupNames(RecordingInfo recording)
        {
            var list = new List<string>();

            if (recording.IsSeries)
            {
                list.Add(recording.Name);
            }

            if (recording.IsKids)
            {
                list.Add("Kids");
            }

            if (recording.IsMovie)
            {
                list.Add("Movies");
            }

            if (recording.IsNews)
            {
                list.Add("News");
            }

            if (recording.IsSports)
            {
                list.Add("Sports");
            }

            if (!recording.IsSports && !recording.IsNews && !recording.IsMovie && !recording.IsKids && !recording.IsSeries)
            {
                list.Add("Others");
            }

            return list;
        }

        private List<Guid> GetRecordingGroupIds(RecordingInfo recording)
        {
            return GetRecordingGroupNames(recording).Select(i => i.ToLower()
                .GetMD5())
                .ToList();
        }

        public async Task<QueryResult<RecordingGroupDto>> GetRecordingGroups(RecordingGroupQuery query, CancellationToken cancellationToken)
        {
            var dtoOptions = new DtoOptions();
            dtoOptions.Fields.Remove(ItemFields.SyncInfo);

            var recordingResult = await GetRecordings(new RecordingQuery
            {
                UserId = query.UserId

            }, dtoOptions, cancellationToken).ConfigureAwait(false);

            var recordings = recordingResult.Items;

            var groups = new List<RecordingGroupDto>();

            var series = recordings
                .Where(i => i.IsSeries)
                .ToLookup(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            groups.AddRange(series.OrderByString(i => i.Key).Select(i => new RecordingGroupDto
            {
                Name = i.Key,
                RecordingCount = i.Count()
            }));

            groups.Add(new RecordingGroupDto
            {
                Name = "Kids",
                RecordingCount = recordings.Count(i => i.IsKids)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "Movies",
                RecordingCount = recordings.Count(i => i.IsMovie)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "News",
                RecordingCount = recordings.Count(i => i.IsNews)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "Sports",
                RecordingCount = recordings.Count(i => i.IsSports)
            });

            groups.Add(new RecordingGroupDto
            {
                Name = "Others",
                RecordingCount = recordings.Count(i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries)
            });

            groups = groups
                .Where(i => i.RecordingCount > 0)
                .ToList();

            foreach (var group in groups)
            {
                group.Id = group.Name.ToLower().GetMD5().ToString("N");
            }

            return new QueryResult<RecordingGroupDto>
            {
                Items = groups.ToArray(),
                TotalRecordCount = groups.Count
            };
        }

        class LiveStreamData
        {
            internal MediaSourceInfo Info;
            internal string ItemId;
            internal bool IsChannel;
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var parts = id.Split(new[] { '_' }, 2);

                var service = _services.FirstOrDefault(i => string.Equals(i.GetType().FullName.GetMD5().ToString("N"), parts[0], StringComparison.OrdinalIgnoreCase));

                if (service == null)
                {
                    throw new ArgumentException("Service not found.");
                }

                id = parts[1];

                LiveStreamData data;
                _openStreams.TryRemove(id, out data);

                _logger.Info("Closing live stream from {0}, stream Id: {1}", service.Name, id);

                await service.CloseLiveStream(id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error closing live stream", ex);

                throw;
            }
            finally
            {
                _liveStreamSemaphore.Release();
            }
        }

        public GuideInfo GetGuideInfo()
        {
            var programs = GetPrograms().OrderBy(i => i.StartDate).ToList();

            var startDate = programs.Count == 0 ?
                DateTime.MinValue :
                programs[0].StartDate;

            var endDate = programs.Count == 0 ?
                DateTime.MinValue :
                programs[programs.Count - 1].StartDate;

            return new GuideInfo
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private readonly object _disposeLock = new object();
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                lock (_disposeLock)
                {
                    foreach (var stream in _openStreams.Values.ToList())
                    {
                        var task = CloseLiveStream(stream.Info.Id, CancellationToken.None);

                        Task.WaitAll(task);
                    }

                    _openStreams.Clear();
                }
            }
        }

        private async Task<IEnumerable<LiveTvServiceInfo>> GetServiceInfos(CancellationToken cancellationToken)
        {
            var tasks = Services.Select(i => GetServiceInfo(i, cancellationToken));

            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<LiveTvServiceInfo> GetServiceInfo(ILiveTvService service, CancellationToken cancellationToken)
        {
            var info = new LiveTvServiceInfo
            {
                Name = service.Name
            };

            var tunerIdPrefix = service.GetType().FullName.GetMD5().ToString("N") + "_";

            try
            {
                var statusInfo = await service.GetStatusInfoAsync(cancellationToken).ConfigureAwait(false);

                info.Status = statusInfo.Status;
                info.StatusMessage = statusInfo.StatusMessage;
                info.Version = statusInfo.Version;
                info.HasUpdateAvailable = statusInfo.HasUpdateAvailable;
                info.HomePageUrl = service.HomePageUrl;

                info.Tuners = statusInfo.Tuners.Select(i =>
                {
                    string channelName = null;

                    if (!string.IsNullOrEmpty(i.ChannelId))
                    {
                        var internalChannelId = _tvDtoService.GetInternalChannelId(service.Name, i.ChannelId);
                        var channel = GetInternalChannel(internalChannelId);
                        channelName = channel == null ? null : channel.Name;
                    }

                    var dto = _tvDtoService.GetTunerInfoDto(service.Name, i, channelName);

                    dto.Id = tunerIdPrefix + dto.Id;

                    return dto;

                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting service status info from {0}", ex, service.Name ?? string.Empty);

                info.Status = LiveTvServiceStatus.Unavailable;
                info.StatusMessage = ex.Message;
            }

            return info;
        }

        public async Task<LiveTvInfo> GetLiveTvInfo(CancellationToken cancellationToken)
        {
            var services = await GetServiceInfos(CancellationToken.None).ConfigureAwait(false);
            var servicesList = services.ToList();

            var info = new LiveTvInfo
            {
                Services = servicesList.ToList(),
                IsEnabled = servicesList.Count > 0
            };

            info.EnabledUsers = _userManager.Users
                .Where(IsLiveTvEnabled)
                .Select(i => i.Id.ToString("N"))
                .ToList();

            return info;
        }

        private bool IsLiveTvEnabled(User user)
        {
            return user.Policy.EnableLiveTvAccess && Services.Count > 0;
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
            var parts = id.Split(new[] { '_' }, 2);

            var service = _services.FirstOrDefault(i => string.Equals(i.GetType().FullName.GetMD5().ToString("N"), parts[0], StringComparison.OrdinalIgnoreCase));

            if (service == null)
            {
                throw new ArgumentException("Service not found.");
            }

            return service.ResetTuner(parts[1], cancellationToken);
        }

        public async Task<BaseItemDto> GetLiveTvFolder(string userId, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(userId) ? null : _userManager.GetUserById(userId);

            var folder = await GetInternalLiveTvFolder(userId, cancellationToken).ConfigureAwait(false);

            return _dtoService.GetBaseItemDto(folder, new DtoOptions(), user);
        }

        public async Task<Folder> GetInternalLiveTvFolder(string userId, CancellationToken cancellationToken)
        {
            var name = _localization.GetLocalizedString("ViewTypeLiveTV");
            var user = _userManager.GetUserById(userId);
            return await _libraryManager.GetNamedView(user, name, "livetv", "zz_" + name, cancellationToken).ConfigureAwait(false);
        }
    }
}
