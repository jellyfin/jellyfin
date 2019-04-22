using System;
using System.Linq;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Sorting;
using Jellyfin.Model.Querying;

namespace Jellyfin.Server.Implementations.Sorting
{
    public class StudioComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));

            if (y == null)
                throw new ArgumentNullException(nameof(y));
            return AlphanumComparator.CompareValues(x.Studios.FirstOrDefault() ?? string.Empty, y.Studios.FirstOrDefault() ?? string.Empty);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.Studio;
    }
}
