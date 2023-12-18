#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Extensions;

namespace MediaBrowser.Controller.Library
{
    public static class NameExtensions
    {
        public static IEnumerable<string> DistinctNames(this IEnumerable<string> names)
            => names.DistinctBy(RemoveDiacritics, StringComparer.OrdinalIgnoreCase);

        private static string RemoveDiacritics(string? name)
        {
            if (name is null)
            {
                return string.Empty;
            }

            return name.RemoveDiacritics();
        }
    }
}
