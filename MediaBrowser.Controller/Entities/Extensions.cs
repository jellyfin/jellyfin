using MediaBrowser.Model.Entities;
using System;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds the trailer URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="url">The URL.</param>
        /// <param name="isDirectLink">if set to <c>true</c> [is direct link].</param>
        /// <exception cref="System.ArgumentNullException">url</exception>
        public static void AddTrailerUrl(this IHasTrailers item, string url, bool isDirectLink)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException("url");
            }

            var current = item.RemoteTrailers.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));

            if (current == null)
            {
                item.RemoteTrailers.Add(new MediaUrl
                {
                    Url = url
                });
            }
        }
    }
}
