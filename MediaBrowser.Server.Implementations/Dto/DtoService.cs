using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Dto
{
    public class DtoService : IDtoService
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly IItemRepository _itemRepo;

        private readonly IImageProcessor _imageProcessor;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IProviderManager _providerManager;

        public DtoService(ILogger logger, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IImageProcessor imageProcessor, IServerConfigurationManager config, IFileSystem fileSystem, IProviderManager providerManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _config = config;
            _fileSystem = fileSystem;
            _providerManager = providerManager;
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
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (fields == null)
            {
                throw new ArgumentNullException("fields");
            }

            var dto = new BaseItemDto();

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
                AttachUserSpecificInfo(dto, item, user, fields);
            }

            if (fields.Contains(ItemFields.Studios))
            {
                AttachStudios(dto, item);
            }

            AttachBasicFields(dto, item, owner, fields);

            if (fields.Contains(ItemFields.SoundtrackIds))
            {
                var hasSoundtracks = item as IHasSoundtracks;

                if (hasSoundtracks != null)
                {
                    dto.SoundtrackIds = hasSoundtracks.SoundtrackIds
                        .Select(i => i.ToString("N"))
                        .ToArray();
                }
            }

            return dto;
        }

        public BaseItemDto GetItemByNameDto<T>(T item, List<ItemFields> fields, User user = null)
            where T : BaseItem, IItemByName
        {
            var libraryItems = user != null ? user.RootFolder.GetRecursiveChildren(user) : 
                _libraryManager.RootFolder.RecursiveChildren;

            return GetItemByNameDto(item, fields, item.GetTaggedItems(libraryItems).ToList(), user);
        }

        public BaseItemDto GetItemByNameDto<T>(T item, List<ItemFields> fields, List<BaseItem> taggedItems, User user = null)
            where T : BaseItem, IItemByName
        {
            var dto = GetBaseItemDto(item, fields, user);

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

                dto.AdultVideoCount = taggedItems.Count(i => i is AdultVideo);
                dto.AlbumCount = taggedItems.Count(i => i is MusicAlbum);
                dto.EpisodeCount = taggedItems.Count(i => i is Episode);
                dto.GameCount = taggedItems.Count(i => i is Game);
                dto.MovieCount = taggedItems.Count(i => i is Movie);
                dto.MusicVideoCount = taggedItems.Count(i => i is MusicVideo);
                dto.SeriesCount = taggedItems.Count(i => i is Series);
                dto.SongCount = taggedItems.Count(i => i is Audio);
                dto.TrailerCount = taggedItems.Count(i => i is Trailer);
            }

            dto.ChildCount = taggedItems.Count;

            return dto;
        }

        /// <summary>
        /// Attaches the user specific info.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="fields">The fields.</param>
        private void AttachUserSpecificInfo(BaseItemDto dto, BaseItem item, User user, List<ItemFields> fields)
        {
            if (item.IsFolder)
            {
                var folder = (Folder)item;

                dto.ChildCount = GetChildCount(folder, user);

                if (!(folder is UserRootFolder))
                {
                    SetSpecialCounts(folder, user, dto, fields);
                }
            }

            var userData = _userDataRepository.GetUserData(user.Id, item.GetUserDataKey());

            dto.UserData = GetUserItemDataDto(userData);

            if (item.IsFolder)
            {
                dto.UserData.Played = dto.PlayedPercentage.HasValue && dto.PlayedPercentage.Value >= 100;
            }

            dto.PlayAccess = item.GetPlayAccess(user);
        }

        private int GetChildCount(Folder folder, User user)
        {
            return folder.GetChildren(user, true)
                .Count();
        }

        public UserDto GetUserDto(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var dto = new UserDto
            {
                Id = user.Id.ToString("N"),
                Name = user.Name,
                HasPassword = !String.IsNullOrEmpty(user.Password),
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate,
                Configuration = user.Configuration
            };

            var image = user.GetImageInfo(ImageType.Primary, 0);

            if (image != null)
            {
                dto.PrimaryImageTag = GetImageCacheTag(user, image);

                try
                {
                    AttachPrimaryImageAspectRatio(dto, user);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, user.Name);
                }
            }

            return dto;
        }

        public SessionInfoDto GetSessionInfoDto(SessionInfo session)
        {
            var dto = new SessionInfoDto
            {
                Client = session.Client,
                DeviceId = session.DeviceId,
                DeviceName = session.DeviceName,
                Id = session.Id.ToString("N"),
                LastActivityDate = session.LastActivityDate,
                NowPlayingPositionTicks = session.NowPlayingPositionTicks,
                SupportsRemoteControl = session.SupportsRemoteControl,
                IsPaused = session.IsPaused,
                IsMuted = session.IsMuted,
                NowViewingContext = session.NowViewingContext,
                NowViewingItemId = session.NowViewingItemId,
                NowViewingItemName = session.NowViewingItemName,
                NowViewingItemType = session.NowViewingItemType,
                ApplicationVersion = session.ApplicationVersion,
                CanSeek = session.CanSeek,
                QueueableMediaTypes = session.QueueableMediaTypes,
                PlayableMediaTypes = session.PlayableMediaTypes,
                RemoteEndPoint = session.RemoteEndPoint,
                AdditionalUsers = session.AdditionalUsers,
                SupportsFullscreenToggle = session.SupportsFullscreenToggle,
                SupportsNavigationControl = session.SupportsNavigationControl,
                SupportsOsdToggle = session.SupportsOsdToggle
            };

            if (session.NowPlayingItem != null)
            {
                dto.NowPlayingItem = GetNowPlayingInfo(session.NowPlayingItem, session.NowPlayingMediaVersionId, session.NowPlayingRunTimeTicks);
            }

            if (session.UserId.HasValue)
            {
                dto.UserId = session.UserId.Value.ToString("N");
            }
            dto.UserName = session.UserName;

            return dto;
        }

        /// <summary>
        /// Converts a BaseItem to a BaseItemInfo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="mediaVersionId">The media version identifier.</param>
        /// <param name="nowPlayingRuntimeTicks">The now playing runtime ticks.</param>
        /// <returns>BaseItemInfo.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private BaseItemInfo GetNowPlayingInfo(BaseItem item, string mediaVersionId, long? nowPlayingRuntimeTicks)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var info = new BaseItemInfo
            {
                Id = GetDtoId(item),
                Name = item.Name,
                MediaType = item.MediaType,
                Type = item.GetClientTypeName(),
                RunTimeTicks = nowPlayingRuntimeTicks,
                MediaVersionId = mediaVersionId
            };

            info.PrimaryImageTag = GetImageCacheTag(item, ImageType.Primary);

            var backropItem = item.HasImage(ImageType.Backdrop) ? item : null;

            var thumbItem = item.HasImage(ImageType.Thumb) ? item : null;

            if (thumbItem == null)
            {
                var episode = item as Episode;

                if (episode != null)
                {
                    var series = episode.Series;

                    if (series != null && series.HasImage(ImageType.Thumb))
                    {
                        thumbItem = series;
                    }
                }
            }

            if (backropItem == null)
            {
                var episode = item as Episode;

                if (episode != null)
                {
                    var series = episode.Series;

                    if (series != null && series.HasImage(ImageType.Backdrop))
                    {
                        backropItem = series;
                    }
                }
            }

            if (thumbItem == null)
            {
                thumbItem = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Thumb));
            }

            if (thumbItem != null)
            {
                info.ThumbImageTag = GetImageCacheTag(thumbItem, ImageType.Thumb);
                info.ThumbItemId = GetDtoId(thumbItem);
            }

            if (thumbItem != null)
            {
                info.BackdropImageTag = GetImageCacheTag(backropItem, ImageType.Backdrop);
                info.BackdropItemId = GetDtoId(backropItem);
            }

            return info;
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

        private void SetMusicVideoProperties(BaseItemDto dto, MusicVideo item)
        {
            if (!string.IsNullOrEmpty(item.Album))
            {
                var parentAlbum = _libraryManager.RootFolder
                    .GetRecursiveChildren(i => i is MusicAlbum)
                    .FirstOrDefault(i => string.Equals(i.Name, item.Album, StringComparison.OrdinalIgnoreCase));

                if (parentAlbum != null)
                {
                    dto.AlbumId = GetDtoId(parentAlbum);
                }
            }

            dto.Album = item.Album;
            dto.Artists = string.IsNullOrEmpty(item.Artist) ? new List<string>() : new List<string> { item.Artist };
        }

        private void SetGameProperties(BaseItemDto dto, Game item)
        {
            dto.Players = item.PlayersSupported;
            dto.GameSystem = item.GameSystem;
        }

        private void SetGameSystemProperties(BaseItemDto dto, GameSystem item)
        {
            dto.GameSystem = item.GameSystemName;
        }

        /// <summary>
        /// Gets the backdrop image tags.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{System.String}.</returns>
        private List<Guid> GetBackdropImageTags(BaseItem item)
        {
            return GetCacheTags(item, ImageType.Backdrop).ToList();
        }

        /// <summary>
        /// Gets the screenshot image tags.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{Guid}.</returns>
        private List<Guid> GetScreenshotImageTags(BaseItem item)
        {
            var hasScreenshots = item as IHasScreenshots;
            if (hasScreenshots == null)
            {
                return new List<Guid>();
            }
            return GetCacheTags(item, ImageType.Screenshot).ToList();
        }

        private IEnumerable<Guid> GetCacheTags(BaseItem item, ImageType type)
        {
            return item.GetImages(type)
                .Select(p => GetImageCacheTag(item, p))
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToList();
        }

        private Guid? GetImageCacheTag(BaseItem item, ImageType type)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, type);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error getting {0} image info", ex, type);
                return null;
            }
        }

        private Guid? GetImageCacheTag(BaseItem item, ItemImageInfo image)
        {
            try
            {
                return _imageProcessor.GetImageCacheTag(item, image);
            }
            catch (IOException ex)
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
            var people = item.People.OrderBy(i => i.SortOrder ?? int.MaxValue).ThenBy(i => i.Type).ToList();

            // Attach People by transforming them into BaseItemPerson (DTO)
            dto.People = new BaseItemPerson[people.Count];

            var dictionary = people.Select(p => p.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Select(c =>
                {
                    try
                    {
                        return _libraryManager.GetPerson(c);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error getting person {0}", ex, c);
                        return null;
                    }

                }).Where(i => i != null)
                .DistinctBy(i => i.Name)
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
                }

                dto.People[i] = baseItemPerson;
            }
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
            var parent = item.Parent ?? owner;

            while (parent != null)
            {
                if (parent.GetImages(ImageType.Backdrop).Any())
                {
                    return parent;
                }

                parent = parent.Parent;
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
            var parent = item.Parent ?? owner;

            while (parent != null)
            {
                if (parent.HasImage(type))
                {
                    return parent;
                }

                parent = parent.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets the chapter info dto.
        /// </summary>
        /// <param name="chapterInfo">The chapter info.</param>
        /// <param name="item">The item.</param>
        /// <returns>ChapterInfoDto.</returns>
        public ChapterInfoDto GetChapterInfoDto(ChapterInfo chapterInfo, BaseItem item)
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


        /// <summary>
        /// Gets a BaseItem based upon it's client-side item id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem GetItemByDtoId(string id, Guid? userId = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            BaseItem item = null;

            if (userId.HasValue)
            {
                item = _libraryManager.GetItemById(new Guid(id));
            }

            // If we still don't find it, look within individual user views
            if (item == null && !userId.HasValue)
            {
                foreach (var user in _userManager.Users)
                {
                    item = GetItemByDtoId(id, user.Id);

                    if (item != null)
                    {
                        break;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Sets simple property values on a DTOBaseItem
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="fields">The fields.</param>
        private void AttachBasicFields(BaseItemDto dto, BaseItem item, BaseItem owner, List<ItemFields> fields)
        {
            if (fields.Contains(ItemFields.DateCreated))
            {
                dto.DateCreated = item.DateCreated;
            }

            if (fields.Contains(ItemFields.DisplayMediaType))
            {
                dto.DisplayMediaType = item.DisplayMediaType;
            }

            dto.IsUnidentified = item.IsUnidentified;

            if (fields.Contains(ItemFields.Settings))
            {
                dto.LockedFields = item.LockedFields;
                dto.LockData = item.DontFetchMeta;
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

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null)
            {
                dto.Metascore = hasMetascore.Metascore;
            }

            if (fields.Contains(ItemFields.AwardSummary))
            {
                var hasAwards = item as IHasAwards;
                if (hasAwards != null)
                {
                    dto.AwardSummary = hasAwards.AwardSummary;
                }
            }

            dto.BackdropImageTags = GetBackdropImageTags(item);

            if (fields.Contains(ItemFields.ScreenshotImageTags))
            {
                dto.ScreenshotImageTags = GetScreenshotImageTags(item);
            }

            if (fields.Contains(ItemFields.Genres))
            {
                dto.Genres = item.Genres;
            }

            dto.ImageTags = new Dictionary<ImageType, Guid>();

            // Prevent implicitly captured closure
            var currentItem = item;
            foreach (var image in currentItem.ImageInfos.Where(i => !currentItem.AllowsMultipleImages(i.Type)))
            {
                var tag = GetImageCacheTag(item, image);

                if (tag.HasValue)
                {
                    dto.ImageTags[image.Type] = tag.Value;
                }
            }

            dto.Id = GetDtoId(item);
            dto.IndexNumber = item.IndexNumber;
            dto.IsFolder = item.IsFolder;
            dto.MediaType = item.MediaType;
            dto.LocationType = item.LocationType;

            var hasLang = item as IHasPreferredMetadataLanguage;

            if (hasLang != null)
            {
                dto.PreferredMetadataCountryCode = hasLang.PreferredMetadataCountryCode;
                dto.PreferredMetadataLanguage = hasLang.PreferredMetadataLanguage;
            }

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
                dto.LocalTrailerCount = hasTrailers.LocalTrailerIds.Count;
            }

            var hasDisplayOrder = item as IHasDisplayOrder;
            if (hasDisplayOrder != null)
            {
                dto.DisplayOrder = hasDisplayOrder.DisplayOrder;
            }

            var collectionFolder = item as CollectionFolder;
            if (collectionFolder != null)
            {
                dto.CollectionType = collectionFolder.CollectionType;
            }

            if (fields.Contains(ItemFields.RemoteTrailers))
            {
                dto.RemoteTrailers = hasTrailers != null ?
                    hasTrailers.RemoteTrailers :
                    new List<MediaUrl>();
            }

            dto.Name = item.Name;
            dto.OfficialRating = item.OfficialRating;

            var hasOverview = fields.Contains(ItemFields.Overview);
            var hasHtmlOverview = fields.Contains(ItemFields.OverviewHtml);

            if (hasOverview || hasHtmlOverview)
            {
                var strippedOverview = string.IsNullOrEmpty(item.Overview) ? item.Overview : item.Overview.StripHtml();

                if (hasOverview)
                {
                    dto.Overview = strippedOverview;
                }

                // Only supply the html version if there was actually html content
                if (hasHtmlOverview)
                {
                    dto.OverviewHtml = item.Overview;
                }
            }

            // If there are no backdrops, indicate what parent has them in case the Ui wants to allow inheritance
            if (dto.BackdropImageTags.Count == 0)
            {
                var parentWithBackdrop = GetParentBackdropItem(item, owner);

                if (parentWithBackdrop != null)
                {
                    dto.ParentBackdropItemId = GetDtoId(parentWithBackdrop);
                    dto.ParentBackdropImageTags = GetBackdropImageTags(parentWithBackdrop);
                }
            }

            if (item.Parent != null && fields.Contains(ItemFields.ParentId))
            {
                dto.ParentId = GetDtoId(item.Parent);
            }

            dto.ParentIndexNumber = item.ParentIndexNumber;

            // If there is no logo, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasLogo)
            {
                var parentWithLogo = GetParentImageItem(item, ImageType.Logo, owner);

                if (parentWithLogo != null)
                {
                    dto.ParentLogoItemId = GetDtoId(parentWithLogo);

                    dto.ParentLogoImageTag = GetImageCacheTag(parentWithLogo, ImageType.Logo);
                }
            }

            // If there is no art, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasArtImage)
            {
                var parentWithImage = GetParentImageItem(item, ImageType.Art, owner);

                if (parentWithImage != null)
                {
                    dto.ParentArtItemId = GetDtoId(parentWithImage);

                    dto.ParentArtImageTag = GetImageCacheTag(parentWithImage, ImageType.Art);
                }
            }

            // If there is no thumb, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasThumb)
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
                var locationType = item.LocationType;

                if (locationType != LocationType.Remote && locationType != LocationType.Virtual)
                {
                    dto.Path = GetMappedPath(item.Path);
                }
                else
                {
                    dto.Path = item.Path;
                }
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
            dto.VoteCount = item.VoteCount;

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                if (fields.Contains(ItemFields.IndexOptions))
                {
                    dto.IndexOptions = folder.IndexByOptionStrings.ToArray();
                }
            }

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
                dto.Artists = audio.Artists;

                var albumParent = audio.FindParent<MusicAlbum>();

                if (albumParent != null)
                {
                    dto.AlbumId = GetDtoId(albumParent);

                    dto.AlbumPrimaryImageTag = GetImageCacheTag(albumParent, ImageType.Primary);
                }

                dto.MediaVersions = GetMediaVersions(audio);
                dto.MediaVersionCount = 1;
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                dto.Artists = album.Artists;

                dto.SoundtrackIds = album.SoundtrackIds
                    .Select(i => i.ToString("N"))
                    .ToArray();
            }

            var hasAlbumArtist = item as IHasAlbumArtist;

            if (hasAlbumArtist != null)
            {
                dto.AlbumArtist = hasAlbumArtist.AlbumArtist;
            }

            // Add video info
            var video = item as Video;
            if (video != null)
            {
                dto.VideoType = video.VideoType;
                dto.Video3DFormat = video.Video3DFormat;
                dto.IsoType = video.IsoType;
                dto.IsHD = video.IsHD;

                dto.PartCount = video.AdditionalPartIds.Count + 1;
                dto.MediaVersionCount = video.AlternateVersionCount + 1;

                if (fields.Contains(ItemFields.MediaVersions))
                {
                    dto.MediaVersions = GetMediaVersions(video);
                }

                if (fields.Contains(ItemFields.Chapters))
                {
                    List<ChapterInfoDto> chapters;

                    if (dto.MediaVersions != null && dto.MediaVersions.Count > 0)
                    {
                        chapters = _itemRepo.GetChapters(item.Id).Select(c => GetChapterInfoDto(c, item)).ToList();
                    }
                    else
                    {
                        chapters = _itemRepo.GetChapters(video.Id)
                            .Select(c => GetChapterInfoDto(c, item))
                            .ToList();
                    }

                    dto.Chapters = chapters;
                }
            }

            if (fields.Contains(ItemFields.MediaStreams))
            {
                // Add VideoInfo
                var iHasMediaStreams = item as IHasMediaStreams;

                if (iHasMediaStreams != null)
                {
                    List<MediaStream> mediaStreams;

                    if (dto.MediaVersions != null && dto.MediaVersions.Count > 0)
                    {
                        mediaStreams = dto.MediaVersions.Where(i => i.IsPrimaryVersion)
                            .SelectMany(i => i.MediaStreams)
                            .ToList();
                    }
                    else
                    {
                        mediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery
                        {
                            ItemId = item.Id

                        }).ToList();
                    }

                    dto.MediaStreams = mediaStreams;
                }
            }

            // Add MovieInfo
            var movie = item as Movie;

            if (movie != null)
            {
                var specialFeatureCount = movie.SpecialFeatureIds.Count;

                if (specialFeatureCount > 0)
                {
                    dto.SpecialFeatureCount = specialFeatureCount;
                }

                if (fields.Contains(ItemFields.TmdbCollectionName))
                {
                    dto.TmdbCollectionName = movie.TmdbCollectionName;
                }
            }

            // Add EpisodeInfo
            var episode = item as Episode;

            if (episode != null)
            {
                dto.IndexNumberEnd = episode.IndexNumberEnd;

                dto.DvdSeasonNumber = episode.DvdSeasonNumber;
                dto.DvdEpisodeNumber = episode.DvdEpisodeNumber;
                dto.AirsAfterSeasonNumber = episode.AirsAfterSeasonNumber;
                dto.AirsBeforeEpisodeNumber = episode.AirsBeforeEpisodeNumber;
                dto.AirsBeforeSeasonNumber = episode.AirsBeforeSeasonNumber;
                dto.AbsoluteEpisodeNumber = episode.AbsoluteEpisodeNumber;

                var seasonId = episode.SeasonId;
                if (seasonId.HasValue)
                {
                    dto.SeasonId = seasonId.Value.ToString("N");
                }
            }

            // Add SeriesInfo
            var series = item as Series;

            if (series != null)
            {
                dto.AirDays = series.AirDays;
                dto.AirTime = series.AirTime;
                dto.Status = series.Status;

                dto.SpecialFeatureCount = series.SpecialFeatureIds.Count;

                dto.SeasonCount = series.SeasonCount;

                if (fields.Contains(ItemFields.Settings))
                {
                    dto.DisplaySpecialsWithSeasons = series.DisplaySpecialsWithSeasons;
                }

                dto.AnimeSeriesIndex = series.AnimeSeriesIndex;
            }

            if (episode != null)
            {
                series = item.FindParent<Series>();

                dto.SeriesId = GetDtoId(series);
                dto.SeriesName = series.Name;
                dto.AirTime = series.AirTime;
                dto.SeriesStudio = series.Studios.FirstOrDefault();

                dto.SeriesThumbImageTag = GetImageCacheTag(series, ImageType.Thumb);

                dto.SeriesPrimaryImageTag = GetImageCacheTag(series, ImageType.Primary);
            }

            // Add SeasonInfo
            var season = item as Season;

            if (season != null)
            {
                series = item.FindParent<Series>();

                dto.SeriesId = GetDtoId(series);
                dto.SeriesName = series.Name;
                dto.AirTime = series.AirTime;
                dto.SeriesStudio = series.Studios.FirstOrDefault();

                dto.SeriesPrimaryImageTag = GetImageCacheTag(series, ImageType.Primary);
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
        }

        private List<MediaVersionInfo> GetMediaVersions(Video item)
        {
            var result = item.GetAlternateVersions().Select(i => GetVersionInfo(i, false)).ToList();

            result.Add(GetVersionInfo(item, true));

            return result.OrderBy(i =>
            {
                if (item.VideoType == VideoType.VideoFile)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i => i.Video3DFormat.HasValue ? 1 : 0)
            .ThenByDescending(i =>
            {
                var stream = i.MediaStreams.FirstOrDefault(m => m.Type == MediaStreamType.Video);

                return stream == null || stream.Width == null ? 0 : stream.Width.Value;
            })
            .ThenBy(i => i.IsPrimaryVersion ? 0 : 1)
            .ToList();
        }

        private List<MediaVersionInfo> GetMediaVersions(Audio item)
        {
            var result = new List<MediaVersionInfo>
            {
                GetVersionInfo(item, true)
            };

            return result;
        }

        private MediaVersionInfo GetVersionInfo(Video i, bool isPrimary)
        {
            var mediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery {ItemId = i.Id}).ToList();

            return new MediaVersionInfo
            {
                Id = i.Id.ToString("N"),
                IsoType = i.IsoType,
                LocationType = i.LocationType,
                MediaStreams = mediaStreams,
                Name = GetAlternateVersionName(i, mediaStreams),
                Path = GetMappedPath(i),
                RunTimeTicks = i.RunTimeTicks,
                Video3DFormat = i.Video3DFormat,
                VideoType = i.VideoType,
                IsPrimaryVersion = isPrimary
            };
        }

        private MediaVersionInfo GetVersionInfo(Audio i, bool isPrimary)
        {
            return new MediaVersionInfo
            {
                Id = i.Id.ToString("N"),
                LocationType = i.LocationType,
                MediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery { ItemId = i.Id }).ToList(),
                Name = i.Name,
                Path = GetMappedPath(i),
                RunTimeTicks = i.RunTimeTicks,
                IsPrimaryVersion = isPrimary
            };
        }

        private string GetMappedPath(IHasMetadata item)
        {
            var path = item.Path;

            var locationType = item.LocationType;

            if (locationType != LocationType.FileSystem && locationType != LocationType.Offline)
            {
                return path;
            }

            foreach (var map in _config.Configuration.PathSubstitutions)
            {
                path = _fileSystem.SubstitutePath(path, map.From, map.To);
            }

            return path;
        }

        private string GetAlternateVersionName(Video video, List<MediaStream> mediaStreams)
        {
            var terms = new List<string>();

            var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);
            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            if (video.Video3DFormat.HasValue)
            {
                terms.Add("3D");
            }

            if (video.VideoType == VideoType.BluRay)
            {
                terms.Add("Bluray");
            }
            else if (video.VideoType == VideoType.Dvd)
            {
                terms.Add("DVD");
            }
            else if (video.VideoType == VideoType.HdDvd)
            {
                terms.Add("HD-DVD");
            }
            else if (video.VideoType == VideoType.Iso)
            {
                if (video.IsoType.HasValue)
                {
                    if (video.IsoType.Value == IsoType.BluRay)
                    {
                        terms.Add("Bluray");
                    }
                    else if (video.IsoType.Value == IsoType.Dvd)
                    {
                        terms.Add("DVD");
                    }
                }
                else
                {
                    terms.Add("ISO");
                }
            }

            if (videoStream != null)
            {
                if (videoStream.Width.HasValue)
                {
                    if (videoStream.Width.Value >= 3800)
                    {
                        terms.Add("4K");
                    }
                    else if (videoStream.Width.Value >= 1900)
                    {
                        terms.Add("1080P");
                    }
                    else if (videoStream.Width.Value >= 1270)
                    {
                        terms.Add("720P");
                    }
                    else if (videoStream.Width.Value >= 700)
                    {
                        terms.Add("480P");
                    }
                    else
                    {
                        terms.Add("SD");
                    }
                }
            }

            if (videoStream != null && !string.IsNullOrWhiteSpace(videoStream.Codec))
            {
                terms.Add(videoStream.Codec.ToUpper());
            }

            if (audioStream != null)
            {
                var audioCodec = string.Equals(audioStream.Codec, "dca", StringComparison.OrdinalIgnoreCase)
                    ? audioStream.Profile
                    : audioStream.Codec;

                if (!string.IsNullOrEmpty(audioCodec))
                {
                    terms.Add(audioCodec.ToUpper());
                }
            }

            return string.Join("/", terms.ToArray());
        }

        private string GetMappedPath(string path)
        {
            foreach (var map in _config.Configuration.PathSubstitutions)
            {
                path = _fileSystem.SubstitutePath(path, map.From, map.To);
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
        /// <returns>Task.</returns>
        private void SetSpecialCounts(Folder folder, User user, BaseItemDto dto, List<ItemFields> fields)
        {
            var rcentlyAddedItemCount = 0;
            var recursiveItemCount = 0;
            var unplayed = 0;
            long runtime = 0;

            DateTime? dateLastMediaAdded = null;
            double totalPercentPlayed = 0;

            IEnumerable<BaseItem> children;

            var season = folder as Season;

            if (season != null)
            {
                children = season.GetEpisodes(user).Where(i => i.LocationType != LocationType.Virtual);
            }
            else
            {
                children = folder.GetRecursiveChildren(user, i => !i.IsFolder && i.LocationType != LocationType.Virtual);
            }

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

                // Check is recently added
                if (child.IsRecentlyAdded())
                {
                    rcentlyAddedItemCount++;
                }

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
            }

            dto.RecursiveItemCount = recursiveItemCount;
            dto.RecentlyAddedItemCount = rcentlyAddedItemCount;
            dto.RecursiveUnplayedItemCount = unplayed;

            if (recursiveItemCount > 0)
            {
                dto.PlayedPercentage = totalPercentPlayed / recursiveItemCount;
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
            var imageInfo = item.GetImageInfo(ImageType.Primary, 0);

            if (imageInfo == null)
            {
                return;
            }

            var path = imageInfo.Path;

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            var dateModified = imageInfo.DateModified;

            ImageSize size;

            try
            {
                size = _imageProcessor.GetImageSize(path, dateModified);
            }
            catch (FileNotFoundException)
            {
                _logger.Error("Image file does not exist: {0}", path);
                return;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to determine primary image aspect ratio for {0}", ex, path);
                return;
            }

            dto.OriginalPrimaryImageAspectRatio = size.Width / size.Height;

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

            dto.PrimaryImageAspectRatio = size.Width / size.Height;
        }
    }
}
