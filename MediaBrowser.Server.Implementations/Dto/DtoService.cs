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

        public async Task<List<BaseItemDto>> GetBaseItemDtos(IEnumerable<BaseItem> items, DtoOptions options, User user = null, BaseItem owner = null)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var syncDictionary = GetSyncedItemProgress(options);

            var list = new List<BaseItemDto>();
            var programTuples = new List<Tuple<BaseItem, BaseItemDto>>();
            var channelTuples = new List<Tuple<BaseItemDto, LiveTvChannel>>();

            foreach (var item in items)
            {
                var dto = await GetBaseItemDtoInternal(item, options, user, owner).ConfigureAwait(false);

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
                        var libraryItems = byName.GetTaggedItems(new InternalItemsQuery(user)
                        {
                            Recursive = true
                        });

                        SetItemByNameInfo(item, dto, libraryItems.ToList(), user);
                    }
                }

                FillSyncInfo(dto, item, options, user, syncDictionary);

                list.Add(dto);
            }

            if (programTuples.Count > 0)
            {
                await _livetvManager().AddInfoToProgramDto(programTuples, options.Fields, user).ConfigureAwait(false);
            }

            if (channelTuples.Count > 0)
            {
                _livetvManager().AddChannelInfo(channelTuples, options, user);
            }

            return list;
        }

        public BaseItemDto GetBaseItemDto(BaseItem item, DtoOptions options, User user = null, BaseItem owner = null)
        {
            var syncDictionary = GetSyncedItemProgress(options);

            var dto = GetBaseItemDtoInternal(item, options, user, owner).Result;
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

                FillSyncInfo(dto, item, options, user, syncDictionary);
                return dto;
            }

            FillSyncInfo(dto, item, options, user, syncDictionary);

            return dto;
        }

        private List<BaseItem> GetTaggedItems(IItemByName byName, User user)
        {
            var items = byName.GetTaggedItems(new InternalItemsQuery(user)
            {
                Recursive = true

            }).ToList();

            return items;
        }

        public Dictionary<string, SyncedItemProgress> GetSyncedItemProgress(DtoOptions options)
        {
            if (!options.Fields.Contains(ItemFields.BasicSyncInfo) &&
                !options.Fields.Contains(ItemFields.SyncInfo))
            {
                return new Dictionary<string, SyncedItemProgress>();
            }

            var deviceId = options.DeviceId;
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return new Dictionary<string, SyncedItemProgress>();
            }

            var caps = _deviceManager().GetCapabilities(deviceId);
            if (caps == null || !caps.SupportsSync)
            {
                return new Dictionary<string, SyncedItemProgress>();
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
            });
        }

        public void FillSyncInfo(IEnumerable<Tuple<BaseItem, BaseItemDto>> tuples, DtoOptions options, User user)
        {
            if (options.Fields.Contains(ItemFields.BasicSyncInfo) ||
                options.Fields.Contains(ItemFields.SyncInfo))
            {
                var syncProgress = GetSyncedItemProgress(options);

                foreach (var tuple in tuples)
                {
                    var item = tuple.Item1;

                    FillSyncInfo(tuple.Item2, item, options, user, syncProgress);
                }
            }
        }

        private void FillSyncInfo(IHasSyncInfo dto, BaseItem item, DtoOptions options, User user, Dictionary<string, SyncedItemProgress> syncProgress)
        {
            var hasFullSyncInfo = options.Fields.Contains(ItemFields.SyncInfo);

            if (!options.Fields.Contains(ItemFields.BasicSyncInfo) &&
                !hasFullSyncInfo)
            {
                return;
            }

            if (dto.SupportsSync ?? false)
            {
                SyncedItemProgress syncStatus;
                if (syncProgress.TryGetValue(dto.Id, out syncStatus))
                {
                    if (syncStatus.Status == SyncJobItemStatus.Synced)
                    {
                        dto.SyncPercent = 100;
                    }
                    else
                    {
                        dto.SyncPercent = syncStatus.Progress;
                    }

                    if (hasFullSyncInfo)
                    {
                        dto.HasSyncJob = true;
                        dto.SyncStatus = syncStatus.Status;
                    }
                }
            }
        }

        private async Task<BaseItemDto> GetBaseItemDtoInternal(BaseItem item, DtoOptions options, User user = null, BaseItem owner = null)
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
                await AttachUserSpecificInfo(dto, item, user, options).ConfigureAwait(false);
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

        public BaseItemDto GetItemByNameDto(BaseItem item, DtoOptions options, List<BaseItem> taggedItems, Dictionary<string, SyncedItemProgress> syncProgress, User user = null)
        {
            var dto = GetBaseItemDtoInternal(item, options, user).Result;

            if (taggedItems != null && options.Fields.Contains(ItemFields.ItemCounts))
            {
                SetItemByNameInfo(item, dto, taggedItems, user);
            }

            FillSyncInfo(dto, item, options, user, syncProgress);

            return dto;
        }

        private void SetItemByNameInfo(BaseItem item, BaseItemDto dto, List<BaseItem> taggedItems, User user = null)
        {
            if (item is MusicArtist)
            {
                dto.AlbumCount = taggedItems.Count(i => i is MusicAlbum);
                dto.MusicVideoCount = taggedItems.Count(i => i is MusicVideo);
                dto.SongCount = taggedItems.Count(i => i is Audio);
            }
            else if (item is MusicGenre)
            {
                dto.ArtistCount = taggedItems.Count(i => i is MusicArtist);
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

                dto.ArtistCount = taggedItems.Count(i => i is MusicArtist);
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
        private async Task AttachUserSpecificInfo(BaseItemDto dto, BaseItem item, User user, DtoOptions dtoOptions)
        {
            var fields = dtoOptions.Fields;

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                if (dtoOptions.EnableUserData)
                {
                    dto.UserData = await _userDataRepository.GetUserDataDto(item, dto, user).ConfigureAwait(false);
                }

                if (!dto.ChildCount.HasValue && item.SourceType == SourceType.Library)
                {
                    dto.ChildCount = GetChildCount(folder, user);
                }

                if (fields.Contains(ItemFields.CumulativeRunTimeTicks))
                {
                    dto.CumulativeRunTimeTicks = item.RunTimeTicks;
                }

                if (fields.Contains(ItemFields.DateLastMediaAdded))
                {
                    dto.DateLastMediaAdded = folder.DateLastMediaAdded;
                }
            }

            else
            {
                if (dtoOptions.EnableUserData)
                {
                    dto.UserData = _userDataRepository.GetUserDataDto(item, user).Result;
                }
            }

            dto.PlayAccess = item.GetPlayAccess(user);

            if (fields.Contains(ItemFields.BasicSyncInfo) || fields.Contains(ItemFields.SyncInfo))
            {
                var userCanSync = user != null && user.Policy.EnableSync;
                if (userCanSync && _syncManager.SupportsSync(item))
                {
                    dto.SupportsSync = true;
                }
            }

            if (fields.Contains(ItemFields.SeasonUserData))
            {
                var episode = item as Episode;

                if (episode != null)
                {
                    var season = episode.Season;

                    if (season != null)
                    {
                        dto.SeasonUserData = await _userDataRepository.GetUserDataDto(season, user).ConfigureAwait(false);
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
            // Right now this is too slow to calculate for top level folders on a per-user basis
            // Just return something so that apps that are expecting a value won't think the folders are empty
            if (folder is ICollectionFolder || folder is UserView)
            {
                return new Random().Next(1, 10);
            }

            return folder.GetChildCount(user);
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
                var parentAlbum = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(MusicAlbum).Name },
                    Name = item.Album

                }).FirstOrDefault();

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

        private List<string> GetImageTags(BaseItem item, List<ItemImageInfo> images)
        {
            return images
                .Select(p => GetImageCacheTag(item, p))
                .Where(i => i != null)
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
                    DateModified = chapterInfo.ImageDateModified
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
            dto.Container = item.Container;

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
                dto.Tags = item.Tags;
            }

            if (fields.Contains(ItemFields.Keywords))
            {
                dto.Keywords = item.Keywords;
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
                dto.BackdropImageTags = GetImageTags(item, item.GetImages(ImageType.Backdrop).Take(backdropLimit).ToList());
            }

            if (fields.Contains(ItemFields.ScreenshotImageTags))
            {
                var screenshotLimit = options.GetImageLimit(ImageType.Screenshot);
                if (screenshotLimit > 0)
                {
                    dto.ScreenshotImageTags = GetImageTags(item, item.GetImages(ImageType.Screenshot).Take(screenshotLimit).ToList());
                }
            }

            if (fields.Contains(ItemFields.Genres))
            {
                dto.Genres = item.Genres;
            }

            if (options.EnableImages)
            {
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
            }

            dto.Id = GetDtoId(item);
            dto.IndexNumber = item.IndexNumber;
            dto.ParentIndexNumber = item.ParentIndexNumber;

            if (item.IsFolder)
            {
                dto.IsFolder = true;
            }
            else if (item is IHasMediaSources)
            {
                dto.IsFolder = false;
            }

            dto.MediaType = item.MediaType;
            dto.LocationType = item.LocationType;
            if (item.IsHD.HasValue && item.IsHD.Value)
            {
                dto.IsHD = item.IsHD;
            }
            dto.Audio = item.Audio;

            dto.PreferredMetadataCountryCode = item.PreferredMetadataCountryCode;
            dto.PreferredMetadataLanguage = item.PreferredMetadataLanguage;

            dto.CriticRating = item.CriticRating;

            if (fields.Contains(ItemFields.CriticRatingSummary))
            {
                dto.CriticRatingSummary = item.CriticRatingSummary;
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

            if (fields.Contains(ItemFields.OriginalTitle))
            {
                dto.OriginalTitle = item.OriginalTitle;
            }

            if (fields.Contains(ItemFields.ShortOverview))
            {
                dto.ShortOverview = item.ShortOverview;
            }

            if (fields.Contains(ItemFields.ParentId))
            {
                var displayParentId = item.DisplayParentId;
                if (displayParentId.HasValue)
                {
                    dto.ParentId = displayParentId.Value.ToString("N");
                }
            }

            AddInheritedImages(dto, item, options, owner);

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

                var artistItems = _libraryManager.GetArtists(new InternalItemsQuery
                {
                    EnableTotalRecordCount = false,
                    ItemIds = new[] { item.Id.ToString("N") }
                });

                dto.ArtistItems = artistItems.Items
                    .Select(i =>
                    {
                        var artist = i.Item1;
                        return new NameIdPair
                        {
                            Name = artist.Name,
                            Id = artist.Id.ToString("N")
                        };
                    })
                    .ToList();
            }

            var hasAlbumArtist = item as IHasAlbumArtist;
            if (hasAlbumArtist != null)
            {
                dto.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();

                var artistItems = _libraryManager.GetAlbumArtists(new InternalItemsQuery
                {
                    EnableTotalRecordCount = false,
                    ItemIds = new[] { item.Id.ToString("N") }
                });

                dto.AlbumArtists = artistItems.Items
                    .Select(i =>
                    {
                        var artist = i.Item1;
                        return new NameIdPair
                        {
                            Name = artist.Name,
                            Id = artist.Id.ToString("N")
                        };
                    })
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
                    var mediaSourceCount = video.MediaSourceCount;
                    if (mediaSourceCount != 1)
                    {
                        dto.MediaSourceCount = mediaSourceCount;
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
                dto.SeriesName = episode.SeriesName;

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

                var seasonId = episode.SeasonId;
                if (seasonId.HasValue)
                {
                    dto.SeasonId = seasonId.Value.ToString("N");
                }

                dto.SeasonName = episode.SeasonName;

                var seriesId = episode.SeriesId;
                if (seriesId.HasValue)
                {
                    dto.SeriesId = seriesId.Value.ToString("N");
                }

                Series episodeSeries = null;

                if (fields.Contains(ItemFields.SeriesGenres))
                {
                    episodeSeries = episodeSeries ?? episode.Series;
                    if (episodeSeries != null)
                    {
                        dto.SeriesGenres = episodeSeries.Genres.ToList();
                    }
                }

                //if (fields.Contains(ItemFields.SeriesPrimaryImage))
                {
                    episodeSeries = episodeSeries ?? episode.Series;
                    if (episodeSeries != null)
                    {
                        dto.SeriesPrimaryImageTag = GetImageCacheTag(episodeSeries, ImageType.Primary);
                    }
                }

                if (fields.Contains(ItemFields.SeriesStudio))
                {
                    episodeSeries = episodeSeries ?? episode.Series;
                    if (episodeSeries != null)
                    {
                        dto.SeriesStudio = episodeSeries.Studios.FirstOrDefault();
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

                dto.AnimeSeriesIndex = series.AnimeSeriesIndex;
            }

            // Add SeasonInfo
            var season = item as Season;
            if (season != null)
            {
                dto.SeriesName = season.SeriesName;

                var seriesId = season.SeriesId;
                if (seriesId.HasValue)
                {
                    dto.SeriesId = seriesId.Value.ToString("N");
                }

                series = null;

                if (fields.Contains(ItemFields.SeriesStudio))
                {
                    series = series ?? season.Series;
                    if (series != null)
                    {
                        dto.SeriesStudio = series.Studios.FirstOrDefault();
                    }
                }

                if (fields.Contains(ItemFields.SeriesPrimaryImage))
                {
                    series = series ?? season.Series;
                    if (series != null)
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

        private void AddInheritedImages(BaseItemDto dto, BaseItem item, DtoOptions options, BaseItem owner)
        {
            var logoLimit = options.GetImageLimit(ImageType.Logo);
            var artLimit = options.GetImageLimit(ImageType.Art);
            var thumbLimit = options.GetImageLimit(ImageType.Thumb);
            var backdropLimit = options.GetImageLimit(ImageType.Backdrop);

            if (logoLimit == 0 && artLimit == 0 && thumbLimit == 0 && backdropLimit == 0)
            {
                return;
            }

            BaseItem parent = null;
            var isFirst = true;

            while (((!dto.HasLogo && logoLimit > 0) || (!dto.HasArtImage && artLimit > 0) || (!dto.HasThumb && thumbLimit > 0) || parent is Series) &&
                (parent = parent ?? (isFirst ? item.GetParent() ?? owner : parent)) != null)
            {
                if (parent == null)
                {
                    break;
                }

                var allImages = parent.ImageInfos;

                if (logoLimit > 0 && !dto.HasLogo && dto.ParentLogoItemId == null)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Logo);

                    if (image != null)
                    {
                        dto.ParentLogoItemId = GetDtoId(parent);
                        dto.ParentLogoImageTag = GetImageCacheTag(parent, image);
                    }
                }
                if (artLimit > 0 && !dto.HasArtImage && dto.ParentArtItemId == null)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Art);

                    if (image != null)
                    {
                        dto.ParentArtItemId = GetDtoId(parent);
                        dto.ParentArtImageTag = GetImageCacheTag(parent, image);
                    }
                }
                if (thumbLimit > 0 && !dto.HasThumb && (dto.ParentThumbItemId == null || parent is Series))
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Thumb);

                    if (image != null)
                    {
                        dto.ParentThumbItemId = GetDtoId(parent);
                        dto.ParentThumbImageTag = GetImageCacheTag(parent, image);
                    }
                }
                if (backdropLimit > 0 && !dto.HasBackdrop)
                {
                    var images = allImages.Where(i => i.Type == ImageType.Backdrop).Take(backdropLimit).ToList();

                    if (images.Count > 0)
                    {
                        dto.ParentBackdropItemId = GetDtoId(parent);
                        dto.ParentBackdropImageTags = GetImageTags(parent, images);
                    }
                }

                isFirst = false;
                parent = parent.GetParent();
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
            catch
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
