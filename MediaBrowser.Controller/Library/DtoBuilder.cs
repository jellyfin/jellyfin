using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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

namespace MediaBrowser.Controller.Library
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

        public DtoBuilder(ILogger logger, ILibraryManager libraryManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
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

            if (fields.Contains(ItemFields.PrimaryImageAspectRatio))
            {
                try
                {
                    tasks.Add(AttachPrimaryImageAspectRatio(dto, item));
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, item.Name);
                }
            }

            if (fields.Contains(ItemFields.Studios))
            {
                dto.Studios = item.Studios;
            }

            if (fields.Contains(ItemFields.People))
            {
                tasks.Add(AttachPeople(dto, item));
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

            if (fields.Contains(ItemFields.PrimaryImageAspectRatio))
            {
                try
                {
                    tasks.Add(AttachPrimaryImageAspectRatio(dto, item));
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, item.Name);
                }
            }

            if (fields.Contains(ItemFields.Studios))
            {
                dto.Studios = item.Studios;
            }

            if (fields.Contains(ItemFields.People))
            {
                tasks.Add(AttachPeople(dto, item));
            }

            AttachBasicFields(dto, item, fields);

            AttachUserSpecificInfo(dto, item, user, fields);

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
            if (fields.Contains(ItemFields.UserData))
            {
                var userData = item.GetUserData(user, false);

                if (userData != null)
                {
                    dto.UserData = GetUserItemDataDto(userData);
                }
            }

            if (item.IsFolder && fields.Contains(ItemFields.DisplayPreferences))
            {
                dto.DisplayPreferences = ((Folder)item).GetDisplayPreferences(user, false) ?? new DisplayPreferences { UserId = user.Id };
            }

            if (item.IsFolder)
            {
                if (fields.Contains(ItemFields.ItemCounts))
                {
                    var folder = (Folder)item;

                    // Skip sorting since all we want is a count
                    dto.ChildCount = folder.GetChildren(user).Count();

                    SetSpecialCounts(folder, user, dto);
                }
            }
        }

        /// <summary>
        /// Attaches the primary image aspect ratio.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        private async Task AttachPrimaryImageAspectRatio(IItemDto dto, BaseItem item)
        {
            var path = item.PrimaryImagePath;

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var metaFileEntry = item.ResolveArgs.GetMetaFileByPath(path);

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            var dateModified = metaFileEntry == null ? File.GetLastWriteTimeUtc(path) : metaFileEntry.Value.LastWriteTimeUtc;

            ImageSize size;

            try
            {
                size = await Kernel.Instance.ImageManager.GetImageSize(path, dateModified).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                _logger.Error("Image file does not exist: {0}", path);
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

            dto.AspectRatio = item.AspectRatio;

            dto.BackdropImageTags = GetBackdropImageTags(item);

            if (fields.Contains(ItemFields.Genres))
            {
                dto.Genres = item.Genres;
            }

            if (item.Images != null)
            {
                dto.ImageTags = new Dictionary<ImageType, Guid>();

                foreach (var image in item.Images)
                {
                    ImageType type;

                    if (Enum.TryParse(image.Key, true, out type))
                    {
                        dto.ImageTags[type] = Kernel.Instance.ImageManager.GetImageCacheTag(item, type, image.Value);
                    }
                }
            }

            dto.Id = GetClientItemId(item);
            dto.IndexNumber = item.IndexNumber;
            dto.IsFolder = item.IsFolder;
            dto.Language = item.Language;
            dto.MediaType = item.MediaType;
            dto.LocationType = item.LocationType;

            var localTrailerCount = item.LocalTrailers == null ? 0 : item.LocalTrailers.Count;

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
            var audio = item as Audio;
            if (audio != null)
            {
                if (fields.Contains(ItemFields.AudioInfo))
                {
                    dto.Album = audio.Album;
                    dto.AlbumArtist = audio.AlbumArtist;
                    dto.Artist = audio.Artist;
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
                var specialFeatureCount = movie.SpecialFeatures == null ? 0 : movie.SpecialFeatures.Count;

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
        }

        /// <summary>
        /// Since it can be slow to make all of these calculations independently, this method will provide a way to do them all at once
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="user">The user.</param>
        /// <param name="dto">The dto.</param>
        private static void SetSpecialCounts(Folder folder, User user, BaseItemDto dto)
        {
            var rcentlyAddedItemCount = 0;
            var recursiveItemCount = 0;

            double totalPercentPlayed = 0;

            // Loop through each recursive child
            foreach (var child in folder.GetRecursiveChildren(user).Where(i => !i.IsFolder))
            {
                var userdata = child.GetUserData(user, false);

                recursiveItemCount++;

                // Check is recently added
                if (child.IsRecentlyAdded(user))
                {
                    rcentlyAddedItemCount++;
                }

                // Incrememt totalPercentPlayed
                if (userdata != null)
                {
                    if (userdata.PlayCount > 0)
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
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>Task.</returns>
        private async Task AttachPeople(BaseItemDto dto, BaseItem item)
        {
            if (item.People == null)
            {
                return;
            }

            // Attach People by transforming them into BaseItemPerson (DTO)
            dto.People = new BaseItemPerson[item.People.Count];

            var entities = await Task.WhenAll(item.People.Select(c =>

                    Task.Run(async () =>
                    {
                        try
                        {
                            return await _libraryManager.GetPerson(c.Name).ConfigureAwait(false);
                        }
                        catch (IOException ex)
                        {
                            _logger.ErrorException("Error getting person {0}", ex, c.Name);
                            return null;
                        }
                    })

            )).ConfigureAwait(false);

            for (var i = 0; i < item.People.Count; i++)
            {
                var person = item.People[i];

                var baseItemPerson = new BaseItemPerson
                {
                    Name = person.Name,
                    Role = person.Role,
                    Type = person.Type
                };

                var ibnObject = entities[i];

                if (ibnObject != null)
                {
                    var primaryImagePath = ibnObject.PrimaryImagePath;

                    if (!string.IsNullOrEmpty(primaryImagePath))
                    {
                        baseItemPerson.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(ibnObject, ImageType.Primary, primaryImagePath);
                    }
                }

                dto.People[i] = baseItemPerson;
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
        /// Gets the library update info.
        /// </summary>
        /// <param name="changeEvent">The <see cref="ChildrenChangedEventArgs" /> instance containing the event data.</param>
        /// <returns>LibraryUpdateInfo.</returns>
        public static LibraryUpdateInfo GetLibraryUpdateInfo(ChildrenChangedEventArgs changeEvent)
        {
            return new LibraryUpdateInfo
            {
                Folder = GetBaseItemInfo(changeEvent.Folder),
                ItemsAdded = changeEvent.ItemsAdded.Select(GetBaseItemInfo),
                ItemsRemoved = changeEvent.ItemsRemoved.Select(i => i.Id),
                ItemsUpdated = changeEvent.ItemsUpdated.Select(i => i.Id)
            };
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
                throw new ArgumentNullException();
            }

            return new UserItemDataDto
            {
                IsFavorite = data.IsFavorite,
                Likes = data.Likes,
                PlaybackPositionTicks = data.PlaybackPositionTicks,
                PlayCount = data.PlayCount,
                Rating = data.Rating,
                Played = data.Played
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
                Type = item.GetType().Name,
                IsFolder = item.IsFolder,
                RunTimeTicks = item.RunTimeTicks
            };

            var imagePath = item.PrimaryImagePath;

            if (!string.IsNullOrEmpty(imagePath))
            {
                info.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Primary, imagePath);
            }

            if (item.BackdropImagePaths != null && item.BackdropImagePaths.Count > 0)
            {
                imagePath = item.BackdropImagePaths[0];

                if (!string.IsNullOrEmpty(imagePath))
                {
                    info.BackdropImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(item, ImageType.Backdrop, imagePath);
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

            return item.Id.ToString();
        }

        /// <summary>
        /// Converts a User to a DTOUser
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>DtoUser.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task<UserDto> GetUserDto(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var dto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                HasPassword = !String.IsNullOrEmpty(user.Password),
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate,
                Configuration = user.Configuration
            };
            
            var image = user.PrimaryImagePath;

            if (!string.IsNullOrEmpty(image))
            {
                dto.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(user, ImageType.Primary, image);

                try
                {
                    await AttachPrimaryImageAspectRatio(dto, user).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, user.Name);
                }
            }
            
            return dto;
        }

        /// <summary>
        /// Gets a BaseItem based upon it's client-side item id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>BaseItem.</returns>
        public static BaseItem GetItemByClientId(string id, IUserManager userManager, ILibraryManager libraryManager, Guid? userId = null)
        {
            var isIdEmpty = string.IsNullOrEmpty(id);

            // If the item is an indexed folder we have to do a special routine to get it
            var isIndexFolder = !isIdEmpty &&
                                id.IndexOf(IndexFolderDelimeter, StringComparison.OrdinalIgnoreCase) != -1;

            if (isIndexFolder)
            {
                if (userId.HasValue)
                {
                    return GetIndexFolder(id, userId.Value, userManager, libraryManager);
                }
            }

            BaseItem item = null;

            if (userId.HasValue)
            {
                item = isIdEmpty
                           ? userManager.GetUserById(userId.Value).RootFolder
                           : libraryManager.GetItemById(new Guid(id), userId.Value);
            }
            else if (!isIndexFolder)
            {
                item = libraryManager.GetItemById(new Guid(id));
            }

            // If we still don't find it, look within individual user views
            if (item == null && !userId.HasValue)
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
    }
}
