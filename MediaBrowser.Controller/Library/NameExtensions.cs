using MediaBrowser.Common.Extensions;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Library
{
    public static class NameExtensions
    {
        public static bool AreEqual(string name1, string name2)
        {
            name1 = NormalizeForComparison(name1);
            name2 = NormalizeForComparison(name2);

            return string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsAny(IEnumerable<string> names, string name)
        {
            name = NormalizeForComparison(name);

            return names.Any(i => string.Equals(NormalizeForComparison(i), name, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeForComparison(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return name.RemoveDiacritics();
        }

        public static IEnumerable<string> DistinctNames(this IEnumerable<string> names)
        {
            return names.DistinctBy(NormalizeForComparison, StringComparer.OrdinalIgnoreCase);
        }
    }
}
