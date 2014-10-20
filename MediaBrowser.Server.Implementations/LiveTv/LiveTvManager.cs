using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Channels;
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
using System.IO;
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
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ITaskManager _taskManager;
        private readonly IJsonSerializer _jsonSerializer;

        private readonly IDtoService _dtoService;
        private readonly ILocalizationManager _localization;

        private readonly LiveTvDtoService _tvDtoService;

        private readonly List<ILiveTvService> _services = new List<ILiveTvService>();

        private readonly ConcurrentDictionary<string, LiveStreamData> _openStreams =
            new ConcurrentDictionary<string, LiveStreamData>();

        private List<Guid> _channelIdList = new List<Guid>();
        private Dictionary<Guid, LiveTvProgram> _programs = new Dictionary<Guid, LiveTvProgram>();
        private readonly ConcurrentDictionary<Guid, bool> _refreshedPrograms = new ConcurrentDictionary<Guid, bool>();

        private readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

        public LiveTvManager(IServerConfigurationManager config, IFileSystem fileSystem, ILogger logger, IItemRepository itemRepo, IImageProcessor imageProcessor, IUserDataManager userDataManager, IDtoService dtoService, IUserManager userManager, ILibraryManager libraryManager, ITaskManager taskManager, ILocalizationManager localization, IJsonSerializer jsonSerializer)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
            _itemRepo = itemRepo;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _taskManager = taskManager;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
            _dtoService = dtoService;
            _userDataManager = userDataManager;

            _tvDtoService = new LiveTvDtoService(dtoService, userDataManager, imageProcessor, logger);
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IReadOnlyList<ILiveTvService> Services
        {
            get { return _services; }
        }

        public ILiveTvService ActiveService { get; private set; }

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

            SetActiveService(GetConfiguration().ActiveService);
        }

        private void SetActiveService(string name)
        {
            var service = _services.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)) ??
                _services.FirstOrDefault();

            SetActiveService(service);
        }

        private void SetActiveService(ILiveTvService service)
        {
            if (ActiveService != null)
            {
                ActiveService.DataSourceChanged -= service_DataSourceChanged;
            }

            ActiveService = service;

            if (service != null)
            {
                service.DataSourceChanged += service_DataSourceChanged;
            }
        }

        void service_DataSourceChanged(object sender, EventArgs e)
        {
            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();
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
                    .Where(i => i.IsParentalAllowed(currentUser))
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
            })
            .ThenBy(i =>
            {
                double number = 0;

                if (!string.IsNullOrEmpty(i.Number))
                {
                    double.TryParse(i.Number, out number);
                }

                return number;

            }).ThenBy(i => i.Name);

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
                var currentProgram = await GetCurrentProgram(channel.ExternalId, cancellationToken).ConfigureAwait(false);

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

        public async Task<LiveTvProgram> GetInternalProgram(string id, CancellationToken cancellationToken)
        {
            var guid = new Guid(id);

            LiveTvProgram obj = null;

            _programs.TryGetValue(guid, out obj);

            if (obj != null)
            {
                await RefreshIfNeeded(obj, cancellationToken).ConfigureAwait(false);
            }
            return obj;
        }

        private Task RefreshIfNeeded(IEnumerable<LiveTvProgram> programs, CancellationToken cancellationToken)
        {
            var list = programs.ToList();

            Task.Run(async () =>
            {
                foreach (var program in list)
                {
                    await RefreshIfNeeded(program, CancellationToken.None).ConfigureAwait(false);
                }

            }, cancellationToken);

            return Task.FromResult(true);
        }

        private async Task RefreshIfNeeded(LiveTvProgram program, CancellationToken cancellationToken)
        {
            if (_refreshedPrograms.ContainsKey(program.Id))
            {
                return;
            }

            _refreshedPrograms.TryAdd(program.Id, true);

            await program.RefreshMetadata(cancellationToken).ConfigureAwait(false);
        }

        public async Task<ILiveTvRecording> GetInternalRecording(string id, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            var recording = recordings.FirstOrDefault(i => _tvDtoService.GetInternalRecordingId(service.Name, i.Id) == new Guid(id));

            return await GetRecording(recording, service.Name, cancellationToken).ConfigureAwait(false);
        }

        private readonly SemaphoreSlim _liveStreamSemaphore = new SemaphoreSlim(1, 1);

        public async Task<ChannelMediaInfo> GetRecordingStream(string id, CancellationToken cancellationToken)
        {
            return await GetLiveStream(id, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ChannelMediaInfo> GetChannelStream(string id, CancellationToken cancellationToken)
        {
            return await GetLiveStream(id, true, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ChannelMediaInfo> GetLiveStream(string id, bool isChannel, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var service = ActiveService;
                ChannelMediaInfo info;

                if (isChannel)
                {
                    var channel = GetInternalChannel(id);
                    _logger.Info("Opening channel stream from {0}, external channel Id: {1}", service.Name, channel.ExternalId);

                    info = await service.GetChannelStream(channel.ExternalId, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
                    var recording = recordings.First(i => _tvDtoService.GetInternalRecordingId(service.Name, i.Id) == new Guid(id));

                    _logger.Info("Opening recording stream from {0}, external recording Id: {1}", service.Name, recording.Id);
                    info = await service.GetRecordingStream(recording.Id, cancellationToken).ConfigureAwait(false);
                }

                _logger.Info("Live stream info: {0}", _jsonSerializer.SerializeToString(info));
                Sanitize(info);

                var data = new LiveStreamData
                {
                    Info = info,
                    ConsumerCount = 1,
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

        private void Sanitize(ChannelMediaInfo info)
        {
            // Clean some bad data coming from providers

            if (info.AudioBitrate.HasValue && info.AudioBitrate <= 0)
            {
                info.AudioBitrate = null;
            }
            if (info.VideoBitrate.HasValue && info.VideoBitrate <= 0)
            {
                info.VideoBitrate = null;
            }
            if (info.AudioChannels.HasValue && info.AudioChannels <= 0)
            {
                info.AudioChannels = null;
            }
            if (info.Framerate.HasValue && info.Framerate <= 0)
            {
                info.Framerate = null;
            }
            if (info.Width.HasValue && info.Width <= 0)
            {
                info.Width = null;
            }
            if (info.Height.HasValue && info.Height <= 0)
            {
                info.Height = null;
            }
            if (info.AudioSampleRate.HasValue && info.AudioSampleRate <= 0)
            {
                info.AudioSampleRate = null;
            }
            if (info.VideoLevel.HasValue && info.VideoLevel <= 0)
            {
                info.VideoLevel = null;
            }
        }

        private async Task<LiveTvChannel> GetChannel(ChannelInfo channelInfo, string serviceName, CancellationToken cancellationToken)
        {
            var path = Path.Combine(_config.ApplicationPaths.ItemsByNamePath, "tvchannels", _fileSystem.GetValidFilename(channelInfo.Name));

            var fileInfo = new DirectoryInfo(path);

            var isNew = false;

            if (!fileInfo.Exists)
            {
                _logger.Debug("Creating directory {0}", path);

                Directory.CreateDirectory(path);
                fileInfo = new DirectoryInfo(path);

                if (!fileInfo.Exists)
                {
                    throw new IOException("Path not created: " + path);
                }

                isNew = true;
            }

            var id = _tvDtoService.GetInternalChannelId(serviceName, channelInfo.Id);

            var item = _itemRepo.RetrieveItem(id) as LiveTvChannel;

            if (item == null || !string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                item = new LiveTvChannel
                {
                    Name = channelInfo.Name,
                    Id = id,
                    DateCreated = _fileSystem.GetCreationTimeUtc(fileInfo),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(fileInfo),
                    Path = path
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

        private LiveTvProgram GetProgram(ProgramInfo info, ChannelType channelType, string serviceName, CancellationToken cancellationToken)
        {
            var id = _tvDtoService.GetInternalProgramId(serviceName, info.Id);

            var item = _itemRepo.RetrieveItem(id) as LiveTvProgram;

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
            item.PremiereDate = info.OriginalAirDate;
            item.ProviderImagePath = info.ImagePath;
            item.ProviderImageUrl = info.ImageUrl;
            item.RunTimeTicks = (info.EndDate - info.StartDate).Ticks;
            item.StartDate = info.StartDate;

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
            var program = await GetInternalProgram(id, cancellationToken).ConfigureAwait(false);

            var channel = GetChannel(program);

            var dto = _tvDtoService.GetProgramInfoDto(program, channel, user);

            await AddRecordingInfo(new[] { dto }, cancellationToken).ConfigureAwait(false);

            return dto;
        }

        public async Task<QueryResult<ProgramInfoDto>> GetPrograms(ProgramQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<LiveTvProgram> programs = _programs.Values;

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

            if (query.ChannelIdList.Length > 0)
            {
                var guids = query.ChannelIdList.Select(i => new Guid(i)).ToList();
                var serviceName = ActiveService.Name;

                programs = programs.Where(i =>
                {
                    var programChannelId = i.ExternalChannelId;

                    var internalProgramChannelId = _tvDtoService.GetInternalChannelId(serviceName, programChannelId);

                    return guids.Contains(internalProgramChannelId);
                });
            }

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            if (user != null)
            {
                // Avoid implicitly captured closure
                var currentUser = user;
                programs = programs.Where(i => i.IsParentalAllowed(currentUser));
            }

            var programList = programs.ToList();

            var returnArray = programList
                .Select(i =>
                {
                    var channel = GetChannel(i);

                    return _tvDtoService.GetProgramInfoDto(i, channel, user);
                })
                .ToArray();

            await RefreshIfNeeded(programList, cancellationToken).ConfigureAwait(false);

            await AddRecordingInfo(returnArray, cancellationToken).ConfigureAwait(false);

            var result = new QueryResult<ProgramInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };

            return result;
        }

        public async Task<QueryResult<LiveTvProgram>> GetRecommendedProgramsInternal(RecommendedProgramQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<LiveTvProgram> programs = _programs.Values;

            var user = _userManager.GetUserById(query.UserId);

            // Avoid implicitly captured closure
            var currentUser = user;
            programs = programs.Where(i => i.IsParentalAllowed(currentUser));

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

            var serviceName = ActiveService.Name;

            var programList = programs.ToList();

            var genres = programList.SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => _libraryManager.GetGenre(i))
                .ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

            programs = programList.OrderBy(i => i.HasImage(ImageType.Primary) ? 0 : 1)
                .ThenByDescending(i => GetRecommendationScore(i, user.Id, serviceName, genres))
                .ThenBy(i => i.StartDate);

            if (query.Limit.HasValue)
            {
                programs = programs.Take(query.Limit.Value)
                    .OrderBy(i => i.StartDate);
            }

            programList = programs.ToList();

            await RefreshIfNeeded(programList, cancellationToken).ConfigureAwait(false);

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

        private int GetRecommendationScore(LiveTvProgram program, Guid userId, string serviceName, Dictionary<string, Genre> genres)
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

            var internalChannelId = _tvDtoService.GetInternalChannelId(serviceName, program.ExternalChannelId);
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
            var timers = await ActiveService.GetTimersAsync(cancellationToken).ConfigureAwait(false);
            var timerList = timers.ToList();

            foreach (var program in programs)
            {
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
            await _refreshSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await RefreshChannelsInternal(progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        private async Task RefreshChannelsInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Avoid implicitly captured closure
            var service = ActiveService;

            if (service == null)
            {
                progress.Report(100);
                return;
            }

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

                    var programEntities = channelPrograms.Select(program => GetProgram(program, currentChannel.ChannelType, service.Name, cancellationToken));

                    programs.AddRange(programEntities);
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

            _programs = programs.ToDictionary(i => i.Id);
            _refreshedPrograms.Clear();
            progress.Report(90);

            // Load these now which will prefetch metadata
            await GetRecordings(new RecordingQuery(), cancellationToken).ConfigureAwait(false);
            progress.Report(100);
        }

        public async Task CleanDatabase(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            if (service == null)
            {
                progress.Report(100);
                return;
            }

            await _refreshSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await DeleteOldPrograms(_programs.Keys.ToList(), progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        private async Task DeleteOldPrograms(List<Guid> currentIdList, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = _itemRepo.GetItemsOfType(typeof(LiveTvProgram)).ToList();

            var numComplete = 0;

            foreach (var program in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!currentIdList.Contains(program.Id))
                {
                    await _libraryManager.DeleteItem(program).ConfigureAwait(false);
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

            // No less than 2, no more than 7
            return Math.Max(2, Math.Min(days, 7));
        }

        private async Task<IEnumerable<Tuple<string, ChannelInfo>>> GetChannels(ILiveTvService service, CancellationToken cancellationToken)
        {
            var channels = await service.GetChannelsAsync(cancellationToken).ConfigureAwait(false);

            return channels.Select(i => new Tuple<string, ChannelInfo>(service.Name, i));
        }

        public async Task<QueryResult<BaseItem>> GetInternalRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            if (service == null)
            {
                return new QueryResult<BaseItem>
                {
                    Items = new BaseItem[] { }
                };
            }

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var recordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            if (user != null && !IsLiveTvEnabled(user))
            {
                recordings = new List<RecordingInfo>();
            }

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);

                var currentServiceName = service.Name;

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalChannelId(currentServiceName, i.ChannelId) == guid);
            }

            if (!string.IsNullOrEmpty(query.Id))
            {
                var guid = new Guid(query.Id);

                var currentServiceName = service.Name;

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalRecordingId(currentServiceName, i.Id) == guid);
            }

            if (!string.IsNullOrEmpty(query.GroupId))
            {
                var guid = new Guid(query.GroupId);

                recordings = recordings.Where(i => GetRecordingGroupIds(i).Contains(guid));
            }

            if (query.IsInProgress.HasValue)
            {
                var val = query.IsInProgress.Value;
                recordings = recordings.Where(i => (i.Status == RecordingStatus.InProgress) == val);
            }

            if (query.Status.HasValue)
            {
                var val = query.Status.Value;
                recordings = recordings.Where(i => (i.Status == val));
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                var currentServiceName = service.Name;

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(currentServiceName, i.SeriesTimerId) == guid);
            }

            recordings = recordings.OrderByDescending(i => i.StartDate);

            IEnumerable<ILiveTvRecording> entities = await GetEntities(recordings, service.Name, cancellationToken).ConfigureAwait(false);

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

        public async Task<QueryResult<RecordingInfoDto>> GetRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            if (service == null)
            {
                return new QueryResult<RecordingInfoDto>
                {
                    Items = new RecordingInfoDto[] { }
                };
            }

            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var internalResult = await GetInternalRecordings(query, cancellationToken).ConfigureAwait(false);

            var returnArray = internalResult.Items.Cast<ILiveTvRecording>()
                .Select(i =>
                {
                    var channel = string.IsNullOrEmpty(i.RecordingInfo.ChannelId) ? null : GetInternalChannel(_tvDtoService.GetInternalChannelId(service.Name, i.RecordingInfo.ChannelId));
                    return _tvDtoService.GetRecordingInfoDto(i, channel, service, user);
                })
                .ToArray();

            return new QueryResult<RecordingInfoDto>
            {
                Items = returnArray,
                TotalRecordCount = internalResult.TotalRecordCount
            };
        }

        private Task<ILiveTvRecording[]> GetEntities(IEnumerable<RecordingInfo> recordings, string serviceName, CancellationToken cancellationToken)
        {
            var tasks = recordings.Select(i => GetRecording(i, serviceName, cancellationToken));

            return Task.WhenAll(tasks);
        }

        private IEnumerable<ILiveTvService> GetServices(string serviceName, string channelId)
        {
            IEnumerable<ILiveTvService> services = _services;

            if (string.IsNullOrEmpty(serviceName) && !string.IsNullOrEmpty(channelId))
            {
                var channel = GetInternalChannel(channelId);

                if (channel != null)
                {
                    serviceName = channel.ServiceName;
                }
            }

            if (!string.IsNullOrEmpty(serviceName))
            {
                services = services.Where(i => string.Equals(i.Name, serviceName, StringComparison.OrdinalIgnoreCase));
            }

            return services;
        }

        public async Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;
            var timers = await service.GetTimersAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                var guid = new Guid(query.ChannelId);
                timers = timers.Where(i => guid == _tvDtoService.GetInternalChannelId(service.Name, i.ChannelId));
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                var currentServiceName = service.Name;

                timers = timers
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(currentServiceName, i.SeriesTimerId) == guid);
            }

            var returnList = new List<TimerInfoDto>();

            foreach (var i in timers)
            {
                var program = string.IsNullOrEmpty(i.ProgramId) ?
                    null :
                    await GetInternalProgram(_tvDtoService.GetInternalProgramId(service.Name, i.ProgramId).ToString("N"), cancellationToken).ConfigureAwait(false);

                var channel = string.IsNullOrEmpty(i.ChannelId) ? null : GetInternalChannel(_tvDtoService.GetInternalChannelId(service.Name, i.ChannelId));

                returnList.Add(_tvDtoService.GetTimerInfoDto(i, service, program, channel));
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
            var recording = await GetRecording(recordingId, CancellationToken.None).ConfigureAwait(false);

            if (recording == null)
            {
                throw new ResourceNotFoundException(string.Format("Recording with Id {0} not found", recordingId));
            }

            var service = GetServices(recording.ServiceName, null)
                .First();

            await service.DeleteRecordingAsync(recording.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CancelTimer(string id)
        {
            var timer = await GetTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer == null)
            {
                throw new ResourceNotFoundException(string.Format("Timer with Id {0} not found", id));
            }

            var service = GetServices(timer.ServiceName, null)
                .First();

            await service.CancelTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task CancelSeriesTimer(string id)
        {
            var timer = await GetSeriesTimer(id, CancellationToken.None).ConfigureAwait(false);

            if (timer == null)
            {
                throw new ResourceNotFoundException(string.Format("Timer with Id {0} not found", id));
            }

            var service = GetServices(timer.ServiceName, null)
                .First();

            await service.CancelSeriesTimerAsync(timer.ExternalId, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<RecordingInfoDto> GetRecording(string id, CancellationToken cancellationToken, User user = null)
        {
            var results = await GetRecordings(new RecordingQuery
            {
                UserId = user == null ? null : user.Id.ToString("N"),
                Id = id

            }, cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault();
        }

        public async Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetTimers(new TimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.CurrentCulture));
        }

        public async Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken)
        {
            var results = await GetSeriesTimers(new SeriesTimerQuery(), cancellationToken).ConfigureAwait(false);

            return results.Items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.CurrentCulture));
        }

        public async Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken)
        {
            var service = ActiveService;

            var timers = await service.GetSeriesTimersAsync(cancellationToken).ConfigureAwait(false);

            if (string.Equals(query.SortBy, "Priority", StringComparison.OrdinalIgnoreCase))
            {
                timers = query.SortOrder == SortOrder.Descending ?
                    timers.OrderBy(i => i.Priority).ThenByStringDescending(i => i.Name) :
                    timers.OrderByDescending(i => i.Priority).ThenByString(i => i.Name);
            }
            else
            {
                timers = query.SortOrder == SortOrder.Descending ?
                    timers.OrderByStringDescending(i => i.Name) :
                    timers.OrderByString(i => i.Name);
            }

            var returnArray = timers
                .Select(i =>
                {
                    string channelName = null;

                    if (!string.IsNullOrEmpty(i.ChannelId))
                    {
                        var internalChannelId = _tvDtoService.GetInternalChannelId(service.Name, i.ChannelId);
                        var channel = GetInternalChannel(internalChannelId);
                        channelName = channel == null ? null : channel.Name;
                    }

                    return _tvDtoService.GetSeriesTimerInfoDto(i, service, channelName);

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

            var currentProgram = await GetCurrentProgram(channel.ExternalId, cancellationToken).ConfigureAwait(false);

            var dto = _tvDtoService.GetChannelInfoDto(channel, currentProgram, user);

            return dto;
        }

        private async Task<LiveTvProgram> GetCurrentProgram(string externalChannelId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var program = _programs.Values
                .Where(i => string.Equals(externalChannelId, i.ExternalChannelId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.StartDate)
                .SkipWhile(i => now >= (i.EndDate ?? DateTime.MinValue))
                .FirstOrDefault();

            if (program != null)
            {
                await RefreshIfNeeded(program, cancellationToken).ConfigureAwait(false);
            }

            return program;
        }

        private async Task<SeriesTimerInfo> GetNewTimerDefaultsInternal(CancellationToken cancellationToken, LiveTvProgram program = null)
        {
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

            var info = await ActiveService.GetNewTimerDefaultsAsync(cancellationToken, programInfo).ConfigureAwait(false);

            info.Id = null;

            return info;
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken)
        {
            var info = await GetNewTimerDefaultsInternal(cancellationToken).ConfigureAwait(false);

            var obj = _tvDtoService.GetSeriesTimerInfoDto(info, ActiveService, null);

            return obj;
        }

        public async Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken)
        {
            var program = await GetInternalProgram(programId, cancellationToken).ConfigureAwait(false);
            var programDto = await GetProgram(programId, cancellationToken).ConfigureAwait(false);

            var defaults = await GetNewTimerDefaultsInternal(cancellationToken, program).ConfigureAwait(false);
            var info = _tvDtoService.GetSeriesTimerInfoDto(defaults, ActiveService, null);

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
            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            var info = await _tvDtoService.GetTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            await service.CreateTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            var info = await _tvDtoService.GetSeriesTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            await service.CreateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

            await service.UpdateTimerAsync(info, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetSeriesTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = string.IsNullOrEmpty(timer.ServiceName) ? ActiveService : GetServices(timer.ServiceName, null).First();

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
            var recordingResult = await GetRecordings(new RecordingQuery
            {
                UserId = query.UserId

            }, cancellationToken).ConfigureAwait(false);

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
            internal ChannelMediaInfo Info;
            internal int ConsumerCount;
            internal string ItemId;
            internal bool IsChannel;
        }

        public async Task CloseLiveStream(string id, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var service = ActiveService;

                LiveStreamData data;
                if (_openStreams.TryGetValue(id, out data))
                {
                    if (data.ConsumerCount > 1)
                    {
                        data.ConsumerCount--;
                        _logger.Info("Decrementing live stream client count.");
                        return;
                    }

                }
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
            var programs = _programs.ToList();

            var startDate = _programs.Count == 0 ? DateTime.MinValue :
                programs.Select(i => i.Value.StartDate).Min();

            var endDate = programs.Count == 0 ? DateTime.MinValue :
                programs.Select(i => i.Value.StartDate).Max();

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

                    return _tvDtoService.GetTunerInfoDto(service.Name, i, channelName);

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

            var activeServiceInfo = ActiveService == null ? null :
                servicesList.FirstOrDefault(i => string.Equals(i.Name, ActiveService.Name, StringComparison.OrdinalIgnoreCase));

            var info = new LiveTvInfo
            {
                Services = servicesList.ToList(),
                ActiveServiceName = activeServiceInfo == null ? null : activeServiceInfo.Name,
                IsEnabled = ActiveService != null,
                Status = activeServiceInfo == null ? LiveTvServiceStatus.Unavailable : activeServiceInfo.Status,
                StatusMessage = activeServiceInfo == null ? null : activeServiceInfo.StatusMessage
            };

            info.EnabledUsers = _userManager.Users
                .Where(IsLiveTvEnabled)
                .Select(i => i.Id.ToString("N"))
                .ToList();

            return info;
        }

        private bool IsLiveTvEnabled(User user)
        {
            return user.Configuration.EnableLiveTvAccess && ActiveService != null;
        }

        public IEnumerable<User> GetEnabledUsers()
        {
            var service = ActiveService;

            return _userManager.Users
                .Where(i => i.Configuration.EnableLiveTvAccess && service != null);
        }

        /// <summary>
        /// Resets the tuner.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task ResetTuner(string id, CancellationToken cancellationToken)
        {
            return ActiveService.ResetTuner(id, cancellationToken);
        }

        public async Task<BaseItemDto> GetLiveTvFolder(string userId, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(userId) ? null : _userManager.GetUserById(userId);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var folder = await GetInternalLiveTvFolder(userId, cancellationToken).ConfigureAwait(false);

            return _dtoService.GetBaseItemDto(folder, fields, user);
        }

        public async Task<Folder> GetInternalLiveTvFolder(string userId, CancellationToken cancellationToken)
        {
            var name = _localization.GetLocalizedString("ViewTypeLiveTV");
            return await _libraryManager.GetNamedView(name, "livetv", "zz_" + name, cancellationToken).ConfigureAwait(false);
        }
    }
}
