using MediaBrowser.Model.Entities;
using System;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public static class Extensions
    {
        /// <summary>
        /// Adds the tagline.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="tagline">The tagline.</param>
        /// <exception cref="System.ArgumentNullException">tagline</exception>
        public static void AddTagline(this BaseItem item, string tagline)
        {
            if (string.IsNullOrWhiteSpace(tagline))
            {
                throw new ArgumentNullException("tagline");
            }

            if (!item.Taglines.Contains(tagline, StringComparer.OrdinalIgnoreCase))
            {
                item.Taglines.Add(tagline);
            }
        }

        public static void AddTrailerUrl(this BaseItem item, string url, bool isDirectLink)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException("url");
            }

            var current = item.RemoteTrailers.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));

            if (current != null)
            {
                current.IsDirectLink = isDirectLink;
            }
            else
            {
                item.RemoteTrailers.Add(new MediaUrl
                {
                    Url = url,
                    IsDirectLink = isDirectLink
                });
            }
        }
    }
}
