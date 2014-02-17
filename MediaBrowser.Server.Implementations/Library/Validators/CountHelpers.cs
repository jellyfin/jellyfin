using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class CountHelpers
    /// </summary>
    internal static class CountHelpers
    {
        private static CountType? GetCountType(BaseItem item)
        {
            if (item is Movie)
            {
                return CountType.Movie;
            }
            if (item is Episode)
            {
                return CountType.Episode;
            }
            if (item is Game)
            {
                return CountType.Game;
            }
            if (item is Audio)
            {
                return CountType.Song;
            }
            if (item is Trailer)
            {
                return CountType.Trailer;
            }
            if (item is Series)
            {
                return CountType.Series;
            }
            if (item is MusicAlbum)
            {
                return CountType.MusicAlbum;
            }
            if (item is MusicVideo)
            {
                return CountType.MusicVideo;
            }
            if (item is AdultVideo)
            {
                return CountType.AdultVideo;
            }

            return null;
        }

        /// <summary>
        /// Increments the count.
        /// </summary>
        /// <param name="counts">The counts.</param>
        /// <param name="key">The key.</param>
        internal static void IncrementCount(Dictionary<CountType, int> counts, CountType key)
        {
            int count;

            if (counts.TryGetValue(key, out count))
            {
                count++;
                counts[key] = count;
            }
            else
            {
                counts.Add(key, 1);
            }
        }

        /// <summary>
        /// Gets the counts.
        /// </summary>
        /// <param name="counts">The counts.</param>
        /// <returns>ItemByNameCounts.</returns>
        internal static ItemByNameCounts GetCounts(Dictionary<CountType, int> counts)
        {
            return new ItemByNameCounts
            {
                AdultVideoCount = GetCount(counts, CountType.AdultVideo),
                AlbumCount = GetCount(counts, CountType.MusicAlbum),
                EpisodeCount = GetCount(counts, CountType.Episode),
                GameCount = GetCount(counts, CountType.Game),
                MovieCount = GetCount(counts, CountType.Movie),
                MusicVideoCount = GetCount(counts, CountType.MusicVideo),
                SeriesCount = GetCount(counts, CountType.Series),
                SongCount = GetCount(counts, CountType.Song),
                TrailerCount = GetCount(counts, CountType.Trailer),
                TotalCount = GetCount(counts, CountType.Total)
            };
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <param name="counts">The counts.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetCount(Dictionary<CountType, int> counts, CountType key)
        {
            int count;

            if (counts.TryGetValue(key, out count))
            {
                return count;
            }

            return 0;
        }

        /// <summary>
        /// Sets the item counts.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="media">The media.</param>
        /// <param name="names">The names.</param>
        /// <param name="masterDictionary">The master dictionary.</param>
        internal static void SetItemCounts(Guid userId, BaseItem media, IEnumerable<string> names, Dictionary<string, Dictionary<Guid, Dictionary<CountType, int>>> masterDictionary)
        {
            var countType = GetCountType(media);

            foreach (var name in names)
            {
                Dictionary<Guid, Dictionary<CountType, int>> libraryCounts;

                if (!masterDictionary.TryGetValue(name, out libraryCounts))
                {
                    libraryCounts = new Dictionary<Guid, Dictionary<CountType, int>>();
                    masterDictionary.Add(name, libraryCounts);
                }

                var userLibId = userId/* ?? Guid.Empty*/;
                Dictionary<CountType, int> userDictionary;

                if (!libraryCounts.TryGetValue(userLibId, out userDictionary))
                {
                    userDictionary = new Dictionary<CountType, int>();
                    libraryCounts.Add(userLibId, userDictionary);
                }

                if (countType.HasValue)
                {
                    IncrementCount(userDictionary, countType.Value);
                }

                IncrementCount(userDictionary, CountType.Total);
            }
        }
    }

    internal enum CountType
    {
        AdultVideo,
        MusicAlbum,
        Episode,
        Game,
        Movie,
        MusicVideo,
        Series,
        Song,
        Trailer,
        Total
    }
}
