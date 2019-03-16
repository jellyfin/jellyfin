using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class BaseApiService
    /// </summary>
    public class BaseApiService : IService, IRequiresRequest
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger => ApiEntryPoint.Instance.Logger;

        /// <summary>
        /// Gets or sets the HTTP result factory.
        /// </summary>
        /// <value>The HTTP result factory.</value>
        public IHttpResultFactory ResultFactory => ApiEntryPoint.Instance.ResultFactory;

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }

        public string GetHeader(string name)
        {
            return Request.Headers[name];
        }

        public static string[] SplitValue(string value, char delim)
        {
            if (value == null)
            {
                return Array.Empty<string>();
            }

            return value.Split(new[] { delim }, StringSplitOptions.RemoveEmptyEntries);
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
            if (!userId.Equals(auth.UserId))
            {
                if (!authenticatedUser.Policy.IsAdministrator)
                {
                    throw new SecurityException("Unauthorized access.");
                }
            }
            else if (restrictUserPreferences)
            {
                if (!authenticatedUser.Policy.EnableUserPreferenceAccess)
                {
                    throw new SecurityException("Unauthorized access.");
                }
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

            if (!options.ContainsField(Model.Querying.ItemFields.RecursiveItemCount)
                || !options.ContainsField(Model.Querying.ItemFields.ChildCount))
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
                    arr[oldLen] = Model.Querying.ItemFields.RecursiveItemCount;
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
                    arr[oldLen] = Model.Querying.ItemFields.ChildCount;
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

            if (result == null)
            {
                result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    Name = name.Replace(BaseItem.SlugChar, '/'),
                    IncludeItemTypes = new[] { typeof(T).Name },
                    DtoOptions = dtoOptions

                }).OfType<T>().FirstOrDefault();
            }

            if (result == null)
            {
                result = libraryManager.GetItemList(new InternalItemsQuery
                {
                    Name = name.Replace(BaseItem.SlugChar, '?'),
                    IncludeItemTypes = new[] { typeof(T).Name },
                    DtoOptions = dtoOptions

                }).OfType<T>().FirstOrDefault();
            }

            return result;
        }

        protected string GetPathValue(int index)
        {
            var pathInfo = Parse(Request.PathInfo);
            var first = pathInfo[0];

            // backwards compatibility
            if (string.Equals(first, "mediabrowser", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(first, "emby", StringComparison.OrdinalIgnoreCase))
            {
                index++;
            }

            return pathInfo[index];
        }

        private static string[] Parse(string pathUri)
        {
            var actionParts = pathUri.Split(new[] { "://" }, StringSplitOptions.None);

            var pathInfo = actionParts[actionParts.Length - 1];

            var optionsPos = pathInfo.LastIndexOf('?');
            if (optionsPos != -1)
            {
                pathInfo = pathInfo.Substring(0, optionsPos);
            }

            var args = pathInfo.Split('/');

            return args.Skip(1).ToArray();
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
