using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Dto;
using System;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Server.Implementations.Sorting
{
    /// <summary>
    /// Class PremiereDateComparer
    /// </summary>
    public class PremiereDateComparer : IBaseItemComparer
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
        private DateTime GetDate(BaseItem x)
        {
            if (x.PremiereDate.HasValue)
            {
                return x.PremiereDate.Value;
            }
            
            if (x.ProductionYear.HasValue)
            {
                return new DateTime(x.ProductionYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return DateTime.MaxValue;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.PremiereDate; }
        }
    }
}
