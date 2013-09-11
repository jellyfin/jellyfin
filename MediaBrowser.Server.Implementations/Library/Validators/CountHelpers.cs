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
        /// <summary>
        /// Adds to dictionary.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="counts">The counts.</param>
        internal static void AddToDictionary(BaseItem item, Dictionary<CountType, int> counts)
        {
            if (item is Movie)
            {
                IncrementCount(counts, CountType.Movie);
            }
            else if (item is Trailer)
            {
                IncrementCount(counts, CountType.Trailer);
            }
            else if (item is Series)
            {
                IncrementCount(counts, CountType.Series);
            }
            else if (item is Game)
            {
                IncrementCount(counts, CountType.Game);
            }
            else if (item is Audio)
            {
                IncrementCount(counts, CountType.Song);
            }
            else if (item is MusicAlbum)
            {
                IncrementCount(counts, CountType.MusicAlbum);
            }
            else if (item is Episode)
            {
                IncrementCount(counts, CountType.Episode);
            }
            else if (item is MusicVideo)
            {
                IncrementCount(counts, CountType.MusicVideo);
            }
            else if (item is AdultVideo)
            {
                IncrementCount(counts, CountType.AdultVideo);
            }

            IncrementCount(counts, CountType.Total);
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
        internal static void SetItemCounts(Guid userId, BaseItem media, List<string> names, Dictionary<string, Dictionary<Guid, Dictionary<CountType, int>>> masterDictionary)
        {
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

                AddToDictionary(media, userDictionary);
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
