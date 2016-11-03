using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Entities;
using System;
using System.Globalization;
using MediaBrowser.Controller.Extensions;

namespace Emby.Server.Implementations.FileOrganization
{
    public static class NameUtils
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        internal static Tuple<T, int> GetMatchScore<T>(string sortedName, int? year, T series)
            where T : BaseItem
        {
            var score = 0;

            var seriesNameWithoutYear = series.Name;
            if (series.ProductionYear.HasValue)
            {
                seriesNameWithoutYear = seriesNameWithoutYear.Replace(series.ProductionYear.Value.ToString(UsCulture), String.Empty);
            }

            if (IsNameMatch(sortedName, seriesNameWithoutYear))
            {
                score++;

                if (year.HasValue && series.ProductionYear.HasValue)
                {
                    if (year.Value == series.ProductionYear.Value)
                    {
                        score++;
                    }
                    else
                    {
                        // Regardless of name, return a 0 score if the years don't match
                        return new Tuple<T, int>(series, 0);
                    }
                }
            }

            return new Tuple<T, int>(series, score);
        }


        private static bool IsNameMatch(string name1, string name2)
        {
            name1 = GetComparableName(name1);
            name2 = GetComparableName(name2);

            return String.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetComparableName(string name)
        {
            name = name.RemoveDiacritics();

            name = " " + name + " ";

            name = name.Replace(".", " ")
            .Replace("_", " ")
            .Replace(" and ", " ")
            .Replace(".and.", " ")
            .Replace("&", " ")
            .Replace("!", " ")
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace(":", " ")
            .Replace(",", " ")
            .Replace("-", " ")
            .Replace("'", " ")
            .Replace("[", " ")
            .Replace("]", " ")
            .Replace(" a ", String.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" the ", String.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", String.Empty);

            return name.Trim();
        }
    }
}
