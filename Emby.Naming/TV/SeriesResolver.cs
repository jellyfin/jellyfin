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
        /// Regex that matches strings of at least 2 characters separated by a dot or underscore.
        /// Used for removing separators between words, i.e turns "The_show" into "The show" while
        /// preserving names like "S.H.O.W".
        /// </summary>
        [GeneratedRegex(@"((?<a>[^\._]{2,})[\._]*)|([\._](?<b>[^\._]{2,}))")]
        private static partial Regex SeriesNameRegex();

        /// <summary>
        /// Regex that matches titles with year in parentheses. Captures the title (which may be
        /// numeric) before the year, i.e. turns "1923 (2022)" into "1923".
        /// </summary>
        [GeneratedRegex(@"(?<title>.+?)\s*\(\d{4}\)")]
        private static partial Regex TitleWithYearRegex();

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
                        Name = seriesName
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

            if (!string.IsNullOrEmpty(seriesName))
            {
                seriesName = SeriesNameRegex().Replace(seriesName, "${a} ${b}").Trim();
            }

            return new SeriesInfo(path)
            {
                Name = seriesName
            };
        }
    }
}
