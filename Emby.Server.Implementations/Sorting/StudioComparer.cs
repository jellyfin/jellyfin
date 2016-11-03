using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;
using System.Linq;

namespace Emby.Server.Implementations.Sorting
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
            return AlphanumComparator.CompareValues(x.Studios.FirstOrDefault() ?? string.Empty, y.Studios.FirstOrDefault() ?? string.Empty);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.Studio; }
        }
    }
}
