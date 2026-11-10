using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Extension methods for items with artists.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the album artists followed by the artists, without duplicates.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="item">The item.</param>
        /// <returns>The distinct artist names.</returns>
        public static IEnumerable<string> GetAllArtists<T>(this T item)
            where T : IHasArtist, IHasAlbumArtist
        {
            return item.AlbumArtists.Concat(item.Artists).DistinctNames();
        }
    }
}
