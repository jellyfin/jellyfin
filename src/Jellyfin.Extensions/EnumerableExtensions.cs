using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

    /// <summary>
    /// Gets all flags from an enum.
    /// </summary>
    /// <param name="value">The enum.</param>
    /// <returns>The IEnumerable{Enum} containing all flags.</returns>
    public static IEnumerable<Enum> GetFlags(this Enum value)
    {
        return GetFlags(value, Enum.GetValues(value.GetType()).Cast<Enum>().ToArray());
    }

    /// <summary>
    /// Gets all individual flags from an enum.
    /// </summary>
    /// <param name="value">The enum.</param>
    /// <returns>The IEnumerable{Enum} containeing all individual flags.</returns>
    public static IEnumerable<Enum> GetIndividualFlags(this Enum value)
    {
        return GetFlags(value, GetFlagValues(value.GetType()).ToArray());
    }

    private static IEnumerable<Enum> GetFlags(Enum value, Enum[] values)
    {
        long bits = Convert.ToInt64(value, CultureInfo.InvariantCulture);
        List<Enum> results = new List<Enum>();
        for (int i = values.Length - 1; i >= 0; i--)
        {
            long mask = Convert.ToInt64(values[i], CultureInfo.InvariantCulture);
            if (i == 0 && mask == 0L)
            {
                break;
            }

            if ((bits & mask) == mask)
            {
                results.Add(values[i]);
                bits -= mask;
            }
        }

        if (bits != 0L)
        {
            return Enumerable.Empty<Enum>();
        }

        if (Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0L)
        {
            return results.Reverse<Enum>();
        }

        if (bits == Convert.ToInt64(value, CultureInfo.InvariantCulture) && values.Length > 0 && Convert.ToInt64(values[0], CultureInfo.InvariantCulture) == 0L)
        {
            return values.Take(1);
        }

        return Enumerable.Empty<Enum>();
    }

    private static IEnumerable<Enum> GetFlagValues(Type enumType)
    {
        long flag = 0x1;
        foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
        {
            long bits = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (bits == 0L)
            {
                continue;
            }

            while (flag < bits)
            {
                flag <<= 1;
            }

            if (flag == bits)
            {
                yield return value;
            }
        }
    }
}
