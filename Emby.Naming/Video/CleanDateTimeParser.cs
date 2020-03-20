#pragma warning disable CS1591
#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Emby.Naming.Video
{
    /// <summary>
    /// <see href="http://kodi.wiki/view/Advancedsettings.xml#video" />.
    /// </summary>
    public static class CleanDateTimeParser
    {
        public static CleanDateTimeResult Clean(string name, IReadOnlyList<Regex> cleanDateTimeRegexes)
        {
            CleanDateTimeResult result = new CleanDateTimeResult(name);
            var len = cleanDateTimeRegexes.Count;
            for (int i = 0; i < len; i++)
            {
                if (TryClean(name, cleanDateTimeRegexes[i], ref result))
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
