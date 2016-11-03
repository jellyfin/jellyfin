using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;
using System;

namespace Emby.Server.Implementations.Sorting
{
    public class AirTimeComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return DateTime.Compare(GetValue(x), GetValue(y));
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.String.</returns>
        private DateTime GetValue(BaseItem x)
        {
            var series = x as Series;

            if (series == null)
            {
                var season = x as Season;

                if (season != null)
                {
                    series = season.Series;
                }
                else
                {
                    var episode = x as Episode;

                    if (episode != null)
                    {
                        series = episode.Series;
                    }
                }
            }

            if (series != null)
            {
                DateTime result;
                if (DateTime.TryParse(series.AirTime, out result))
                {
                    return result;
                }
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.AirTime; }
        }
    }
}
