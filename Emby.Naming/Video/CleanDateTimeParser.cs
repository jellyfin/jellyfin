#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Globalization;
using System.Linq;
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
            => _options.CleanDateTimeRegexes.Select(i => Clean(name, i))
                .FirstOrDefault(i => i.HasChanged) ??
                new CleanDateTimeResult { Name = name };

        private static CleanDateTimeResult Clean(string name, Regex expression)
        {
            var result = new CleanDateTimeResult();

            var match = expression.Match(name);

            if (match.Success
                && match.Groups.Count == 5
                && match.Groups[1].Success
                && match.Groups[2].Success
                && int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
            {
                name = match.Groups[1].Value.TrimEnd();
                result.Year = year;
                result.HasChanged = true;
            }

            result.Name = name;
            return result;
        }
    }
}
