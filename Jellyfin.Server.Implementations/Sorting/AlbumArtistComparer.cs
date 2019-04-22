using System;
using System.Linq;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Sorting;
using Jellyfin.Model.Querying;

namespace Jellyfin.Server.Implementations.Sorting
{
    /// <summary>
    /// Class AlbumArtistComparer
    /// </summary>
    public class AlbumArtistComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
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
            var audio = x as IHasAlbumArtist;

            return audio?.AlbumArtists.FirstOrDefault();
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.AlbumArtist;
    }
}
