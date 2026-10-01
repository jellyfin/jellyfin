#pragma warning disable CS1591

using System;
using System.IO;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    public class AiredEpisodeOrderComparer : IBaseItemComparer
    {
        private static readonly Regex EpisodePartRegex = new(
            @"(?:^|[ _.-])(?:cd|dvd|part|pt|dis[ck])[ _.-]*(?<number>[0-9]+|[a-d])(?:$|[ _.-])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public ItemSortBy Type => ItemSortBy.AiredEpisodeOrder;

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem? x, BaseItem? y)
        {
            ArgumentNullException.ThrowIfNull(x);

            ArgumentNullException.ThrowIfNull(y);

            var episode1 = x as Episode;
            var episode2 = y as Episode;

            if (episode1 is null)
            {
                if (episode2 is null)
                {
                    return 0;
                }

                return 1;
            }

            if (episode2 is null)
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

        private static long GetSpecialCompareValue(Episode item)
        {
            // First sort by season number
            // Since there are three sort orders, pad with 9 digits (3 for each, figure 1000 episode buffer should be enough)
            var val = (item.AirsAfterSeasonNumber ?? item.AirsBeforeSeasonNumber ?? 0) * 1000000000L;

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
            var xValue = ((x.ParentIndexNumber ?? -1) * 1000) + (x.IndexNumber ?? -1);
            var yValue = ((y.ParentIndexNumber ?? -1) * 1000) + (y.IndexNumber ?? -1);
            var comparisonResult = xValue.CompareTo(yValue);
            if (comparisonResult == 0)
            {
                comparisonResult = GetAdditionalPartIndex(x, y).CompareTo(GetAdditionalPartIndex(y, x));
            }

            if (comparisonResult == 0)
            {
                comparisonResult = GetFilenamePartIndex(x).CompareTo(GetFilenamePartIndex(y));
            }

            // If equal, compare premiere dates
            if (comparisonResult == 0 && x.PremiereDate.HasValue && y.PremiereDate.HasValue)
            {
                comparisonResult = DateTime.Compare(x.PremiereDate.Value, y.PremiereDate.Value);
            }

            return comparisonResult;
        }

        private static int GetAdditionalPartIndex(Episode item, Episode other)
        {
            if (item.OwnerId.IsEmpty())
            {
                return 0;
            }

            var owner = item.OwnerId.Equals(other.Id)
                ? other as Video
                : item.GetOwner() as Video;

            if (owner is null)
            {
                return int.MaxValue;
            }

            var index = Array.FindIndex(
                owner.AdditionalParts,
                path => string.Equals(path, item.Path, StringComparison.Ordinal));

            return index < 0 ? int.MaxValue : index + 1;
        }

        private static int GetFilenamePartIndex(Episode item)
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                return int.MaxValue;
            }

            var match = EpisodePartRegex.Match(Path.GetFileNameWithoutExtension(item.Path));
            if (!match.Success)
            {
                return int.MaxValue;
            }

            var number = match.Groups["number"].Value;
            if (int.TryParse(number, out var partNumber))
            {
                return partNumber;
            }

            return char.ToUpperInvariant(number[0]) - 'A' + 1;
        }
    }
}
