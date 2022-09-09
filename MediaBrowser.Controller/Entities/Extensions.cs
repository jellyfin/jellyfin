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
            ArgumentNullException.ThrowIfNull(url, nameof(url));

            if (url.Length == 0)
            {
                throw new ArgumentException("String can't be empty", nameof(url));
            }

            var current = item.RemoteTrailers.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));

            if (current == null)
            {
                var mediaUrl = new MediaUrl
                {
                    Url = url
                };

                if (item.RemoteTrailers.Count == 0)
                {
                    item.RemoteTrailers = new[] { mediaUrl };
                }
                else
                {
                    var oldIds = item.RemoteTrailers;
                    var newIds = new MediaUrl[oldIds.Count + 1];
                    oldIds.CopyTo(newIds);
                    newIds[oldIds.Count] = mediaUrl;
                    item.RemoteTrailers = newIds;
                }
            }
        }
    }
}
