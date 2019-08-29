using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class ArtistComparer.
    /// </summary>
    public class ArtistComparer : IBaseItemComparer
    {
        /// <inheritdoc />
        public string Name => ItemSortBy.Artist;

        /// <inheritdoc />
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(GetValue(x), GetValue(y), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.String.</returns>
        private static string GetValue(BaseItem x)
        {
            if (!(x is Audio audio))
            {
                return string.Empty;
            }

            return audio.Artists.Count == 0 ? null : audio.Artists[0];
        }
    }
}
