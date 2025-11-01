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
        [GeneratedRegex(@"^\s*((?<seasonnumber>(?>\d+))(?:st|nd|rd|th|\.)*(?!\s*[Ee]\d+))\s*(?:[[시즌]*|[シーズン]*|[sS](?:eason|æson|aison|taffel|eries|tagione|äsong|eizoen|easong|ezon|ezona|ezóna|ezonul)*|[tT](?:emporada)*|[kK](?:ausi)*|[Сс](?:езон)*)\s*(?<rightpart>.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex ProcessPre();

        [GeneratedRegex(@"^\s*(?:[[시즌]*|[シーズン]*|[sS](?:eason|æson|aison|taffel|eries|tagione|äsong|eizoen|easong|ezon|ezona|ezóna|ezonul)*|[tT](?:emporada)*|[kK](?:ausi)*|[Сс](?:езон)*)\s*(?<seasonnumber>(?>\d+)(?!\s*[Ee]\d+))(?<rightpart>.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex ProcessPost();

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
            string filename = Path.GetFileName(path);
            filename = Regex.Replace(filename, "[ ._-]", string.Empty);

            if (parentFolderName is not null)
            {
                parentFolderName = Regex.Replace(parentFolderName, "[ ._-]", string.Empty);
                filename = filename.Replace(parentFolderName, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            if (supportSpecialAliases)
            {
                if (string.Equals(filename, "specials", StringComparison.OrdinalIgnoreCase))
                {
                    return (0, true);
                }

                if (string.Equals(filename, "extras", StringComparison.OrdinalIgnoreCase))
                {
                    return (0, true);
                }
            }

            if (supportNumericSeasonFolders)
            {
                if (int.TryParse(filename, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                {
                    return (val, true);
                }
            }

            if (filename.Length > 0 && (filename[0] == 'S' || filename[0] == 's'))
            {
                var testFilename = filename.AsSpan()[1..];

                if (int.TryParse(testFilename, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                {
                    return (val, true);
                }
            }

            var preMatch = ProcessPre().Match(filename);
            if (preMatch.Success)
            {
                return CheckMatch(preMatch);
            }
            else
            {
                var postMatch = ProcessPost().Match(filename);
                return CheckMatch(postMatch);
            }
        }

        private static (int? SeasonNumber, bool IsSeasonFolder) CheckMatch(Match match)
        {
            var numberString = match.Groups["seasonnumber"];
            if (numberString.Success)
            {
                if (int.TryParse(numberString.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seasonNumber))
                {
                    return (seasonNumber, true);
                }
            }

            return (null, false);
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
