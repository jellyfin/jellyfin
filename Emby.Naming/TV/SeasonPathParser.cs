#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;

namespace Emby.Naming.TV
{
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
        private static (int? seasonNumber, bool isSeasonFolder) GetSeasonNumberFromPath(
            string path,
            bool supportSpecialAliases,
            bool supportNumericSeasonFolders)
        {
            var filename = Path.GetFileName(path) ?? string.Empty;

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

            if (filename.StartsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                var testFilename = filename.Substring(1);

                if (int.TryParse(testFilename, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                {
                    return (val, true);
                }
            }

            // Look for one of the season folder names
            foreach (var name in _seasonFolderNames)
            {
                if (filename.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    var result = GetSeasonNumberFromPathSubstring(filename.Replace(name, " ", StringComparison.OrdinalIgnoreCase));
                    if (result.seasonNumber.HasValue)
                    {
                        return result;
                    }

                    break;
                }
            }

            var parts = filename.Split(new[] { '.', '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (TryGetSeasonNumberFromPart(parts[i], out int seasonNumber))
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
        private static (int? seasonNumber, bool isSeasonFolder) GetSeasonNumberFromPathSubstring(ReadOnlySpan<char> path)
        {
            var numericStart = -1;
            var length = 0;

            var hasOpenParenth = false;
            var isSeasonFolder = true;

            // Find out where the numbers start, and then keep going until they end
            for (var i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path[i]))
                {
                    if (!hasOpenParenth)
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
                    hasOpenParenth = true;
                }
                else if (currentChar == ')')
                {
                    hasOpenParenth = false;
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
