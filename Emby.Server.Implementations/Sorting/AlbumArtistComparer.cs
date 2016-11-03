using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;
using System;

namespace Emby.Server.Implementations.Sorting
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
        private string GetValue(BaseItem x)
        {
            var audio = x as IHasAlbumArtist;

            return audio != null ? audio.AlbumArtists.FirstOrDefault() : null;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.AlbumArtist; }
        }
    }
}
