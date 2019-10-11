using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Emby.Naming.TV
{
    public class SeasonPathParser
    {
        public SeasonPathParserResult Parse(string path, bool supportSpecialAliases, bool supportNumericSeasonFolders)
        {
            var result = new SeasonPathParserResult();

            var seasonNumberInfo = GetSeasonNumberFromPath(path, supportSpecialAliases, supportNumericSeasonFolders);

            result.SeasonNumber = seasonNumberInfo.seasonNumber;

            if (result.SeasonNumber.HasValue)
            {
                result.Success = true;
                result.IsSeasonFolder = seasonNumberInfo.isSeasonFolder;
            }

            return result;
        }

        /// <summary>
        /// A season folder must contain one of these somewhere in the name
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
                var index = filename.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    var result = GetSeasonNumberFromPathSubstring(filename.Replace(name, " ", StringComparison.OrdinalIgnoreCase));
                    if (result.Item1.HasValue)
                    {
                        return result;
                    }

                    break;
                }
            }

            var parts = filename.Split(new[] { '.', '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var resultNumber = parts.Select(GetSeasonNumberFromPart).FirstOrDefault(i => i.HasValue);
            return (resultNumber, true);
        }

        private static int? GetSeasonNumberFromPart(string part)
        {
            if (part.Length < 2 || !part.StartsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            part = part.Substring(1);

            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Extracts the season number from the second half of the Season folder name (everything after "Season", or "Staffel")
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static (int? seasonNumber, bool isSeasonFolder) GetSeasonNumberFromPathSubstring(string path)
        {
            var numericStart = -1;
            var length = 0;

            var hasOpenParenth = false;
            var isSeasonFolder = true;

            // Find out where the numbers start, and then keep going until they end
            for (var i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path, i))
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
                if (currentChar.Equals('('))
                {
                    hasOpenParenth = true;
                }
                else if (currentChar.Equals(')'))
                {
                    hasOpenParenth = false;
                }
            }

            if (numericStart == -1)
            {
                return (null, isSeasonFolder);
            }

            return (int.Parse(path.Substring(numericStart, length), CultureInfo.InvariantCulture), isSeasonFolder);
        }
    }
}
