#pragma warning disable CS1591

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Extensions;

namespace MediaBrowser.Controller.Sorting
{
    public static class SortExtensions
    {
        private static readonly StringComparer _comparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        public static IEnumerable<T> OrderByString<T>(this IEnumerable<T> list, Func<T, string> getName)
        {
            return list.OrderBy(getName, _comparer);
        }

        public static IEnumerable<T> OrderByStringDescending<T>(this IEnumerable<T> list, Func<T, string> getName)
        {
            return list.OrderByDescending(getName, _comparer);
        }

        public static IOrderedEnumerable<T> ThenByString<T>(this IOrderedEnumerable<T> list, Func<T, string> getName)
        {
            return list.ThenBy(getName, _comparer);
        }

        public static IOrderedEnumerable<T> ThenByStringDescending<T>(this IOrderedEnumerable<T> list, Func<T, string> getName)
        {
            return list.ThenByDescending(getName, _comparer);
        }
    }
}
