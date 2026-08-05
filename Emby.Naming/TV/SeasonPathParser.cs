using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Class to parse season paths.
    /// </summary>
    public static partial class SeasonPathParser
    {
        private const string SeasonKeywordPattern =
            @"시즌|シーズン|сезон" +
            @"|season|sæson|saison|staffel|series|stagione|säsong|seizoen|seasong" +
            @"|sezon|sezona|sezóna|sezonul|série|séria|serie|seria|temporada|kausi";

        private static readonly Regex CleanNameRegex = new(@"[ ._\-\[\]]", RegexOptions.Compiled);

        [GeneratedRegex(@"^\s*((?<seasonnumber>(?>\d+))(?:st|nd|rd|th|\.)*(?!\s*[Ee]\d+))\s*(?:" + SeasonKeywordPattern + @")\s*(?<rightpart>.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex ProcessPre();

        [GeneratedRegex(@"^\s*(?:" + SeasonKeywordPattern + @")\s*(?<seasonnumber>\d+?)(?=\d{3,4}p|[^\d]|$)(?!\s*[Ee]\d)(?<rightpart>.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex ProcessPost();

        [GeneratedRegex(@"[sS](\d{1,4})(?!\d|[eE]\d)(?=\.|_|-|\[|\]|\s|$)", RegexOptions.None)]
        private static partial Regex SeasonPrefix();

        [GeneratedRegex(SeasonKeywordPattern, RegexOptions.IgnoreCase)]
        private static partial Regex SeasonKeyword();

        /// <summary>
        /// Attempts to parse season number from path.
        /// </summary>
        /// <param name="path">Path to season.</param>
        /// <param name="parentPath">Folder name of the parent.</param>
        /// <param name="supportSpecialAliases">Support special aliases when parsing.</param>
        /// <param name="supportNumericSeasonFolders">Support numeric season folders when parsing.</param>
        /// <returns>Returns <see cref="SeasonPathParserResult"/> object.</returns>
        public static SeasonPathParserResult Parse(string path, string? parentPath, bool supportSpecialAliases, bool supportNumericSeasonFolders)
        {
            var result = new SeasonPathParserResult();
            var parentFolderName = parentPath is null ? null : new DirectoryInfo(parentPath).Name;

            var (seasonNumber, isSeasonFolder) = GetSeasonNumberFromPath(path, parentFolderName, supportSpecialAliases, supportNumericSeasonFolders);

            result.SeasonNumber = seasonNumber;

            if (result.SeasonNumber.HasValue)
            {
                result.Success = true;
                result.IsSeasonFolder = isSeasonFolder;
            }

            return result;
        }

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="parentFolderName">The parent folder name.</param>
        /// <param name="supportSpecialAliases">if set to <c>true</c> [support special aliases].</param>
        /// <param name="supportNumericSeasonFolders">if set to <c>true</c> [support numeric season folders].</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static (int? SeasonNumber, bool IsSeasonFolder) GetSeasonNumberFromPath(
            string path,
            string? parentFolderName,
            bool supportSpecialAliases,
            bool supportNumericSeasonFolders)
        {
            var fileName = Path.GetFileName(path);

            var seasonPrefixMatch = SeasonPrefix().Match(fileName);
            if (seasonPrefixMatch.Success &&
                int.TryParse(seasonPrefixMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
            {
                return (val, true);
            }

            string filename = CleanNameRegex.Replace(fileName, string.Empty);

            if (parentFolderName is not null)
            {
                var cleanParent = CleanNameRegex.Replace(parentFolderName, string.Empty);
                filename = filename.Replace(cleanParent, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            if (supportSpecialAliases &&
                (filename.Equals("specials", StringComparison.OrdinalIgnoreCase) ||
                 filename.Equals("extras", StringComparison.OrdinalIgnoreCase)))
            {
                return (0, true);
            }

            if (supportNumericSeasonFolders &&
                int.TryParse(filename, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            {
                return (val, true);
            }

            bool isMixedLibrary = !supportNumericSeasonFolders && !supportSpecialAliases;
            var preMatch = ProcessPre().Match(filename);
            if (preMatch.Success)
            {
                if (isMixedLibrary && !SeasonKeyword().IsMatch(fileName))
                {
                    return (null, false);
                }

                return CheckMatch(preMatch);
            }

            var postMatch = ProcessPost().Match(filename);
            if (postMatch.Success)
            {
                if (isMixedLibrary && !SeasonKeyword().IsMatch(fileName))
                {
                    return (null, false);
                }

                return CheckMatch(postMatch);
            }

            // Fallback: handle series-prefixed season folders like
            // "Solar Opposites - Season 1" or "Show (2020) - Season 3".
            // The anchored ProcessPre/ProcessPost regexes require the season keyword
            // at the start of the string, which fails when the series name precedes it.
            // Find each season keyword occurrence and try parsing from that position.
            // Iterate all keyword matches to handle cases like "The Series Finale - Season 1"
            // where the series name itself contains a season keyword (e.g. "Series").
            var keywordMatches = SeasonKeyword().Matches(filename);
            foreach (Match kwMatch in keywordMatches)
            {
                // Skip keyword matches that have a digit immediately before them —
                // this indicates an episode pattern like "Episode 1 Season 2"
                if (kwMatch.Index > 0 && char.IsDigit(filename[kwMatch.Index - 1]))
                {
                    continue;
                }

                if (isMixedLibrary && !SeasonKeyword().IsMatch(fileName))
                {
                    return (null, false);
                }

                var fromKeyword = filename[kwMatch.Index..];

                preMatch = ProcessPre().Match(fromKeyword);
                if (preMatch.Success)
                {
                    return CheckMatch(preMatch);
                }

                postMatch = ProcessPost().Match(fromKeyword);
                if (postMatch.Success)
                {
                    return CheckMatch(postMatch);
                }
            }

            return (null, false);
        }

        private static (int? SeasonNumber, bool IsSeasonFolder) CheckMatch(Match match)
        {
            var numberString = match.Groups["seasonnumber"];
            if (numberString.Success)
            {
                if (int.TryParse(numberString.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seasonNumber))
                {
                    return (SanitizeSeasonNumber(seasonNumber), true);
                }
            }

            return (null, false);
        }

        /// <summary>
        /// Sanitizes a season number that may have absorbed an adjacent year value
        /// due to CleanNameRegex removing delimiters (e.g. "Season 7 (2023)" becomes
        /// "Season72023" after cleaning, which would parse as 72023).
        /// If the number ends with a plausible 4-digit year (1900-2099), strip it.
        /// </summary>
        private static int? SanitizeSeasonNumber(int seasonNumber)
        {
            // No TV show has 10,000+ seasons — if we got a number this large,
            // it likely absorbed an adjacent year like "Season 7 2023" → 72023.
            if (seasonNumber >= 10000)
            {
                var suffix = seasonNumber % 10000;
                // Check if the last 4 digits look like a plausible year
                if (suffix >= 1900 && suffix <= 2099)
                {
                    var prefix = seasonNumber / 10000;
                    // Only strip if the remaining prefix is a reasonable season number (1-99)
                    if (prefix > 0 && prefix < 100)
                    {
                        return (int)prefix;
                    }
                }
            }

            return seasonNumber;
        }

        /// <summary>
        /// Extracts the season number from the second half of the Season folder name (everything after "Season", or "Staffel").
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static (int? SeasonNumber, bool IsSeasonFolder) GetSeasonNumberFromPathSubstring(ReadOnlySpan<char> path)
        {
            var numericStart = -1;
            var length = 0;

            var hasOpenParenthesis = false;
            var isSeasonFolder = true;

            // Find out where the numbers start, and then keep going until they end
            for (var i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path[i]))
                {
                    if (!hasOpenParenthesis)
                    {
                        if (numericStart == -1)
                        {
                            numericStart = i;
                        }

                        length++;
                    }
                }
                else if (numericStart != -1)
                {
                    // There's other stuff after the season number, e.g. episode number
                    isSeasonFolder = false;
                    break;
                }

                var currentChar = path[i];
                if (currentChar == '(')
                {
                    hasOpenParenthesis = true;
                }
                else if (currentChar == ')')
                {
                    hasOpenParenthesis = false;
                }
            }

            if (numericStart == -1)
            {
                return (null, isSeasonFolder);
            }

            return (int.Parse(path.Slice(numericStart, length), provider: CultureInfo.InvariantCulture), isSeasonFolder);
        }
    }
}
