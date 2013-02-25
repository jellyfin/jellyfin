using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using System;
using System.Net;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
        /// <summary>
        /// Gets a User by Id
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>User.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public static User GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var guid = new Guid(id);

            return Kernel.Instance.GetUserById(guid);
        }

        /// <summary>
        /// Determines whether [is API URL match] [the specified URL].
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if [is API URL match] [the specified URL]; otherwise, <c>false</c>.</returns>
        public static bool IsApiUrlMatch(string url, HttpListenerRequest request)
        {
            url = "/api/" + url;

            return request.Url.LocalPath.EndsWith(url, StringComparison.OrdinalIgnoreCase);
        }
    }
}
