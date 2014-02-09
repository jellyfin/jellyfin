using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class BaseApiService
    /// </summary>
    [AuthorizationRequestFilter]
    public class BaseApiService : IHasResultFactory, IRestfulService
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

        /// <summary>
        /// To the optimized result using cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory function.</param>
        /// <returns>System.Object.</returns>
        protected object ToOptimizedResultUsingCache<T>(Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn)
           where T : class
        {
            return ResultFactory.GetOptimizedResultUsingCache(Request, cacheKey, lastDateModified, cacheDuration, factoryFn);
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
        /// To the cached result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">cacheKey</exception>
        protected object ToCachedResult<T>(Guid cacheKey, DateTime lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn, string contentType)
          where T : class
        {
            return ResultFactory.GetCachedResult(Request, cacheKey, lastDateModified, cacheDuration, factoryFn, contentType);
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

        private readonly char[] _dashReplaceChars = new[] { '?', '/' };
        private const char SlugChar = '-';

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

        protected IList<BaseItem> GetAllLibraryItems(Guid? userId, IUserManager userManager, ILibraryManager libraryManager)
        {
            if (userId.HasValue)
            {
                var user = userManager.GetUserById(userId.Value);

                return userManager.GetUserById(userId.Value).RootFolder.GetRecursiveChildren(user, null);
            }

            return libraryManager.RootFolder.GetRecursiveChildren();
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

            return libraryManager.GetAllArtists()
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
                .Distinct(StringComparer.OrdinalIgnoreCase)
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

            return libraryManager.RootFolder.GetRecursiveChildren(i => i is Game)
                .SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
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

            return libraryManager.RootFolder.GetRecursiveChildren()
                .SelectMany(i => i.Studios)
                .Distinct(StringComparer.OrdinalIgnoreCase)
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

            return libraryManager.RootFolder.GetRecursiveChildren()
                .SelectMany(i => i.People)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(i =>
                {
                    i = _dashReplaceChars.Aggregate(i, (current, c) => current.Replace(c, SlugChar));

                    return string.Equals(i, name, StringComparison.OrdinalIgnoreCase);

                }) ?? name;
        }

        /// <summary>
        /// Gets the name of the item by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>Task{BaseItem}.</returns>
        /// <exception cref="System.ArgumentException"></exception>
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
