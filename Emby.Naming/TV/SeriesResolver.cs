using System;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Used to resolve information about series from path.
    /// </summary>
    public static partial class SeriesResolver
    {
        /// <summary>
        /// Regex that matches a run of dots or underscores that separates two words, where a word is
        /// at least 2 characters long. Used for removing separators between words, i.e turns
        /// "The_show" into "The show" while preserving acronyms like "S.H.O.W", whose single letters
        /// are a word on neither side. Whitespace bounds a word too, so the dot in
        /// "Marvel's Agents of S.H.I.E.L.D." is read against the "S" beside it rather than against
        /// the whole run of words before it.
        /// </summary>
        [GeneratedRegex(@"(?<=[^\s\._]{2})[\._]+|[\._]+(?=[^\s\._]{2})")]
        private static partial Regex SeriesNameRegex();

        /// <summary>
        /// Regex that matches titles with year in parentheses. Captures the title (which may be
        /// numeric) before the year, i.e. turns "1923 (2022)" into "1923". A year range such as
        /// "The Show (1989-2018)" or "The Show (2015-)" is accepted too, capturing the first year.
        /// </summary>
        [GeneratedRegex(@"(?<title>.+?)\s*\((?<year>[0-9]{4})(?:\s*[-–]\s*(?:[0-9]{4})?)?\)")]
        private static partial Regex TitleWithYearRegex();

        /// <summary>
        /// Regex that matches release folder names of season packs, i.e. turns
        /// "The Show - The Complete Series (Season 1, 2 &amp; 3) + Extras" or "The Show Seasons 1-5"
        /// into "The Show". "Season" must be followed by a number so titles like
        /// "The Show Season of Change" are left alone.
        /// </summary>
        [GeneratedRegex(@"^(?<title>.+?)[\s._]*(?:[-–(\[][\s._]*)?(?:the[\s._]+)?(?:complete[\s._]+(?:series|collection|seasons?|box[\s._]*set)\b|seasons?[\s._]*[0-9]{1,3}\b).*$", RegexOptions.IgnoreCase)]
        private static partial Regex SeasonPackRegex();

        /// <summary>
        /// Resolve information about series from path.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object passed to <see cref="SeriesPathParser"/>.</param>
        /// <param name="path">Path to series.</param>
        /// <returns>SeriesInfo.</returns>
        public static SeriesInfo Resolve(NamingOptions options, string path)
        {
            string seriesName = Path.GetFileName(path);

            // First check if the filename matches a title with year pattern (handles numeric titles)
            if (!string.IsNullOrEmpty(seriesName))
            {
                var titleWithYearMatch = TitleWithYearRegex().Match(seriesName);
                if (titleWithYearMatch.Success)
                {
                    seriesName = titleWithYearMatch.Groups["title"].Value.Trim();
                    return new SeriesInfo(path)
                    {
                        Name = seriesName,
                        Year = int.TryParse(titleWithYearMatch.Groups["year"].ValueSpan, out var year) ? year : null
                    };
                }
            }

            SeriesPathParserResult result = SeriesPathParser.Parse(options, path);
            if (result.Success)
            {
                if (!string.IsNullOrEmpty(result.SeriesName))
                {
                    seriesName = result.SeriesName;
                }
            }
            else if (!string.IsNullOrEmpty(seriesName))
            {
                var seasonPackMatch = SeasonPackRegex().Match(seriesName);
                var title = seasonPackMatch.Groups["title"].Value.TrimEnd(' ', '_', '.', '-', '–');

                // Don't reduce a name like "The Complete Series" to just "The"
                if (seasonPackMatch.Success && !title.Equals("the", StringComparison.OrdinalIgnoreCase))
                {
                    seriesName = title;
                }
            }

            if (!string.IsNullOrEmpty(seriesName))
            {
                seriesName = SeriesNameRegex().Replace(seriesName, " ").Trim();
            }

            return new SeriesInfo(path)
            {
                Name = seriesName
            };
        }
    }
}
