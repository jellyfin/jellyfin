using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class PremiereDateComparer.
    /// </summary>
    public class PremiereDateComparer : IBaseItemComparer
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public ItemSortBy Type => ItemSortBy.PremiereDate;

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem? x, BaseItem? y)
        {
            return GetDate(x).CompareTo(GetDate(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private static DateTime GetDate(BaseItem? x)
        {
            if (x is null)
            {
                return DateTime.MinValue;
            }

            if (x.PremiereDate.HasValue)
            {
                return x.PremiereDate.Value;
            }

            if (x.ProductionYear.HasValue)
            {
                try
                {
                    return new DateTime(x.ProductionYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Don't blow up if the item has a bad ProductionYear, just return MinValue
                }
            }

            return DateTime.MinValue;
        }
    }
}
