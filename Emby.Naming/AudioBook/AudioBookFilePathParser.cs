#nullable enable
#pragma warning disable CS1591

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.AudioBook
{
    public class AudioBookFilePathParser
    {
        private readonly NamingOptions _options;

        public AudioBookFilePathParser(NamingOptions options)
        {
            _options = options;
        }

        public AudioBookFilePathParserResult Parse(string path)
        {
            AudioBookFilePathParserResult result = default;
            var fileName = Path.GetFileNameWithoutExtension(path);
            foreach (var expression in _options.AudioBookPartsExpressions)
            {
                var match = new Regex(expression, RegexOptions.IgnoreCase).Match(fileName);
                if (match.Success)
                {
                    if (!result.ChapterNumber.HasValue)
                    {
                        var value = match.Groups["chapter"];
                        if (value.Success)
                        {
                            if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            {
                                result.ChapterNumber = intValue;
                            }
                        }
                    }

                    if (!result.PartNumber.HasValue)
                    {
                        var value = match.Groups["part"];
                        if (value.Success)
                        {
                            if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            {
                                result.PartNumber = intValue;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
