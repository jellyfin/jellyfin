using System;
using System.Globalization;
using System.IO;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Class to parse season paths.
    /// </summary>
    public static class SeasonPathParser
    {
        /// <summary>
        /// A season folder must contain one of these somewhere in the name.
        /// </summary>
        private static readonly string[] _seasonFolderNames =
        {
            "season",
            "sæson",
            "temporada",
            "saison",
            "staffel",
            "series",
            "сезон",
            "stagione"
        };

        private static readonly char[] _splitChars = ['.', '_', ' ', '-'];

        /// <summary>
        /// Attempts to parse season number from path.
        /// </summary>
        /// <param name="path">Path to season.</param>
        /// <param name="supportSpecialAliases">Support special aliases when parsing.</param>
        /// <param name="supportNumericSeasonFolders">Support numeric season folders when parsing.</param>
        /// <returns>Returns <see cref="SeasonPathParserResult"/> object.</returns>
        public static SeasonPathParserResult Parse(string path, bool supportSpecialAliases, bool supportNumericSeasonFolders)
        {
            var result = new SeasonPathParserResult();

            var (seasonNumber, isSeasonFolder) = GetSeasonNumberFromPath(path, supportSpecialAliases, supportNumericSeasonFolders);

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
        /// <param name="supportSpecialAliases">if set to <c>true</c> [support special aliases].</param>
        /// <param name="supportNumericSeasonFolders">if set to <c>true</c> [support numeric season folders].</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static (int? SeasonNumber, bool IsSeasonFolder) GetSeasonNumberFromPath(
            string path,
            bool supportSpecialAliases,
            bool supportNumericSeasonFolders)
        {
            string filename = Path.GetFileName(path);

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

            if (TryGetSeasonNumberFromPart(filename, out int seasonNumber))
            {
                return (seasonNumber, true);
            }

            // Look for one of the season folder names
            foreach (var name in _seasonFolderNames)
            {
                if (filename.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    var result = GetSeasonNumberFromPathSubstring(filename.Replace(name, " ", StringComparison.OrdinalIgnoreCase));
                    if (result.SeasonNumber.HasValue)
                    {
                        return result;
                    }

                    break;
                }
            }

            var parts = filename.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (TryGetSeasonNumberFromPart(part, out seasonNumber))
                {
                    return (seasonNumber, true);
                }
            }

            return (null, true);
        }

        private static bool TryGetSeasonNumberFromPart(ReadOnlySpan<char> part, out int seasonNumber)
        {
            seasonNumber = 0;
            if (part.Length < 2 || !part.StartsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (int.TryParse(part.Slice(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                seasonNumber = value;
                return true;
            }

            return false;
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
