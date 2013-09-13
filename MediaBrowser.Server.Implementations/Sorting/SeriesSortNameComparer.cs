using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;
using System;

namespace MediaBrowser.Server.Implementations.Sorting
{
    class SeriesSortNameComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(GetValue(x), GetValue(y), StringComparison.CurrentCultureIgnoreCase);
        }

        private string GetValue(BaseItem item)
        {
            Series series = null;

            var season = item as Season;

            if (season != null)
            {
                series = season.Series;
            }

            var episode = item as Episode;

            if (episode != null)
            {
                series = episode.Series;
            }

            if (series == null)
            {
                series = item as Series;
            }

            return series != null ? series.SortName : null;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.SeriesSortName; }
        }
    }
}
