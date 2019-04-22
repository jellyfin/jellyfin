using System;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Sorting;
using Jellyfin.Model.Querying;

namespace Jellyfin.Server.Implementations.Sorting
{
    /// <summary>
    /// Class NameComparer
    /// </summary>
    public class NameComparer : IBaseItemComparer
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

            return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.Name;
    }
}
