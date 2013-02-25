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
