using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class IndexNumberComparer.
    /// </summary>
    public class IndexNumberComparer : IBaseItemComparer
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public ItemSortBy Type => ItemSortBy.IndexNumber;

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem? x, BaseItem? y)
        {
            ArgumentNullException.ThrowIfNull(x);

            ArgumentNullException.ThrowIfNull(y);

            if (!x.IndexNumber.HasValue && !y.IndexNumber.HasValue)
            {
                return 0;
            }

            if (!x.IndexNumber.HasValue)
            {
                return -1;
            }

            if (!y.IndexNumber.HasValue)
            {
                return 1;
            }

            return x.IndexNumber.Value.CompareTo(y.IndexNumber.Value);
        }
    }
}
