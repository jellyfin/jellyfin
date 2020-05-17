using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class BaseApiService
    /// </summary>
    public abstract class BaseApiService : IService, IRequiresRequest
    {
        public BaseApiService(
            ILogger<BaseApiService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory)
        {
            Logger = logger;
            ServerConfigurationManager = serverConfigurationManager;
            ResultFactory = httpResultFactory;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger<BaseApiService> Logger { get; }

        /// <summary>
        /// Gets or sets the server configuration manager.
        /// </summary>
        /// <value>The server configuration manager.</value>
        protected IServerConfigurationManager ServerConfigurationManager { get; }

        /// <summary>
        /// Gets the HTTP result factory.
        /// </summary>
        /// <value>The HTTP result factory.</value>
        protected IHttpResultFactory ResultFactory { get; }

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }

        public string GetHeader(string name) => Request.Headers[name];

        public static string[] SplitValue(string value, char delim)
        {
            return value == null
                ? Array.Empty<string>()
                : value.Split(new[] { delim }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static Guid[] GetGuids(string value)
        {
            if (value == null)
            {
                return Array.Empty<Guid>();
            }

            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(i => new Guid(i))
                        .ToArray();
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
            return ResultFactory.GetResult(Request, result);
        }

        protected void AssertCanUpdateUser(IAuthorizationContext authContext, IUserManager userManager, Guid userId, bool restrictUserPreferences)
        {
            var auth = authContext.GetAuthorizationInfo(Request);

            var authenticatedUser = auth.User;

            // If they're going to update the record of another user, they must be an administrator
            if ((!userId.Equals(auth.UserId) && !authenticatedUser.Policy.IsAdministrator)
                || (restrictUserPreferences && !authenticatedUser.Policy.EnableUserPreferenceAccess))
            {
                throw new SecurityException("Unauthorized access.");
            }
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <returns>SessionInfo.</returns>
        protected SessionInfo GetSession(ISessionContext sessionContext)
        {
            var session = sessionContext.GetSession(Request);

            if (session == null)
            {
                throw new ArgumentException("Session not found.");
            }

            return session;
        }

        protected DtoOptions GetDtoOptions(IAuthorizationContext authContext, object request)
        {
            var options = new DtoOptions();

            if (request is IHasItemFields hasFields)
            {
                options.Fields = hasFields.GetItemFields();
            }

            if (!options.ContainsField(ItemFields.RecursiveItemCount)
                || !options.ContainsField(ItemFields.ChildCount))
            {
                var client = authContext.GetAuthorizationInfo(Request).Client ?? string.Empty;
                if (client.IndexOf("kodi", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("wmc", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("media center", StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.IndexOf("classic", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    int oldLen = options.Fields.Length;
                    var arr = new ItemFields[oldLen + 1];
                    options.Fields.CopyTo(arr, 0);
                    arr[oldLen] = ItemFields.RecursiveItemCount;
                    options.Fields = arr;
                }

                if (client.IndexOf("kodi", StringComparison.OrdinalIgnoreCase) != -1 ||
                   client.IndexOf("wmc", StringComparison.OrdinalIgnoreCase) != -1 ||
                   client.IndexOf("media center", StringComparison.OrdinalIgnoreCase) != -1 ||
                   client.IndexOf("classic", StringComparison.OrdinalIgnoreCase) != -1 ||
                   client.IndexOf("roku", StringComparison.OrdinalIgnoreCase) != -1 ||
                   client.IndexOf("samsung", StringComparison.OrdinalIgnoreCase) != -1 ||
                   client.IndexOf("androidtv", StringComparison.OrdinalIgnoreCase) != -1)
                {

                    int oldLen = options.Fields.Length;
                    var arr = new ItemFields[oldLen + 1];
                    options.Fields.CopyTo(arr, 0);
                    arr[oldLen] = ItemFields.ChildCount;
                    options.Fields = arr;
                }
            }

            if (request is IHasDtoOptions hasDtoOptions)
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
                    options.ImageTypes = hasDtoOptions.EnableImageTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(v => (ImageType)Enum.Parse(typeof(ImageType), v, true))
                                                                        .ToArray();
                }
            }

            return options;
        }

        protected MusicArtist GetArtist(string name, ILibraryManager libraryManager, DtoOptions dtoOptions)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = GetItemFromSlugName<MusicArtist>(libraryManager, name, dtoOptions);

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetArtist(name, dtoOptions);
        }

        protected Studio GetStudio(string name, ILibraryManager libraryManager, DtoOptions dtoOptions)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = GetItemFromSlugName<Studio>(libraryManager, name, dtoOptions);

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetStudio(name);
        }

        protected Genre GetGenre(string name, ILibraryManager libraryManager, DtoOptions dtoOptions)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = GetItemFromSlugName<Genre>(libraryManager, name, dtoOptions);

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetGenre(name);
        }

        protected MusicGenre GetMusicGenre(string name, ILibraryManager libraryManager, DtoOptions dtoOptions)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = GetItemFromSlugName<MusicGenre>(libraryManager, name, dtoOptions);

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetMusicGenre(name);
        }

        protected Person GetPerson(string name, ILibraryManager libraryManager, DtoOptions dtoOptions)
        {
            if (name.IndexOf(BaseItem.SlugChar) != -1)
            {
                var result = GetItemFromSlugName<Person>(libraryManager, name, dtoOptions);

                if (result != null)
                {
                    return result;
                }
            }

            return libraryManager.GetPerson(name);
        }

        private T GetItemFromSlugName<T>(ILibraryManager libraryManager, string name, DtoOptions dtoOptions)
            where T : BaseItem, new()
        {
            var result = libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '&'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions

            }).OfType<T>().FirstOrDefault();

            result ??= libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '/'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions

            }).OfType<T>().FirstOrDefault();

            result ??= libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '?'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions

            }).OfType<T>().FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Gets the path segment at the specified index.
        /// </summary>
        /// <param name="index">The index of the path segment.</param>
        /// <returns>The path segment at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException" >Path doesn't contain enough segments.</exception>
        /// <exception cref="InvalidDataException" >Path doesn't start with the base url.</exception>
        protected internal ReadOnlySpan<char> GetPathValue(int index)
        {
            static void ThrowIndexOutOfRangeException()
                => throw new IndexOutOfRangeException("Path doesn't contain enough segments.");

            static void ThrowInvalidDataException()
                => throw new InvalidDataException("Path doesn't start with the base url.");

            ReadOnlySpan<char> path = Request.PathInfo;

            // Remove the protocol part from the url
            int pos = path.LastIndexOf("://");
            if (pos != -1)
            {
                path = path.Slice(pos + 3);
            }

            // Remove the query string
            pos = path.LastIndexOf('?');
            if (pos != -1)
            {
                path = path.Slice(0, pos);
            }

            // Remove the domain
            pos = path.IndexOf('/');
            if (pos != -1)
            {
                path = path.Slice(pos);
            }

            // Remove base url
            string baseUrl = ServerConfigurationManager.Configuration.BaseUrl;
            int baseUrlLen = baseUrl.Length;
            if (baseUrlLen != 0)
            {
                if (path.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Slice(baseUrlLen);
                }
                else
                {
                    // The path doesn't start with the base url,
                    // how did we get here?
                    ThrowInvalidDataException();
                }
            }

            // Remove leading /
            path = path.Slice(1);

            // Backwards compatibility
            const string Emby = "emby/";
            if (path.StartsWith(Emby, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Slice(Emby.Length);
            }

            const string MediaBrowser = "mediabrowser/";
            if (path.StartsWith(MediaBrowser, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Slice(MediaBrowser.Length);
            }

            // Skip segments until we are at the right index
            for (int i = 0; i < index; i++)
            {
                pos = path.IndexOf('/');
                if (pos == -1)
                {
                    ThrowIndexOutOfRangeException();
                }

                path = path.Slice(pos + 1);
            }

            // Remove the rest
            pos = path.IndexOf('/');
            if (pos != -1)
            {
                path = path.Slice(0, pos);
            }

            return path;
        }

        /// <summary>
        /// Gets the name of the item by.
        /// </summary>
        protected BaseItem GetItemByName(string name, string type, ILibraryManager libraryManager, DtoOptions dtoOptions)
        {
            if (type.Equals("Person", StringComparison.OrdinalIgnoreCase))
            {
                return GetPerson(name, libraryManager, dtoOptions);
            }
            else if (type.Equals("Artist", StringComparison.OrdinalIgnoreCase))
            {
                return GetArtist(name, libraryManager, dtoOptions);
            }
            else if (type.Equals("Genre", StringComparison.OrdinalIgnoreCase))
            {
                return GetGenre(name, libraryManager, dtoOptions);
            }
            else if (type.Equals("MusicGenre", StringComparison.OrdinalIgnoreCase))
            {
                return GetMusicGenre(name, libraryManager, dtoOptions);
            }
            else if (type.Equals("Studio", StringComparison.OrdinalIgnoreCase))
            {
                return GetStudio(name, libraryManager, dtoOptions);
            }
            else if (type.Equals("Year", StringComparison.OrdinalIgnoreCase))
            {
                return libraryManager.GetYear(int.Parse(name));
            }

            throw new ArgumentException("Invalid type", nameof(type));
        }
    }
}
