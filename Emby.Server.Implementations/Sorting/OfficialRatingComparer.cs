#pragma warning disable CS1591

using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    public class OfficialRatingComparer : IBaseItemComparer
    {
        private readonly ILocalizationManager _localization;

        public OfficialRatingComparer(ILocalizationManager localization)
        {
            _localization = localization;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.OfficialRating;

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

            var levelX = string.IsNullOrEmpty(x.OfficialRating) ? 0 : _localization.GetRatingLevel(x.OfficialRating) ?? 0;
            var levelY = string.IsNullOrEmpty(y.OfficialRating) ? 0 : _localization.GetRatingLevel(y.OfficialRating) ?? 0;

            return levelX.CompareTo(levelY);
        }
    }
}
