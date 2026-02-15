using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Allows comparing artists of albums. Only the first artist of each album is considered.
    /// </summary>
    public class AlbumArtistComparer : IBaseItemComparer
    {
        /// <summary>
        /// Gets the item type this comparer compares.
        /// </summary>
        public ItemSortBy Type => ItemSortBy.AlbumArtist;

        /// <summary>
        /// Compares the specified arguments on their primary artist.
        /// </summary>
        /// <param name="x">First item to compare.</param>
        /// <param name="y">Second item to compare.</param>
        /// <returns>Zero if equal, else negative or positive number to indicate order.</returns>
        public int Compare(BaseItem? x, BaseItem? y)
        {
            return string.Compare(GetFirstAlbumArtist(x), GetFirstAlbumArtist(y), StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetFirstAlbumArtist(BaseItem? x)
        {
            if (x is IHasAlbumArtist audio
                && audio.AlbumArtists.Count != 0)
            {
                return audio.AlbumArtists[0];
            }

            return null;
        }
    }
}
