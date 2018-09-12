using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Emby.Naming.TV;

namespace Emby.Naming.AudioBook
{
    public class AudioBookFilePathParser
    {
        private readonly NamingOptions _options;

        public AudioBookFilePathParser(NamingOptions options)
        {
            _options = options;
        }

        public AudioBookFilePathParserResult Parse(string path, bool IsDirectory)
        {
            AudioBookFilePathParserResult result = Parse(path);
            return !result.Success ? new AudioBookFilePathParserResult() : result;
        }

        private AudioBookFilePathParserResult Parse(string path)
        {
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
                            int intValue;
                            if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
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
                            int intValue;
                            if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
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
