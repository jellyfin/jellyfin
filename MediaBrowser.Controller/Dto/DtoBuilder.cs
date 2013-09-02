using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;

namespace MediaBrowser.Controller.Dto
{
    /// <summary>
    /// Generates DTO's from domain entities
    /// </summary>
    public class DtoBuilder
    {
        /// <summary>
        /// The index folder delimeter
        /// </summary>
        const string IndexFolderDelimeter = "-index-";

        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataRepository _userDataRepository;
        private readonly IItemRepository _itemRepo;

        public DtoBuilder(ILogger logger, ILibraryManager libraryManager, IUserDataRepository userDataRepository, IItemRepository itemRepo)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _itemRepo = itemRepo;
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
        public async Task<BaseItemDto> GetBaseItemDto(BaseItem item, List<ItemFields> fields, User user = null, BaseItem owner = null)
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

            var tasks = new List<Task>();

            if (fields.Contains(ItemFields.Studios))
            {
                tasks.Add(AttachStudios(dto, item));
            }

            if (fields.Contains(ItemFields.People))
            {
                tasks.Add(AttachPeople(dto, item));
            }

            if (fields.Contains(ItemFields.PrimaryImageAspectRatio))
            {
                try
                {
                    await AttachPrimaryImageAspectRatio(dto, item, _logger).ConfigureAwait(false);
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

            AttachBasicFields(dto, item, owner, fields);

            if (fields.Contains(ItemFields.SoundtrackIds))
            {
                dto.SoundtrackIds = item.SoundtrackIds
                    .Select(i => i.ToString("N"))
                    .ToArray();
            }

            // Make sure all the tasks we kicked off have completed.
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

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
                var hasItemCounts = fields.Contains(ItemFields.ItemCounts);

                if (hasItemCounts || fields.Contains(ItemFields.CumulativeRunTimeTicks))
                {
                    var folder = (Folder)item;

                    if (hasItemCounts)
                    {
                        dto.ChildCount = folder.GetChildren(user, true).Count();
                    }

                    SetSpecialCounts(folder, user, dto, _userDataRepository);
                }
            }

            var userData = _userDataRepository.GetUserData(user.Id, item.GetUserDataKey());

            dto.UserData = GetUserItemDataDto(userData);

            if (item.IsFolder)
            {
                dto.UserData.Played = dto.PlayedPercentage.HasValue && dto.PlayedPercentage.Value >= 100;
            }
        }

