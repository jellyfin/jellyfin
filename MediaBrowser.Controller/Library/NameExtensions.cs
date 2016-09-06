using MediaBrowser.Common.Extensions;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Controller.Library
{
    public static class NameExtensions
    {
        public static bool AreEqual(string x, string y)
        {
            if (string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y))
            {
                return true;
            }

            return string.Compare(x, y, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
        }

        public static bool EqualsAny(IEnumerable<string> names, string x)
        {
            x = NormalizeForComparison(x);

            return names.Any(y => string.Compare(x, y, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0);
        }

        private static string NormalizeForComparison(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            return name;
            //return name.RemoveDiacritics();
        }

        private static string RemoveDiacritics(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            //return name;
            return name.RemoveDiacritics();
        }

        public static IEnumerable<string> DistinctNames(this IEnumerable<string> names)
        {
            return names.DistinctBy(RemoveDiacritics, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class DistinctNameComparer : IComparer<string>, IEqualityComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (string.IsNullOrWhiteSpace(x) && string.IsNullOrWhiteSpace(y))
            {
                return 0;
            }

            return string.Compare(x.RemoveDiacritics(), y.RemoveDiacritics(), StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(string x, string y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(string obj)
        {
            return (obj ?? string.Empty).GetHashCode();
        }
    }
}
