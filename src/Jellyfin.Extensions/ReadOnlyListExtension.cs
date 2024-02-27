using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Static extensions for the <see cref="IReadOnlyList{T}"/> interface.
    /// </summary>
    public static class ReadOnlyListExtension
    {
        /// <summary>
        /// Finds the index of the desired item.
        /// </summary>
        /// <param name="source">The source list.</param>
        /// <param name="value">The value to fine.</param>
        /// <typeparam name="T">The type of item to find.</typeparam>
        /// <returns>Index if found, else -1.</returns>
        public static int IndexOf<T>(this IReadOnlyList<T> source, T value)
        {
            if (source is IList<T> list)
            {
                return list.IndexOf(value);
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (Equals(value, source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds the index of the predicate.
        /// </summary>
        /// <param name="source">The source list.</param>
        /// <param name="match">The value to find.</param>
        /// <typeparam name="T">The type of item to find.</typeparam>
        /// <returns>Index if found, else -1.</returns>
        public static int FindIndex<T>(this IReadOnlyList<T> source, Predicate<T> match)
        {
            if (source is List<T> list)
            {
                return list.FindIndex(match);
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (match(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Get the first or default item from a list.
        /// </summary>
        /// <param name="source">The source list.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns>The first item or default if list is empty.</returns>
        public static T? FirstOrDefault<T>(this IReadOnlyList<T>? source)
        {
            if (source is null || source.Count == 0)
            {
                return default;
            }

            return source[0];
        }

        /// <summary>
        /// Converts a ReadOnlyList{TIn} to ReadOnlyList{TOut}.
        /// </summary>
        /// <param name="source">The source list.</param>
        /// <param name="converter">The converter to use.</param>
        /// <typeparam name="TIn">The input type.</typeparam>
        /// <typeparam name="TOut">The output type.</typeparam>
        /// <returns>The converted list.</returns>
        public static IReadOnlyList<TOut> ConvertAll<TIn, TOut>(this IReadOnlyList<TIn>? source, Converter<TIn, TOut> converter)
        {
            if (source is null || source.Count == 0)
            {
                return Array.Empty<TOut>();
            }

            return source switch
            {
                List<TIn> list => list.ConvertAll(converter),
                TIn[] array => Array.ConvertAll(array, converter),
                _ => source.Select(s => converter(s)).ToList()
            };
        }
    }
}
