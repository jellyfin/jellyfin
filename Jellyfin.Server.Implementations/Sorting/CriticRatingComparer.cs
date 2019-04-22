using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Sorting;
using Jellyfin.Model.Querying;

namespace Jellyfin.Server.Implementations.Sorting
{
    /// <summary>
    /// Class CriticRatingComparer
    /// </summary>
    public class CriticRatingComparer : IBaseItemComparer
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

        private static float GetValue(BaseItem x)
        {
            return x.CriticRating ?? 0;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.CriticRating;
    }
}
