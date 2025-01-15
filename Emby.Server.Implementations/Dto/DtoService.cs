#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using Book = MediaBrowser.Controller.Entities.Book;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Person = MediaBrowser.Controller.Entities.Person;
using Photo = MediaBrowser.Controller.Entities.Photo;
using Season = MediaBrowser.Controller.Entities.TV.Season;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Emby.Server.Implementations.Dto
{
    public class DtoService : IDtoService
    {
        private readonly ILogger<DtoService> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly IItemRepository _itemRepo;

        private readonly IImageProcessor _imageProcessor;
        private readonly IProviderManager _providerManager;
        private readonly IRecordingsManager _recordingsManager;

        private readonly IApplicationHost _appHost;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly Lazy<ILiveTvManager> _livetvManagerFactory;

        private readonly ITrickplayManager _trickplayManager;
        private readonly IChapterRepository _chapterRepository;

        public DtoService(
            ILogger<DtoService> logger,
            ILibraryManager libraryManager,
            IUserDataManager userDataRepository,
            IItemRepository itemRepo,
            IImageProcessor imageProcessor,
            IProviderManager providerManager,
            IRecordingsManager recordingsManager,
            IApplicationHost appHost,
            IMediaSourceManager mediaSourceManager,
            Lazy<ILiveTvManager> livetvManagerFactory,
            ITrickplayManager trickplayManager,
            IChapterRepository chapterRepository)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _providerManager = providerManager;
            _recordingsManager = recordingsManager;
            _appHost = appHost;
            _mediaSourceManager = mediaSourceManager;
            _livetvManagerFactory = livetvManagerFactory;
            _trickplayManager = trickplayManager;
            _chapterRepository = chapterRepository;
        }

        private ILiveTvManager LivetvManager => _livetvManagerFactory.Value;

        /// <inheritdoc />
        public IReadOnlyList<BaseItemDto> GetBaseItemDtos(IReadOnlyList<BaseItem> items, DtoOptions options, User? user = null, BaseItem? owner = null)
        {
            var accessibleItems = user is null ? items : items.Where(x => x.IsVisible(user)).ToList();
            var returnItems = new BaseItemDto[accessibleItems.Count];
            List<(BaseItem, BaseItemDto)>? programTuples = null;
            List<(BaseItemDto, LiveTvChannel)>? channelTuples = null;

            for (int index = 0; index < accessibleItems.Count; index++)
            {
                var item = accessibleItems[index];
                var dto = GetBaseItemDtoInternal(item, options, user, owner);

                if (item is LiveTvChannel tvChannel)
                {
                    (channelTuples ??= new()).Add((dto, tvChannel));
                }
                else if (item is LiveTvProgram)
                {
                    (programTuples ??= new()).Add((item, dto));
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

                        SetItemByNameInfo(item, dto, libraryItems);
                    }
                }

                returnItems[index] = dto;
            }

            if (programTuples is not null)
            {
                LivetvManager.AddInfoToProgramDto(programTuples, options.Fields, user).GetAwaiter().GetResult();
            }

            if (channelTuples is not null)
            {
                LivetvManager.AddChannelInfo(channelTuples, options, user);
            }

            return returnItems;
        }

        public BaseItemDto GetBaseItemDto(BaseItem item, DtoOptions options, User? user = null, BaseItem? owner = null)
        {
            var dto = GetBaseItemDtoInternal(item, options, user, owner);
            if (item is LiveTvChannel tvChannel)
            {
                LivetvManager.AddChannelInfo(new[] { (dto, tvChannel) }, options, user);
            }
            else if (item is LiveTvProgram)
            {
                LivetvManager.AddInfoToProgramDto(new[] { (item, dto) }, options.Fields, user).GetAwaiter().GetResult();
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
                        }));
            }

            return dto;
        }

        private static IReadOnlyList<BaseItem> GetTaggedItems(IItemByName byName, User? user, DtoOptions options)
        {
            return byName.GetTaggedItems(
                new InternalItemsQuery(user)
                {
                    Recursive = true,
                    DtoOptions = options
                });
        }

        private BaseItemDto GetBaseItemDtoInternal(BaseItem item, DtoOptions options, User? user = null, BaseItem? owner = null)
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
                AttachPeople(dto, item, user);
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
                    _logger.LogError(ex, "Error generating PrimaryImageAspectRatio for {ItemName}", item.Name);
                }
            }

            if (options.ContainsField(ItemFields.DisplayPreferencesId))
            {
                dto.DisplayPreferencesId = item.DisplayPreferencesId.ToString("N", CultureInfo.InvariantCulture);
            }

            if (user is not null)
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
                dto.CanDelete = user is null
                    ? item.CanDelete()
                    : item.CanDelete(user);
            }

            if (options.ContainsField(ItemFields.CanDownload))
            {
                dto.CanDownload = user is null
                    ? item.CanDownload()
                    : item.CanDownload(user);
            }

            if (options.ContainsField(ItemFields.Etag))
            {
                dto.Etag = item.GetEtag(user);
            }

            var activeRecording = _recordingsManager.GetActiveRecordingInfo(item.Path);
            if (activeRecording is not null)
            {
                dto.Type = BaseItemKind.Recording;
                dto.CanDownload = false;
                dto.RunTimeTicks = null;

                if (!string.IsNullOrEmpty(dto.SeriesName))
                {
                    dto.EpisodeTitle = dto.Name;
                    dto.Name = dto.SeriesName;
                }

                LivetvManager.AddInfoToRecordingDto(item, dto, activeRecording, user);
            }

            if (item is Audio audio)
            {
                dto.HasLyrics = audio.GetMediaStreams().Any(s => s.Type == MediaStreamType.Lyric);
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

                var containers = container.Split(',');
                if (containers.Length < 2)
                {
                    continue;
                }

                var path = mediaSource.Path;
                string? fileExtensionContainer = null;

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

                        if (!string.IsNullOrEmpty(path) && containers.Contains(path, StringComparison.OrdinalIgnoreCase))
                        {
                            fileExtensionContainer = path;
                        }
                    }
                }

                mediaSource.Container = fileExtensionContainer ?? containers[0];
            }
        }

        /// <inheritdoc />
        public BaseItemDto GetItemByNameDto(BaseItem item, DtoOptions options, List<BaseItem>? taggedItems, User? user = null)
        {
            var dto = GetBaseItemDtoInternal(item, options, user);

            if (taggedItems is not null && options.ContainsField(ItemFields.ItemCounts))
            {
                SetItemByNameInfo(item, dto, taggedItems);
            }

            return dto;
        }

        private static void SetItemByNameInfo(BaseItem item, BaseItemDto dto, IReadOnlyList<BaseItem> taggedItems)
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
                        var folderChildCount = folder.LinkedChildren.Length;
                        // The default is an empty array, so we can't reliably use the count when it's empty
                        if (folderChildCount > 0)
                        {
                            dto.ChildCount ??= folderChildCount;
                        }
                    }

                    if (options.ContainsField(ItemFields.ChildCount))
                    {
                        dto.ChildCount ??= GetChildCount(folder, user);
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
        }

        private static int GetChildCount(Folder folder, User user)
        {
            // Right now this is too slow to calculate for top level folders on a per-user basis
            // Just return something so that apps that are expecting a value won't think the folders are empty
            if (folder is ICollectionFolder || folder is UserView)
            {
                return Random.Shared.Next(1, 10);
            }

            return folder.GetChildCount(user);
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

            if (album is not null)
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
                    IncludeItemTypes = new[] { BaseItemKind.MusicAlbum },
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
                .Where(i => i is not null)
                .ToArray()!; // null values got filtered out
        }

        private string? GetImageCacheTag(BaseItem item, ItemImageInfo image)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {ImageType} image info for {Path}", image.Type, image.Path);
                return null;
            }
        }

        /// <summary>
        /// Attaches People DTO's to a DTOBaseItem.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="user">The requesting user.</param>
        private void AttachPeople(BaseItemDto dto, BaseItem item, User? user = null)
        {
            // Ordering by person type to ensure actors and artists are at the front.
            // This is taking advantage of the fact that they both begin with A
            // This should be improved in the future
            var people = _libraryManager.GetPeople(item).OrderBy(i => i.SortOrder ?? int.MaxValue)
                .ThenBy(i =>
                {
                    if (i.IsType(PersonKind.Actor))
                    {
                        return 0;
                    }

                    if (i.IsType(PersonKind.GuestStar))
                    {
                        return 1;
                    }

                    if (i.IsType(PersonKind.Director))
                    {
                        return 2;
                    }

                    if (i.IsType(PersonKind.Writer))
                    {
                        return 3;
                    }

                    if (i.IsType(PersonKind.Producer))
                    {
                        return 4;
                    }

                    if (i.IsType(PersonKind.Composer))
                    {
                        return 4;
                    }

                    return 10;
                })
                .ToList();

            var list = new List<BaseItemPerson>();

            Dictionary<string, Person> dictionary = people.Select(p => p.Name)
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
                }).Where(i => i is not null)
                .Where(i => user is null || i!.IsVisible(user))
                .DistinctBy(x => x!.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i!.Name, StringComparer.OrdinalIgnoreCase)!; // null values got filtered out

            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];

                var baseItemPerson = new BaseItemPerson
                {
                    Name = person.Name,
                    Role = person.Role,
                    Type = person.Type
                };

                if (dictionary.TryGetValue(person.Name, out Person? entity))
                {
                    baseItemPerson.PrimaryImageTag = GetTagAndFillBlurhash(dto, entity, ImageType.Primary);
                    baseItemPerson.Id = entity.Id;
                    if (dto.ImageBlurHashes is not null)
                    {
                        // Only add BlurHash for the person's image.
                        baseItemPerson.ImageBlurHashes = new Dictionary<ImageType, Dictionary<string, string>>();
                        foreach (var (imageType, blurHash) in dto.ImageBlurHashes)
                        {
                            if (blurHash is not null)
                            {
                                baseItemPerson.ImageBlurHashes[imageType] = new Dictionary<string, string>();
                                foreach (var (imageId, blurHashValue) in blurHash)
                                {
                                    if (string.Equals(baseItemPerson.PrimaryImageTag, imageId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        baseItemPerson.ImageBlurHashes[imageType][imageId] = blurHashValue;
                                    }
                                }
                            }
                        }
                    }

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

        private string? GetTagAndFillBlurhash(BaseItemDto dto, BaseItem item, ImageType imageType, int imageIndex = 0)
        {
            var image = item.GetImageInfo(imageType, imageIndex);
            if (image is not null)
            {
                return GetTagAndFillBlurhash(dto, item, image);
            }

            return null;
        }

        private string? GetTagAndFillBlurhash(BaseItemDto dto, BaseItem item, ItemImageInfo image)
        {
            var tag = GetImageCacheTag(item, image);
            if (tag is null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(image.BlurHash))
            {
                dto.ImageBlurHashes ??= new Dictionary<ImageType, Dictionary<string, string>>();

                if (!dto.ImageBlurHashes.TryGetValue(image.Type, out var value))
                {
                    value = new Dictionary<string, string>();
                    dto.ImageBlurHashes[image.Type] = value;
                }

                value[tag] = image.BlurHash;
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
                dto.ImageBlurHashes ??= new Dictionary<ImageType, Dictionary<string, string>>();

                dto.ImageBlurHashes[imageType] = hashes;
            }

            return tags;
        }

        /// <summary>
        /// Sets simple property values on a DTOBaseItem.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="options">The options.</param>
        private void AttachBasicFields(BaseItemDto dto, BaseItem item, BaseItem? owner, DtoOptions options)
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

            if (item is IHasAspectRatio hasAspectRatio)
            {
                dto.AspectRatio = hasAspectRatio.AspectRatio;
            }

            dto.ImageBlurHashes = new Dictionary<ImageType, Dictionary<string, string>>();

            var backdropLimit = options.GetImageLimit(ImageType.Backdrop);
            if (backdropLimit > 0)
            {
                dto.BackdropImageTags = GetTagsAndFillBlurhashes(dto, item, ImageType.Backdrop, backdropLimit);
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

                        if (tag is not null)
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

            if (item is not LiveTvProgram)
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

                dto.Taglines ??= Array.Empty<string>();
            }

            dto.Type = item.GetBaseItemKind();
            if ((item.CommunityRating ?? 0) > 0)
            {
                dto.CommunityRating = item.CommunityRating;
            }

            if (item is ISupportsPlaceHolders supportsPlaceHolders && supportsPlaceHolders.IsPlaceHolder)
            {
                dto.IsPlaceHolder = supportsPlaceHolders.IsPlaceHolder;
            }

            if (item.LUFS.HasValue)
            {
                // -18 LUFS reference, same as ReplayGain 2.0, compatible with ReplayGain 1.0
                dto.NormalizationGain = -18f - item.LUFS;
            }
            else if (item.NormalizationGain.HasValue)
            {
                dto.NormalizationGain = item.NormalizationGain;
            }

            // Add audio info
            if (item is Audio audio)
            {
                dto.Album = audio.Album;
                dto.ExtraType = audio.ExtraType;

                var albumParent = audio.AlbumEntity;

                if (albumParent is not null)
                {
                    dto.AlbumId = albumParent.Id;
                    dto.AlbumPrimaryImageTag = GetTagAndFillBlurhash(dto, albumParent, ImageType.Primary);
                }

                // if (options.ContainsField(ItemFields.MediaSourceCount))
                // {
                // Songs always have one
                // }
            }

            if (item is IHasArtist hasArtist)
            {
                dto.Artists = hasArtist.Artists;

                // var artistItems = _libraryManager.GetArtists(new InternalItemsQuery
                // {
                //    EnableTotalRecordCount = false,
                //    ItemIds = new[] { item.Id.ToString("N", CultureInfo.InvariantCulture) }
                // });

                // dto.ArtistItems = artistItems.Items
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
                // var foundArtists = artistItems.Items.Select(i => i.Item1.Name).ToList();
                dto.ArtistItems = hasArtist.Artists
                    // .Except(foundArtists, new DistinctNameComparer())
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
                        if (artist is not null)
                        {
                            return new NameGuidPair
                            {
                                Name = artist.Name,
                                Id = artist.Id
                            };
                        }

                        return null;
                    }).Where(i => i is not null).ToArray();
            }

            if (item is IHasAlbumArtist hasAlbumArtist)
            {
                dto.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();

                // var artistItems = _libraryManager.GetAlbumArtists(new InternalItemsQuery
                // {
                //    EnableTotalRecordCount = false,
                //    ItemIds = new[] { item.Id.ToString("N", CultureInfo.InvariantCulture) }
                // });

                // dto.AlbumArtists = artistItems.Items
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
                    // .Except(foundArtists, new DistinctNameComparer())
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
                        if (artist is not null)
                        {
                            return new NameGuidPair
                            {
                                Name = artist.Name,
                                Id = artist.Id
                            };
                        }

                        return null;
                    }).Where(i => i is not null).ToArray();
            }

            // Add video info
            if (item is Video video)
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
                    dto.Chapters = _chapterRepository.GetChapters(item.Id).ToList();
                }

                if (options.ContainsField(ItemFields.Trickplay))
                {
                    dto.Trickplay = _trickplayManager.GetTrickplayManifest(item).GetAwaiter().GetResult();
                }

                dto.ExtraType = video.ExtraType;
            }

            if (options.ContainsField(ItemFields.MediaStreams))
            {
                // Add VideoInfo
                if (item is IHasMediaSources)
                {
                    MediaStream[] mediaStreams;

                    if (dto.MediaSources is not null && dto.MediaSources.Length > 0)
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

            BaseItem[]? allExtras = null;

            if (options.ContainsField(ItemFields.SpecialFeatureCount))
            {
                allExtras = item.GetExtras().ToArray();
                dto.SpecialFeatureCount = allExtras.Count(i => i.ExtraType.HasValue && BaseItem.DisplayExtraTypes.Contains(i.ExtraType.Value));
            }

            if (options.ContainsField(ItemFields.LocalTrailerCount))
            {
                if (item is IHasTrailers hasTrailers)
                {
                    dto.LocalTrailerCount = hasTrailers.LocalTrailers.Count;
                }
                else
                {
                    dto.LocalTrailerCount = (allExtras ?? item.GetExtras()).Count(i => i.ExtraType == ExtraType.Trailer);
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

                Series? episodeSeries = null;

                // this block will add the series poster for episodes without a poster
                // TODO maybe remove the if statement entirely
                // if (options.ContainsField(ItemFields.SeriesPrimaryImage))
                {
                    episodeSeries ??= episode.Series;
                    if (episodeSeries is not null)
                    {
                        dto.SeriesPrimaryImageTag = GetTagAndFillBlurhash(dto, episodeSeries, ImageType.Primary);
                        if (dto.ImageTags is null || !dto.ImageTags.ContainsKey(ImageType.Primary))
                        {
                            AttachPrimaryImageAspectRatio(dto, episodeSeries);
                        }
                    }
                }

                if (options.ContainsField(ItemFields.SeriesStudio))
                {
                    episodeSeries ??= episode.Series;
                    if (episodeSeries is not null)
                    {
                        dto.SeriesStudio = episodeSeries.Studios.FirstOrDefault();
                    }
                }
            }

            // Add SeriesInfo
            Series? series;
            if (item is Series tmp)
            {
                series = tmp;
                dto.AirDays = series.AirDays;
                dto.AirTime = series.AirTime;
                dto.Status = series.Status?.ToString();
            }

            // Add SeasonInfo
            if (item is Season season)
            {
                dto.SeriesName = season.SeriesName;
                dto.SeriesId = season.SeriesId;

                series = null;

                if (options.ContainsField(ItemFields.SeriesStudio))
                {
                    series ??= season.Series;
                    if (series is not null)
                    {
                        dto.SeriesStudio = series.Studios.FirstOrDefault();
                    }
                }

                // this block will add the series poster for seasons without a poster
                // TODO maybe remove the if statement entirely
                // if (options.ContainsField(ItemFields.SeriesPrimaryImage))
                {
                    series ??= season.Series;
                    if (series is not null)
                    {
                        dto.SeriesPrimaryImageTag = GetTagAndFillBlurhash(dto, series, ImageType.Primary);
                        if (dto.ImageTags is null || !dto.ImageTags.ContainsKey(ImageType.Primary))
                        {
                            AttachPrimaryImageAspectRatio(dto, series);
                        }
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
                if (channel is not null)
                {
                    dto.ChannelName = channel.Name;
                }
            }
        }

        private BaseItem? GetImageDisplayParent(BaseItem currentItem, BaseItem originalItem)
        {
            if (currentItem is MusicAlbum musicAlbum)
            {
                var artist = musicAlbum.GetMusicArtist(new DtoOptions(false));
                if (artist is not null)
                {
                    return artist;
                }
            }

            var parent = currentItem.DisplayParent ?? currentItem.GetOwner() ?? currentItem.GetParent();

            if (parent is null && originalItem is not UserRootFolder && originalItem is not UserView && originalItem is not AggregateFolder && originalItem is not ICollectionFolder && originalItem is not Channel)
            {
                parent = _libraryManager.GetCollectionFolders(originalItem).FirstOrDefault();
            }

            return parent;
        }

        private void AddInheritedImages(BaseItemDto dto, BaseItem item, DtoOptions options, BaseItem? owner)
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

            BaseItem? parent = null;
            var isFirst = true;

            var imageTags = dto.ImageTags;

            while ((!(imageTags is not null && imageTags.ContainsKey(ImageType.Logo)) && logoLimit > 0)
                || (!(imageTags is not null && imageTags.ContainsKey(ImageType.Art)) && artLimit > 0)
                || (!(imageTags is not null && imageTags.ContainsKey(ImageType.Thumb)) && thumbLimit > 0)
                || parent is Series)
            {
                parent ??= isFirst ? GetImageDisplayParent(item, item) ?? owner : parent;
                if (parent is null)
                {
                    break;
                }

                var allImages = parent.ImageInfos;

                if (logoLimit > 0 && !(imageTags is not null && imageTags.ContainsKey(ImageType.Logo)) && dto.ParentLogoItemId is null)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Logo);

                    if (image is not null)
                    {
                        dto.ParentLogoItemId = parent.Id;
                        dto.ParentLogoImageTag = GetTagAndFillBlurhash(dto, parent, image);
                    }
                }

                if (artLimit > 0 && !(imageTags is not null && imageTags.ContainsKey(ImageType.Art)) && dto.ParentArtItemId is null)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Art);

                    if (image is not null)
                    {
                        dto.ParentArtItemId = parent.Id;
                        dto.ParentArtImageTag = GetTagAndFillBlurhash(dto, parent, image);
                    }
                }

                if (thumbLimit > 0 && !(imageTags is not null && imageTags.ContainsKey(ImageType.Thumb)) && (dto.ParentThumbItemId is null || parent is Series) && parent is not ICollectionFolder && parent is not UserView)
                {
                    var image = allImages.FirstOrDefault(i => i.Type == ImageType.Thumb);

                    if (image is not null)
                    {
                        dto.ParentThumbItemId = parent.Id;
                        dto.ParentThumbImageTag = GetTagAndFillBlurhash(dto, parent, image);
                    }
                }

                if (backdropLimit > 0 && !((dto.BackdropImageTags is not null && dto.BackdropImageTags.Length > 0) || (dto.ParentBackdropImageTags is not null && dto.ParentBackdropImageTags.Length > 0)))
                {
                    var images = allImages.Where(i => i.Type == ImageType.Backdrop).Take(backdropLimit).ToList();

                    if (images.Count > 0)
                    {
                        dto.ParentBackdropItemId = parent.Id;
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

        private string GetMappedPath(BaseItem item, BaseItem? ownerItem)
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
        public void AttachPrimaryImageAspectRatio(IItemDto dto, BaseItem item)
        {
            dto.PrimaryImageAspectRatio = GetPrimaryImageAspectRatio(item);
        }

        public double? GetPrimaryImageAspectRatio(BaseItem item)
        {
            var imageInfo = item.GetImageInfo(ImageType.Primary, 0);

            if (imageInfo is null)
            {
                return null;
            }

            if (!imageInfo.IsLocalFile)
            {
                return item.GetDefaultPrimaryImageAspectRatio();
            }

            try
            {
                var size = _imageProcessor.GetImageDimensions(item, imageInfo);
                var width = size.Width;
                var height = size.Height;
                if (width > 0 && height > 0)
                {
                    return (double)width / height;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine primary image aspect ratio for {ImagePath}", imageInfo.Path);
            }

            return item.GetDefaultPrimaryImageAspectRatio();
        }
    }
}
