using System;
using System.Collections.Generic;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Provides <c>Shuffle</c> extensions methods for <see cref="IList{T}" />.
    /// </summary>
    public static class ShuffleExtensions
    {
        /// <summary>
        /// Shuffles the items in a list.
        /// </summary>
        /// <param name="list">The list that should get shuffled.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            list.Shuffle(Random.Shared);
        }

        /// <summary>
        /// Shuffles the items in a list.
        /// </summary>
        /// <param name="list">The list that should get shuffled.</param>
        /// <param name="rng">The random number generator to use.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
