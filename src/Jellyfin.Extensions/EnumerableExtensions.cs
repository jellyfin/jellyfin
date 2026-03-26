using System;
using System.Collections.Generic;

namespace Jellyfin.Extensions;

/// <summary>
/// Static extensions for the <see cref="IEnumerable{T}"/> interface.
/// </summary>
public static class EnumerableExtensions
{
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

    /// <summary>
    /// Gets an IEnumerable consisting of all flags of an enum.
    /// </summary>
    /// <param name="flags">The flags enum.</param>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>The IEnumerable{Enum}.</returns>
    public static IEnumerable<T> GetUniqueFlags<T>(this T flags)
        where T : struct, Enum
    {
        foreach (T value in Enum.GetValues<T>())
        {
            if (flags.HasFlag(value))
            {
                yield return value;
            }
        }
    }
}
