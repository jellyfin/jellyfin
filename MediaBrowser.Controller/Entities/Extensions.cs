using System;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Entities;

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
        public static void AddTrailerUrl(this BaseItem item, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
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
