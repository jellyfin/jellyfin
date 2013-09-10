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
        internal static void AddToDictionary(BaseItem item, Dictionary<string, int> counts)
        {
            if (item is Movie)
            {
                IncrementCount(counts, "Movie");
            }
            else if (item is Trailer)
            {
                IncrementCount(counts, "Trailer");
            }
            else if (item is Series)
            {
                IncrementCount(counts, "Series");
            }
            else if (item is Game)
            {
                IncrementCount(counts, "Game");
            }
            else if (item is Audio)
            {
                IncrementCount(counts, "Audio");
            }
            else if (item is MusicAlbum)
            {
                IncrementCount(counts, "MusicAlbum");
            }
            else if (item is Episode)
            {
                IncrementCount(counts, "Episode");
            }
            else if (item is MusicVideo)
            {
                IncrementCount(counts, "MusicVideo");
            }
            else if (item is AdultVideo)
            {
                IncrementCount(counts, "AdultVideo");
            }

            IncrementCount(counts, "Total");
        }

        /// <summary>
        /// Increments the count.
        /// </summary>
        /// <param name="counts">The counts.</param>
        /// <param name="key">The key.</param>
        internal static void IncrementCount(Dictionary<string, int> counts, string key)
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
        internal static ItemByNameCounts GetCounts(Dictionary<string, int> counts)
        {
            return new ItemByNameCounts
            {
                AdultVideoCount = GetCount(counts, "AdultVideo"),
                AlbumCount = GetCount(counts, "MusicAlbum"),
                EpisodeCount = GetCount(counts, "Episode"),
                GameCount = GetCount(counts, "Game"),
                MovieCount = GetCount(counts, "Movie"),
                MusicVideoCount = GetCount(counts, "MusicVideo"),
                SeriesCount = GetCount(counts, "Series"),
                SongCount = GetCount(counts, "Audio"),
                TrailerCount = GetCount(counts, "Trailer"),
                TotalCount = GetCount(counts, "Total")
            };
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <param name="counts">The counts.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetCount(Dictionary<string, int> counts, string key)
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
        internal static void SetItemCounts(Guid? userId, BaseItem media, List<string> names, Dictionary<string, Dictionary<Guid, Dictionary<string, int>>> masterDictionary)
        {
            foreach (var name in names)
            {
                Dictionary<Guid, Dictionary<string, int>> libraryCounts;

                if (!masterDictionary.TryGetValue(name, out libraryCounts))
                {
                    libraryCounts = new Dictionary<Guid, Dictionary<string, int>>();
                    masterDictionary.Add(name, libraryCounts);
                }

                var userLibId = userId ?? Guid.Empty;
                Dictionary<string, int> userDictionary;

                if (!libraryCounts.TryGetValue(userLibId, out userDictionary))
                {
                    userDictionary = new Dictionary<string, int>();
                    libraryCounts.Add(userLibId, userDictionary);
                }

                AddToDictionary(media, userDictionary);
            }
        }
    }
}
