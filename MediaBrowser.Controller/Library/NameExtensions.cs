#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Extensions;

namespace MediaBrowser.Controller.Library
{
    public static class NameExtensions
    {
        public static IEnumerable<string> DistinctNames(this IEnumerable<string> names)
            => names.GroupBy(RemoveDiacritics, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First());

        private static string RemoveDiacritics(string? name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            return name.RemoveDiacritics();
        }
    }
}
