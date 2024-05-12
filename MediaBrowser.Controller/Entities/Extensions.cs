using System;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds the trailer URL.
        /// </summary>
        /// <param name="item">Media item.</param>
        /// <param name="url">Trailer URL.</param>
        public static void AddTrailerUrl(this BaseItem item, string url)
        {
            ArgumentException.ThrowIfNullOrEmpty(url);

            var current = item.RemoteTrailers.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));

            if (current is null)
            {
                var mediaUrl = new MediaUrl
                {
                    Url = url
                };

                if (item.RemoteTrailers.Count == 0)
                {
                    item.RemoteTrailers = [mediaUrl];
                }
                else
                {
                    item.RemoteTrailers = [..item.RemoteTrailers, mediaUrl];
                }
            }
        }
    }
}
