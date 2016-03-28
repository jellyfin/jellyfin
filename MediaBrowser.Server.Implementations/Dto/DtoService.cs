using MediaBrowser.Common;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Dto
{
    public class DtoService : IDtoService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly IItemRepository _itemRepo;

        private readonly IImageProcessor _imageProcessor;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IProviderManager _providerManager;

        private readonly Func<IChannelManager> _channelManagerFactory;
        private readonly ISyncManager _syncManager;
        private readonly IApplicationHost _appHost;
        private readonly Func<IDeviceManager> _deviceManager;
        private readonly Func<IMediaSourceManager> _mediaSourceManager;
        private readonly Func<ILiveTvManager> _livetvManager;

        public DtoService(ILogger logger, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IImageProcessor imageProcessor, IServerConfigurationManager config, IFileSystem fileSystem, IProviderManager providerManager, Func<IChannelManager> channelManagerFactory, ISyncManager syncManager, IApplicationHost appHost, Func<IDeviceManager> deviceManager, Func<IMediaSourceManager> mediaSourceManager, Func<ILiveTvManager> livetvManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _config = config;
            _fileSystem = fileSystem;
            _providerManager = providerManager;
            _channelManagerFactory = channelManagerFactory;
            _syncManager = syncManager;
            _appHost = appHost;
            _deviceManager = deviceManager;
            _mediaSourceManager = mediaSourceManager;
            _livetvManager = livetvManager;
        }

        /// <summary>
        /// Converts a BaseItem to a DTOBaseItem
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="user">The user.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public BaseItemDto GetBaseItemDto(BaseItem item, List<ItemFields> fields, User user = null, BaseItem owner = null)
        {
            var options = new DtoOptions
            {
                Fields = fields
            };

            return GetBaseItemDto(item, options, user, owner);
        }

        public IEnumerable<BaseItemDto> GetBaseItemDtos(IEnumerable<BaseItem> items, DtoOptions options, User user = null, BaseItem owner = null)
        {
            var syncJobItems = GetSyncedItemProgress(options);
            var syncDictionary = GetSyncedItemProgressDictionary(syncJobItems);

            var list = new List<BaseItemDto>();
            var programTuples = new List<Tuple<BaseItem, BaseItemDto>> { };
            var channelTuples = new List<Tuple<BaseItemDto, LiveTvChannel>> { };

            foreach (var item in items)
            {
                var dto = GetBaseItemDtoInternal(item, options, syncDictionary, user, owner);

                var tvChannel = item as LiveTvChannel;
                if (tvChannel != null)
                {
                    channelTuples.Add(new Tuple<BaseItemDto, LiveTvChannel>(dto, tvChannel));
                }
                else if (item is LiveTvProgram)
                {
                    programTuples.Add(new Tuple<BaseItem, BaseItemDto>(item, dto));
                }

                var byName = item as IItemByName;

                if (byName != null)
                {
                    if (options.Fields.Contains(ItemFields.ItemCounts))
                    {
                        var itemFilter = byName.GetItemFilter();

                        var libraryItems = user != null ?
                           user.RootFolder.GetRecursiveChildren(user, itemFilter) :
                           _libraryManager.RootFolder.GetRecursiveChildren(itemFilter);

                        SetItemByNameInfo(item, dto, libraryItems.ToList(), user);
                    }
                }

                FillSyncInfo(dto, item, syncJobItems, options, user);

                list.Add(dto);
            }

            if (programTuples.Count > 0)
            {
                var task = _livetvManager().AddInfoToProgramDto(programTuples, options.Fields, user);
                Task.WaitAll(task);
            }

            if (channelTuples.Count > 0)
            {
                _livetvManager().AddChannelInfo(channelTuples, options, user);
            }

            return list;
        }

        private Dictionary<string, SyncedItemProgress> GetSyncedItemProgressDictionary(IEnumerable<SyncedItemProgress> items)
        {
            var dict = new Dictionary<string, SyncedItemProgress>();

            foreach (var item in items)
            {
                dict[item.ItemId] = item;
            }

            return dict;
        }

        public BaseItemDto GetBaseItemDto(BaseItem item, DtoOptions options, User user = null, BaseItem owner = null)
        {
            var syncProgress = GetSyncedItemProgress(options);

            var dto = GetBaseItemDtoInternal(item, options, GetSyncedItemProgressDictionary(syncProgress), user, owner);
            var tvChannel = item as LiveTvChannel;
            if (tvChannel != null)
            {
                var list = new List<Tuple<BaseItemDto, LiveTvChannel>> { new Tuple<BaseItemDto, LiveTvChannel>(dto, tvChannel) };
                _livetvManager().AddChannelInfo(list, options, user);
            }
            else if (item is LiveTvProgram)
            {
                var list = new List<Tuple<BaseItem, BaseItemDto>> { new Tuple<BaseItem, BaseItemDto>(item, dto) };
                var task = _livetvManager().AddInfoToProgramDto(list, options.Fields, user);
                Task.WaitAll(task);
            }

            var byName = item as IItemByName;

            if (byName != null)
            {
                if (options.Fields.Contains(ItemFields.ItemCounts))
                {
                    SetItemByNameInfo(item, dto, GetTaggedItems(byName, user), user);
                }

                FillSyncInfo(dto, item, options, user, syncProgress);
                return dto;
            }

            FillSyncInfo(dto, item, options, user, syncProgress);

            return dto;
        }

        private List<BaseItem> GetTaggedItems(IItemByName byName, User user)
        {
            var person = byName as Person;

            if (person != null)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Person = byName.Name

                }, new string[] { });

                return items.ToList();
            }

            var itemFilter = byName.GetItemFilter();

            return user != null ?
               user.RootFolder.GetRecursiveChildren(user, itemFilter).ToList() :
               _libraryManager.RootFolder.GetRecursiveChildren(itemFilter).ToList();
        }

        private SyncedItemProgress[] GetSyncedItemProgress(DtoOptions options)
        {
            if (!options.Fields.Contains(ItemFields.SyncInfo))
            {
                return new SyncedItemProgress[] { };
            }

            var deviceId = options.DeviceId;
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return new SyncedItemProgress[] { };
            }

            var caps = _deviceManager().GetCapabilities(deviceId);
            if (caps == null || !caps.SupportsSync)
            {
                return new SyncedItemProgress[] { };
            }

            return _syncManager.GetSyncedItemProgresses(new SyncJobItemQuery
            {
                TargetId = deviceId,
                Statuses = new[]
                {
                    SyncJobItemStatus.Converting,
                    SyncJobItemStatus.Queued,
                    SyncJobItemStatus.Transferring,
                    SyncJobItemStatus.ReadyToTransfer,
                    SyncJobItemStatus.Synced
                }
            }).Items;
        }

        public void FillSyncInfo(IEnumerable<Tuple<BaseItem, BaseItemDto>> tuples, DtoOptions options, User user)
        {
            if (options.Fields.Contains(ItemFields.SyncInfo))
            {
                var syncProgress = GetSyncedItemProgress(options);

                foreach (var tuple in tuples)
                {
                    var item = tuple.Item1;

                    FillSyncInfo(tuple.Item2, item, syncProgress, options, user);
                }
            }
        }

        private void FillSyncInfo(IHasSyncInfo dto, BaseItem item, DtoOptions options, User user, SyncedItemProgress[] syncProgress)
        {
            if (options.Fields.Contains(ItemFields.SyncInfo))
            {
                var userCanSync = user != null && user.Policy.EnableSync;
                dto.SupportsSync = userCanSync && _syncManager.SupportsSync(item);
            }

            if (dto.SupportsSync ?? false)
            {
                dto.HasSyncJob = syncProgress.Any(i => i.Status != SyncJobItemStatus.Synced && string.Equals(i.ItemId, dto.Id, StringComparison.OrdinalIgnoreCase));
                dto.IsSynced = syncProgress.Any(i => i.Status == SyncJobItemStatus.Synced && string.Equals(i.ItemId, dto.Id, StringComparison.OrdinalIgnoreCase));

                if (dto.IsSynced.Value)
                {
                    dto.SyncStatus = SyncJobItemStatus.Synced;
                }

                else if (dto.HasSyncJob.Value)
                {
                    dto.SyncStatus = SyncJobItemStatus.Queued;
                }
            }
        }

        private void FillSyncInfo(IHasSyncInfo dto, BaseItem item, SyncedItemProgress[] syncProgress, DtoOptions options, User user)
        {
            if (options.Fields.Contains(ItemFields.SyncInfo))
            {
                var userCanSync = user != null && user.Policy.EnableSync;
                dto.SupportsSync = userCanSync && _syncManager.SupportsSync(item);
            }

            if (dto.SupportsSync ?? false)
            {
                dto.HasSyncJob = syncProgress.Any(i => i.Status != SyncJobItemStatus.Synced && string.Equals(i.ItemId, dto.Id, StringComparison.OrdinalIgnoreCase));
                dto.IsSynced = syncProgress.Any(i => i.Status == SyncJobItemStatus.Synced && string.Equals(i.ItemId, dto.Id, StringComparison.OrdinalIgnoreCase));

                if (dto.IsSynced.Value)
                {
                    dto.SyncStatus = SyncJobItemStatus.Synced;
                }

                else if (dto.HasSyncJob.Value)
                {
                    dto.SyncStatus = SyncJobItemStatus.Queued;
                }
            }
        }

        private BaseItemDto GetBaseItemDtoInternal(BaseItem item, DtoOptions options, Dictionary<string, SyncedItemProgress> syncProgress, User user = null, BaseItem owner = null)
        {
            var fields = options.Fields;

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (fields == null)
            {
                throw new ArgumentNullException("fields");
            }

            var dto = new BaseItemDto
            {
                ServerId = _appHost.SystemId
            };

            if (item.SourceType == SourceType.Channel)
            {
                dto.SourceType = item.SourceType.ToString();
            }

            if (fields.Contains(ItemFields.People))
            {
                AttachPeople(dto, item);
            }

            if (fields.Contains(ItemFields.PrimaryImageAspectRatio))
            {
                try
                {
                    AttachPrimaryImageAspectRatio(dto, item);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, item.Name);
                }
            }

            if (fields.Contains(ItemFields.DisplayPreferencesId))
            {
                dto.DisplayPreferencesId = item.DisplayPreferencesId.ToString("N");
            }

            if (user != null)
            {
                AttachUserSpecificInfo(dto, item, user, fields, syncProgress);
            }

            var hasMediaSources = item as IHasMediaSources;
            if (hasMediaSources != null)
            {
                if (fields.Contains(ItemFields.MediaSources))
                {
                    if (user == null)
                    {
                        dto.MediaSources = _mediaSourceManager().GetStaticMediaSources(hasMediaSources, true).ToList();
                    }
                    else
                    {
                        dto.MediaSources = _mediaSourceManager().GetStaticMediaSources(hasMediaSources, true, user).ToList();
                    }
                }
            }

            if (fields.Contains(ItemFields.Studios))
            {
                AttachStudios(dto, item);
            }

            AttachBasicFields(dto, item, owner, options);

            var collectionFolder = item as ICollectionFolder;
            if (collectionFolder != null)
            {
                dto.OriginalCollectionType = collectionFolder.CollectionType;

                dto.CollectionType = user == null ?
                    collectionFolder.CollectionType :
                    collectionFolder.GetViewType(user);
            }

            var playlist = item as Playlist;
            if (playlist != null)
            {
                AttachLinkedChildImages(dto, playlist, user, options);
            }

            if (fields.Contains(ItemFields.CanDelete))
            {
                dto.CanDelete = user == null
                    ? item.CanDelete()
                    : item.CanDelete(user);
            }

            if (fields.Contains(ItemFields.CanDownload))
            {
                dto.CanDownload = user == null
                    ? item.CanDownload()
                    : item.CanDownload(user);
            }

            if (fields.Contains(ItemFields.Etag))
            {
                dto.Etag = item.GetEtag(user);
            }

            if (item is ILiveTvRecording)
            {
                _livetvManager().AddInfoToRecordingDto(item, dto, user);
            }

            return dto;
        }

        public BaseItemDto GetItemByNameDto(BaseItem item, DtoOptions options, List<BaseItem> taggedItems, User user = null)
        {
            var syncProgress = GetSyncedItemProgress(options);

            var dto = GetBaseItemDtoInternal(item, options, GetSyncedItemProgressDictionary(syncProgress), user);

            if (options.Fields.Contains(ItemFields.ItemCounts))
            {
                SetItemByNameInfo(item, dto, taggedItems, user);
            }

            FillSyncInfo(dto, item, options, user, syncProgress);

            return dto;
        }

        private void SetItemByNameInfo(BaseItem item, BaseItemDto dto, List<BaseItem> taggedItems, User user = null)
        {
            if (item is MusicArtist || item is MusicGenre)
            {
                dto.AlbumCount = taggedItems.Count(i => i is MusicAlbum);
                dto.MusicVideoCount = taggedItems.Count(i => i is MusicVideo);
                dto.SongCount = taggedItems.Count(i => i is Audio);
            }
            else if (item is GameGenre)
            {
                dto.GameCount = taggedItems.Count(i => i is Game);
            }
            else
            {
                // This populates them all and covers Genre, Person, Studio, Year

                dto.AlbumCount = taggedItems.Count(i => i is MusicAlbum);
                dto.EpisodeCount = taggedItems.Count(i => i is Episode);
                dto.GameCount = taggedItems.Count(i => i is Game);
                dto.MovieCount = taggedItems.Count(i => i is Movie);
                dto.TrailerCount = taggedItems.Count(i => i is Trailer);
                dto.MusicVideoCount = taggedItems.Count(i => i is MusicVideo);
                dto.SeriesCount = taggedItems.Count(i => i is Series);
                dto.SongCount = taggedItems.Count(i => i is Audio);
            }

            dto.ChildCount = taggedItems.Count;
        }

        /// <summary>
        /// Attaches the user specific info.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="syncProgress">The synchronize progress.</param>
        private void AttachUserSpecificInfo(BaseItemDto dto, BaseItem item, User user, List<ItemFields> fields, Dictionary<string, SyncedItemProgress> syncProgress)
        {
            if (item.IsFolder)
            {
                var userData = _userDataRepository.GetUserData(user.Id, item.GetUserDataKey());

                // Skip the user data manager because we've already looped through the recursive tree and don't want to do it twice
                // TODO: Improve in future
                dto.UserData = GetUserItemDataDto(userData);

                var folder = (Folder)item;

                if (item.SourceType == SourceType.Library)
                {
                    dto.ChildCount = GetChildCount(folder, user);

                    // These are just far too slow. 
                    if (!(folder is UserRootFolder) && !(folder is UserView) && !(folder is ICollectionFolder))
                    {
                        SetSpecialCounts(folder, user, dto, fields, syncProgress);
                    }
                }

                dto.UserData.Played = dto.UserData.PlayedPercentage.HasValue && dto.UserData.PlayedPercentage.Value >= 100;
            }

            else
            {
                dto.UserData = _userDataRepository.GetUserDataDto(item, user);
            }

            dto.PlayAccess = item.GetPlayAccess(user);

            if (fields.Contains(ItemFields.SeasonUserData))
            {
                var episode = item as Episode;

                if (episode != null)
                {
                    var season = episode.Season;

                    if (season != null)
                    {
                        dto.SeasonUserData = _userDataRepository.GetUserDataDto(season, user);
                    }
                }
            }

            var userView = item as UserView;
            if (userView != null)
            {
                dto.HasDynamicCategories = userView.ContainsDynamicCategories(user);
            }

            var collectionFolder = item as ICollectionFolder;
            if (collectionFolder != null)
            {
                dto.HasDynamicCategories = false;
            }
        }

        private int GetChildCount(Folder folder, User user)
        {
            return folder.GetChildren(user, true)
                .Count();
        }

        /// <summary>
        /// Gets client-side Id of a server-side BaseItem
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetDtoId(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return item.Id.ToString("N");
        }

        /// <summary>
        /// Converts a UserItemData to a DTOUserItemData
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>DtoUserItemData.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public UserItemDataDto GetUserItemDataDto(UserItemData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            return new UserItemDataDto
            {
                IsFavorite = data.IsFavorite,
                Likes = data.Likes,
                PlaybackPositionTicks = data.PlaybackPositionTicks,
                PlayCount = data.PlayCount,
                Rating = data.Rating,
                Played = data.Played,
                LastPlayedDate = data.LastPlayedDate,
                Key = data.Key
            };
        }
        private void SetBookProperties(BaseItemDto dto, Book item)
        {
            dto.SeriesName = item.SeriesName;
        }
        private void SetPhotoProperties(BaseItemDto dto, Photo item)
        {
            dto.Width = item.Width;
            dto.Height = item.Height;
            dto.CameraMake = item.CameraMake;
            dto.CameraModel = item.CameraModel;
            dto.Software = item.Software;
            dto.ExposureTime = item.ExposureTime;
            dto.FocalLength = item.FocalLength;
            dto.ImageOrientation = item.Orientation;
            dto.Aperture = item.Aperture;
            dto.ShutterSpeed = item.ShutterSpeed;

            dto.Latitude = item.Latitude;
            dto.Longitude = item.Longitude;
            dto.Altitude = item.Altitude;
            dto.IsoSpeedRating = item.IsoSpeedRating;

            var album = item.Album;

            if (album != null)
            {
                dto.Album = album.Name;
                dto.AlbumId = album.Id.ToString("N");
            }
        }

        private void SetMusicVideoProperties(BaseItemDto dto, MusicVideo item)
        {
            if (!string.IsNullOrEmpty(item.Album))
            {
                var parentAlbum = _libraryManager.RootFolder
                    .GetRecursiveChildren(i => i is MusicAlbum && string.Equals(i.Name, item.Album, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (parentAlbum != null)
                {
                    dto.AlbumId = GetDtoId(parentAlbum);
                }
            }

            dto.Album = item.Album;
        }

        private void SetGameProperties(BaseItemDto dto, Game item)
        {
            dto.Players = item.PlayersSupported;
            dto.GameSystem = item.GameSystem;
            dto.MultiPartGameFiles = item.MultiPartGameFiles;
        }

        private void SetGameSystemProperties(BaseItemDto dto, GameSystem item)
        {
            dto.GameSystem = item.GameSystemName;
        }

        private List<string> GetBackdropImageTags(BaseItem item, int limit)
        {
            return GetCacheTags(item, ImageType.Backdrop, limit).ToList();
        }

        private List<string> GetScreenshotImageTags(BaseItem item, int limit)
        {
            var hasScreenshots = item as IHasScreenshots;
            if (hasScreenshots == null)
            {
                return new List<string>();
            }
            return GetCacheTags(item, ImageType.Screenshot, limit).ToList();
        }

        private IEnumerable<string> GetCacheTags(BaseItem item, ImageType type, int limit)
        {
            return item.GetImages(type)
                // Convert to a list now in case GetImageCacheTag is slow
                .ToList()
                .Select(p => GetImageCacheTag(item, p))
                .Where(i => i != null)
                .Take(limit)
                .ToList();
        }

        private string GetImageCacheTag(BaseItem item, ImageType type)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, type);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting {0} image info", ex, type);
                return null;
            }
        }

        private string GetImageCacheTag(BaseItem item, ItemImageInfo image)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, image);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting {0} image info for {1}", ex, image.Type, image.Path);
                return null;
            }
        }

        /// <summary>
        /// Attaches People DTO's to a DTOBaseItem
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        private void AttachPeople(BaseItemDto dto, BaseItem item)
        {
            // Ordering by person type to ensure actors and artists are at the front.
            // This is taking advantage of the fact that they both begin with A
            // This should be improved in the future
            var people = _libraryManager.GetPeople(item).OrderBy(i => i.SortOrder ?? int.MaxValue)
                .ThenBy(i =>
                {
                    if (i.IsType(PersonType.Actor))
                    {
                        return 0;
                    }
                    if (i.IsType(PersonType.GuestStar))
                    {
                        return 1;
                    }
                    if (i.IsType(PersonType.Director))
                    {
                        return 2;
                    }
                    if (i.IsType(PersonType.Writer))
                    {
                        return 3;
                    }
                    if (i.IsType(PersonType.Producer))
                    {
                        return 4;
                    }
                    if (i.IsType(PersonType.Composer))
                    {
                        return 4;
                    }

                    return 10;
                })
                .ToList();

            var list = new List<BaseItemPerson>();

            var dictionary = people.Select(p => p.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Select(c =>
                {
                    try
                    {
                        return _libraryManager.GetPerson(c);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting person {0}", ex, c);
                        return null;
                    }

                }).Where(i => i != null)
                .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];

                var baseItemPerson = new BaseItemPerson
                {
                    Name = person.Name,
                    Role = person.Role,
                    Type = person.Type
                };

                Person entity;

                if (dictionary.TryGetValue(person.Name, out entity))
                {
                    baseItemPerson.PrimaryImageTag = GetImageCacheTag(entity, ImageType.Primary);
                    baseItemPerson.Id = entity.Id.ToString("N");
                    list.Add(baseItemPerson);
                }
            }

            dto.People = list.ToArray();
        }

        /// <summary>
        /// Attaches the studios.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        private void AttachStudios(BaseItemDto dto, BaseItem item)
        {
            var studios = item.Studios.ToList();

            dto.Studios = new StudioDto[studios.Count];

            var dictionary = studios.Distinct(StringComparer.OrdinalIgnoreCase).Select(name =>
            {
                try
                {
                    return _libraryManager.GetStudio(name);
                }
                catch (IOException ex)
                {
                    _logger.ErrorException("Error getting studio {0}", ex, name);
                    return null;
                }
            })
            .Where(i => i != null)
            .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < studios.Count; i++)
            {
                var studio = studios[i];

                var studioDto = new StudioDto
                {
                    Name = studio
                };

                Studio entity;

                if (dictionary.TryGetValue(studio, out entity))
                {
                    studioDto.Id = entity.Id.ToString("N");
                    studioDto.PrimaryImageTag = GetImageCacheTag(entity, ImageType.Primary);
                }

                dto.Studios[i] = studioDto;
            }
        }

        /// <summary>
        /// If an item does not any backdrops, this can be used to find the first parent that does have one
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem GetParentBackdropItem(BaseItem item, BaseItem owner)
        {
            var parent = item.GetParent() ?? owner;

            while (parent != null)
            {
                if (parent.GetImages(ImageType.Backdrop).Any())
                {
                    return parent;
                }

                parent = parent.GetParent();
            }

            return null;
        }

        /// <summary>
        /// If an item does not have a logo, this can be used to find the first parent that does have one
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem GetParentImageItem(BaseItem item, ImageType type, BaseItem owner)
        {
            var parent = item.GetParent() ?? owner;

            while (parent != null)
            {
                if (parent.HasImage(type))
                {
                    return parent;
                }

                parent = parent.GetParent();
            }

            return null;
        }

        /// <summary>
        /// Gets the chapter info dto.
        /// </summary>
        /// <param name="chapterInfo">The chapter info.</param>
        /// <param name="item">The item.</param>
        /// <returns>ChapterInfoDto.</returns>
        private ChapterInfoDto GetChapterInfoDto(ChapterInfo chapterInfo, BaseItem item)
        {
            var dto = new ChapterInfoDto
            {
                Name = chapterInfo.Name,
                StartPositionTicks = chapterInfo.StartPositionTicks
            };

            if (!string.IsNullOrEmpty(chapterInfo.ImagePath))
            {
                dto.ImageTag = GetImageCacheTag(item, new ItemImageInfo
                {
                    Path = chapterInfo.ImagePath,
                    Type = ImageType.Chapter,
                    DateModified = _fileSystem.GetLastWriteTimeUtc(chapterInfo.ImagePath)
                });
            }

            return dto;
        }

        public List<ChapterInfoDto> GetChapterInfoDtos(BaseItem item)
        {
            return _itemRepo.GetChapters(item.Id)
                .Select(c => GetChapterInfoDto(c, item))
                .ToList();
        }

        /// <summary>
        /// Sets simple property values on a DTOBaseItem
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="options">The options.</param>
        private void AttachBasicFields(BaseItemDto dto, BaseItem item, BaseItem owner, DtoOptions options)
        {
            var fields = options.Fields;

            if (fields.Contains(ItemFields.DateCreated))
            {
                dto.DateCreated = item.DateCreated;
            }

            if (fields.Contains(ItemFields.DisplayMediaType))
            {
                dto.DisplayMediaType = item.DisplayMediaType;
            }

            if (fields.Contains(ItemFields.Settings))
            {
                dto.LockedFields = item.LockedFields;
                dto.LockData = item.IsLocked;
                dto.ForcedSortName = item.ForcedSortName;
            }

            var hasBudget = item as IHasBudget;
            if (hasBudget != null)
            {
                if (fields.Contains(ItemFields.Budget))
                {
                    dto.Budget = hasBudget.Budget;
                }

                if (fields.Contains(ItemFields.Revenue))
                {
                    dto.Revenue = hasBudget.Revenue;
                }
            }

            dto.EndDate = item.EndDate;

            if (fields.Contains(ItemFields.HomePageUrl))
            {
                dto.HomePageUrl = item.HomePageUrl;
            }

            if (fields.Contains(ItemFields.ExternalUrls))
            {
                dto.ExternalUrls = _providerManager.GetExternalUrls(item).ToArray();
            }

            if (fields.Contains(ItemFields.Tags))
            {
                var hasTags = item as IHasTags;
                if (hasTags != null)
                {
                    dto.Tags = hasTags.Tags;
                }

                if (dto.Tags == null)
                {
                    dto.Tags = new List<string>();
                }
            }

            if (fields.Contains(ItemFields.Keywords))
            {
                var hasTags = item as IHasKeywords;
                if (hasTags != null)
                {
                    dto.Keywords = hasTags.Keywords;
                }

                if (dto.Keywords == null)
                {
                    dto.Keywords = new List<string>();
                }
            }

            if (fields.Contains(ItemFields.ProductionLocations))
            {
                SetProductionLocations(item, dto);
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                dto.AspectRatio = hasAspectRatio.AspectRatio;
            }

            if (fields.Contains(ItemFields.Metascore))
            {
                var hasMetascore = item as IHasMetascore;
                if (hasMetascore != null)
                {
                    dto.Metascore = hasMetascore.Metascore;
                }
            }

            if (fields.Contains(ItemFields.AwardSummary))
            {
                var hasAwards = item as IHasAwards;
                if (hasAwards != null)
                {
                    dto.AwardSummary = hasAwards.AwardSummary;
                }
            }

            var backdropLimit = options.GetImageLimit(ImageType.Backdrop);
            if (backdropLimit > 0)
            {
                dto.BackdropImageTags = GetBackdropImageTags(item, backdropLimit);
            }

            if (fields.Contains(ItemFields.ScreenshotImageTags))
            {
                var screenshotLimit = options.GetImageLimit(ImageType.Screenshot);
                if (screenshotLimit > 0)
                {
                    dto.ScreenshotImageTags = GetScreenshotImageTags(item, screenshotLimit);
                }
            }

            if (fields.Contains(ItemFields.Genres))
            {
                dto.Genres = item.Genres;
            }

            dto.ImageTags = new Dictionary<ImageType, string>();

            // Prevent implicitly captured closure
            var currentItem = item;
            foreach (var image in currentItem.ImageInfos.Where(i => !currentItem.AllowsMultipleImages(i.Type))
                .ToList())
            {
                if (options.GetImageLimit(image.Type) > 0)
                {
                    var tag = GetImageCacheTag(item, image);

                    if (tag != null)
                    {
                        dto.ImageTags[image.Type] = tag;
                    }
                }
            }

            dto.Id = GetDtoId(item);
            dto.IndexNumber = item.IndexNumber;
            dto.IsFolder = item.IsFolder;
            dto.MediaType = item.MediaType;
            dto.LocationType = item.LocationType;
            if (item.IsHD.HasValue && item.IsHD.Value)
            {
                dto.IsHD = item.IsHD;
            }
            dto.Audio = item.Audio;

            dto.PreferredMetadataCountryCode = item.PreferredMetadataCountryCode;
            dto.PreferredMetadataLanguage = item.PreferredMetadataLanguage;

            var hasCriticRating = item as IHasCriticRating;
            if (hasCriticRating != null)
            {
                dto.CriticRating = hasCriticRating.CriticRating;

                if (fields.Contains(ItemFields.CriticRatingSummary))
                {
                    dto.CriticRatingSummary = hasCriticRating.CriticRatingSummary;
                }
            }

            var hasTrailers = item as IHasTrailers;
            if (hasTrailers != null)
            {
                dto.LocalTrailerCount = hasTrailers.GetTrailerIds().Count;
            }

            var hasDisplayOrder = item as IHasDisplayOrder;
            if (hasDisplayOrder != null)
            {
                dto.DisplayOrder = hasDisplayOrder.DisplayOrder;
            }

            var userView = item as UserView;
            if (userView != null)
            {
                dto.CollectionType = userView.ViewType;
            }

            if (fields.Contains(ItemFields.RemoteTrailers))
            {
                dto.RemoteTrailers = hasTrailers != null ?
                    hasTrailers.RemoteTrailers :
                    new List<MediaUrl>();
            }

            dto.Name = item.Name;
            dto.OfficialRating = item.OfficialRating;

            if (fields.Contains(ItemFields.Overview))
            {
                dto.Overview = item.Overview;
            }

            if (fields.Contains(ItemFields.ShortOverview))
            {
                var hasShortOverview = item as IHasShortOverview;
                if (hasShortOverview != null)
                {
                    dto.ShortOverview = hasShortOverview.ShortOverview;
                }
            }

            // If there are no backdrops, indicate what parent has them in case the Ui wants to allow inheritance
            if (backdropLimit > 0 && dto.BackdropImageTags.Count == 0)
            {
                var parentWithBackdrop = GetParentBackdropItem(item, owner);

                if (parentWithBackdrop != null)
                {
                    dto.ParentBackdropItemId = GetDtoId(parentWithBackdrop);
                    dto.ParentBackdropImageTags = GetBackdropImageTags(parentWithBackdrop, backdropLimit);
                }
            }

            if (fields.Contains(ItemFields.ParentId))
            {
                var displayParent = item.DisplayParent;
                if (displayParent != null)
                {
                    dto.ParentId = GetDtoId(displayParent);
                }
            }

            dto.ParentIndexNumber = item.ParentIndexNumber;

            // If there is no logo, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasLogo && options.GetImageLimit(ImageType.Logo) > 0)
            {
                var parentWithLogo = GetParentImageItem(item, ImageType.Logo, owner);

                if (parentWithLogo != null)
                {
                    dto.ParentLogoItemId = GetDtoId(parentWithLogo);

                    dto.ParentLogoImageTag = GetImageCacheTag(parentWithLogo, ImageType.Logo);
                }
            }

            // If there is no art, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasArtImage && options.GetImageLimit(ImageType.Art) > 0)
            {
                var parentWithImage = GetParentImageItem(item, ImageType.Art, owner);

                if (parentWithImage != null)
                {
                    dto.ParentArtItemId = GetDtoId(parentWithImage);

                    dto.ParentArtImageTag = GetImageCacheTag(parentWithImage, ImageType.Art);
                }
            }

            // If there is no thumb, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasThumb && options.GetImageLimit(ImageType.Thumb) > 0)
            {
                var parentWithImage = GetParentImageItem(item, ImageType.Thumb, owner);

                if (parentWithImage != null)
                {
                    dto.ParentThumbItemId = GetDtoId(parentWithImage);

                    dto.ParentThumbImageTag = GetImageCacheTag(parentWithImage, ImageType.Thumb);
                }
            }

            if (fields.Contains(ItemFields.Path))
            {
                dto.Path = GetMappedPath(item);
            }

            dto.PremiereDate = item.PremiereDate;
            dto.ProductionYear = item.ProductionYear;

            if (fields.Contains(ItemFields.ProviderIds))
            {
                dto.ProviderIds = item.ProviderIds;
            }

            dto.RunTimeTicks = item.RunTimeTicks;

            if (fields.Contains(ItemFields.SortName))
            {
                dto.SortName = item.SortName;
            }

            if (fields.Contains(ItemFields.CustomRating))
            {
                dto.CustomRating = item.CustomRating;
            }

            if (fields.Contains(ItemFields.Taglines))
            {
                var hasTagline = item as IHasTaglines;
                if (hasTagline != null)
                {
                    dto.Taglines = hasTagline.Taglines;
                }

                if (dto.Taglines == null)
                {
                    dto.Taglines = new List<string>();
                }
            }

            dto.Type = item.GetClientTypeName();
            dto.CommunityRating = item.CommunityRating;

            if (fields.Contains(ItemFields.VoteCount))
            {
                dto.VoteCount = item.VoteCount;
            }

            //if (item.IsFolder)
            //{
            //    var folder = (Folder)item;

            //    if (fields.Contains(ItemFields.IndexOptions))
            //    {
            //        dto.IndexOptions = folder.IndexByOptionStrings.ToArray();
            //    }
            //}

            var supportsPlaceHolders = item as ISupportsPlaceHolders;
            if (supportsPlaceHolders != null)
            {
                dto.IsPlaceHolder = supportsPlaceHolders.IsPlaceHolder;
            }

            // Add audio info
            var audio = item as Audio;
            if (audio != null)
            {
                dto.Album = audio.Album;
                dto.ExtraType = audio.ExtraType;

                var albumParent = audio.AlbumEntity;

                if (albumParent != null)
                {
                    dto.AlbumId = GetDtoId(albumParent);

                    dto.AlbumPrimaryImageTag = GetImageCacheTag(albumParent, ImageType.Primary);
                }

                //if (fields.Contains(ItemFields.MediaSourceCount))
                //{
                // Songs always have one
                //}
            }

            var hasArtist = item as IHasArtist;
            if (hasArtist != null)
            {
                dto.Artists = hasArtist.Artists;

                dto.ArtistItems = hasArtist
                    .Artists
                    .Select(i =>
                    {
                        try
                        {
                            var artist = _libraryManager.GetArtist(i);
                            return new NameIdPair
                            {
                                Name = artist.Name,
                                Id = artist.Id.ToString("N")
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting artist", ex);
                            return null;
                        }
                    })
                    .Where(i => i != null)
                    .ToList();
            }

            var hasAlbumArtist = item as IHasAlbumArtist;
            if (hasAlbumArtist != null)
            {
                dto.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();

                dto.AlbumArtists = hasAlbumArtist
                    .AlbumArtists
                    .Select(i =>
                    {
                        try
                        {
                            var artist = _libraryManager.GetArtist(i);
                            return new NameIdPair
                            {
                                Name = artist.Name,
                                Id = artist.Id.ToString("N")
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting album artist", ex);
                            return null;
                        }
                    })
                    .Where(i => i != null)
                    .ToList();
            }

            // Add video info
            var video = item as Video;
            if (video != null)
            {
                dto.VideoType = video.VideoType;
                dto.Video3DFormat = video.Video3DFormat;
                dto.IsoType = video.IsoType;

                if (video.HasSubtitles)
                {
                    dto.HasSubtitles = video.HasSubtitles;
                }

                if (video.AdditionalParts.Count != 0)
                {
                    dto.PartCount = video.AdditionalParts.Count + 1;
                }

                if (fields.Contains(ItemFields.MediaSourceCount))
                {
                    if (video.MediaSourceCount != 1)
                    {
                        dto.MediaSourceCount = video.MediaSourceCount;
                    }
                }

                if (fields.Contains(ItemFields.Chapters))
                {
                    dto.Chapters = GetChapterInfoDtos(item);
                }

                dto.ExtraType = video.ExtraType;
            }

            if (fields.Contains(ItemFields.MediaStreams))
            {
                // Add VideoInfo
                var iHasMediaSources = item as IHasMediaSources;

                if (iHasMediaSources != null)
                {
                    List<MediaStream> mediaStreams;

                    if (dto.MediaSources != null && dto.MediaSources.Count > 0)
                    {
                        mediaStreams = dto.MediaSources.Where(i => new Guid(i.Id) == item.Id)
                            .SelectMany(i => i.MediaStreams)
                            .ToList();
                    }
                    else
                    {
                        mediaStreams = _mediaSourceManager().GetStaticMediaSources(iHasMediaSources, true).First().MediaStreams;
                    }

                    dto.MediaStreams = mediaStreams;
                }
            }

            var hasSpecialFeatures = item as IHasSpecialFeatures;
            if (hasSpecialFeatures != null)
            {
                var specialFeatureCount = hasSpecialFeatures.SpecialFeatureIds.Count;

                if (specialFeatureCount > 0)
                {
                    dto.SpecialFeatureCount = specialFeatureCount;
                }
            }

            // Add EpisodeInfo
            var episode = item as Episode;
            if (episode != null)
            {
                dto.IndexNumberEnd = episode.IndexNumberEnd;

                if (fields.Contains(ItemFields.AlternateEpisodeNumbers))
                {
                    dto.DvdSeasonNumber = episode.DvdSeasonNumber;
                    dto.DvdEpisodeNumber = episode.DvdEpisodeNumber;
                    dto.AbsoluteEpisodeNumber = episode.AbsoluteEpisodeNumber;
                }

                if (fields.Contains(ItemFields.SpecialEpisodeNumbers))
                {
                    dto.AirsAfterSeasonNumber = episode.AirsAfterSeasonNumber;
                    dto.AirsBeforeEpisodeNumber = episode.AirsBeforeEpisodeNumber;
                    dto.AirsBeforeSeasonNumber = episode.AirsBeforeSeasonNumber;
                }

                var episodeSeason = episode.Season;
                if (episodeSeason != null)
                {
                    dto.SeasonId = episodeSeason.Id.ToString("N");

                    if (fields.Contains(ItemFields.SeasonName))
                    {
                        dto.SeasonName = episodeSeason.Name;
                    }
                }

                if (fields.Contains(ItemFields.SeriesGenres))
                {
                    var episodeseries = episode.Series;
                    if (episodeseries != null)
                    {
                        dto.SeriesGenres = episodeseries.Genres.ToList();
                    }
                }
            }

            // Add SeriesInfo
            var series = item as Series;
            if (series != null)
            {
                dto.AirDays = series.AirDays;
                dto.AirTime = series.AirTime;
                dto.SeriesStatus = series.Status;

                if (fields.Contains(ItemFields.Settings))
                {
                    dto.DisplaySpecialsWithSeasons = series.DisplaySpecialsWithSeasons;
                }

                dto.AnimeSeriesIndex = series.AnimeSeriesIndex;
            }

            if (episode != null)
            {
                series = episode.Series;

                if (series != null)
                {
                    dto.SeriesId = GetDtoId(series);
                    dto.SeriesName = series.Name;

                    if (fields.Contains(ItemFields.AirTime))
                    {
                        dto.AirTime = series.AirTime;
                    }

                    if (options.GetImageLimit(ImageType.Thumb) > 0)
                    {
                        dto.SeriesThumbImageTag = GetImageCacheTag(series, ImageType.Thumb);
                    }

                    if (options.GetImageLimit(ImageType.Primary) > 0)
                    {
                        dto.SeriesPrimaryImageTag = GetImageCacheTag(series, ImageType.Primary);
                    }

                    if (fields.Contains(ItemFields.SeriesStudio))
                    {
                        dto.SeriesStudio = series.Studios.FirstOrDefault();
                    }
                }
            }

            // Add SeasonInfo
            var season = item as Season;
            if (season != null)
            {
                series = season.Series;

                if (series != null)
                {
                    dto.SeriesId = GetDtoId(series);
                    dto.SeriesName = series.Name;
                    dto.AirTime = series.AirTime;
                    dto.SeriesStudio = series.Studios.FirstOrDefault();

                    if (options.GetImageLimit(ImageType.Primary) > 0)
                    {
                        dto.SeriesPrimaryImageTag = GetImageCacheTag(series, ImageType.Primary);
                    }
                }
            }

            var game = item as Game;

            if (game != null)
            {
                SetGameProperties(dto, game);
            }

            var gameSystem = item as GameSystem;

            if (gameSystem != null)
            {
                SetGameSystemProperties(dto, gameSystem);
            }

            var musicVideo = item as MusicVideo;
            if (musicVideo != null)
            {
                SetMusicVideoProperties(dto, musicVideo);
            }

            var book = item as Book;
            if (book != null)
            {
                SetBookProperties(dto, book);
            }

            var photo = item as Photo;
            if (photo != null)
            {
                SetPhotoProperties(dto, photo);
            }

            dto.ChannelId = item.ChannelId;

            if (item.SourceType == SourceType.Channel && !string.IsNullOrWhiteSpace(item.ChannelId))
            {
                var channel = _libraryManager.GetItemById(item.ChannelId);
                if (channel != null)
                {
                    dto.ChannelName = channel.Name;
                }
            }
        }

        private void AttachLinkedChildImages(BaseItemDto dto, Folder folder, User user, DtoOptions options)
        {
            List<BaseItem> linkedChildren = null;

            var backdropLimit = options.GetImageLimit(ImageType.Backdrop);

            if (backdropLimit > 0 && dto.BackdropImageTags.Count == 0)
            {
                linkedChildren = user == null
                    ? folder.GetRecursiveChildren().ToList()
                    : folder.GetRecursiveChildren(user).ToList();

                var parentWithBackdrop = linkedChildren.FirstOrDefault(i => i.GetImages(ImageType.Backdrop).Any());

                if (parentWithBackdrop != null)
                {
                    dto.ParentBackdropItemId = GetDtoId(parentWithBackdrop);
                    dto.ParentBackdropImageTags = GetBackdropImageTags(parentWithBackdrop, backdropLimit);
                }
            }

            if (!dto.ImageTags.ContainsKey(ImageType.Primary) && options.GetImageLimit(ImageType.Primary) > 0)
            {
                if (linkedChildren == null)
                {
                    linkedChildren = user == null
                        ? folder.GetRecursiveChildren().ToList()
                        : folder.GetRecursiveChildren(user).ToList();
                }
                var parentWithImage = linkedChildren.FirstOrDefault(i => i.GetImages(ImageType.Primary).Any());

                if (parentWithImage != null)
                {
                    dto.ParentPrimaryImageItemId = GetDtoId(parentWithImage);
                    dto.ParentPrimaryImageTag = GetImageCacheTag(parentWithImage, ImageType.Primary);
                }
            }
        }

        private string GetMappedPath(IHasMetadata item)
        {
            var path = item.Path;

            var locationType = item.LocationType;

            if (locationType == LocationType.FileSystem || locationType == LocationType.Offline)
            {
                foreach (var map in _config.Configuration.PathSubstitutions)
                {
                    path = _libraryManager.SubstitutePath(path, map.From, map.To);
                }
            }

            return path;
        }

        private void SetProductionLocations(BaseItem item, BaseItemDto dto)
        {
            var hasProductionLocations = item as IHasProductionLocations;

            if (hasProductionLocations != null)
            {
                dto.ProductionLocations = hasProductionLocations.ProductionLocations;
            }

            var person = item as Person;
            if (person != null)
            {
                dto.ProductionLocations = new List<string>();
                if (!string.IsNullOrEmpty(person.PlaceOfBirth))
                {
                    dto.ProductionLocations.Add(person.PlaceOfBirth);
                }
            }

            if (dto.ProductionLocations == null)
            {
                dto.ProductionLocations = new List<string>();
            }
        }

        /// <summary>
        /// Since it can be slow to make all of these calculations independently, this method will provide a way to do them all at once
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="user">The user.</param>
        /// <param name="dto">The dto.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="syncProgress">The synchronize progress.</param>
        /// <returns>Task.</returns>
        private void SetSpecialCounts(Folder folder, User user, BaseItemDto dto, List<ItemFields> fields, Dictionary<string, SyncedItemProgress> syncProgress)
        {
            var recursiveItemCount = 0;
            var unplayed = 0;
            long runtime = 0;

            DateTime? dateLastMediaAdded = null;
            double totalPercentPlayed = 0;
            double totalSyncPercent = 0;
            var addSyncInfo = fields.Contains(ItemFields.SyncInfo);

            var children = folder.GetItems(new InternalItemsQuery
            {
                IsFolder = false,
                Recursive = true,
                ExcludeLocationTypes = new[] { LocationType.Virtual },
                User = user

            }).Result.Items;

            // Loop through each recursive child
            foreach (var child in children)
            {
                if (!dateLastMediaAdded.HasValue)
                {
                    dateLastMediaAdded = child.DateCreated;
                }
                else
                {
                    dateLastMediaAdded = new[] { dateLastMediaAdded.Value, child.DateCreated }.Max();
                }

                var userdata = _userDataRepository.GetUserData(user.Id, child.GetUserDataKey());

                recursiveItemCount++;

                var isUnplayed = true;

                // Incrememt totalPercentPlayed
                if (userdata != null)
                {
                    if (userdata.Played)
                    {
                        totalPercentPlayed += 100;

                        isUnplayed = false;
                    }
                    else if (userdata.PlaybackPositionTicks > 0 && child.RunTimeTicks.HasValue && child.RunTimeTicks.Value > 0)
                    {
                        double itemPercent = userdata.PlaybackPositionTicks;
                        itemPercent /= child.RunTimeTicks.Value;
                        totalPercentPlayed += itemPercent;
                    }
                }

                if (isUnplayed)
                {
                    unplayed++;
                }

                runtime += child.RunTimeTicks ?? 0;

                if (addSyncInfo)
                {
                    double percent = 0;
                    SyncedItemProgress syncItemProgress;
                    if (syncProgress.TryGetValue(child.Id.ToString("N"), out syncItemProgress))
                    {
                        switch (syncItemProgress.Status)
                        {
                            case SyncJobItemStatus.Synced:
                                percent = 100;
                                break;
                            case SyncJobItemStatus.Converting:
                            case SyncJobItemStatus.ReadyToTransfer:
                            case SyncJobItemStatus.Transferring:
                                percent = 50;
                                break;
                        }
                    }
                    totalSyncPercent += percent;
                }
            }

            dto.RecursiveItemCount = recursiveItemCount;
            dto.UserData.UnplayedItemCount = unplayed;

            if (recursiveItemCount > 0)
            {
                dto.UserData.PlayedPercentage = totalPercentPlayed / recursiveItemCount;

                if (addSyncInfo)
                {
                    var pct = totalSyncPercent / recursiveItemCount;
                    if (pct > 0)
                    {
                        dto.SyncPercent = pct;
                    }
                }
            }

            if (runtime > 0 && fields.Contains(ItemFields.CumulativeRunTimeTicks))
            {
                dto.CumulativeRunTimeTicks = runtime;
            }

            if (fields.Contains(ItemFields.DateLastMediaAdded))
            {
                dto.DateLastMediaAdded = dateLastMediaAdded;
            }
        }

        /// <summary>
        /// Attaches the primary image aspect ratio.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        public void AttachPrimaryImageAspectRatio(IItemDto dto, IHasImages item)
        {
            dto.PrimaryImageAspectRatio = GetPrimaryImageAspectRatio(item);
        }

        public double? GetPrimaryImageAspectRatio(IHasImages item)
        {
            var imageInfo = item.GetImageInfo(ImageType.Primary, 0);

            if (imageInfo == null || !imageInfo.IsLocalFile)
            {
                return null;
            }

            ImageSize size;

            try
            {
                size = _imageProcessor.GetImageSize(imageInfo);
            }
            catch (Exception ex)
            {
                //_logger.ErrorException("Failed to determine primary image aspect ratio for {0}", ex, path);
                return null;
            }

            var supportedEnhancers = _imageProcessor.GetSupportedEnhancers(item, ImageType.Primary).ToList();

            foreach (var enhancer in supportedEnhancers)
            {
                try
                {
                    size = enhancer.GetEnhancedImageSize(item, ImageType.Primary, 0, size);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in image enhancer: {0}", ex, enhancer.GetType().Name);
                }
            }

            var width = size.Width;
            var height = size.Height;

            if (width == 0 || height == 0)
            {
                return null;
            }

            var photo = item as Photo;
            if (photo != null && photo.Orientation.HasValue)
            {
                switch (photo.Orientation.Value)
                {
                    case ImageOrientation.LeftBottom:
                    case ImageOrientation.LeftTop:
                    case ImageOrientation.RightBottom:
                    case ImageOrientation.RightTop:
                        var temp = height;
                        height = width;
                        width = temp;
                        break;
                }
            }

            return width / height;
        }
    }
}
