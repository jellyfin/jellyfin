using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
        /// <summary>
        /// Gets an Item by Id, or the root item if none is supplied
        /// </summary>
        public static BaseItem GetItemById(string id)
        {
            Guid guid = string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);

            return Kernel.Instance.GetItemById(guid);
        }

        /// <summary>
        /// Gets a User by Id
        /// </summary>
        /// <param name="logActivity">Whether or not to update the user's LastActivityDate</param>
        public static User GetUserById(string id, bool logActivity)
        {
            var guid = new Guid(id);

            var user = Kernel.Instance.Users.FirstOrDefault(u => u.Id == guid);

            if (logActivity)
            {
                LogUserActivity(user);
            }

            return user;
        }

        /// <summary>
        /// Gets the default User
        /// </summary>
        /// <param name="logActivity">Whether or not to update the user's LastActivityDate</param>
        public static User GetDefaultUser(bool logActivity)
        {
            User user = Kernel.Instance.GetDefaultUser();

            if (logActivity)
            {
                LogUserActivity(user);
            }

            return user;
        }

        /// <summary>
        /// Updates LastActivityDate for a given User
        /// </summary>
        public static void LogUserActivity(User user)
        {
            user.LastActivityDate = DateTime.UtcNow;
            Kernel.Instance.SaveUser(user);
        }

        /// <summary>
        /// Converts a BaseItem to a DTOBaseItem
        /// </summary>
        public async static Task<DtoBaseItem> GetDtoBaseItem(BaseItem item, User user,
            bool includeChildren = true,
            bool includePeople = true)
        {
            var dto = new DtoBaseItem();

            var tasks = new List<Task>();

            tasks.Add(AttachStudios(dto, item));

            if (includeChildren)
            {
                tasks.Add(AttachChildren(dto, item, user));
                tasks.Add(AttachLocalTrailers(dto, item, user));
            }

            if (includePeople)
            {
                tasks.Add(AttachPeople(dto, item));
            }

            AttachBasicFields(dto, item, user);

            // Make sure all the tasks we kicked off have completed.
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            return dto;
        }

        /// <summary>
        /// Sets simple property values on a DTOBaseItem
        /// </summary>
        private static void AttachBasicFields(DtoBaseItem dto, BaseItem item, User user)
        {
            dto.AspectRatio = item.AspectRatio;
            dto.BackdropCount = item.BackdropImagePaths == null ? 0 : item.BackdropImagePaths.Count();
            dto.DateCreated = item.DateCreated;
            dto.DisplayMediaType = item.DisplayMediaType;

            if (item.Genres != null)
            {
                dto.Genres = item.Genres.ToArray();
            }

            dto.HasArt = !string.IsNullOrEmpty(item.ArtImagePath);
            dto.HasBanner = !string.IsNullOrEmpty(item.BannerImagePath);
            dto.HasLogo = !string.IsNullOrEmpty(item.LogoImagePath);
            dto.HasPrimaryImage = !string.IsNullOrEmpty(item.PrimaryImagePath);
            dto.HasThumb = !string.IsNullOrEmpty(item.ThumbnailImagePath);
            dto.Id = item.Id;
            dto.IsNew = item.IsRecentlyAdded(user);
            dto.IndexNumber = item.IndexNumber;
            dto.IsFolder = item.IsFolder;
            dto.Language = item.Language;
            dto.LocalTrailerCount = item.LocalTrailers == null ? 0 : item.LocalTrailers.Count();
            dto.Name = item.Name;
            dto.OfficialRating = item.OfficialRating;
            dto.Overview = item.Overview;

            // If there are no backdrops, indicate what parent has them in case the Ui wants to allow inheritance
            if (dto.BackdropCount == 0)
            {
                int backdropCount;
                dto.ParentBackdropItemId = GetParentBackdropItemId(item, out backdropCount);
                dto.ParentBackdropCount = backdropCount;
            }

            if (item.Parent != null)
            {
                dto.ParentId = item.Parent.Id;
            }

            dto.ParentIndexNumber = item.ParentIndexNumber;

            // If there is no logo, indicate what parent has one in case the Ui wants to allow inheritance
            if (!dto.HasLogo)
            {
                dto.ParentLogoItemId = GetParentLogoItemId(item);
            }

            dto.Path = item.Path;

            dto.PremiereDate = item.PremiereDate;
            dto.ProductionYear = item.ProductionYear;
            dto.ProviderIds = item.ProviderIds;
            dto.RunTimeTicks = item.RunTimeTicks;
            dto.SortName = item.SortName;

            if (item.Taglines != null)
            {
                dto.Taglines = item.Taglines.ToArray();
            }

            dto.TrailerUrl = item.TrailerUrl;
            dto.Type = item.GetType().Name;
            dto.CommunityRating = item.CommunityRating;

            dto.UserData = GetDtoUserItemData(item.GetUserData(user, false));

            var folder = item as Folder;

            if (folder != null)
            {
                dto.SpecialCounts = folder.GetSpecialCounts(user);

                dto.IsRoot = folder.IsRoot;
                dto.IsVirtualFolder = folder.IsVirtualFolder;
            }

            // Add AudioInfo
            var audio = item as Audio;

            if (audio != null)
            {
                dto.AudioInfo = new AudioInfo
                {
                    Album = audio.Album,
                    AlbumArtist = audio.AlbumArtist,
                    Artist = audio.Artist,
                    BitRate = audio.BitRate,
                    Channels = audio.Channels
                };
            }

            // Add VideoInfo
            var video = item as Video;

            if (video != null)
            {
                dto.VideoInfo = new VideoInfo
                {
                    Height = video.Height,
                    Width = video.Width,
                    Codec = video.Codec,
                    VideoType = video.VideoType,
                    ScanType = video.ScanType
                };

                if (video.AudioStreams != null)
                {
                    dto.VideoInfo.AudioStreams = video.AudioStreams.ToArray();
                }

                if (video.Subtitles != null)
                {
                    dto.VideoInfo.Subtitles = video.Subtitles.ToArray();
                }
            }

            // Add SeriesInfo
            var series = item as Series;

            if (series != null)
            {
                DayOfWeek[] airDays = series.AirDays == null ? new DayOfWeek[] { } : series.AirDays.ToArray(); 

                dto.SeriesInfo = new SeriesInfo
                {
                    AirDays = airDays,
                    AirTime = series.AirTime,
                    Status = series.Status
                };
            }

            // Add MovieInfo
            var movie = item as Movie;

            if (movie != null)
            {
                int specialFeatureCount = movie.SpecialFeatures == null ? 0 : movie.SpecialFeatures.Count();

                dto.MovieInfo = new MovieInfo
                {
                    SpecialFeatureCount = specialFeatureCount
                };
            }
        }

        /// <summary>
        /// Attaches Studio DTO's to a DTOBaseItem
        /// </summary>
        private static async Task AttachStudios(DtoBaseItem dto, BaseItem item)
        {
            // Attach Studios by transforming them into BaseItemStudio (DTO)
            if (item.Studios != null)
            {
                Studio[] entities = await Task.WhenAll(item.Studios.Select(c => Kernel.Instance.ItemController.GetStudio(c))).ConfigureAwait(false);

                dto.Studios = new BaseItemStudio[entities.Length];

                for (int i = 0; i < entities.Length; i++)
                {
                    Studio entity = entities[i];
                    var baseItemStudio = new BaseItemStudio{};

                    baseItemStudio.Name = entity.Name;

                    baseItemStudio.HasImage = !string.IsNullOrEmpty(entity.PrimaryImagePath);

                    dto.Studios[i] = baseItemStudio;
                }
            }
        }

        /// <summary>
        /// Attaches child DTO's to a DTOBaseItem
        /// </summary>
        private static async Task AttachChildren(DtoBaseItem dto, BaseItem item, User user)
        {
            var folder = item as Folder;

            if (folder != null)
            {
                IEnumerable<BaseItem> children = folder.GetChildren(user);

                dto.Children = await Task.WhenAll(children.Select(c => GetDtoBaseItem(c, user, false, false))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attaches trailer DTO's to a DTOBaseItem
        /// </summary>
        private static async Task AttachLocalTrailers(DtoBaseItem dto, BaseItem item, User user)
        {
            if (item.LocalTrailers != null && item.LocalTrailers.Any())
            {
                dto.LocalTrailers = await Task.WhenAll(item.LocalTrailers.Select(c => GetDtoBaseItem(c, user, false, false))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attaches People DTO's to a DTOBaseItem
        /// </summary>
        private static async Task AttachPeople(DtoBaseItem dto, BaseItem item)
        {
            // Attach People by transforming them into BaseItemPerson (DTO)
            if (item.People != null)
            {
                IEnumerable<Person> entities = await Task.WhenAll(item.People.Select(c => Kernel.Instance.ItemController.GetPerson(c.Key))).ConfigureAwait(false);

                dto.People = item.People.Select(p =>
                {
                    var baseItemPerson = new BaseItemPerson{};

                    baseItemPerson.Name = p.Key;
                    baseItemPerson.Overview = p.Value.Overview;
                    baseItemPerson.Type = p.Value.Type;

                    Person ibnObject = entities.First(i => i.Name.Equals(p.Key, StringComparison.OrdinalIgnoreCase));

                    if (ibnObject != null)
                    {
                        baseItemPerson.HasImage = !string.IsNullOrEmpty(ibnObject.PrimaryImagePath);
                    }

                    return baseItemPerson;
                }).ToArray();
            }
        }

        /// <summary>
        /// If an item does not any backdrops, this can be used to find the first parent that does have one
        /// </summary>
        private static Guid? GetParentBackdropItemId(BaseItem item, out int backdropCount)
        {
            backdropCount = 0;

            var parent = item.Parent;

            while (parent != null)
            {
                if (parent.BackdropImagePaths != null && parent.BackdropImagePaths.Any())
                {
                    backdropCount = parent.BackdropImagePaths.Count();
                    return parent.Id;
                }

                parent = parent.Parent;
            }

            return null;
        }

        /// <summary>
        /// If an item does not have a logo, this can be used to find the first parent that does have one
        /// </summary>
        private static Guid? GetParentLogoItemId(BaseItem item)
        {
            var parent = item.Parent;

            while (parent != null)
            {
                if (!string.IsNullOrEmpty(parent.LogoImagePath))
                {
                    return parent.Id;
                }

                parent = parent.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets an ImagesByName entity along with the number of items containing it
        /// </summary>
        public static IbnItem GetIbnItem(BaseEntity entity, int itemCount)
        {
            return new IbnItem
            {
                Id = entity.Id,
                BaseItemCount = itemCount,
                HasImage = !string.IsNullOrEmpty(entity.PrimaryImagePath),
                Name = entity.Name
            };
        }

        /// <summary>
        /// Converts a User to a DTOUser
        /// </summary>
        public static DtoUser GetDtoUser(User user)
        {
            return new DtoUser
            {
                Id = user.Id,
                Name = user.Name,
                HasImage = !string.IsNullOrEmpty(user.PrimaryImagePath),
                HasPassword = !string.IsNullOrEmpty(user.Password),
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate
            };
        }

        /// <summary>
        /// Converts a UserItemData to a DTOUserItemData
        /// </summary>
        public static DtoUserItemData GetDtoUserItemData(UserItemData data)
        {
            if (data == null)
            {
                return null;
            }

            return new DtoUserItemData
            {
                IsFavorite = data.IsFavorite,
                Likes = data.Likes,
                PlaybackPositionTicks = data.PlaybackPositionTicks,
                PlayCount = data.PlayCount,
                Rating = data.Rating
            };
        }

        public static bool IsApiUrlMatch(string url, HttpListenerRequest request)
        {
            url = "/api/" + url;

            return request.Url.LocalPath.EndsWith(url, StringComparison.OrdinalIgnoreCase);
        }
    }
}
