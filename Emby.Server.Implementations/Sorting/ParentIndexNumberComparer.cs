using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class ParentIndexNumberComparer.
    /// </summary>
    public class ParentIndexNumberComparer : IBaseItemComparer
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public ItemSortBy Type => ItemSortBy.ParentIndexNumber;

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

            if (!x.ParentIndexNumber.HasValue && !y.ParentIndexNumber.HasValue)
            {
                return 0;
            }

            if (!x.ParentIndexNumber.HasValue)
            {
                return -1;
            }

            if (!y.ParentIndexNumber.HasValue)
            {
                return 1;
            }

            return x.ParentIndexNumber.Value.CompareTo(y.ParentIndexNumber.Value);
        }
    }
}
