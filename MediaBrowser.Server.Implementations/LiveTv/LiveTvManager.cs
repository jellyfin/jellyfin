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
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using IniParser;
using IniParser.Model;
using MediaBrowser.Common.Events;
using MediaBrowser.Model.Events;

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

        private readonly SemaphoreSlim _refreshRecordingsLock = new SemaphoreSlim(1, 1);

        private readonly List<ITunerHost> _tunerHosts = new List<ITunerHost>();
        private readonly List<IListingsProvider> _listingProviders = new List<IListingsProvider>();
        private readonly IFileSystem _fileSystem;

        public event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCancelled;
        public event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCancelled;
        public event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCreated;
        public event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCreated;

        public LiveTvManager(IApplicationHost appHost, IServerConfigurationManager config, ILogger logger, IItemRepository itemRepo, IImageProcessor imageProcessor, IUserDataManager userDataManager, IDtoService dtoService, IUserManager userManager, ILibraryManager libraryManager, ITaskManager taskManager, ILocalizationManager localization, IJsonSerializer jsonSerializer, IProviderManager providerManager, IFileSystem fileSystem)
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
            _fileSystem = fileSystem;
            _dtoService = dtoService;
            _userDataManager = userDataManager;

            _tvDtoService = new LiveTvDtoService(dtoService, userDataManager, imageProcessor, logger, appHost, _libraryManager);
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
        /// <param name="tunerHosts">The tuner hosts.</param>
        /// <param name="listingProviders">The listing providers.</param>
        public void AddParts(IEnumerable<ILiveTvService> services, IEnumerable<ITunerHost> tunerHosts, IEnumerable<IListingsProvider> listingProviders)
        {
            _services.AddRange(services);
            _tunerHosts.AddRange(tunerHosts);
            _listingProviders.AddRange(listingProviders);

            foreach (var service in _services)
            {
                service.DataSourceChanged += service_DataSourceChanged;
            }
        }

        public List<ITunerHost> TunerHosts
        {
            get { return _tunerHosts; }
        }

        public List<IListingsProvider> ListingProviders
        {
            get { return _listingProviders; }
        }

        void service_DataSourceChanged(object sender, EventArgs e)
        {
            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();
        }

        public async Task<QueryResult<LiveTvChannel>> GetInternalChannels(LiveTvChannelQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var topFolder = await GetInternalLiveTvFolder(cancellationToken).ConfigureAwait(false);

            var channels = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(LiveTvChannel).Name },
                SortBy = new[] { ItemSortBy.SortName },
                TopParentIds = new[] { topFolder.Id.ToString("N") }

            }).Cast<LiveTvChannel>();

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
                        .Where(i => _userDataManager.GetUserData(user, i).IsFavorite == val);
                }

                if (query.IsLiked.HasValue)
                {
                    var val = query.IsLiked.Value;

                    channels = channels
                        .Where(i =>
                        {
                            var likes = _userDataManager.GetUserData(user, i).Likes;

                            return likes.HasValue && likes.Value == val;
                        });
                }

                if (query.IsDisliked.HasValue)
                {
                    var val = query.IsDisliked.Value;

                    channels = channels
                        .Where(i =>
                        {
                            var likes = _userDataManager.GetUserData(user, i).Likes;

                            return likes.HasValue && likes.Value != val;
                        });
                }
            }

            var enableFavoriteSorting = query.EnableFavoriteSorting;

            channels = channels.OrderBy(i =>
            {
                if (enableFavoriteSorting)
                {
                    var userData = _userDataManager.GetUserData(user, i);

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

        public LiveTvChannel GetInternalChannel(string id)
        {
            return GetInternalChannel(new Guid(id));
        }

        private LiveTvChannel GetInternalChannel(Guid id)
        {
            return _libraryManager.GetItemById(id) as LiveTvChannel;
        }

        internal LiveTvProgram GetInternalProgram(string id)
        {
            return _libraryManager.GetItemById(id) as LiveTvProgram;
        }

        internal LiveTvProgram GetInternalProgram(Guid id)
        {
            return _libraryManager.GetItemById(id) as LiveTvProgram;
        }

        public async Task<BaseItem> GetInternalRecording(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            var result = await GetInternalRecordings(new RecordingQuery
            {
                Id = id

            }, cancellationToken).ConfigureAwait(false);

            return result.Items.FirstOrDefault();
        }

        private readonly SemaphoreSlim _liveStreamSemaphore = new SemaphoreSlim(1, 1);

        public async Task<MediaSourceInfo> GetRecordingStream(string id, CancellationToken cancellationToken)
        {
            return await GetLiveStream(id, null, false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MediaSourceInfo> GetChannelStream(string id, string mediaSourceId, CancellationToken cancellationToken)
        {
            return await GetLiveStream(id, mediaSourceId, true, cancellationToken).ConfigureAwait(false);
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

            var sources = await service.GetChannelStreamMediaSources(item.ExternalId, cancellationToken).ConfigureAwait(false);

            if (sources.Count == 0)
            {
                throw new NotImplementedException();
            }

            var list = sources.ToList();

            foreach (var source in list)
            {
                Normalize(source, service, item.ChannelType == ChannelType.TV);
            }

            return list;
        }

        private ILiveTvService GetService(ILiveTvRecording item)
        {
            return GetService(item.ServiceName);
        }

        private ILiveTvService GetService(BaseItem item)
        {
            return GetService(item.ServiceName);
        }

        private ILiveTvService GetService(string name)
        {
            return _services.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<MediaSourceInfo> GetLiveStream(string id, string mediaSourceId, bool isChannel, CancellationToken cancellationToken)
        {
            await _liveStreamSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (string.Equals(id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
            {
                mediaSourceId = null;
            }

            try
            {
                MediaSourceInfo info;
                bool isVideo;
                ILiveTvService service;

                if (isChannel)
                {
                    var channel = GetInternalChannel(id);
                    isVideo = channel.ChannelType == ChannelType.TV;
                    service = GetService(channel);
                    _logger.Info("Opening channel stream from {0}, external channel Id: {1}", service.Name, channel.ExternalId);
                    info = await service.GetChannelStream(channel.ExternalId, mediaSourceId, cancellationToken).ConfigureAwait(false);
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
                    service = GetService(recording);

                    _logger.Info("Opening recording stream from {0}, external recording Id: {1}", service.Name, recording.ExternalId);
                    info = await service.GetRecordingStream(recording.ExternalId, null, cancellationToken).ConfigureAwait(false);
                    info.RequiresClosing = true;

                    if (info.RequiresClosing)
                    {
                        var idPrefix = service.GetType().FullName.GetMD5().ToString("N") + "_";

                        info.LiveStreamId = idPrefix + info.Id;
                    }
                }

                _logger.Info("Live stream info: {0}", _jsonSerializer.SerializeToString(info));
                Normalize(info, service, isVideo);

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

        private void Normalize(MediaSourceInfo mediaSource, ILiveTvService service, bool isVideo)
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

            // Set the total bitrate if not already supplied
            if (!mediaSource.Bitrate.HasValue)
            {
                var total = mediaSource.MediaStreams.Select(i => i.BitRate ?? 0).Sum();

                if (total > 0)
                {
                    mediaSource.Bitrate = total;
                }
            }

            if (!(service is EmbyTV.EmbyTV))
            {
                // We can't trust that we'll be able to direct stream it through emby server,  no matter what the provider says
                mediaSource.SupportsDirectStream = false;
                mediaSource.SupportsTranscoding = true;
                foreach (var stream in mediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Video && string.IsNullOrWhiteSpace(stream.NalLengthSize))
                    {
                        stream.NalLengthSize = "0";
                    }
                }
            }
        }

        private async Task<LiveTvChannel> GetChannel(ChannelInfo channelInfo, string serviceName, Guid parentFolderId, CancellationToken cancellationToken)
        {
            var isNew = false;
            var forceUpdate = false;

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

            if (!string.Equals(channelInfo.Id, item.ExternalId))
            {
                isNew = true;
            }
            item.ExternalId = channelInfo.Id;

            if (!item.ParentId.Equals(parentFolderId))
            {
                isNew = true;
            }
            item.ParentId = parentFolderId;

            item.ChannelType = channelInfo.ChannelType;
            item.ServiceName = serviceName;
            item.Number = channelInfo.Number;

            //if (!string.Equals(item.ProviderImageUrl, channelInfo.ImageUrl, StringComparison.OrdinalIgnoreCase))
            //{
            //    isNew = true;
            //    replaceImages.Add(ImageType.Primary);
            //}
            //if (!string.Equals(item.ProviderImagePath, channelInfo.ImagePath, StringComparison.OrdinalIgnoreCase))
            //{
            //    isNew = true;
            //    replaceImages.Add(ImageType.Primary);
            //}

            if (!item.HasImage(ImageType.Primary))
            {
                if (!string.IsNullOrWhiteSpace(channelInfo.ImagePath))
                {
                    item.SetImagePath(ImageType.Primary, channelInfo.ImagePath);
                    forceUpdate = true;
                }
                else if (!string.IsNullOrWhiteSpace(channelInfo.ImageUrl))
                {
                    item.SetImagePath(ImageType.Primary, channelInfo.ImageUrl);
                    forceUpdate = true;
                }
            }

            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = channelInfo.Name;
            }

            if (isNew)
            {
                await _libraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);
            }
            else if (forceUpdate)
            {
                await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
            }

            await item.RefreshMetadata(new MetadataRefreshOptions(_fileSystem)
            {
                ForceSave = isNew || forceUpdate

            }, cancellationToken);

            return item;
        }

        private async Task<LiveTvProgram> GetProgram(ProgramInfo info, LiveTvChannel channel, ChannelType channelType, string serviceName, CancellationToken cancellationToken)
        {
            var id = _tvDtoService.GetInternalProgramId(serviceName, info.Id);

            var item = _libraryManager.GetItemById(id) as LiveTvProgram;
            var isNew = false;
            var forceUpdate = false;

            if (item == null)
            {
                isNew = true;
                item = new LiveTvProgram
                {
                    Name = info.Name,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    ExternalEtag = info.Etag
                };
            }

            if (!item.ParentId.Equals(channel.Id))
            {
                forceUpdate = true;
            }
            item.ParentId = channel.Id;

            //item.ChannelType = channelType;
            if (!string.Equals(item.ServiceName, serviceName, StringComparison.Ordinal))
            {
                forceUpdate = true;
            }
            item.ServiceName = serviceName;

            item.Audio = info.Audio;
            item.ChannelId = channel.Id.ToString("N");
            item.CommunityRating = item.CommunityRating ?? info.CommunityRating;

            item.EpisodeTitle = info.EpisodeTitle;
            item.ExternalId = info.Id;
            item.Genres = info.Genres;
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
            item.OfficialRating = item.OfficialRating ?? info.OfficialRating;
            item.Overview = item.Overview ?? info.Overview;
            item.RunTimeTicks = (info.EndDate - info.StartDate).Ticks;

            if (item.StartDate != info.StartDate)
            {
                forceUpdate = true;
            }
            item.StartDate = info.StartDate;

            if (item.EndDate != info.EndDate)
            {
                forceUpdate = true;
            }
            item.EndDate = info.EndDate;

            item.HomePageUrl = info.HomePageUrl;

            item.ProductionYear = info.ProductionYear;
            item.PremiereDate = info.OriginalAirDate;

            item.IndexNumber = info.EpisodeNumber;
            item.ParentIndexNumber = info.SeasonNumber;

            if (!item.HasImage(ImageType.Primary))
            {
                if (!string.IsNullOrWhiteSpace(info.ImagePath))
                {
                    item.SetImage(new ItemImageInfo
                    {
                        Path = info.ImagePath,
                        Type = ImageType.Primary,
                        IsPlaceholder = true
                    }, 0);
                }
                else if (!string.IsNullOrWhiteSpace(info.ImageUrl))
                {
                    item.SetImage(new ItemImageInfo
                    {
                        Path = info.ImageUrl,
                        Type = ImageType.Primary,
                        IsPlaceholder = true
                    }, 0);
                }
            }

            if (isNew)
            {
                await _libraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);
            }
            else if (forceUpdate || string.IsNullOrWhiteSpace(info.Etag))
            {
                await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Increment this whenver some internal change deems it necessary
                var etag = info.Etag + "4";

                if (!string.Equals(etag, item.ExternalEtag, StringComparison.OrdinalIgnoreCase))
                {
                    item.ExternalEtag = etag;
                    await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
                }
            }

            _providerManager.QueueRefresh(item.Id, new MetadataRefreshOptions(_fileSystem));

            return item;
        }

        private async Task<Guid> CreateRecordingRecord(RecordingInfo info, string serviceName, Guid parentFolderId, CancellationToken cancellationToken)
        {
            var isNew = false;

            var id = _tvDtoService.GetInternalRecordingId(serviceName, info.Id);

            var item = _itemRepo.RetrieveItem(id);

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

            item.ChannelId = _tvDtoService.GetInternalChannelId(serviceName, info.ChannelId).ToString("N");
            item.CommunityRating = info.CommunityRating;
            item.OfficialRating = info.OfficialRating;
            item.Overview = info.Overview;
            item.EndDate = info.EndDate;
            item.Genres = info.Genres;
            item.PremiereDate = info.OriginalAirDate;

            var recording = (ILiveTvRecording)item;

            recording.ExternalId = info.Id;

            var dataChanged = false;

            recording.Audio = info.Audio;
            recording.EndDate = info.EndDate;
            recording.EpisodeTitle = info.EpisodeTitle;
            recording.IsHD = info.IsHD;
            recording.IsKids = info.IsKids;
            recording.IsLive = info.IsLive;
            recording.IsMovie = info.IsMovie;
            recording.IsNews = info.IsNews;
            recording.IsPremiere = info.IsPremiere;
            recording.IsRepeat = info.IsRepeat;
            recording.IsSports = info.IsSports;
            recording.SeriesTimerId = info.SeriesTimerId;
            recording.StartDate = info.StartDate;

            if (!dataChanged)
            {
                dataChanged = recording.IsSeries != info.IsSeries;
            }
            recording.IsSeries = info.IsSeries;

            if (!item.ParentId.Equals(parentFolderId))
            {
                dataChanged = true;
            }
            item.ParentId = parentFolderId;

            if (!item.HasImage(ImageType.Primary))
            {
                if (!string.IsNullOrWhiteSpace(info.ImagePath))
                {
                    item.SetImage(new ItemImageInfo
                    {
                        Path = info.ImagePath,
                        Type = ImageType.Primary,
                        IsPlaceholder = true
                    }, 0);
                }
                else if (!string.IsNullOrWhiteSpace(info.ImageUrl))
                {
                    item.SetImage(new ItemImageInfo
                    {
                        Path = info.ImageUrl,
                        Type = ImageType.Primary,
                        IsPlaceholder = true
                    }, 0);
                }
            }

            var statusChanged = info.Status != recording.Status;

            recording.Status = info.Status;

            recording.ServiceName = serviceName;

            if (!string.IsNullOrEmpty(info.Path))
            {
                if (!dataChanged)
                {
                    dataChanged = !string.Equals(item.Path, info.Path);
                }
                var fileInfo = _fileSystem.GetFileInfo(info.Path);

                recording.DateCreated = _fileSystem.GetCreationTimeUtc(fileInfo);
                recording.DateModified = _fileSystem.GetLastWriteTimeUtc(fileInfo);
                item.Path = info.Path;
            }
            else if (!string.IsNullOrEmpty(info.Url))
            {
                if (!dataChanged)
                {
                    dataChanged = !string.Equals(item.Path, info.Url);
                }
                item.Path = info.Url;
            }

            var metadataRefreshMode = MetadataRefreshMode.Default;

            if (isNew)
            {
                await _libraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);
            }
            else if (dataChanged || info.DateLastUpdated > recording.DateLastSaved || statusChanged)
            {
                metadataRefreshMode = MetadataRefreshMode.FullRefresh;
                await _libraryManager.UpdateItem(item, ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
            }

            if (info.Status != RecordingStatus.InProgress)
            {
                _providerManager.QueueRefresh(item.Id, new MetadataRefreshOptions(_fileSystem)
                {
                    MetadataRefreshMode = metadataRefreshMode
                });
            }

            return item.Id;
        }

        public async Task<BaseItemDto> GetProgram(string id, CancellationToken cancellationToken, User user = null)
        {
            var program = GetInternalProgram(id);

            var dto = _dtoService.GetBaseItemDto(program, new DtoOptions(), user);

            var list = new List<Tuple<BaseItemDto, string, string>>();
            list.Add(new Tuple<BaseItemDto, string, string>(dto, program.ServiceName, program.ExternalId));

            await AddRecordingInfo(list, cancellationToken).ConfigureAwait(false);

            return dto;
        }

        public async Task<QueryResult<BaseItemDto>> GetPrograms(ProgramQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var topFolder = await GetInternalLiveTvFolder(cancellationToken).ConfigureAwait(false);

            if (query.SortBy.Length == 0)
            {
                // Unless something else was specified, order by start date to take advantage of a specialized index
                query.SortBy = new[] { ItemSortBy.StartDate };
            }

            var internalQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(LiveTvProgram).Name },
                MinEndDate = query.MinEndDate,
                MinStartDate = query.MinStartDate,
                MaxEndDate = query.MaxEndDate,
                MaxStartDate = query.MaxStartDate,
                ChannelIds = query.ChannelIds,
                IsMovie = query.IsMovie,
                IsSports = query.IsSports,
                IsKids = query.IsKids,
                Genres = query.Genres,
                StartIndex = query.StartIndex,
                Limit = query.Limit,
                SortBy = query.SortBy,
                SortOrder = query.SortOrder ?? SortOrder.Ascending,
                EnableTotalRecordCount = query.EnableTotalRecordCount,
                TopParentIds = new[] { topFolder.Id.ToString("N") }
            };

            if (query.HasAired.HasValue)
            {
                if (query.HasAired.Value)
                {
                    internalQuery.MaxEndDate = DateTime.UtcNow;
                }
                else
                {
                    internalQuery.MinEndDate = DateTime.UtcNow;
                }
            }

            var queryResult = _libraryManager.QueryItems(internalQuery);

            var returnArray = (await _dtoService.GetBaseItemDtos(queryResult.Items, options, user).ConfigureAwait(false)).ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnArray,
                TotalRecordCount = queryResult.TotalRecordCount
            };

            return result;
        }

        public async Task<QueryResult<LiveTvProgram>> GetRecommendedProgramsInternal(RecommendedProgramQuery query, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(query.UserId);

            var topFolder = await GetInternalLiveTvFolder(cancellationToken).ConfigureAwait(false);

            var internalQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(LiveTvProgram).Name },
                IsAiring = query.IsAiring,
                IsMovie = query.IsMovie,
                IsSports = query.IsSports,
                IsKids = query.IsKids,
                EnableTotalRecordCount = query.EnableTotalRecordCount,
                SortBy = new[] { ItemSortBy.StartDate },
                TopParentIds = new[] { topFolder.Id.ToString("N") }
            };

            if (query.Limit.HasValue)
            {
                internalQuery.Limit = Math.Max(query.Limit.Value * 4, 200);
            }

            if (query.HasAired.HasValue)
            {
                if (query.HasAired.Value)
                {
                    internalQuery.MaxEndDate = DateTime.UtcNow;
                }
                else
                {
                    internalQuery.MinEndDate = DateTime.UtcNow;
                }
            }

            IEnumerable<LiveTvProgram> programs = _libraryManager.QueryItems(internalQuery).Items.Cast<LiveTvProgram>();

            var programList = programs.ToList();

            var factorChannelWatchCount = (query.IsAiring ?? false) || (query.IsKids ?? false) || (query.IsSports ?? false) || (query.IsMovie ?? false);

            programs = programList.OrderBy(i => i.HasImage(ImageType.Primary) ? 0 : 1)
                .ThenByDescending(i => GetRecommendationScore(i, user.Id, factorChannelWatchCount))
                .ThenBy(i => i.StartDate);

            if (query.Limit.HasValue)
            {
                programs = programs.Take(query.Limit.Value);
            }

            programList = programs.ToList();

            var returnArray = programList.ToArray();

            var result = new QueryResult<LiveTvProgram>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Length
            };

            return result;
        }

        public async Task<QueryResult<BaseItemDto>> GetRecommendedPrograms(RecommendedProgramQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            var internalResult = await GetRecommendedProgramsInternal(query, cancellationToken).ConfigureAwait(false);

            var user = _userManager.GetUserById(query.UserId);

            var returnArray = (await _dtoService.GetBaseItemDtos(internalResult.Items, options, user).ConfigureAwait(false)).ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnArray,
                TotalRecordCount = internalResult.TotalRecordCount
            };

            return result;
        }

        private int GetRecommendationScore(LiveTvProgram program, Guid userId, bool factorChannelWatchCount)
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

            var channel = GetInternalChannel(program.ChannelId);

            var channelUserdata = _userDataManager.GetUserData(userId, channel);

            if (channelUserdata.Likes ?? false)
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

            if (factorChannelWatchCount)
            {
                score += channelUserdata.PlayCount;
            }

            return score;
        }

        private async Task AddRecordingInfo(IEnumerable<Tuple<BaseItemDto, string, string>> programs, CancellationToken cancellationToken)
        {
            var timers = new Dictionary<string, List<TimerInfo>>();

            foreach (var programTuple in programs)
            {
                var program = programTuple.Item1;
                var serviceName = programTuple.Item2;
                var externalProgramId = programTuple.Item3;

                if (string.IsNullOrWhiteSpace(serviceName))
                {
                    continue;
                }

                List<TimerInfo> timerList;
                if (!timers.TryGetValue(serviceName, out timerList))
                {
                    try
                    {
                        var tempTimers = await GetService(serviceName).GetTimersAsync(cancellationToken).ConfigureAwait(false);
                        timers[serviceName] = timerList = tempTimers.ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting timer infos", ex);
                        timers[serviceName] = timerList = new List<TimerInfo>();
                    }
                }

                var timer = timerList.FirstOrDefault(i => string.Equals(i.ProgramId, externalProgramId, StringComparison.OrdinalIgnoreCase));

                if (timer != null)
                {
                    program.TimerId = _tvDtoService.GetInternalTimerId(serviceName, timer.Id)
                        .ToString("N");

                    if (!string.IsNullOrEmpty(timer.SeriesTimerId))
                    {
                        program.SeriesTimerId = _tvDtoService.GetInternalSeriesTimerId(serviceName, timer.SeriesTimerId)
                            .ToString("N");
                    }
                }
            }
        }

        internal Task RefreshChannels(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return RefreshChannelsInternal(progress, cancellationToken);
        }

        private async Task RefreshChannelsInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            EmbyTV.EmbyTV.Current.CreateRecordingFolders();

            var numComplete = 0;
            double progressPerService = _services.Count == 0
                ? 0
                : 1 / _services.Count;

            var newChannelIdList = new List<Guid>();
            var newProgramIdList = new List<Guid>();

            foreach (var service in _services)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.Debug("Refreshing guide from {0}", service.Name);

                try
                {
                    var innerProgress = new ActionableProgress<double>();
                    innerProgress.RegisterAction(p => progress.Report(p * progressPerService));

                    var idList = await RefreshChannelsInternal(service, innerProgress, cancellationToken).ConfigureAwait(false);

                    newChannelIdList.AddRange(idList.Item1);
                    newProgramIdList.AddRange(idList.Item2);
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

            await CleanDatabaseInternal(newChannelIdList, new[] { typeof(LiveTvChannel).Name }, progress, cancellationToken).ConfigureAwait(false);
            await CleanDatabaseInternal(newProgramIdList, new[] { typeof(LiveTvProgram).Name }, progress, cancellationToken).ConfigureAwait(false);

            var coreService = _services.OfType<EmbyTV.EmbyTV>().FirstOrDefault();

            if (coreService != null)
            {
                await coreService.RefreshSeriesTimers(cancellationToken, new Progress<double>()).ConfigureAwait(false);
            }

            // Load these now which will prefetch metadata
            var dtoOptions = new DtoOptions();
            dtoOptions.Fields.Remove(ItemFields.SyncInfo);
            dtoOptions.Fields.Remove(ItemFields.BasicSyncInfo);
            await GetRecordings(new RecordingQuery(), dtoOptions, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }

        private async Task<Tuple<List<Guid>, List<Guid>>> RefreshChannelsInternal(ILiveTvService service, IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(10);

            var allChannels = await GetChannels(service, cancellationToken).ConfigureAwait(false);
            var allChannelsList = allChannels.ToList();

            var list = new List<LiveTvChannel>();

            var numComplete = 0;
            var parentFolder = await GetInternalLiveTvFolder(cancellationToken).ConfigureAwait(false);
            var parentFolderId = parentFolder.Id;

            foreach (var channelInfo in allChannelsList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = await GetChannel(channelInfo.Item2, channelInfo.Item1, parentFolderId, cancellationToken).ConfigureAwait(false);

                    list.Add(item);
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

            progress.Report(15);

            numComplete = 0;
            var programs = new List<Guid>();
            var channels = new List<Guid>();

            var guideDays = GetGuideDays(list.Count);

            _logger.Info("Refreshing guide with {0} days of guide data", guideDays);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var currentChannel in list)
            {
                channels.Add(currentChannel.Id);
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var start = DateTime.UtcNow.AddHours(-1);
                    var end = start.AddDays(guideDays);

                    var channelPrograms = await service.GetProgramsAsync(currentChannel.ExternalId, start, end, cancellationToken).ConfigureAwait(false);

                    foreach (var program in channelPrograms)
                    {
                        var programItem = await GetProgram(program, currentChannel, currentChannel.ChannelType, service.Name, cancellationToken).ConfigureAwait(false);

                        programs.Add(programItem.Id);
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
            progress.Report(100);

            return new Tuple<List<Guid>, List<Guid>>(channels, programs);
        }

        private async Task CleanDatabaseInternal(List<Guid> currentIdList, string[] validTypes, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = _itemRepo.GetItemIdsList(new InternalItemsQuery
            {
                IncludeItemTypes = validTypes

            }).ToList();

            var numComplete = 0;

            foreach (var itemId in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (itemId == Guid.Empty)
                {
                    // Somehow some invalid data got into the db. It probably predates the boundary checking
                    continue;
                }

                if (!currentIdList.Contains(itemId))
                {
                    var item = _libraryManager.GetItemById(itemId);

                    if (item != null)
                    {
                        await _libraryManager.DeleteItem(item, new DeleteOptions
                        {
                            DeleteFileLocation = false

                        }).ConfigureAwait(false);
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= list.Count;

                progress.Report(100 * percent);
            }
        }

        private const int MaxGuideDays = 14;
        private double GetGuideDays(int channelCount)
        {
            var config = GetConfiguration();

            if (config.GuideDays.HasValue)
            {
                return Math.Max(1, Math.Min(config.GuideDays.Value, MaxGuideDays));
            }

            var programsPerDay = channelCount * 48;

            const int maxPrograms = 24000;

            var days = Math.Round((double)maxPrograms / programsPerDay);

            return Math.Max(3, Math.Min(days, MaxGuideDays));
        }

        private async Task<IEnumerable<Tuple<string, ChannelInfo>>> GetChannels(ILiveTvService service, CancellationToken cancellationToken)
        {
            var channels = await service.GetChannelsAsync(cancellationToken).ConfigureAwait(false);

            return channels.Select(i => new Tuple<string, ChannelInfo>(service.Name, i));
        }

        private DateTime _lastRecordingRefreshTime;
        private async Task RefreshRecordings(CancellationToken cancellationToken)
        {
            const int cacheMinutes = 5;

            if ((DateTime.UtcNow - _lastRecordingRefreshTime).TotalMinutes < cacheMinutes)
            {
                return;
            }

            await _refreshRecordingsLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if ((DateTime.UtcNow - _lastRecordingRefreshTime).TotalMinutes < cacheMinutes)
                {
                    return;
                }

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
                var folder = await GetInternalLiveTvFolder(cancellationToken).ConfigureAwait(false);
                var parentFolderId = folder.Id;

                var recordingTasks = results.SelectMany(i => i.ToList()).Select(i => CreateRecordingRecord(i.Item1, i.Item2.Name, parentFolderId, cancellationToken));

                var idList = await Task.WhenAll(recordingTasks).ConfigureAwait(false);

                await CleanDatabaseInternal(idList.ToList(), new[] { typeof(LiveTvVideoRecording).Name, typeof(LiveTvAudioRecording).Name }, new Progress<double>(), cancellationToken).ConfigureAwait(false);

                _lastRecordingRefreshTime = DateTime.UtcNow;
            }
            finally
            {
                _refreshRecordingsLock.Release();
            }
        }

        private QueryResult<BaseItem> GetEmbyRecordings(RecordingQuery query, User user)
        {
            if (user == null || (query.IsInProgress ?? false))
            {
                return new QueryResult<BaseItem>();
            }

            var folders = EmbyTV.EmbyTV.Current.GetRecordingFolders()
                .SelectMany(i => i.Locations)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => _libraryManager.FindByPath(i, true))
                .Where(i => i != null)
                .Where(i => i.IsVisibleStandalone(user))
                .ToList();

            if (folders.Count == 0)
            {
                return new QueryResult<BaseItem>();
            }

            return _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                MediaTypes = new[] { MediaType.Video },
                Recursive = true,
                AncestorIds = folders.Select(i => i.Id.ToString("N")).ToArray(),
                IsFolder = false,
                ExcludeLocationTypes = new[] { LocationType.Virtual },
                Limit = Math.Min(200, query.Limit ?? int.MaxValue),
                SortBy = new[] { ItemSortBy.DateCreated },
                SortOrder = SortOrder.Descending,
                EnableTotalRecordCount = query.EnableTotalRecordCount
            });
        }

        public async Task<QueryResult<BaseItem>> GetInternalRecordings(RecordingQuery query, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);
            if (user != null && !IsLiveTvEnabled(user))
            {
                return new QueryResult<BaseItem>();
            }

            if (_services.Count == 1)
            {
                return GetEmbyRecordings(query, user);
            }

            await RefreshRecordings(cancellationToken).ConfigureAwait(false);

            var internalQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(LiveTvVideoRecording).Name, typeof(LiveTvAudioRecording).Name }
            };

            if (!string.IsNullOrEmpty(query.ChannelId))
            {
                internalQuery.ChannelIds = new[] { query.ChannelId };
            }

            var queryResult = _libraryManager.GetItemList(internalQuery);
            IEnumerable<ILiveTvRecording> recordings = queryResult.Cast<ILiveTvRecording>();

            if (!string.IsNullOrWhiteSpace(query.Id))
            {
                var guid = new Guid(query.Id);

                recordings = recordings
                    .Where(i => i.Id == guid);
            }

            if (!string.IsNullOrWhiteSpace(query.GroupId))
            {
                var guid = new Guid(query.GroupId);

                recordings = recordings.Where(i => GetRecordingGroupIds(i).Contains(guid));
            }

            if (query.IsInProgress.HasValue)
            {
                var val = query.IsInProgress.Value;
                recordings = recordings.Where(i => i.Status == RecordingStatus.InProgress == val);
            }

            if (query.Status.HasValue)
            {
                var val = query.Status.Value;
                recordings = recordings.Where(i => i.Status == val);
            }

            if (!string.IsNullOrEmpty(query.SeriesTimerId))
            {
                var guid = new Guid(query.SeriesTimerId);

                recordings = recordings
                    .Where(i => _tvDtoService.GetInternalSeriesTimerId(i.ServiceName, i.SeriesTimerId) == guid);
            }

            recordings = recordings.OrderByDescending(i => i.StartDate);

            var entityList = recordings.ToList();
            IEnumerable<ILiveTvRecording> entities = entityList;

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

        public async Task AddInfoToProgramDto(List<Tuple<BaseItem, BaseItemDto>> tuples, List<ItemFields> fields, User user = null)
        {
            var recordingTuples = new List<Tuple<BaseItemDto, string, string>>();

            foreach (var tuple in tuples)
            {
                var program = (LiveTvProgram)tuple.Item1;
                var dto = tuple.Item2;

                dto.StartDate = program.StartDate;
                dto.EpisodeTitle = program.EpisodeTitle;

                if (program.IsRepeat)
                {
                    dto.IsRepeat = program.IsRepeat;
                }
                if (program.IsMovie)
                {
                    dto.IsMovie = program.IsMovie;
                }
                if (program.IsSeries)
                {
                    dto.IsSeries = program.IsSeries;
                }
                if (program.IsSports)
                {
                    dto.IsSports = program.IsSports;
                }
                if (program.IsLive)
                {
                    dto.IsLive = program.IsLive;
                }
                if (program.IsNews)
                {
                    dto.IsNews = program.IsNews;
                }
                if (program.IsKids)
                {
                    dto.IsKids = program.IsKids;
                }
                if (program.IsPremiere)
                {
                    dto.IsPremiere = program.IsPremiere;
                }

                if (fields.Contains(ItemFields.ChannelInfo))
                {
                    var channel = GetInternalChannel(program.ChannelId);

                    if (channel != null)
                    {
                        dto.ChannelName = channel.Name;
                        dto.MediaType = channel.MediaType;
                        dto.ChannelNumber = channel.Number;

                        if (channel.HasImage(ImageType.Primary))
                        {
                            dto.ChannelPrimaryImageTag = _tvDtoService.GetImageTag(channel);
                        }
                    }
                }

                var service = GetService(program);
                var serviceName = service == null ? null : service.Name;

                if (fields.Contains(ItemFields.ServiceName))
                {
                    dto.ServiceName = serviceName;
                }

                recordingTuples.Add(new Tuple<BaseItemDto, string, string>(dto, serviceName, program.ExternalId));
            }

            await AddRecordingInfo(recordingTuples, CancellationToken.None).ConfigureAwait(false);
        }

        public void AddInfoToRecordingDto(BaseItem item, BaseItemDto dto, User user = null)
        {
            var recording = (ILiveTvRecording)item;
            var service = GetService(recording);

            var channel = string.IsNullOrWhiteSpace(recording.ChannelId) ? null : GetInternalChannel(recording.ChannelId);

            var info = recording;

            dto.SeriesTimerId = string.IsNullOrEmpty(info.SeriesTimerId)
                ? null
                : _tvDtoService.GetInternalSeriesTimerId(service.Name, info.SeriesTimerId).ToString("N");

            dto.StartDate = info.StartDate;
            dto.RecordingStatus = info.Status;
            dto.IsRepeat = info.IsRepeat;
            dto.EpisodeTitle = info.EpisodeTitle;
            dto.IsMovie = info.IsMovie;
            dto.IsSeries = info.IsSeries;
            dto.IsSports = info.IsSports;
            dto.IsLive = info.IsLive;
            dto.IsNews = info.IsNews;
            dto.IsKids = info.IsKids;
            dto.IsPremiere = info.IsPremiere;

            dto.CanDelete = user == null
                ? recording.CanDelete()
                : recording.CanDelete(user);

            if (dto.MediaSources == null)
            {
                dto.MediaSources = recording.GetMediaSources(true).ToList();
            }

            if (dto.MediaStreams == null)
            {
                dto.MediaStreams = dto.MediaSources.SelectMany(i => i.MediaStreams).ToList();
            }

            if (info.Status == RecordingStatus.InProgress && info.EndDate.HasValue)
            {
                var now = DateTime.UtcNow.Ticks;
                var start = info.StartDate.Ticks;
                var end = info.EndDate.Value.Ticks;

                var pct = now - start;
                pct /= end;
                pct *= 100;
                dto.CompletionPercentage = pct;
            }

            if (channel != null)
            {
                dto.ChannelName = channel.Name;

                if (channel.HasImage(ImageType.Primary))
                {
                    dto.ChannelPrimaryImageTag = _tvDtoService.GetImageTag(channel);
                }
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetRecordings(RecordingQuery query, DtoOptions options, CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(query.UserId) ? null : _userManager.GetUserById(query.UserId);

            var internalResult = await GetInternalRecordings(query, cancellationToken).ConfigureAwait(false);

            var returnArray = (await _dtoService.GetBaseItemDtos(internalResult.Items, options, user).ConfigureAwait(false)).ToArray();

            return new QueryResult<BaseItemDto>
            {
                Items = returnArray,
                TotalRecordCount = internalResult.TotalRecordCount
            };
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

        public Task OnRecordingFileDeleted(BaseItem recording)
        {
            var service = GetService(recording);

            if (service is EmbyTV.EmbyTV)
            {
                // We can't trust that we'll be able to direct stream it through emby server,  no matter what the provider says
                return service.DeleteRecordingAsync(recording.ExternalId, CancellationToken.None);
            }

            return Task.FromResult(true);
        }

        public async Task DeleteRecording(string recordingId)
        {
            var recording = await GetInternalRecording(recordingId, CancellationToken.None).ConfigureAwait(false);

            if (recording == null)
            {
                throw new ResourceNotFoundException(string.Format("Recording with Id {0} not found", recordingId));
            }

            await DeleteRecording((BaseItem)recording).ConfigureAwait(false);
        }

        public async Task DeleteRecording(BaseItem recording)
        {
            var service = GetService(recording.ServiceName);

            try
            {
                await service.DeleteRecordingAsync(recording.ExternalId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ResourceNotFoundException)
            {

            }

            _lastRecordingRefreshTime = DateTime.MinValue;

            // This is the responsibility of the live tv service
            await _libraryManager.DeleteItem((BaseItem)recording, new DeleteOptions
            {
                DeleteFileLocation = false

            }).ConfigureAwait(false);

            _lastRecordingRefreshTime = DateTime.MinValue;
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
            _lastRecordingRefreshTime = DateTime.MinValue;

            EventHelper.QueueEventIfNotNull(TimerCancelled, this, new GenericEventArgs<TimerEventInfo>
            {
                Argument = new TimerEventInfo
                {
                    Id = id
                }
            }, _logger);
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
            _lastRecordingRefreshTime = DateTime.MinValue;

            EventHelper.QueueEventIfNotNull(SeriesTimerCancelled, this, new GenericEventArgs<TimerEventInfo>
            {
                Argument = new TimerEventInfo
                {
                    Id = id
                }
            }, _logger);
        }

        public async Task<BaseItemDto> GetRecording(string id, DtoOptions options, CancellationToken cancellationToken, User user = null)
        {
            var item = await GetInternalRecording(id, cancellationToken).ConfigureAwait(false);

            if (item == null)
            {
                return null;
            }

            return _dtoService.GetBaseItemDto((BaseItem)item, options, user);
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

        public void AddChannelInfo(List<Tuple<BaseItemDto, LiveTvChannel>> tuples, DtoOptions options, User user)
        {
            var now = DateTime.UtcNow;

            var channelIds = tuples.Select(i => i.Item2.Id.ToString("N")).Distinct().ToArray();

            var programs = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(LiveTvProgram).Name },
                ChannelIds = channelIds,
                MaxStartDate = now,
                MinEndDate = now,
                Limit = channelIds.Length,
                SortBy = new[] { "StartDate" },
                TopParentIds = new[] { GetInternalLiveTvFolder(CancellationToken.None).Result.Id.ToString("N") }

            }).ToList();

            foreach (var tuple in tuples)
            {
                var dto = tuple.Item1;
                var channel = tuple.Item2;

                dto.Number = channel.Number;
                dto.ChannelNumber = channel.Number;
                dto.ChannelType = channel.ChannelType;
                dto.ServiceName = GetService(channel).Name;

                dto.MediaSources = channel.GetMediaSources(true).ToList();

                var channelIdString = channel.Id.ToString("N");
                var currentProgram = programs.FirstOrDefault(i => string.Equals(i.ChannelId, channelIdString));

                if (currentProgram != null)
                {
                    dto.CurrentProgram = _dtoService.GetBaseItemDto(currentProgram, options, user);
                }
            }
        }

        private async Task<Tuple<SeriesTimerInfo, ILiveTvService>> GetNewTimerDefaultsInternal(CancellationToken cancellationToken, LiveTvProgram program = null)
        {
            var service = program != null && !string.IsNullOrWhiteSpace(program.ServiceName) ?
                GetService(program) :
                _services.FirstOrDefault();

            ProgramInfo programInfo = null;

            if (program != null)
            {
                var channel = GetInternalChannel(program.ChannelId);

                programInfo = new ProgramInfo
                {
                    Audio = program.Audio,
                    ChannelId = channel.ExternalId,
                    CommunityRating = program.CommunityRating,
                    EndDate = program.EndDate ?? DateTime.MinValue,
                    EpisodeTitle = program.EpisodeTitle,
                    Genres = program.Genres,
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
                    //ImagePath = program.ExternalImagePath,
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

            info.Days = defaults.Item1.Days;

            info.DayPattern = _tvDtoService.GetDayPattern(info.Days);

            info.Name = program.Name;
            info.ChannelId = programDto.ChannelId;
            info.ChannelName = programDto.ChannelName;
            info.StartDate = program.StartDate;
            info.Name = program.Name;
            info.Overview = program.Overview;
            info.ProgramId = programDto.Id;
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
            var supportsNewTimerIds = service as ISupportsNewTimerIds;
            if (supportsNewTimerIds != null)
            {
                newTimerId = await supportsNewTimerIds.CreateTimer(info, cancellationToken).ConfigureAwait(false);
                newTimerId = _tvDtoService.GetInternalTimerId(timer.ServiceName, newTimerId).ToString("N");
            }
            else
            {
                await service.CreateTimerAsync(info, cancellationToken).ConfigureAwait(false);
            }

            _lastRecordingRefreshTime = DateTime.MinValue;
            _logger.Info("New recording scheduled");

            EventHelper.QueueEventIfNotNull(TimerCreated, this, new GenericEventArgs<TimerEventInfo>
            {
                Argument = new TimerEventInfo
                {
                    ProgramId = _tvDtoService.GetInternalProgramId(timer.ServiceName, info.ProgramId).ToString("N"),
                    Id = newTimerId
                }
            }, _logger);
        }

        public async Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var service = GetService(timer.ServiceName);

            var info = await _tvDtoService.GetSeriesTimerInfo(timer, true, this, cancellationToken).ConfigureAwait(false);

            // Set priority from default values
            var defaultValues = await service.GetNewTimerDefaultsAsync(cancellationToken).ConfigureAwait(false);
            info.Priority = defaultValues.Priority;

            string newTimerId = null;
            var supportsNewTimerIds = service as ISupportsNewTimerIds;
            if (supportsNewTimerIds != null)
            {
                newTimerId = await supportsNewTimerIds.CreateSeriesTimer(info, cancellationToken).ConfigureAwait(false);
                newTimerId = _tvDtoService.GetInternalSeriesTimerId(timer.ServiceName, newTimerId).ToString("N");
            }
            else
            {
                await service.CreateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
            }

            _lastRecordingRefreshTime = DateTime.MinValue;

            EventHelper.QueueEventIfNotNull(SeriesTimerCreated, this, new GenericEventArgs<TimerEventInfo>
            {
                Argument = new TimerEventInfo
                {
                    ProgramId = _tvDtoService.GetInternalProgramId(timer.ServiceName, info.ProgramId).ToString("N"),
                    Id = newTimerId
                }
            }, _logger);
        }

        public async Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = GetService(timer.ServiceName);

            await service.UpdateTimerAsync(info, cancellationToken).ConfigureAwait(false);
            _lastRecordingRefreshTime = DateTime.MinValue;
        }

        public async Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken)
        {
            var info = await _tvDtoService.GetSeriesTimerInfo(timer, false, this, cancellationToken).ConfigureAwait(false);

            var service = GetService(timer.ServiceName);

            await service.UpdateSeriesTimerAsync(info, cancellationToken).ConfigureAwait(false);
            _lastRecordingRefreshTime = DateTime.MinValue;
        }

        private IEnumerable<string> GetRecordingGroupNames(ILiveTvRecording recording)
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

        private List<Guid> GetRecordingGroupIds(ILiveTvRecording recording)
        {
            return GetRecordingGroupNames(recording).Select(i => i.ToLower()
                .GetMD5())
                .ToList();
        }

        public async Task<QueryResult<BaseItemDto>> GetRecordingGroups(RecordingGroupQuery query, CancellationToken cancellationToken)
        {
            var recordingResult = await GetInternalRecordings(new RecordingQuery
            {
                UserId = query.UserId

            }, cancellationToken).ConfigureAwait(false);

            var recordings = recordingResult.Items.OfType<ILiveTvRecording>().ToList();

            var groups = new List<BaseItemDto>();

            var series = recordings
                .Where(i => i.IsSeries)
                .ToLookup(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            groups.AddRange(series.OrderByString(i => i.Key).Select(i => new BaseItemDto
            {
                Name = i.Key,
                RecordingCount = i.Count()
            }));

            groups.Add(new BaseItemDto
            {
                Name = "Kids",
                RecordingCount = recordings.Count(i => i.IsKids)
            });

            groups.Add(new BaseItemDto
            {
                Name = "Movies",
                RecordingCount = recordings.Count(i => i.IsMovie)
            });

            groups.Add(new BaseItemDto
            {
                Name = "News",
                RecordingCount = recordings.Count(i => i.IsNews)
            });

            groups.Add(new BaseItemDto
            {
                Name = "Sports",
                RecordingCount = recordings.Count(i => i.IsSports)
            });

            groups.Add(new BaseItemDto
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

            return new QueryResult<BaseItemDto>
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
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(14);

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
                info.IsVisible = statusInfo.IsVisible;

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
            return user.Policy.EnableLiveTvAccess && (Services.Count > 1 || GetConfiguration().TunerHosts.Count(i => i.IsEnabled) > 0);
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

            var folder = await GetInternalLiveTvFolder(cancellationToken).ConfigureAwait(false);

            return _dtoService.GetBaseItemDto(folder, new DtoOptions(), user);
        }

        public async Task<Folder> GetInternalLiveTvFolder(CancellationToken cancellationToken)
        {
            var name = _localization.GetLocalizedString("ViewTypeLiveTV");
            return await _libraryManager.GetNamedView(name, CollectionType.LiveTv, name, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TunerHostInfo> SaveTunerHost(TunerHostInfo info)
        {
            info = (TunerHostInfo)_jsonSerializer.DeserializeFromString(_jsonSerializer.SerializeToString(info), typeof(TunerHostInfo));

            var provider = _tunerHosts.FirstOrDefault(i => string.Equals(info.Type, i.Type, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new ResourceNotFoundException();
            }

            var configurable = provider as IConfigurableTunerHost;
            if (configurable != null)
            {
                await configurable.Validate(info).ConfigureAwait(false);
            }

            var config = GetConfiguration();

            var index = config.TunerHosts.FindIndex(i => string.Equals(i.Id, info.Id, StringComparison.OrdinalIgnoreCase));

            if (index == -1 || string.IsNullOrWhiteSpace(info.Id))
            {
                info.Id = Guid.NewGuid().ToString("N");
                config.TunerHosts.Add(info);
            }
            else
            {
                config.TunerHosts[index] = info;
            }

            _config.SaveConfiguration("livetv", config);

            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();

            return info;
        }

        public async Task<ListingsProviderInfo> SaveListingProvider(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            info = (ListingsProviderInfo)_jsonSerializer.DeserializeFromString(_jsonSerializer.SerializeToString(info), typeof(ListingsProviderInfo));

            var provider = _listingProviders.FirstOrDefault(i => string.Equals(info.Type, i.Type, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new ResourceNotFoundException();
            }

            await provider.Validate(info, validateLogin, validateListings).ConfigureAwait(false);

            var config = GetConfiguration();

            var index = config.ListingProviders.FindIndex(i => string.Equals(i.Id, info.Id, StringComparison.OrdinalIgnoreCase));

            if (index == -1 || string.IsNullOrWhiteSpace(info.Id))
            {
                info.Id = Guid.NewGuid().ToString("N");
                config.ListingProviders.Add(info);
            }
            else
            {
                config.ListingProviders[index] = info;
            }

            _config.SaveConfiguration("livetv", config);

            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();

            return info;
        }

        public void DeleteListingsProvider(string id)
        {
            var config = GetConfiguration();

            config.ListingProviders = config.ListingProviders.Where(i => !string.Equals(id, i.Id, StringComparison.OrdinalIgnoreCase)).ToList();

            _config.SaveConfiguration("livetv", config);
            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();
        }

        public async Task<TunerChannelMapping> SetChannelMapping(string providerId, string tunerChannelNumber, string providerChannelNumber)
        {
            var config = GetConfiguration();

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

            var mappings = listingsProviderInfo.ChannelMappings.ToList();

            var tunerChannelMappings =
                tunerChannels.Select(i => GetTunerChannelMapping(i, mappings, providerChannels)).ToList();

            _taskManager.CancelIfRunningAndQueue<RefreshChannelsScheduledTask>();

            return tunerChannelMappings.First(i => string.Equals(i.Number, tunerChannelNumber, StringComparison.OrdinalIgnoreCase));
        }

        public TunerChannelMapping GetTunerChannelMapping(ChannelInfo channel, List<NameValuePair> mappings, List<ChannelInfo> providerChannels)
        {
            var result = new TunerChannelMapping
            {
                Name = channel.Number + " " + channel.Name,
                Number = channel.Number
            };

            var mapping = mappings.FirstOrDefault(i => string.Equals(i.Name, channel.Number, StringComparison.OrdinalIgnoreCase));
            var providerChannelNumber = channel.Number;

            if (mapping != null)
            {
                providerChannelNumber = mapping.Value;
            }

            var providerChannel = providerChannels.FirstOrDefault(i => string.Equals(i.Number, providerChannelNumber, StringComparison.OrdinalIgnoreCase));

            if (providerChannel != null)
            {
                result.ProviderChannelNumber = providerChannel.Number;
                result.ProviderChannelName = providerChannel.Name;
            }

            return result;
        }

        public Task<List<NameIdPair>> GetLineups(string providerType, string providerId, string country, string location)
        {
            var config = GetConfiguration();

            if (string.IsNullOrWhiteSpace(providerId))
            {
                var provider = _listingProviders.FirstOrDefault(i => string.Equals(providerType, i.Type, StringComparison.OrdinalIgnoreCase));

                if (provider == null)
                {
                    throw new ResourceNotFoundException();
                }

                return provider.GetLineups(null, country, location);
            }
            else
            {
                var info = config.ListingProviders.FirstOrDefault(i => string.Equals(i.Id, providerId, StringComparison.OrdinalIgnoreCase));

                var provider = _listingProviders.FirstOrDefault(i => string.Equals(info.Type, i.Type, StringComparison.OrdinalIgnoreCase));

                if (provider == null)
                {
                    throw new ResourceNotFoundException();
                }

                return provider.GetLineups(info, country, location);
            }
        }

        public Task<MBRegistrationRecord> GetRegistrationInfo(string channelId, string programId, string feature)
        {
            ILiveTvService service;

            if (string.IsNullOrWhiteSpace(programId))
            {
                var channel = GetInternalChannel(channelId);
                service = GetService(channel);
            }
            else
            {
                var program = GetInternalProgram(programId);
                service = GetService(program);
            }

            var hasRegistration = service as IHasRegistrationInfo;

            if (hasRegistration != null)
            {
                return hasRegistration.GetRegistrationInfo(feature);
            }

            return Task.FromResult(new MBRegistrationRecord
            {
                IsValid = true,
                IsRegistered = true
            });
        }

        public List<NameValuePair> GetSatIniMappings()
        {
            var names = GetType().Assembly.GetManifestResourceNames().Where(i => i.IndexOf("SatIp.ini", StringComparison.OrdinalIgnoreCase) != -1).ToList();

            return names.Select(GetSatIniMappings).Where(i => i != null).DistinctBy(i => i.Value.Split('|')[0]).ToList();
        }

        public NameValuePair GetSatIniMappings(string resource)
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream(resource))
            {
                using (var reader = new StreamReader(stream))
                {
                    var parser = new StreamIniDataParser();
                    IniData data = parser.ReadData(reader);

                    var satType1 = data["SATTYPE"]["1"];
                    var satType2 = data["SATTYPE"]["2"];

                    if (string.IsNullOrWhiteSpace(satType2))
                    {
                        return null;
                    }

                    var srch = "SatIp.ini.";
                    var filename = Path.GetFileName(resource);

                    return new NameValuePair
                    {
                        Name = satType1 + " " + satType2,
                        Value = satType2 + "|" + filename.Substring(filename.IndexOf(srch) + srch.Length)
                    };
                }
            }
        }

        public Task<List<ChannelInfo>> GetSatChannelScanResult(TunerHostInfo info, CancellationToken cancellationToken)
        {
            return new TunerHosts.SatIp.ChannelScan(_logger).Scan(info, cancellationToken);
        }

        public Task<List<ChannelInfo>> GetChannelsForListingsProvider(string id, CancellationToken cancellationToken)
        {
            var info = GetConfiguration().ListingProviders.First(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            return EmbyTV.EmbyTV.Current.GetChannelsForListingsProvider(info, cancellationToken);
        }

        public Task<List<ChannelInfo>> GetChannelsFromListingsProviderData(string id, CancellationToken cancellationToken)
        {
            var info = GetConfiguration().ListingProviders.First(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            var provider = _listingProviders.First(i => string.Equals(i.Type, info.Type, StringComparison.OrdinalIgnoreCase));
            return provider.GetChannels(info, cancellationToken);
        }
    }
}