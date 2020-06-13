#pragma warning disable CS1591

using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    public class AiredEpisodeOrderComparer : IBaseItemComparer
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

            if (x.PremiereDate.HasValue && y.PremiereDate.HasValue)
            {
                var val = DateTime.Compare(x.PremiereDate.Value, y.PremiereDate.Value);

                if (val != 0)
                {
                    //return val;
                }
            }

            var episode1 = x as Episode;
            var episode2 = y as Episode;

            if (episode1 == null)
            {
                if (episode2 == null)
                {
                    return 0;
                }

                return 1;
            }

            if (episode2 == null)
            {
                return -1;
            }

            return Compare(episode1, episode2);
        }

        private int Compare(Episode x, Episode y)
        {
            var isXSpecial = (x.ParentIndexNumber ?? -1) == 0;
            var isYSpecial = (y.ParentIndexNumber ?? -1) == 0;

            if (isXSpecial && isYSpecial)
            {
                return CompareSpecials(x, y);
            }

            if (!isXSpecial && !isYSpecial)
            {
                return CompareEpisodes(x, y);
            }

            if (!isXSpecial)
            {
                return CompareEpisodeToSpecial(x, y);
            }

            return CompareEpisodeToSpecial(y, x) * -1;
        }

        private static int CompareEpisodeToSpecial(Episode x, Episode y)
        {
            // http://thetvdb.com/wiki/index.php?title=Special_Episodes

            var xSeason = x.ParentIndexNumber ?? -1;
            var ySeason = y.AirsAfterSeasonNumber ?? y.AirsBeforeSeasonNumber ?? -1;

            if (xSeason != ySeason)
            {
                return xSeason.CompareTo(ySeason);
            }

            // Special comes after episode
            if (y.AirsAfterSeasonNumber.HasValue)
            {
                return -1;
            }

            var yEpisode = y.AirsBeforeEpisodeNumber;

            // Special comes before the season
            if (!yEpisode.HasValue)
            {
                return 1;
            }

            // Compare episode number
            var xEpisode = x.IndexNumber;

            if (!xEpisode.HasValue)
            {
                // Can't really compare if this happens
                return 0;
            }

            // Special comes before episode
            if (xEpisode.Value == yEpisode.Value)
            {
                return 1;
            }

            return xEpisode.Value.CompareTo(yEpisode.Value);
        }

        private int CompareSpecials(Episode x, Episode y)
        {
            return GetSpecialCompareValue(x).CompareTo(GetSpecialCompareValue(y));
        }

        private static int GetSpecialCompareValue(Episode item)
        {
            // First sort by season number
            // Since there are three sort orders, pad with 9 digits (3 for each, figure 1000 episode buffer should be enough)
            var val = (item.AirsAfterSeasonNumber ?? item.AirsBeforeSeasonNumber ?? 0) * 1000000000;

            // Second sort order is if it airs after the season
            if (item.AirsAfterSeasonNumber.HasValue)
            {
                val += 1000000;
            }

            // Third level is the episode number
            val += (item.AirsBeforeEpisodeNumber ?? 0) * 1000;

            // Finally, if that's still the same, last resort is the special number itself
            val += item.IndexNumber ?? 0;

            return val;
        }

        private static int CompareEpisodes(Episode x, Episode y)
        {
            var xValue = (x.ParentIndexNumber ?? -1) * 1000 + (x.IndexNumber ?? -1);
            var yValue = (y.ParentIndexNumber ?? -1) * 1000 + (y.IndexNumber ?? -1);

            return xValue.CompareTo(yValue);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.AiredEpisodeOrder;
    }
}
