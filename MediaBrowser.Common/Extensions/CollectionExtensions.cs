#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.Extensions
{
    // The MS CollectionExtensions are only available in netcoreapp
    public static class CollectionExtensions
    {
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Shuffles the items in a list.
        /// </summary>
        /// <param name="list">The list that should get shuffled.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var ret);
            return ret;
        }

        /// <summary>
        /// Copies all the elements of the current collection to the specified list
        /// starting at the specified destination array index. The index is specified as a 32-bit integer.
        /// </summary>
        /// <param name="source">The current collection that is the source of the elements.</param>
        /// <param name="destination">The list that is the destination of the elements copied from the current collection.</param>
        /// <param name="index">A 32-bit integer that represents the index in <c>destination</c> at which copying begins.</param>
        /// <typeparam name="T"></typeparam>
        public static void CopyTo<T>(this IReadOnlyList<T> source, IList<T> destination, int index = 0)
        {
            for (int i = 0; i < source.Count; i++)
            {
                destination[index + i] = source[i];
            }
        }
    }
}
