#pragma warning disable CS1591
#pragma warning disable SA1600
#nullable enable

using System.Globalization;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// <see href="http://kodi.wiki/view/Advancedsettings.xml#video" />.
    /// </summary>
    public class CleanDateTimeParser
    {
        private readonly NamingOptions _options;

        public CleanDateTimeParser(NamingOptions options)
        {
            _options = options;
        }

        public CleanDateTimeResult Clean(string name)
        {
            var regexes = _options.CleanDateTimeRegexes;
            var len = regexes.Length;

            CleanDateTimeResult result = new CleanDateTimeResult(name);
            for (int i = 0; i < len; i++)
            {
                if (TryClean(name, regexes[i], ref result))
                {
                    return result;
                }
            }

            return result;
        }

        private static bool TryClean(string name, Regex expression, ref CleanDateTimeResult result)
        {
            var match = expression.Match(name);

            if (match.Success
                && match.Groups.Count == 5
                && match.Groups[1].Success
                && match.Groups[2].Success
                && int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
            {
                result = new CleanDateTimeResult(match.Groups[1].Value.TrimEnd(), year);
                return true;
            }

            return false;
        }
    }
}