        /// <summary>
        /// Attaches the primary image aspect ratio.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="logger">The _logger.</param>
        /// <returns>Task.</returns>
        internal static async Task AttachPrimaryImageAspectRatio(IItemDto dto, BaseItem item, ILogger logger)
        {
            var path = item.PrimaryImagePath;

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var metaFileEntry = item.ResolveArgs.GetMetaFileByPath(path);

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            var dateModified = metaFileEntry == null ? File.GetLastWriteTimeUtc(path) : metaFileEntry.LastWriteTimeUtc;

            ImageSize size;

            try
            {
                size = await Kernel.Instance.ImageManager.GetImageSize(path, dateModified).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                logger.Error("Image file does not exist: {0}", path);
                return;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Failed to determine primary image aspect ratio for {0}", ex, path);
                return;
            }

            dto.OriginalPrimaryImageAspectRatio = size.Width / size.Height;

            var supportedEnhancers = Kernel.Instance.ImageManager.ImageEnhancers.Where(i =>
            {
                try
                {
                    return i.Supports(item, ImageType.Primary);
                }
                catch (Exception ex)
                {
                    logger.ErrorException("Error in image enhancer: {0}", ex, i.GetType().Name);

                    return false;
                }

            }).ToList();


            foreach (var enhancer in supportedEnhancers)
            {
                try
                {
                    size = enhancer.GetEnhancedImageSize(item, ImageType.Primary, 0, size);
                }
                catch (Exception ex)
                {
                    logger.ErrorException("Error in image enhancer: {0}", ex, enhancer.GetType().Name);
                }
            }

            dto.PrimaryImageAspectRatio = size.Width / size.Height;
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

            if (fields.Contains(ItemFields.OriginalRunTimeTicks))
            {
                dto.OriginalRunTimeTicks = item.OriginalRunTimeTicks;
            }

            dto.DisplayMediaType = item.DisplayMediaType;

            if (fields.Contains(ItemFields.MetadataSettings))
            {
                dto.LockedFields = item.LockedFields;
                dto.EnableInternetProviders = !item.DontFetchMeta;
            }

            if (fields.Contains(ItemFields.Budget))
            {
                dto.Budget = item.Budget;
            }

            if (fields.Contains(ItemFields.Revenue))
            {
                dto.Revenue = item.Revenue;
            }

            dto.EndDate = item.EndDate;

            if (fields.Contains(ItemFields.HomePageUrl))
            {
                dto.HomePageUrl = item.HomePageUrl;
            }

            if (fields.Contains(ItemFields.Tags))
            {
                dto.Tags = item.Tags;
            }

            if (fields.Contains(ItemFields.ProductionLocations))
            {
                dto.ProductionLocations = item.ProductionLocations;
            }

            dto.AspectRatio = item.AspectRatio;

            dto.BackdropImageTags = GetBackdropImageTags(item);
            dto.ScreenshotImageTags = GetScreenshotImageTags(item);

            if (fields.Contains(ItemFields.Genres))
            {
                dto.Genres = item.Genres;
            }

            dto.ImageTags = new Dictionary<ImageType, Guid>();

            foreach (var image in item.Images)
            {
                var type = image.Key;

                var tag = GetImageCacheTag(item, type, image.Value);

                if (tag.HasValue)
                {
                    dto.ImageTags[type] = tag.Value;
                }
            }

            dto.Id = GetClientItemId(item);
            dto.IndexNumber = item.IndexNumber;
            dto.IsFolder = item.IsFolder;
            dto.Language = item.Language;
            dto.MediaType = item.MediaType;
            dto.LocationType = item.LocationType;
            dto.CriticRating = item.CriticRating;

            if (fields.Contains(ItemFields.CriticRatingSummary))
            {
                dto.CriticRatingSummary = item.CriticRatingSummary;
            }

            var localTrailerCount = item.LocalTrailerIds.Count;

            if (localTrailerCount > 0)
            {
                dto.LocalTrailerCount = localTrailerCount;
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
                    dto.ParentBackdropItemId = GetClientItemId(parentWithBackdrop);
                    dto.ParentBackdropImageTags = GetBackdropImageTags(parentWithBackdrop);
                }
            }

            if (item.Parent != null && fields.Contains(ItemFields.ParentId))
            {
                dto.ParentId = GetClientItemId(item.Parent);
            }

            dto.ParentIndexNumber = item.ParentIndexNumber;

            // If there is no logo, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasLogo)
            {
                var parentWithLogo = GetParentImageItem(item, ImageType.Logo, owner);

                if (parentWithLogo != null)
                {
                    dto.ParentLogoItemId = GetClientItemId(parentWithLogo);

                    dto.ParentLogoImageTag = GetImageCacheTag(parentWithLogo, ImageType.Logo, parentWithLogo.GetImage(ImageType.Logo));
                }
            }

            // If there is no art, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasArtImage)
            {
                var parentWithImage = GetParentImageItem(item, ImageType.Art, owner);

                if (parentWithImage != null)
                {
                    dto.ParentArtItemId = GetClientItemId(parentWithImage);

                    dto.ParentArtImageTag = GetImageCacheTag(parentWithImage, ImageType.Art, parentWithImage.GetImage(ImageType.Art));
                }
            }

            if (fields.Contains(ItemFields.Path))
            {
                dto.Path = item.Path;
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
                dto.Taglines = item.Taglines;
            }

            if (fields.Contains(ItemFields.RemoteTrailers))
            {
                dto.RemoteTrailers = item.RemoteTrailers;
            }

