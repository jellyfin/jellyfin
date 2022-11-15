using System;
using System.Collections.Generic;

namespace Jellyfin.Extensions;

/// <summary>
/// Static extensions for the <see cref="IEnumerable{T}"/> interface.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Determines whether the value is contained in the source collection.
    /// </summary>
    /// <param name="source">An instance of the <see cref="IEnumerable{String}"/> interface.</param>
    /// <param name="value">The value to look for in the collection.</param>
    /// <param name="stringComparison">The string comparison.</param>
    /// <returns>A value indicating whether the value is contained in the collection.</returns>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    public static bool Contains(this IEnumerable<string> source, ReadOnlySpan<char> value, StringComparison stringComparison)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IList<string> list)
        {
            int len = list.Count;
            for (int i = 0; i < len; i++)
            {
                if (value.Equals(list[i], stringComparison))
                {
                    return true;
                }
            }

            return false;
        }

        foreach (string element in source)
        {
            if (value.Equals(element, stringComparison))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets an IEnumerable from a single item.
    /// </summary>
    /// <param name="item">The item to return.</param>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>The IEnumerable{T}.</returns>
    public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
    {
        yield return item;
    }
}
