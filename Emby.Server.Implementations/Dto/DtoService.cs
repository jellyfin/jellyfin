#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Channels;
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
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Dto
{
    public class DtoService : IDtoService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly IItemRepository _itemRepo;

        private readonly IImageProcessor _imageProcessor;
        private readonly IProviderManager _providerManager;

        private readonly IApplicationHost _appHost;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly Lazy<ILiveTvManager> _livetvManagerFactory;

        private ILiveTvManager LivetvManager => _livetvManagerFactory.Value;

        public DtoService(
            ILogger<DtoService> logger,
            ILibraryManager libraryManager,
            IUserDataManager userDataRepository,
            IItemRepository itemRepo,
            IImageProcessor imageProcessor,
            IProviderManager providerManager,
            IApplicationHost appHost,
            IMediaSourceManager mediaSourceManager,
            Lazy<ILiveTvManager> livetvManagerFactory)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _providerManager = providerManager;
            _appHost = appHost;
            _mediaSourceManager = mediaSourceManager;
            _livetvManagerFactory = livetvManagerFactory;
        }

        /// <summary>
        /// Converts a BaseItem to a DTOBaseItem
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="user">The user.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        public BaseItemDto GetBaseItemDto(BaseItem item, ItemFields[] fields, User user = null, BaseItem owner = null)
        {
            var options = new DtoOptions
            {
                Fields = fields
            };

            return GetBaseItemDto(item, options, user, owner);
        }

        /// <inheritdoc />
        public IReadOnlyList<BaseItemDto> GetBaseItemDtos(IReadOnlyList<BaseItem> items, DtoOptions options, User user = null, BaseItem owner = null)
        {
            var returnItems = new BaseItemDto[items.Count];
            var programTuples = new List<(BaseItem, BaseItemDto)>();
            var channelTuples = new List<(BaseItemDto, LiveTvChannel)>();

            for (int index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var dto = GetBaseItemDtoInternal(item, options, user, owner);

                if (item is LiveTvChannel tvChannel)
                {
                    channelTuples.Add((dto, tvChannel));
                }
                else if (item is LiveTvProgram)
                {
                    programTuples.Add((item, dto));
                }

                if (item is IItemByName byName)
                {
                    if (options.ContainsField(ItemFields.ItemCounts))
                    {
                        var libraryItems = byName.GetTaggedItems(new InternalItemsQuery(user)
                        {
                            Recursive = true,
                            DtoOptions = new DtoOptions(false)
                            {
                                EnableImages = false
                            }
                        });

                        SetItemByNameInfo(item, dto, libraryItems, user);
                    }
                }

                returnItems[index] = dto;
            }

            if (programTuples.Count > 0)
            {
                LivetvManager.AddInfoToProgramDto(programTuples, options.Fields, user).GetAwaiter().GetResult();
            }

            if (channelTuples.Count > 0)
            {
                LivetvManager.AddChannelInfo(channelTuples, options, user);
            }

            return returnItems;
        }

        public BaseItemDto GetBaseItemDto(BaseItem item, DtoOptions options, User user = null, BaseItem owner = null)
        {
            var dto = GetBaseItemDtoInternal(item, options, user, owner);
            if (item is LiveTvChannel tvChannel)
            {
                var list = new List<(BaseItemDto, LiveTvChannel)>(1) { (dto, tvChannel) };
                LivetvManager.AddChannelInfo(list, options, user);
            }
            else if (item is LiveTvProgram)
            {
                var list = new List<(BaseItem, BaseItemDto)>(1) { (item, dto) };
                var task = LivetvManager.AddInfoToProgramDto(list, options.Fields, user);
                Task.WaitAll(task);
            }

            if (item is IItemByName itemByName
                && options.ContainsField(ItemFields.ItemCounts))
            {
                SetItemByNameInfo(
                    item,
                    dto,
                    GetTaggedItems(
                        itemByName,
                        user,
                        new DtoOptions(false)
                        {
                            EnableImages = false
                        }),
                    user);
            }

            return dto;
        }

        private static IList<BaseItem> GetTaggedItems(IItemByName byName, User user, DtoOptions options)
        {
            return byName.GetTaggedItems(
                new InternalItemsQuery(user)
                {
                    Recursive = true,
                    DtoOptions = options
                });
        }

        private BaseItemDto GetBaseItemDtoInternal(BaseItem item, DtoOptions options, User user = null, BaseItem owner = null)
        {
            var dto = new BaseItemDto
            {
                ServerId = _appHost.SystemId
            };

            if (item.SourceType == SourceType.Channel)
            {
                dto.SourceType = item.SourceType.ToString();
            }

            if (options.ContainsField(ItemFields.People))
            {
                AttachPeople(dto, item);
            }

            if (options.ContainsField(ItemFields.PrimaryImageAspectRatio))
            {
                try
                {
                    AttachPrimaryImageAspectRatio(dto, item);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.LogError(ex, "Error generating PrimaryImageAspectRatio for {itemName}", item.Name);
                }
            }

            if (options.ContainsField(ItemFields.DisplayPreferencesId))
            {
                dto.DisplayPreferencesId = item.DisplayPreferencesId.ToString("N", CultureInfo.InvariantCulture);
            }

            if (user != null)
            {
                AttachUserSpecificInfo(dto, item, user, options);
            }

            if (item is IHasMediaSources
                && options.ContainsField(ItemFields.MediaSources))
            {
                dto.MediaSources = _mediaSourceManager.GetStaticMediaSources(item, true, user).ToArray();

                NormalizeMediaSourceContainers(dto);
            }

            if (options.ContainsField(ItemFields.Studios))
            {
                AttachStudios(dto, item);
            }

            AttachBasicFields(dto, item, owner, options);

            if (options.ContainsField(ItemFields.CanDelete))
            {
                dto.CanDelete = user == null
                    ? item.CanDelete()
                    : item.CanDelete(user);
            }

            if (options.ContainsField(ItemFields.CanDownload))
            {
                dto.CanDownload = user == null
                    ? item.CanDownload()
                    : item.CanDownload(user);
            }

            if (options.ContainsField(ItemFields.Etag))
            {
                dto.Etag = item.GetEtag(user);
            }

            var liveTvManager = LivetvManager;
            var activeRecording = liveTvManager.GetActiveRecordingInfo(item.Path);
            if (activeRecording != null)
            {
                dto.Type = "Recording";
                dto.CanDownload = false;
                dto.RunTimeTicks = null;

                if (!string.IsNullOrEmpty(dto.SeriesName))
                {
                    dto.EpisodeTitle = dto.Name;
                    dto.Name = dto.SeriesName;
                }
                liveTvManager.AddInfoToRecordingDto(item, dto, activeRecording, user);
            }

            return dto;
        }

        private static void NormalizeMediaSourceContainers(BaseItemDto dto)
        {
            foreach (var mediaSource in dto.MediaSources)
            {
                var container = mediaSource.Container;
                if (string.IsNullOrEmpty(container))
                {
                    continue;
                }
                var containers = container.Split(new[] { ',' });
                if (containers.Length < 2)
                {
                    continue;
                }

                var path = mediaSource.Path;
                string fileExtensionContainer = null;

                if (!string.IsNullOrEmpty(path))
                {
                    path = Path.GetExtension(path);
                    if (!string.IsNullOrEmpty(path))
                    {
                        path = Path.GetExtension(path);
                        if (!string.IsNullOrEmpty(path))
                        {
                            path = path.TrimStart('.');
                        }

                        if (!string.IsNullOrEmpty(path) && containers.Contains(path, StringComparer.OrdinalIgnoreCase))
                        {
                            fileExtensionContainer = path;
                        }
                    }
                }

                mediaSource.Container = fileExtensionContainer ?? containers[0];
            }
        }

        public BaseItemDto GetItemByNameDto(BaseItem item, DtoOptions options, List<BaseItem> taggedItems, User user = null)
        {
            var dto = GetBaseItemDtoInternal(item, options, user);

            if (taggedItems != null && options.ContainsField(ItemFields.ItemCounts))
            {
                SetItemByNameInfo(item, dto, taggedItems, user);
            }

            return dto;
        }

        private static void SetItemByNameInfo(BaseItem item, BaseItemDto dto, IList<BaseItem> taggedItems, User user = null)
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
            else
            {
                // This populates them all and covers Genre, Person, Studio, Year

                dto.ArtistCount = taggedItems.Count(i => i is MusicArtist);
                dto.AlbumCount = taggedItems.Count(i => i is MusicAlbum);
                dto.EpisodeCount = taggedItems.Count(i => i is Episode);
                dto.MovieCount = taggedItems.Count(i => i is Movie);
                dto.TrailerCount = taggedItems.Count(i => i is Trailer);
                dto.MusicVideoCount = taggedItems.Count(i => i is MusicVideo);
                dto.SeriesCount = taggedItems.Count(i => i is Series);
                dto.ProgramCount = taggedItems.Count(i => i is LiveTvProgram);
                dto.SongCount = taggedItems.Count(i => i is Audio);
            }

            dto.ChildCount = taggedItems.Count;
        }

        /// <summary>
        /// Attaches the user specific info.
        /// </summary>
        private void AttachUserSpecificInfo(BaseItemDto dto, BaseItem item, User user, DtoOptions options)
        {
            if (item.IsFolder)
            {
                var folder = (Folder)item;

                if (options.EnableUserData)
                {
                    dto.UserData = _userDataRepository.GetUserDataDto(item, dto, user, options);
                }

                if (!dto.ChildCount.HasValue && item.SourceType == SourceType.Library)
                {
                    // For these types we can try to optimize and assume these values will be equal
                    if (item is MusicAlbum || item is Season || item is Playlist)
                    {
                        dto.ChildCount = dto.RecursiveItemCount;
                    }

                    if (options.ContainsField(ItemFields.ChildCount))
                    {
                        dto.ChildCount = dto.ChildCount ?? GetChildCount(folder, user);
                    }
                }

                if (options.ContainsField(ItemFields.CumulativeRunTimeTicks))
                {
                    dto.CumulativeRunTimeTicks = item.RunTimeTicks;
                }

                if (options.ContainsField(ItemFields.DateLastMediaAdded))
                {
                    dto.DateLastMediaAdded = folder.DateLastMediaAdded;
                }
            }

            else
            {
                if (options.EnableUserData)
                {
                    dto.UserData = _userDataRepository.GetUserDataDto(item, user);
                }
            }

            if (options.ContainsField(ItemFields.PlayAccess))
            {
                dto.PlayAccess = item.GetPlayAccess(user);
            }

            if (options.ContainsField(ItemFields.BasicSyncInfo))
            {
                var userCanSync = user != null && user.Policy.EnableContentDownloading;
                if (userCanSync && item.SupportsExternalTransfer)
                {
                    dto.SupportsSync = true;
                }
            }
        }

        private static int GetChildCount(Folder folder, User user)
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
        /// <exception cref="ArgumentNullException">item</exception>
        public string GetDtoId(BaseItem item)
        {
            return item.Id.ToString("N", CultureInfo.InvariantCulture);
        }

        private static void SetBookProperties(BaseItemDto dto, Book item)
        {
            dto.SeriesName = item.SeriesName;
        }
        private static void SetPhotoProperties(BaseItemDto dto, Photo item)
        {
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

            var album = item.AlbumEntity;

            if (album != null)
            {
                dto.Album = album.Name;
                dto.AlbumId = album.Id;
            }
        }

        private void SetMusicVideoProperties(BaseItemDto dto, MusicVideo item)
        {
            if (!string.IsNullOrEmpty(item.Album))
            {
                var parentAlbumIds = _libraryManager.GetItemIds(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(MusicAlbum).Name },
                    Name = item.Album,
                    Limit = 1

                });

                if (parentAlbumIds.Count > 0)
                {
                    dto.AlbumId = parentAlbumIds[0];
                }
            }

            dto.Album = item.Album;
        }

        private string[] GetImageTags(BaseItem item, List<ItemImageInfo> images)
        {
            return images
                .Select(p => GetImageCacheTag(item, p))
                .Where(i => i != null)
                .ToArray();
        }

        private string GetImageCacheTag(BaseItem item, ImageType type)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {type} image info", type);
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
                _logger.LogError(ex, "Error getting {imageType} image info for {path}", image.Type, image.Path);
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
                        _logger.LogError(ex, "Error getting person {Name}", c);
                        return null;
                    }

                }).Where(i => i != null)
                .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
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

                if (dictionary.TryGetValue(person.Name, out Person entity))
                {
                    baseItemPerson.PrimaryImageTag = GetTagAndFillBlurhash(dto, entity, ImageType.Primary);
                    baseItemPerson.Id = entity.Id.ToString("N", CultureInfo.InvariantCulture);
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
            dto.Studios = item.Studios
                .Where(i => !string.IsNullOrEmpty(i))
                .Select(i => new NameGuidPair
                {
                    Name = i,
                    Id = _libraryManager.GetStudioId(i)
                })
                .ToArray();
        }

        private void AttachGenreItems(BaseItemDto dto, BaseItem item)
        {
            dto.GenreItems = item.Genres
                .Where(i => !string.IsNullOrEmpty(i))
                .Select(i => new NameGuidPair
                {
                    Name = i,
                    Id = GetGenreId(i, item)
                })
                .ToArray();
        }

        private Guid GetGenreId(string name, BaseItem owner)
        {
            if (owner is IHasMusicGenres)
            {
                return _libraryManager.GetMusicGenreId(name);
            }

            return _libraryManager.GetGenreId(name);
        }

        private string GetTagAndFillBlurhash(BaseItemDto dto, BaseItem item, ImageType imageType, int imageIndex = 0)
        {
            var image = item.GetImageInfo(imageType, imageIndex);
            if (image != null)
            {
                return GetTagAndFillBlurhash(dto, item, image);
            }

            return null;
        }

        private string GetTagAndFillBlurhash(BaseItemDto dto, BaseItem item, ItemImageInfo image)
        {
            var tag = GetImageCacheTag(item, image);
            if (!string.IsNullOrEmpty(image.BlurHash))
            {
                if (dto.ImageBlurHashes == null)
                {
                    dto.ImageBlurHashes = new Dictionary<ImageType, Dictionary<string, string>>();
                }

                if (!dto.ImageBlurHashes.ContainsKey(image.Type))
                {
                    dto.ImageBlurHashes[image.Type] = new Dictionary<string, string>();
                }

                dto.ImageBlurHashes[image.Type][tag] = image.BlurHash;
            }

            return tag;
        }

        private string[] GetTagsAndFillBlurhashes(BaseItemDto dto, BaseItem item, ImageType imageType, int limit)
        {
            return GetTagsAndFillBlurhashes(dto, item, imageType, item.GetImages(imageType).Take(limit).ToList());
        }

        private string[] GetTagsAndFillBlurhashes(BaseItemDto dto, BaseItem item, ImageType imageType, List<ItemImageInfo> images)
        {
            var tags = GetImageTags(item, images);
            var hashes = new Dictionary<string, string>();
            for (int i = 0; i < images.Count; i++)
            {
                var img = images[i];
                if (!string.IsNullOrEmpty(img.BlurHash))
                {
                    var tag = tags[i];
                    hashes[tag] = img.BlurHash;
                }
            }

            if (hashes.Count > 0)
            {
                if (dto.ImageBlurHashes == null)
                {
                    dto.ImageBlurHashes = new Dictionary<ImageType, Dictionary<string, string>>();
                }

                dto.ImageBlurHashes[imageType] = hashes;
            }

            return tags;
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
            if (options.ContainsField(ItemFields.DateCreated))
            {
                dto.DateCreated = item.DateCreated;
            }

            if (options.ContainsField(ItemFields.Settings))
            {
                dto.LockedFields = item.LockedFields;
                dto.LockData = item.IsLocked;
                dto.ForcedSortName = item.ForcedSortName;
            }

            dto.Container = item.Container;
            dto.EndDate = item.EndDate;

            if (options.ContainsField(ItemFields.ExternalUrls))
            {
                dto.ExternalUrls = _providerManager.GetExternalUrls(item).ToArray();
            }

            if (options.ContainsField(ItemFields.Tags))
            {
                dto.Tags = item.Tags;
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                dto.AspectRatio = hasAspectRatio.AspectRatio;
            }

            dto.ImageBlurHashes = new Dictionary<ImageType, Dictionary<string, string>>();

            var backdropLimit = options.GetImageLimit(ImageType.Backdrop);
            if (backdropLimit > 0)
            {
                dto.BackdropImageTags = GetTagsAndFillBlurhashes(dto, item, ImageType.Backdrop, backdropLimit);
            }

            if (options.ContainsField(ItemFields.ScreenshotImageTags))
            {
                var screenshotLimit = options.GetImageLimit(ImageType.Screenshot);
                if (screenshotLimit > 0)
                {
                    dto.ScreenshotImageTags = GetTagsAndFillBlurhashes(dto, item, ImageType.Screenshot, screenshotLimit);
                }
            }

            if (options.ContainsField(ItemFields.Genres))
            {
                dto.Genres = item.Genres;
                AttachGenreItems(dto, item);
            }

            if (options.EnableImages)
            {
                dto.ImageTags = new Dictionary<ImageType, string>();

                // Prevent implicitly captured closure
                var currentItem = item;
                foreach (var image in currentItem.ImageInfos.Where(i => !currentItem.AllowsMultipleImages(i.Type)))
                {
                    if (options.GetImageLimit(image.Type) > 0)
                    {
                        var tag = GetTagAndFillBlurhash(dto, item, image);

                        if (tag != null)
                        {
                            dto.ImageTags[image.Type] = tag;
                        }
                    }
                }
            }

            dto.Id = item.Id;
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

            if (!(item is LiveTvProgram))
            {
                dto.LocationType = item.LocationType;
            }

            dto.Audio = item.Audio;

            if (options.ContainsField(ItemFields.Settings))
            {
                dto.PreferredMetadataCountryCode = item.PreferredMetadataCountryCode;
                dto.PreferredMetadataLanguage = item.PreferredMetadataLanguage;
            }

            dto.CriticRating = item.CriticRating;

            if (item is IHasDisplayOrder hasDisplayOrder)
            {
                dto.DisplayOrder = hasDisplayOrder.DisplayOrder;
            }

            if (item is IHasCollectionType hasCollectionType)
            {
                dto.CollectionType = hasCollectionType.CollectionType;
            }

            if (options.ContainsField(ItemFields.RemoteTrailers))
            {
                dto.RemoteTrailers = item.RemoteTrailers;
            }

            dto.Name = item.Name;
            dto.OfficialRating = item.OfficialRating;

            if (options.ContainsField(ItemFields.Overview))
            {
                dto.Overview = item.Overview;
            }

            if (options.ContainsField(ItemFields.OriginalTitle))
            {
                dto.OriginalTitle = item.OriginalTitle;
            }

            if (options.ContainsField(ItemFields.ParentId))
            {
                dto.ParentId = item.DisplayParentId;
            }

            AddInheritedImages(dto, item, options, owner);

            if (options.ContainsField(ItemFields.Path))
            {
                dto.Path = GetMappedPath(item, owner);
            }

            if (options.ContainsField(ItemFields.EnableMediaSourceDisplay))
            {
                dto.EnableMediaSourceDisplay = item.EnableMediaSourceDisplay;
            }

            dto.PremiereDate = item.PremiereDate;
            dto.ProductionYear = item.ProductionYear;

            if (options.ContainsField(ItemFields.ProviderIds))
            {
                dto.ProviderIds = item.ProviderIds;
            }

            dto.RunTimeTicks = item.RunTimeTicks;

            if (options.ContainsField(ItemFields.SortName))
            {
                dto.SortName = item.SortName;
            }

            if (options.ContainsField(ItemFields.CustomRating))
            {
                dto.CustomRating = item.CustomRating;
            }

            if (options.ContainsField(ItemFields.Taglines))
            {
                if (!string.IsNullOrEmpty(item.Tagline))
                {
                    dto.Taglines = new string[] { item.Tagline };
                }

                if (dto.Taglines == null)
                {
                    dto.Taglines = Array.Empty<string>();
                }
            }

            dto.Type = item.GetClientTypeName();
            if ((item.CommunityRating ?? 0) > 0)
            {
                dto.CommunityRating = item.CommunityRating;
            }

            var supportsPlaceHolders = item as ISupportsPlaceHolders;
            if (supportsPlaceHolders != null && supportsPlaceHolders.IsPlaceHolder)
            {
                dto.IsPlaceHolder = supportsPlaceHolders.IsPlaceHolder;
            }

            // Add audio info
            var audio = item as Audio;
            if (audio != null)
            {
                dto.Album = audio.Album;
                if (audio.ExtraType.HasValue)
                {
                    dto.ExtraType = audio.ExtraType.Value.ToString();
                }

                var albumParent = audio.AlbumEntity;

                if (albumParent != null)
                {
                    dto.AlbumId = albumParent.Id;
                    dto.AlbumPrimaryImageTag = GetTagAndFillBlurhash(dto, albumParent, ImageType.Primary);
                }

                //if (options.ContainsField(ItemFields.MediaSourceCount))
                //{
                // Songs always have one
                //}
            }

            if (item is IHasArtist hasArtist)
            {
                dto.Artists = hasArtist.Artists;

                //var artistItems = _libraryManager.GetArtists(new InternalItemsQuery
                //{
                //    EnableTotalRecordCount = false,
                //    ItemIds = new[] { item.Id.ToString("N", CultureInfo.InvariantCulture) }
                //});

                //dto.ArtistItems = artistItems.Items
                //    .Select(i =>
                //    {
                //        var artist = i.Item1;
                //        return new NameIdPair
                //        {
                //            Name = artist.Name,
                //            Id = artist.Id.ToString("N", CultureInfo.InvariantCulture)
                //        };
                //    })
                //    .ToList();

                // Include artists that are not in the database yet, e.g., just added via metadata editor
                //var foundArtists = artistItems.Items.Select(i => i.Item1.Name).ToList();
                dto.ArtistItems = hasArtist.Artists
                    //.Except(foundArtists, new DistinctNameComparer())
                    .Select(i =>
                    {
                        // This should not be necessary but we're seeing some cases of it
                        if (string.IsNullOrEmpty(i))
                        {
                            return null;
                        }

                        var artist = _libraryManager.GetArtist(i, new DtoOptions(false)
                        {
                            EnableImages = false
                        });
                        if (artist != null)
                        {
                            return new NameGuidPair
                            {
                                Name = artist.Name,
                                Id = artist.Id
                            };
                        }

                        return null;

                    }).Where(i => i != null).ToArray();
            }

            var hasAlbumArtist = item as IHasAlbumArtist;
            if (hasAlbumArtist != null)
            {
                dto.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();

                //var artistItems = _libraryManager.GetAlbumArtists(new InternalItemsQuery
                //{
                //    EnableTotalRecordCount = false,
                //    ItemIds = new[] { item.Id.ToString("N", CultureInfo.InvariantCulture) }
                //});

                //dto.AlbumArtists = artistItems.Items
                //    .Select(i =>
                //    {
                //        var artist = i.Item1;
                //        return new NameIdPair
                //        {
                //            Name = artist.Name,
                //            Id = artist.Id.ToString("N", CultureInfo.InvariantCulture)
                //        };
                //    })
                //    .ToList();

                dto.AlbumArtists = hasAlbumArtist.AlbumArtists
                    //.Except(foundArtists, new DistinctNameComparer())
                    .Select(i =>
                    {
                        // This should not be necessary but we're seeing some cases of it
                        if (string.IsNullOrEmpty(i))
                        {
                            return null;
                        }

                        var artist = _libraryManager.GetArtist(i, new DtoOptions(false)
                        {
                            EnableImages = false
                        });
                        if (artist != null)
                        {
                            return new NameGuidPair
                            {
                                Name = artist.Name,
                                Id = artist.Id
                            };
                        }

                        return null;

                    }).Where(i => i != null).ToArray();
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

                if (video.AdditionalParts.Length != 0)
                {
                    dto.PartCount = video.AdditionalParts.Length + 1;
                }

                if (options.ContainsField(ItemFields.MediaSourceCount))
                {
                    var mediaSourceCount = video.MediaSourceCount;
                    if (mediaSourceCount != 1)
                    {
                        dto.MediaSourceCount = mediaSourceCount;
                    }
                }

                if (options.ContainsField(ItemFields.Chapters))
                {
                    dto.Chapters = _itemRepo.GetChapters(item);
                }

                if (video.ExtraType.HasValue)
                {
                    dto.ExtraType = video.ExtraType.Value.ToString();
                }
            }

            if (options.ContainsField(ItemFields.MediaStreams))
            {
                // Add VideoInfo
                var iHasMediaSources = item as IHasMediaSources;

                if (iHasMediaSources != null)
                {
                    MediaStream[] mediaStreams;

                    if (dto.MediaSources != null && dto.MediaSources.Length > 0)
                    {
                        if (item.SourceType == SourceType.Channel)
                        {
                            mediaStreams = dto.MediaSources[0].MediaStreams.ToArray();
                        }
                        else
                        {
                            string id = item.Id.ToString("N", CultureInfo.InvariantCulture);
                            mediaStreams = dto.MediaSources.Where(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase))
                                .SelectMany(i => i.MediaStreams)
                                .ToArray();
                        }
                    }
                    else
                    {
                        mediaStreams = _mediaSourceManager.GetStaticMediaSources(item, true)[0].MediaStreams.ToArray();
                    }

                    dto.MediaStreams = mediaStreams;
                }
            }

            BaseItem[] allExtras = null;

            if (options.ContainsField(ItemFields.SpecialFeatureCount))
            {
                allExtras = item.GetExtras().ToArray();
                dto.SpecialFeatureCount = allExtras.Count(i => i.ExtraType.HasValue && BaseItem.DisplayExtraTypes.Contains(i.ExtraType.Value));
            }

            if (options.ContainsField(ItemFields.LocalTrailerCount))
            {
                allExtras ??= item.GetExtras().ToArray();
                dto.LocalTrailerCount = allExtras.Count(i => i.ExtraType == ExtraType.Trailer);

                if (item is IHasTrailers hasTrailers)
                {
                    dto.LocalTrailerCount += hasTrailers.GetTrailerCount();
                }
            }

            // Add EpisodeInfo
            if (item is Episode episode)
            {
                dto.IndexNumberEnd = episode.IndexNumberEnd;
                dto.SeriesName = episode.SeriesName;

                if (options.ContainsField(ItemFields.SpecialEpisodeNumbers))
                {
                    dto.AirsAfterSeasonNumber = episode.AirsAfterSeasonNumber;
                    dto.AirsBeforeEpisodeNumber = episode.AirsBeforeEpisodeNumber;
                    dto.AirsBeforeSeasonNumber = episode.AirsBeforeSeasonNumber;
                }

                dto.SeasonName = episode.SeasonName;
                dto.SeasonId = episode.SeasonId;
                dto.SeriesId = episode.SeriesId;

                Series episodeSeries = null;

                // this block will add the series poster for episodes without a poster
                // TODO maybe remove the if statement entirely
                //if (options.ContainsField(ItemFields.SeriesPrimaryImage))
                {
                    episodeSeries = episodeSeries ?? episode.Series;
                    if (episodeSeries != null)
                    {
                        dto.SeriesPrimaryImageTag = GetTagAndFillBlurhash(dto, episodeSeries, ImageType.Primary);
                    }
                }

                if (options.ContainsField(ItemFields.SeriesStudio))
                {
                    episodeSeries = episodeSeries ?? episode.Series;
                    if (episodeSeries != null)
                    {
                        dto.SeriesStudio = episodeSeries.Studios.FirstOrDefault();
                    }
                }
            }

            // Add SeriesInfo
            if (item is Series series)
            {
                dto.AirDays = series.AirDays;
                dto.AirTime = series.AirTime;
                dto.Status = series.Status.HasValue ? series.Status.Value.ToString() : null;
            }

            // Add SeasonInfo
            if (item is Season season)
            {
                dto.SeriesName = season.SeriesName;
                dto.SeriesId = season.SeriesId;

                series = null;

                if (options.ContainsField(ItemFields.SeriesStudio))
                {
                    series = series ?? season.Series;
                    if (series != null)
                    {
                        dto.SeriesStudio = series.Studios.FirstOrDefault();
                    }
                }

                // this block will add the series poster for seasons without a poster
                // TODO maybe remove the if statement entirely
                //if (options.ContainsField(ItemFields.SeriesPrimaryImage))
                {
                    series = series ?? season.Series;
                    if (series != null)
                    {
                        dto.SeriesPrimaryImageTag = GetTagAndFillBlurhash(dto, series, ImageType.Primary);
                    }
                }
            }

            if (item is MusicVideo musicVideo)
            {
                SetMusicVideoProperties(dto, musicVideo);
            }

            if (item is Book book)
            {
                SetBookProperties(dto, book);
            }

            if (options.ContainsField(ItemFields.ProductionLocations))
            {
                if (item.ProductionLocations.Length > 0 || item is Movie)
                {
                    dto.ProductionLocations = item.ProductionLocations;
                }
            }

            if (options.ContainsField(ItemFields.Width))
            {
                var width = item.Width;
                if (width > 0)
                {
                    dto.Width = width;
                }
            }

            if (options.ContainsField(ItemFields.Height))
            {
                var height = item.Height;
                if (height > 0)
                {
                    dto.Height = height;
                }
            }

            if (options.ContainsField(ItemFields.IsHD))
            {
                // Compatibility
                if (item.IsHD)
                {
                    dto.IsHD = true;
                }
            }

            if (item is Photo photo)
            {
                SetPhotoProperties(dto, photo);
            }

            dto.ChannelId = item.ChannelId;

            if (item.SourceType == SourceType.Channel)
            {
                var channel = _libraryManager.GetItemById(item.ChannelId);
                if (channel != null)
                {
                    dto.ChannelName = channel.Name;
                }
            }
        }

        private BaseItem GetImageDisplayParent(BaseItem currentItem, BaseItem originalItem)
        {
            if (currentItem is MusicAlbum musicAlbum)
            {
                var artist = musicAlbum.GetMusicArtist(new DtoOptions(false));
                if (artist != null)
                {
                    return artist;
                }
            }

            var parent = currentItem.DisplayParent ?? currentItem.GetOwner() ?? currentItem.GetParent();

            if (parent == null && !(originalItem is UserRootFolder) && !(originalItem is UserView) && !(originalItem is AggregateFolder) && !(originalItem is ICollectionFolder) && !(originalItem is Channel))
            {
                parent = _libraryManager.GetCollectionFolders(originalItem).FirstOrDefault();
            }

            return parent;
        }

        private void AddInheritedImages(BaseItemDto dto, BaseItem item, DtoOptions options, BaseItem owner)
        {
            if (!item.SupportsInheritedParentImages)
            {
                return;
            }

            var logoLimit = options.GetImageLimit(ImageType.Logo);
            var artLimit = options.GetImageLimit(ImageType.Art);
            var thumbLimit = options.GetImageLimit(ImageType.Thumb);
            var backdropLimit = options.GetImageLimit(ImageType.Backdrop);

            // For now. Emby apps are not using this
            artLimit = 0;

            if (logoLimit == 0 && artLimit == 0 && thumbLimit == 0 && backdropLimit == 0)
            {
                return;
            }

            BaseItem parent = null;
            var isFirst = true;

            var imageTags = dto.ImageTags;

            while (((!(imageTags != null && imageTags.ContainsKey(ImageType.Logo)) && logoLimit > 0) || (!(imageTags != null && imageTags.ContainsKey(ImageType.Art)) && artLimit > 0) || (!(imageTags != null && imageTags.ContainsKey(ImageType.Thumb)) && thumbLimit > 0) || parent is Series) &&
                (parent = parent ?? (isFirst ? GetImageDisplayParent(item, item) ?? owner : parent)) != null)
            {
                if (parent == null)
                {
                    break;
                }

                var allImages = parent.ImageInfos;

                if (logoLimit > 0 && !(imageTags != null && imageTags.ContainsKey(ImageType.Logo)) && dto.ParentLogoItemId == null)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Logo);

                    if (image != null)
                    {
                        dto.ParentLogoItemId = GetDtoId(parent);
                        dto.ParentLogoImageTag = GetTagAndFillBlurhash(dto, parent, image);
                    }
                }
                if (artLimit > 0 && !(imageTags != null && imageTags.ContainsKey(ImageType.Art)) && dto.ParentArtItemId == null)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Art);

                    if (image != null)
                    {
                        dto.ParentArtItemId = GetDtoId(parent);
                        dto.ParentArtImageTag = GetTagAndFillBlurhash(dto, parent, image);
                    }
                }
                if (thumbLimit > 0 && !(imageTags != null && imageTags.ContainsKey(ImageType.Thumb)) && (dto.ParentThumbItemId == null || parent is Series) && !(parent is ICollectionFolder) && !(parent is UserView))
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Thumb);

                    if (image != null)
                    {
                        dto.ParentThumbItemId = GetDtoId(parent);
                        dto.ParentThumbImageTag = GetTagAndFillBlurhash(dto, parent, image);
                    }
                }
                if (backdropLimit > 0 && !((dto.BackdropImageTags != null && dto.BackdropImageTags.Length > 0) || (dto.ParentBackdropImageTags != null && dto.ParentBackdropImageTags.Length > 0)))
                {
                    var images = allImages.Where(i => i.Type == ImageType.Backdrop).Take(backdropLimit).ToList();

                    if (images.Count > 0)
                    {
                        dto.ParentBackdropItemId = GetDtoId(parent);
                        dto.ParentBackdropImageTags = GetTagsAndFillBlurhashes(dto, parent, ImageType.Backdrop, images);
                    }
                }

                isFirst = false;

                if (!parent.SupportsInheritedParentImages)
                {
                    break;
                }

                parent = GetImageDisplayParent(parent, item);
            }
        }

        private string GetMappedPath(BaseItem item, BaseItem ownerItem)
        {
            var path = item.Path;

            if (item.IsFileProtocol)
            {
                path = _libraryManager.GetPathAfterNetworkSubstitution(path, ownerItem ?? item);
            }

            return path;
        }

        /// <summary>
        /// Attaches the primary image aspect ratio.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        public void AttachPrimaryImageAspectRatio(IItemDto dto, BaseItem item)
        {
            dto.PrimaryImageAspectRatio = GetPrimaryImageAspectRatio(item);
        }

        public double? GetPrimaryImageAspectRatio(BaseItem item)
        {
            var imageInfo = item.GetImageInfo(ImageType.Primary, 0);

            if (imageInfo == null)
            {
                return null;
            }

            ImageDimensions size;

            var defaultAspectRatio = item.GetDefaultPrimaryImageAspectRatio();

            if (defaultAspectRatio > 0)
            {
                return defaultAspectRatio;
            }

            if (!imageInfo.IsLocalFile)
            {
                return null;
            }

            try
            {
                size = _imageProcessor.GetImageDimensions(item, imageInfo);

                if (size.Width <= 0 || size.Height <= 0)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine primary image aspect ratio for {0}", imageInfo.Path);
                return null;
            }

            var width = size.Width;
            var height = size.Height;

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            return width / height;
        }
    }
}
