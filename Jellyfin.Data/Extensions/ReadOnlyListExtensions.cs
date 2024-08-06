using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Extensions;

/// <summary>
/// Extensions for handling data from an IReadOnlyList.
/// </summary>
public static class ReadOnlyListExtensions
{
    /// <summary>
    /// Converts each item in a <see cref="IReadOnlyList{TSource}"/> to a new <see cref="IReadOnlyList{TValue}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TValue">The target type.</typeparam>
    /// <param name="source">The source list.</param>
    /// <param name="converter">The converter function.</param>
    /// <returns>A new list containing the converted items.</returns>
    public static IReadOnlyList<TValue> ConvertAll<TSource, TValue>(this IReadOnlyList<TSource> source, Converter<TSource, TValue> converter)
    {
        if (source is TSource[] array)
        {
            return Array.ConvertAll(array, converter);
        }

        if (source is List<TSource> list)
        {
            return list.ConvertAll(converter);
        }

        var valueArray = new TValue[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            valueArray[i] = converter(source[i]);
        }

        return valueArray;
    }
}
