#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Common.Extensions
{
    // The MS CollectionExtensions are only available in netcoreapp
    public static class CollectionExtensions
    {
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

        /// <summary>
        /// Copies all the elements of the current collection to the specified list
        /// starting at the specified destination array index. The index is specified as a 32-bit integer.
        /// </summary>
        /// <param name="source">The current collection that is the source of the elements.</param>
        /// <param name="destination">The list that is the destination of the elements copied from the current collection.</param>
        /// <param name="index">A 32-bit integer that represents the index in <c>destination</c> at which copying begins.</param>
        /// <typeparam name="T"></typeparam>
        public static void CopyTo<T>(this IReadOnlyCollection<T> source, IList<T> destination, int index = 0)
        {
            foreach (T item in source)
            {
                destination[index++] = item;
            }
        }
    }
}
