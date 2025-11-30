using System.Text.RegularExpressions;

namespace Emby.Naming.Book
{
    /// <summary>
    /// Helper class to retrieve basic metadata from a book filename.
    /// </summary>
    public static class BookFileNameParser
    {
        private const string NameMatchGroup = "name";
        private const string IndexMatchGroup = "index";
        private const string YearMatchGroup = "year";
        private const string SeriesNameMatchGroup = "seriesName";

        private static readonly Regex[] _nameMatches =
        [
            // seriesName (seriesYear) #index (of count) (year) where only seriesName and index are required
            new Regex(@"^(?<seriesName>.+?)((\s\((?<seriesYear>[0-9]{4})\))?)\s#(?<index>[0-9]+)((\s\(of\s(?<count>[0-9]+)\))?)((\s\((?<year>[0-9]{4})\))?)$"),
            new Regex(@"^(?<name>.+?)\s\((?<seriesName>.+?),\s#(?<index>[0-9]+)\)((\s\((?<year>[0-9]{4})\))?)$"),
            new Regex(@"^(?<index>[0-9]+)\s\-\s(?<name>.+?)((\s\((?<year>[0-9]{4})\))?)$"),
            new Regex(@"(?<name>.*)\((?<year>[0-9]{4})\)"),
            // last resort matches the whole string as the name
            new Regex(@"(?<name>.*)")
        ];

        /// <summary>
        /// Parse a filename name to retrieve the book name, series name, index, and year.
        /// </summary>
        /// <param name="name">Book filename to parse for information.</param>
        /// <returns>Returns <see cref="BookFileNameParserResult"/> object.</returns>
        public static BookFileNameParserResult Parse(string? name)
        {
            var result = new BookFileNameParserResult();

            if (name == null)
            {
                return result;
            }

            foreach (var regex in _nameMatches)
            {
                var match = regex.Match(name);

                if (!match.Success)
                {
                    continue;
                }

                if (match.Groups.TryGetValue(NameMatchGroup, out Group? nameGroup) && nameGroup.Success)
                {
                    result.Name = nameGroup.Value.Trim();
                }

                if (match.Groups.TryGetValue(IndexMatchGroup, out Group? indexGroup) && indexGroup.Success && int.TryParse(indexGroup.Value, out var index))
                {
                    result.Index = index;
                }

                if (match.Groups.TryGetValue(YearMatchGroup, out Group? yearGroup) && yearGroup.Success && int.TryParse(yearGroup.Value, out var year))
                {
                    result.Year = year;
                }

                if (match.Groups.TryGetValue(SeriesNameMatchGroup, out Group? seriesGroup) && seriesGroup.Success)
                {
                    result.SeriesName = seriesGroup.Value.Trim();
                }

                break;
            }

            return result;
        }
    }
}
