using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using ServiceStack.Text.Controller;
using ServiceStack.Web;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class BaseApiService
    /// </summary>
    public class BaseApiService : IHasResultFactory, IRestfulService, IHasSession
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the HTTP result factory.
        /// </summary>
        /// <value>The HTTP result factory.</value>
        public IHttpResultFactory ResultFactory { get; set; }

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }

        public ISessionContext SessionContext { get; set; }
        public IAuthorizationContext AuthorizationContext { get; set; }

        public string GetHeader(string name)
        {
            return Request.Headers[name];
        }

        /// <summary>
        /// To the optimized result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <returns>System.Object.</returns>
        protected object ToOptimizedResult<T>(T result)
            where T : class
        {
            return ResultFactory.GetOptimizedResult(Request, result);
        }

        protected void AssertCanUpdateUser(IUserManager userManager, string userId)
        {
            var auth = AuthorizationContext.GetAuthorizationInfo(Request);

            var authenticatedUser = userManager.GetUserById(auth.UserId);

            // If they're going to update the record of another user, they must be an administrator
            if (!string.Equals(userId, auth.UserId, StringComparison.OrdinalIgnoreCase))
            {
                if (!authenticatedUser.Policy.IsAdministrator)
                {
                    throw new SecurityException("Unauthorized access.");
                }
            }
            else
            {
                if (!authenticatedUser.Policy.EnableUserPreferenceAccess)
                {
                    throw new SecurityException("Unauthorized access.");
                }
            }
        }

        /// <summary>
        /// To the optimized serialized result using cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <returns>System.Object.</returns>
        protected object ToOptimizedSerializedResultUsingCache<T>(T result)
           where T : class
        {
            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <returns>SessionInfo.</returns>
        protected async Task<SessionInfo> GetSession()
        {
            var session = await SessionContext.GetSession(Request).ConfigureAwait(false);

            if (session == null)
            {
                throw new ArgumentException("Session not found.");
            }

            return session;
        }

        /// <summary>
        /// To the static file result.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Object.</returns>
        protected object ToStaticFileResult(string path)
        {
            return ResultFactory.GetStaticFileResult(Request, path).Result;
        }

        protected DtoOptions GetDtoOptions(object request)
        {
            var options = new DtoOptions();

            options.DeviceId = AuthorizationContext.GetAuthorizationInfo(Request).DeviceId;

            var hasFields = request as IHasItemFields;
            if (hasFields != null)
            {
                options.Fields = hasFields.GetItemFields().ToList();
            }

            var hasDtoOptions = request as IHasDtoOptions;
            if (hasDtoOptions != null)
            {
                options.EnableImages = hasDtoOptions.EnableImages ?? true;

                if (hasDtoOptions.ImageTypeLimit.HasValue)
                {
                    options.ImageTypeLimit = hasDtoOptions.ImageTypeLimit.Value;
                }
                if (hasDtoOptions.EnableUserData.HasValue)
                {
                    options.EnableUserData = hasDtoOptions.EnableUserData.Value;
                }

                if (!string.IsNullOrWhiteSpace(hasDtoOptions.EnableImageTypes))
                {
                    options.ImageTypes = (hasDtoOptions.EnableImageTypes ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).Select(v => (ImageType)Enum.Parse(typeof(ImageType), v, true)).ToList();
                }
            }

            return options;
        }

        protected MusicArtist GetArtist(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    SlugName = name,
                    IncludeItemTypes = new[] { typeof(MusicArtist).Name }

                }).OfType<MusicArtist>().FirstOrDefault();

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetArtist(name);
        }

        protected Studio GetStudio(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    SlugName = name,
                    IncludeItemTypes = new[] { typeof(Studio).Name }

                }).OfType<Studio>().FirstOrDefault();

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetStudio(name);
        }

        protected Genre GetGenre(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    SlugName = name,
                    IncludeItemTypes = new[] { typeof(Genre).Name }

                }).OfType<Genre>().FirstOrDefault();

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetGenre(name);
        }

        protected MusicGenre GetMusicGenre(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    SlugName = name,
                    IncludeItemTypes = new[] { typeof(MusicGenre).Name }

                }).OfType<MusicGenre>().FirstOrDefault();

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetMusicGenre(name);
        }

        protected GameGenre GetGameGenre(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    SlugName = name,
                    IncludeItemTypes = new[] { typeof(GameGenre).Name }

                }).OfType<GameGenre>().FirstOrDefault();

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetGameGenre(name);
        }

        protected Person GetPerson(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    SlugName = name,
                    IncludeItemTypes = new[] { typeof(Person).Name }

                }).OfType<Person>().FirstOrDefault();

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetPerson(name);
        }

        protected string GetPathValue(int index)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var first = pathInfo.GetArgumentValue<string>(0);

            // backwards compatibility
            if (string.Equals(first, "mediabrowser", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(first, "emby", StringComparison.OrdinalIgnoreCase))
            {
                index++;
            }

            return pathInfo.GetArgumentValue<string>(index);
        }

        /// <summary>
        /// Gets the name of the item by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>Task{BaseItem}.</returns>
        protected BaseItem GetItemByName(string name, string type, ILibraryManager libraryManager)
        {
            BaseItem item;

            if (type.IndexOf("Person", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = GetPerson(name, libraryManager);
            }
            else if (type.IndexOf("Artist", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = GetArtist(name, libraryManager);
            }
            else if (type.IndexOf("Genre", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = GetGenre(name, libraryManager);
            }
            else if (type.IndexOf("MusicGenre", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = GetMusicGenre(name, libraryManager);
            }
            else if (type.IndexOf("GameGenre", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = GetGameGenre(name, libraryManager);
            }
            else if (type.IndexOf("Studio", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = GetStudio(name, libraryManager);
            }
            else if (type.IndexOf("Year", StringComparison.OrdinalIgnoreCase) == 0)
            {
                item = libraryManager.GetYear(int.Parse(name));
            }
            else
            {
                throw new ArgumentException();
            }

            return item;
        }
    }
}
