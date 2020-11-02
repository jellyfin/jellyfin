#nullable enable
#pragma warning disable CS1591

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.AudioBook
{
    public class AudioBookNameParser
    {
        private readonly NamingOptions _options;

        public AudioBookNameParser(NamingOptions options)
        {
            _options = options;
        }

        public AudioBookNameParserResult Parse(string name)
        {
            AudioBookNameParserResult result = default;
            foreach (var expression in _options.AudioBookNamesExpressions)
            {
                var match = new Regex(expression, RegexOptions.IgnoreCase).Match(name);
                if (match.Success)
                {
                    if (result.Name == null)
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
                            if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
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
