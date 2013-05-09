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

        public DtoBuilder(ILogger logger, ILibraryManager libraryManager, IUserDataRepository userDataRepository)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Gets the dto base item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fields">The fields.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task<BaseItemDto> GetBaseItemDto(BaseItem item, List<ItemFields> fields)
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

            AttachBasicFields(dto, item, fields);

            // Make sure all the tasks we kicked off have completed.
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            return dto;
        }

        /// <summary>
        /// Converts a BaseItem to a DTOBaseItem
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="fields">The fields.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task<BaseItemDto> GetBaseItemDto(BaseItem item, User user, List<ItemFields> fields)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
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

            tasks.Add(AttachUserSpecificInfo(dto, item, user, fields));

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

            AttachBasicFields(dto, item, fields);

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
        private async Task AttachUserSpecificInfo(BaseItemDto dto, BaseItem item, User user, List<ItemFields> fields)
        {
            if (item.IsFolder && fields.Contains(ItemFields.DisplayPreferencesId))
            {
                dto.DisplayPreferencesId = ((Folder)item).GetDisplayPreferencesId(user.Id).ToString();
            }

            var addUserData = fields.Contains(ItemFields.UserData);

            if (item.IsFolder)
            {
                if (fields.Contains(ItemFields.ItemCounts) || addUserData)
                {
                    var folder = (Folder)item;

                    // Skip sorting since all we want is a count
                    dto.ChildCount = folder.GetChildren(user).Count();

                    await SetSpecialCounts(folder, user, dto, _userDataRepository).ConfigureAwait(false);
                }
            }

            if (addUserData)
            {
                var userData = await _userDataRepository.GetUserData(user.Id, item.GetUserDataKey()).ConfigureAwait(false);

                dto.UserData = GetUserItemDataDto(userData);

                if (item.IsFolder)
                {
                    dto.UserData.Played = dto.PlayedPercentage.HasValue && dto.PlayedPercentage.Value >= 100;
                }
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

            foreach (var enhancer in Kernel.Instance.ImageEnhancers
                .Where(i => i.Supports(item, ImageType.Primary)))
            {

                size = enhancer.GetEnhancedImageSize(item, ImageType.Primary, 0, size);
            }

            dto.PrimaryImageAspectRatio = size.Width / size.Height;
        }

        /// <summary>
        /// Sets simple property values on a DTOBaseItem
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <param name="fields">The fields.</param>
        private void AttachBasicFields(BaseItemDto dto, BaseItem item, List<ItemFields> fields)
        {
            if (fields.Contains(ItemFields.DateCreated))
            {
                dto.DateCreated = item.DateCreated;
            }

            if (fields.Contains(ItemFields.DisplayMediaType))
            {
                dto.DisplayMediaType = item.DisplayMediaType;
            }

            if (fields.Contains(ItemFields.Budget))
            {
                dto.Budget = item.Budget;
            }

            if (fields.Contains(ItemFields.Revenue))
            {
                dto.Revenue = item.Revenue;
            }

            if (fields.Contains(ItemFields.EndDate))
            {
                dto.EndDate = item.EndDate;
            }

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

            if (item.Images != null)
            {
                dto.ImageTags = new Dictionary<ImageType, Guid>();

                foreach (var image in item.Images)
                {
                    var type = image.Key;

                    dto.ImageTags[type] = Kernel.Instance.ImageManager.GetImageCacheTag(item, type, image.Value);
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
                var parentWithBackdrop = GetParentBackdropItem(item);

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
                var parentWithLogo = GetParentLogoItem(item);

                if (parentWithLogo != null)
                {
                    dto.ParentLogoItemId = GetClientItemId(parentWithLogo);

                    dto.ParentLogoImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(parentWithLogo, ImageType.Logo, parentWithLogo.GetImage(ImageType.Logo));
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

            if (fields.Contains(ItemFields.Taglines))
            {
                dto.Taglines = item.Taglines;
            }

            if (fields.Contains(ItemFields.TrailerUrls))
            {
                dto.TrailerUrls = item.TrailerUrls;
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
            if (fields.Contains(ItemFields.AudioInfo))
            {
                var audio = item as Audio;
                if (audio != null)
                {
                    dto.Album = audio.Album;
                    dto.AlbumArtist = audio.AlbumArtist;
                    dto.Artists = new[] { audio.Artist };
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
            }

            // Add video info
            var video = item as Video;
            if (video != null)
            {
                dto.VideoType = video.VideoType;
                dto.VideoFormat = video.VideoFormat;
                dto.IsoType = video.IsoType;

                if (fields.Contains(ItemFields.Chapters) && video.Chapters != null)
                {
                    dto.Chapters = video.Chapters.Select(c => GetChapterInfoDto(c, item)).ToList();
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

            if (fields.Contains(ItemFields.SeriesInfo))
            {
                // Add SeriesInfo
                var series = item as Series;

                if (series != null)
                {
                    dto.AirDays = series.AirDays;
                    dto.AirTime = series.AirTime;
                    dto.Status = series.Status;
                }

                // Add EpisodeInfo
                var episode = item as Episode;

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
            }

            var game = item as BaseGame;

            if (game != null)
            {
                dto.Players = game.PlayersSupported;
            }
        }

        /// <summary>
        /// Since it can be slow to make all of these calculations independently, this method will provide a way to do them all at once
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="user">The user.</param>
        /// <param name="dto">The dto.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <returns>Task.</returns>
        private static async Task SetSpecialCounts(Folder folder, User user, BaseItemDto dto, IUserDataRepository userDataRepository)
        {
            var rcentlyAddedItemCount = 0;
            var recursiveItemCount = 0;

            double totalPercentPlayed = 0;

            // Loop through each recursive child
            foreach (var child in folder.GetRecursiveChildren(user).Where(i => !i.IsFolder).ToList())
            {
                var userdata = await userDataRepository.GetUserData(user.Id, child.GetUserDataKey()).ConfigureAwait(false);

                recursiveItemCount++;

                // Check is recently added
                if (child.IsRecentlyAdded())
                {
                    rcentlyAddedItemCount++;
                }

                // Incrememt totalPercentPlayed
                if (userdata != null)
                {
                    if (userdata.Played)
                    {
                        totalPercentPlayed += 100;
                    }
                    else if (userdata.PlaybackPositionTicks > 0 && child.RunTimeTicks.HasValue && child.RunTimeTicks.Value > 0)
                    {
                        double itemPercent = userdata.PlaybackPositionTicks;
                        itemPercent /= child.RunTimeTicks.Value;
                        totalPercentPlayed += itemPercent;
                    }
                }
            }

            dto.RecursiveItemCount = recursiveItemCount;
            dto.RecentlyAddedItemCount = rcentlyAddedItemCount;

            if (recursiveItemCount > 0)
            {
                dto.PlayedPercentage = totalPercentPlayed / recursiveItemCount;
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
            if (item.People == null)
            {
                return;
            }

            // Ordering by person type to ensure actors and artists are at the front.
            // This is taking advantage of the fact that they both begin with A
            // This should be improved in the future
            var people = item.People.OrderBy(i => i.Type).ToList();

            // Attach People by transforming them into BaseItemPerson (DTO)
            dto.People = new BaseItemPerson[people.Count];

            var entities = await Task.WhenAll(people.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Select(c =>

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

            var dictionary = entities.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

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
                        baseItemPerson.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(entity, ImageType.Primary, primaryImagePath);
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
            if (item.Studios == null)
            {
                return;
            }

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

            var dictionary = entities.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

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
                        studioDto.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(entity, ImageType.Primary, primaryImagePath);
                    }
                }

                dto.Studios[i] = studioDto;
            }
        }

        /// <summary>
        /// If an item does not any backdrops, this can be used to find the first parent that does have one
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem GetParentBackdropItem(BaseItem item)
        {
            var parent = item.Parent;

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
        /// <returns>BaseItem.</returns>
        private BaseItem GetParentLogoItem(BaseItem item)
        {
            var parent = item.Parent;

            while (parent != null)
            {
                if (parent.HasImage(ImageType.Logo))
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
                throw new ArgumentNullException();
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
                dto.ImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Chapter, chapterInfo.ImagePath);
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
                info.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Primary, imagePath);
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

            return item.Id.ToString();
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
            var indexFolder = parentFolder.GetChildren(user, indexBy).FirstOrDefault(i => i.Id == indexFolderId) as Folder;

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
            if (item.BackdropImagePaths == null)
            {
                return new List<Guid>();
            }

            return item.BackdropImagePaths.Select(p => Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Backdrop, p)).ToList();
        }

        /// <summary>
        /// Gets the screenshot image tags.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{Guid}.</returns>
        private List<Guid> GetScreenshotImageTags(BaseItem item)
        {
            if (item.ScreenshotImagePaths == null)
            {
                return new List<Guid>();
            }

            return item.ScreenshotImagePaths.Select(p => Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Screenshot, p)).ToList();
        }
    }
}
