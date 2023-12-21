using System.Globalization;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Helper class to retrieve name and year from audiobook previously retrieved name.
    /// </summary>
    public class AudioBookNameParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookNameParser"/> class.
        /// </summary>
        /// <param name="options">Naming options containing AudioBookNamesExpressions.</param>
        public AudioBookNameParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Parse name and year from previously determined name of audiobook.
        /// </summary>
        /// <param name="name">Name of the audiobook.</param>
        /// <returns>Returns <see cref="AudioBookNameParserResult"/> object.</returns>
        public AudioBookNameParserResult Parse(string name)
        {
            AudioBookNameParserResult result = default;
            foreach (var expression in _options.AudioBookNamesExpressions)
            {
                var match = Regex.Match(name, expression, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (result.Name is null)
                    {
                        var value = match.Groups["name"];
                        if (value.Success)
                        {
                            result.Name = value.Value;
                        }
                    }

                    if (!result.Year.HasValue)
                    {
                        var value = match.Groups["year"];
                        if (value.Success)
                        {
                            if (int.TryParse(value.ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            {
                                result.Year = intValue;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(result.Name))
            {
                result.Name = name;
            }

            return result;
        }
    }
}
