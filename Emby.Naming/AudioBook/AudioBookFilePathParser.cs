#pragma warning disable CS1591

using System;
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
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var result = new AudioBookFilePathParserResult();
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
                                result.ChapterNumber = intValue;
                            }
                        }
                    }
                }
            }

            /*var matches = _iRegexProvider.GetRegex("\\d+", RegexOptions.IgnoreCase).Matches(fileName);
            if (matches.Count > 0)
            {
                if (!result.ChapterNumber.HasValue)
                {
                    result.ChapterNumber = int.Parse(matches[0].Groups[0].Value);
                }
                if (matches.Count > 1)
                {
                    result.PartNumber = int.Parse(matches[matches.Count - 1].Groups[0].Value);
                }
            }*/
            result.Success = result.PartNumber.HasValue || result.ChapterNumber.HasValue;

            return result;
        }
    }
}
