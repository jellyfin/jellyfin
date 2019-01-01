using MediaBrowser.Model.Entities;
using System;
using System.Linq;
using MediaBrowser.Model.Extensions;

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
                throw new ArgumentNullException("url");
            }

            var current = item.RemoteTrailers.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));

            if (current == null)
            {
                var mediaUrl = new MediaUrl
                {
                    Url = url
                };

                if (item.RemoteTrailers.Length == 0)
                {
                    item.RemoteTrailers = new[] { mediaUrl };
                }
                else
                {
                    item.RemoteTrailers = item.RemoteTrailers.Concat(new [] { mediaUrl }).ToArray();
                }
            }
        }
    }
}
