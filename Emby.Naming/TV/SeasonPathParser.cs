using Emby.Naming.Common;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Emby.Naming.TV
{
    public class SeasonPathParser
    {
        private readonly NamingOptions _options;

        public SeasonPathParser(NamingOptions options)
        {
            _options = options;
        }

        public SeasonPathParserResult Parse(string path, bool supportSpecialAliases, bool supportNumericSeasonFolders)
        {
            var result = new SeasonPathParserResult();

            var seasonNumberInfo = GetSeasonNumberFromPath(path, supportSpecialAliases, supportNumericSeasonFolders);

            result.SeasonNumber = seasonNumberInfo.Item1;

            if (result.SeasonNumber.HasValue)
            {
                result.Success = true;
                result.IsSeasonFolder = seasonNumberInfo.Item2;
            }

            return result;
        }

        /// <summary>
        /// A season folder must contain one of these somewhere in the name
        /// </summary>
        private static readonly string[] SeasonFolderNames =
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
        private Tuple<int?, bool> GetSeasonNumberFromPath(string path, bool supportSpecialAliases, bool supportNumericSeasonFolders)
        {
            var filename = Path.GetFileName(path);

            if (supportSpecialAliases)
            {
                if (string.Equals(filename, "specials", StringComparison.OrdinalIgnoreCase))
                {
                    return new Tuple<int?, bool>(0, true);
                }
                if (string.Equals(filename, "extras", StringComparison.OrdinalIgnoreCase))
                {
                    return new Tuple<int?, bool>(0, true);
                }
            }

            if (supportNumericSeasonFolders)
            {
                int val;
                if (int.TryParse(filename, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                {
                    return new Tuple<int?, bool>(val, true);
                }
            }

            if (filename.StartsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                var testFilename = filename.Substring(1);

                int val;
                if (int.TryParse(testFilename, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                {
                    return new Tuple<int?, bool>(val, true);
                }
            }

            // Look for one of the season folder names
            foreach (var name in SeasonFolderNames)
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
            return new Tuple<int?, bool>(resultNumber, true);
        }

        private int? GetSeasonNumberFromPart(string part)
        {
            if (part.Length < 2 || !part.StartsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            part = part.Substring(1);

            int value;
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
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
        private Tuple<int?, bool> GetSeasonNumberFromPathSubstring(string path)
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
                return new Tuple<int?, bool>(null, isSeasonFolder);
            }

            return new Tuple<int?, bool>(int.Parse(path.Substring(numericStart, length), CultureInfo.InvariantCulture), isSeasonFolder);
        }
    }
}
