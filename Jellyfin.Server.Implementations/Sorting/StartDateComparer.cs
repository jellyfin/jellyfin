using System;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.LiveTv;
using Jellyfin.Controller.Sorting;
using Jellyfin.Model.Querying;

namespace Jellyfin.Server.Implementations.Sorting
{
    public class StartDateComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetDate(x).CompareTo(GetDate(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private static DateTime GetDate(BaseItem x)
        {
            var hasStartDate = x as LiveTvProgram;

            if (hasStartDate != null)
            {
                return hasStartDate.StartDate;
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.StartDate;
    }
}