            dto.Type = item.GetType().Name;
            dto.CommunityRating = item.CommunityRating;

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                if (fields.Contains(ItemFields.IndexOptions))
                {
                    dto.IndexOptions = folder.IndexByOptionStrings.ToArray();
                }
            }

            // Add audio info
            var audio = item as Audio;
            if (audio != null)
            {
                dto.Album = audio.Album;
                dto.AlbumArtist = audio.AlbumArtist;
                dto.Artists = new[] { audio.Artist };

                var albumParent = audio.FindParent<MusicAlbum>();

                if (albumParent != null)
                {
                    dto.AlbumId = GetClientItemId(albumParent);

                    var imagePath = albumParent.PrimaryImagePath;

                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        dto.AlbumPrimaryImageTag = GetImageCacheTag(albumParent, ImageType.Primary, imagePath);
                    }
                }
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                var songs = album.RecursiveChildren.OfType<Audio>().ToList();

                dto.AlbumArtist = songs.Select(i => i.AlbumArtist).FirstOrDefault(i => !string.IsNullOrEmpty(i));

                dto.Artists =
                    songs.Select(i => i.Artist ?? string.Empty)
                         .Where(i => !string.IsNullOrEmpty(i))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToArray();
            }

            // Add video info
            var video = item as Video;
            if (video != null)
            {
                dto.VideoType = video.VideoType;
                dto.Video3DFormat = video.Video3DFormat;
                dto.IsoType = video.IsoType;
                dto.MainFeaturePlaylistName = video.MainFeaturePlaylistName;

                dto.PartCount = video.AdditionalPartIds.Count + 1;

                if (fields.Contains(ItemFields.Chapters))
                {
                    dto.Chapters = _itemRepo.GetChapters(video.Id).Select(c => GetChapterInfoDto(c, item)).ToList();
                }
            }

            if (fields.Contains(ItemFields.MediaStreams))
            {
                // Add VideoInfo
                var iHasMediaStreams = item as IHasMediaStreams;

                if (iHasMediaStreams != null)
                {
                    dto.MediaStreams = iHasMediaStreams.MediaStreams;
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
            }

            // Add EpisodeInfo
            var episode = item as Episode;

            if (episode != null)
            {
                dto.IndexNumberEnd = episode.IndexNumberEnd;
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
            }

            if (episode != null)
            {
                series = item.FindParent<Series>();

                dto.SeriesId = GetClientItemId(series);
                dto.SeriesName = series.Name;
            }

            // Add SeasonInfo
            var season = item as Season;

            if (season != null)
            {
                series = item.FindParent<Series>();

                dto.SeriesId = GetClientItemId(series);
                dto.SeriesName = series.Name;
            }

            var game = item as Game;

            if (game != null)
            {
                SetGameProperties(dto, game);
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

        private void SetBookProperties(BaseItemDto dto, Book item)
        {
            dto.SeriesName = item.SeriesName;
        }

        private void SetMusicVideoProperties(BaseItemDto dto, MusicVideo item)
        {
            if (!string.IsNullOrEmpty(item.Album))
            {
                var parentAlbum = _libraryManager.RootFolder
                    .RecursiveChildren
                    .OfType<MusicAlbum>()
                    .FirstOrDefault(i => string.Equals(i.Name, item.Album, StringComparison.OrdinalIgnoreCase));

                if (parentAlbum != null)
                {
                    dto.AlbumId = GetClientItemId(parentAlbum);
                }
            }

            dto.Album = item.Album;
            dto.Artists = string.IsNullOrEmpty(item.Artist) ? new string[] { } : new[] { item.Artist };
        }

        private void SetGameProperties(BaseItemDto dto, Game item)
        {
            dto.Players = item.PlayersSupported;
            dto.GameSystem = item.GameSystem;
        }

        /// <summary>
        /// Since it can be slow to make all of these calculations independently, this method will provide a way to do them all at once
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="user">The user.</param>
        /// <param name="dto">The dto.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <returns>Task.</returns>
        private static void SetSpecialCounts(Folder folder, User user, BaseItemDto dto, IUserDataRepository userDataRepository)
        {
            var rcentlyAddedItemCount = 0;
            var recursiveItemCount = 0;
            var unplayed = 0;
            long runtime = 0;

            double totalPercentPlayed = 0;

            // Loop through each recursive child
            foreach (var child in folder.GetRecursiveChildren(user, true).Where(i => !i.IsFolder).ToList())
            {
                var userdata = userDataRepository.GetUserData(user.Id, child.GetUserDataKey());

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

            if (runtime > 0)
            {
                dto.CumulativeRunTimeTicks = runtime;
            }
        }

        /// <summary>
        /// Attaches People DTO's to a DTOBaseItem
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        private async Task AttachPeople(BaseItemDto dto, BaseItem item)
        {
            // Ordering by person type to ensure actors and artists are at the front.
            // This is taking advantage of the fact that they both begin with A
            // This should be improved in the future
            var people = item.People.OrderBy(i => i.Type).ToList();

            // Attach People by transforming them into BaseItemPerson (DTO)
            dto.People = new BaseItemPerson[people.Count];

            var entities = await Task.WhenAll(people.Select(p => p.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase).Select(c =>
                    Task.Run(async () =>
                    {
                        try
                        {
                            return await _libraryManager.GetPerson(c).ConfigureAwait(false);
                        }
                        catch (IOException ex)
                        {
                            _logger.ErrorException("Error getting person {0}", ex, c);
                            return null;
                        }
                    })

            )).ConfigureAwait(false);

            var dictionary = entities.Where(i => i != null)
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
                    var primaryImagePath = entity.PrimaryImagePath;

                    if (!string.IsNullOrEmpty(primaryImagePath))
                    {
                        baseItemPerson.PrimaryImageTag = GetImageCacheTag(entity, ImageType.Primary, primaryImagePath);
                    }
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
        private async Task AttachStudios(BaseItemDto dto, BaseItem item)
        {
            var studios = item.Studios.ToList();

            dto.Studios = new StudioDto[studios.Count];

            var entities = await Task.WhenAll(studios.Distinct(StringComparer.OrdinalIgnoreCase).Select(c =>

                    Task.Run(async () =>
                    {
                        try
                        {
                            return await _libraryManager.GetStudio(c).ConfigureAwait(false);
                        }
                        catch (IOException ex)
                        {
                            _logger.ErrorException("Error getting studio {0}", ex, c);
                            return null;
                        }
                    })

            )).ConfigureAwait(false);

            var dictionary = entities
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
                    var primaryImagePath = entity.PrimaryImagePath;

                    if (!string.IsNullOrEmpty(primaryImagePath))
                    {
                        studioDto.PrimaryImageTag = GetImageCacheTag(entity, ImageType.Primary, primaryImagePath);
                    }
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
                if (parent.BackdropImagePaths != null && parent.BackdropImagePaths.Count > 0)
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
        /// Converts a UserItemData to a DTOUserItemData
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>DtoUserItemData.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static UserItemDataDto GetUserItemDataDto(UserItemData data)
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
                LastPlayedDate = data.LastPlayedDate
            };
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
                dto.ImageTag = GetImageCacheTag(item, ImageType.Chapter, chapterInfo.ImagePath);
            }

            return dto;
        }

        /// <summary>
        /// Converts a BaseItem to a BaseItemInfo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>BaseItemInfo.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public static BaseItemInfo GetBaseItemInfo(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var info = new BaseItemInfo
            {
                Id = GetClientItemId(item),
                Name = item.Name,
                MediaType = item.MediaType,
                Type = item.GetType().Name,
                IsFolder = item.IsFolder,
                RunTimeTicks = item.RunTimeTicks
            };

            var imagePath = item.PrimaryImagePath;

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    info.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Primary, imagePath);
                }
                catch (IOException)
                {
                }
            }

            return info;
        }

        /// <summary>
        /// Gets client-side Id of a server-side BaseItem
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public static string GetClientItemId(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var indexFolder = item as IndexFolder;

            if (indexFolder != null)
            {
                return GetClientItemId(indexFolder.Parent) + IndexFolderDelimeter + (indexFolder.IndexName ?? string.Empty) + IndexFolderDelimeter + indexFolder.Id;
            }

            return item.Id.ToString("N");
        }

        /// <summary>
        /// Gets a BaseItem based upon it's client-side item id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>BaseItem.</returns>
        public static BaseItem GetItemByClientId(string id, IUserManager userManager, ILibraryManager libraryManager, Guid? userId = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            // If the item is an indexed folder we have to do a special routine to get it
            var isIndexFolder = id.IndexOf(IndexFolderDelimeter, StringComparison.OrdinalIgnoreCase) != -1;

            if (isIndexFolder)
            {
                if (userId.HasValue)
                {
                    return GetIndexFolder(id, userId.Value, userManager, libraryManager);
                }
            }

            BaseItem item = null;

            if (userId.HasValue || !isIndexFolder)
            {
                item = libraryManager.GetItemById(new Guid(id));
            }

            // If we still don't find it, look within individual user views
            if (item == null && !userId.HasValue && isIndexFolder)
            {
                foreach (var user in userManager.Users)
                {
                    item = GetItemByClientId(id, userManager, libraryManager, user.Id);

                    if (item != null)
                    {
                        break;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Finds an index folder based on an Id and userId
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>BaseItem.</returns>
        private static BaseItem GetIndexFolder(string id, Guid userId, IUserManager userManager, ILibraryManager libraryManager)
        {
            var user = userManager.GetUserById(userId);

            var stringSeparators = new[] { IndexFolderDelimeter };

            // Split using the delimeter
            var values = id.Split(stringSeparators, StringSplitOptions.None).ToList();

            // Get the top folder normally using the first id
            var folder = GetItemByClientId(values[0], userManager, libraryManager, userId) as Folder;

            values.RemoveAt(0);

            // Get indexed folders using the remaining values in the id string
            return GetIndexFolder(values, folder, user);
        }

        /// <summary>
        /// Gets indexed folders based on a list of index names and folder id's
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="parentFolder">The parent folder.</param>
        /// <param name="user">The user.</param>
        /// <returns>BaseItem.</returns>
        private static BaseItem GetIndexFolder(List<string> values, Folder parentFolder, User user)
        {
            // The index name is first
            var indexBy = values[0];

            // The index folder id is next
            var indexFolderId = new Guid(values[1]);

            // Remove them from the lst
            values.RemoveRange(0, 2);

            // Get the IndexFolder
            var indexFolder = parentFolder.GetChildren(user, false, indexBy).FirstOrDefault(i => i.Id == indexFolderId) as Folder;

            // Nested index folder
            if (values.Count > 0)
            {
                return GetIndexFolder(values, indexFolder, user);
            }

            return indexFolder;
        }

        /// <summary>
        /// Gets the backdrop image tags.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{System.String}.</returns>
        private List<Guid> GetBackdropImageTags(BaseItem item)
        {
            return item.BackdropImagePaths
                .Select(p => GetImageCacheTag(item, ImageType.Backdrop, p))
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToList();
        }

        /// <summary>
        /// Gets the screenshot image tags.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{Guid}.</returns>
        private List<Guid> GetScreenshotImageTags(BaseItem item)
        {
            return item.ScreenshotImagePaths
                .Select(p => GetImageCacheTag(item, ImageType.Screenshot, p))
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToList();
        }

        private Guid? GetImageCacheTag(BaseItem item, ImageType type, string path)
        {
            try
            {
                return Kernel.Instance.ImageManager.GetImageCacheTag(item, type, path);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error getting {0} image info for {1}", ex, type, path);
                return null;
            }
        }
    }
}
