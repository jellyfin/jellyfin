using System;
using System.Linq;
using System.Collections.Generic;
using MediaBrowser.Controller.Extensions;

namespace MediaBrowser.Controller.Library
{
    public static class NameExtensions
    {
        private static string RemoveDiacritics(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            return name.RemoveDiacritics();
        }

        public static IEnumerable<string> DistinctNames(this IEnumerable<string> names)
            => names.GroupBy(RemoveDiacritics, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First());
    }
}
