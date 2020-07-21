using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class ProductionYearComparer.
    /// </summary>
    public class ProductionYearComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetValue(x).CompareTo(GetValue(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private static int GetValue(BaseItem x)
        {
            if (x.ProductionYear.HasValue)
            {
                return x.ProductionYear.Value;
            }

            if (x.PremiereDate.HasValue)
            {
                return x.PremiereDate.Value.Year;
            }

            return 0;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.ProductionYear;
    }
}
