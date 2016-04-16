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
            return ResultFactory.GetStaticFileResult(Request, path);
        }

        private readonly char[] _dashReplaceChars = { '?', '/', '&' };
        private const char SlugChar = '-';

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

                if (!string.IsNullOrWhiteSpace(hasDtoOptions.EnableImageTypes))
                {
                    options.ImageTypes = (hasDtoOptions.EnableImageTypes ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).Select(v => (ImageType)Enum.Parse(typeof(ImageType), v, true)).ToList();
                }
            }

            return options;
        }

        protected MusicArtist GetArtist(string name, ILibraryManager libraryManager)
        {
            return libraryManager.GetArtist(DeSlugArtistName(name, libraryManager));
        }

        protected Studio GetStudio(string name, ILibraryManager libraryManager)
        {
            return libraryManager.GetStudio(DeSlugStudioName(name, libraryManager));
        }

        protected Genre GetGenre(string name, ILibraryManager libraryManager)
        {
            return libraryManager.GetGenre(DeSlugGenreName(name, libraryManager));
        }

        protected MusicGenre GetMusicGenre(string name, ILibraryManager libraryManager)
        {
            return libraryManager.GetMusicGenre(DeSlugGenreName(name, libraryManager));
        }

        protected GameGenre GetGameGenre(string name, ILibraryManager libraryManager)
        {
            return libraryManager.GetGameGenre(DeSlugGameGenreName(name, libraryManager));
        }

        protected Person GetPerson(string name, ILibraryManager libraryManager)
        {
            return libraryManager.GetPerson(DeSlugPersonName(name, libraryManager));
        }

        /// <summary>
        /// Deslugs an artist name by finding the correct entry in the library
        /// </summary>
        /// <param name="name"></param>
        /// <param name="libraryManager"></param>
        /// <returns></returns>
        protected string DeSlugArtistName(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(SlugChar) == -1)
            {
                return name;
            }

            var items = libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Audio).Name, typeof(MusicVideo).Name, typeof(MusicAlbum).Name }
            });

            return items
                .OfType<IHasArtist>()
                .SelectMany(i => i.AllArtists)
                .DistinctNames()
                .FirstOrDefault(i =>
                {
                    i = _dashReplaceChars.Aggregate(i, (current, c) => current.Replace(c, SlugChar));

                    return string.Equals(i, name, StringComparison.OrdinalIgnoreCase);

                }) ?? name;
        }

        /// <summary>
        /// Deslugs a genre name by finding the correct entry in the library
        /// </summary>
        protected string DeSlugGenreName(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(SlugChar) == -1)
            {
                return name;
            }

            return libraryManager.RootFolder.GetRecursiveChildren()
                .SelectMany(i => i.Genres)
                .DistinctNames()
                .FirstOrDefault(i =>
                {
                    i = _dashReplaceChars.Aggregate(i, (current, c) => current.Replace(c, SlugChar));

                    return string.Equals(i, name, StringComparison.OrdinalIgnoreCase);

                }) ?? name;
        }

        protected string DeSlugGameGenreName(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(SlugChar) == -1)
            {
                return name;
            }

            var items = libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Game).Name }
            });

            return items
                .SelectMany(i => i.Genres)
                .DistinctNames()
                .FirstOrDefault(i =>
                {
                    i = _dashReplaceChars.Aggregate(i, (current, c) => current.Replace(c, SlugChar));

                    return string.Equals(i, name, StringComparison.OrdinalIgnoreCase);

                }) ?? name;
        }

        /// <summary>
        /// Deslugs a studio name by finding the correct entry in the library
        /// </summary>
        protected string DeSlugStudioName(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(SlugChar) == -1)
            {
                return name;
            }

            return libraryManager.RootFolder
                .GetRecursiveChildren()
                .SelectMany(i => i.Studios)
                .DistinctNames()
                .FirstOrDefault(i =>
                {
                    i = _dashReplaceChars.Aggregate(i, (current, c) => current.Replace(c, SlugChar));

                    return string.Equals(i, name, StringComparison.OrdinalIgnoreCase);

                }) ?? name;
        }

        /// <summary>
        /// Deslugs a person name by finding the correct entry in the library
        /// </summary>
        protected string DeSlugPersonName(string name, ILibraryManager libraryManager)
        {
            if (name.IndexOf(SlugChar) == -1)
            {
                return name;
            }

            return libraryManager.GetPeopleNames(new InternalPeopleQuery())
                .FirstOrDefault(i =>
                {
                    i = _dashReplaceChars.Aggregate(i, (current, c) => current.Replace(c, SlugChar));

                    return string.Equals(i, name, StringComparison.OrdinalIgnoreCase);

                }) ?? name;
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
