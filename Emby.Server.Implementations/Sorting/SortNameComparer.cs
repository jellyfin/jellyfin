using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class SortNameComparer.
    /// </summary>
    public class SortNameComparer : IBaseItemComparer
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
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            return string.Compare(x.SortName, y.SortName, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.SortName;
    }
}
