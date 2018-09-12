using System;
using Emby.Naming.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Emby.Naming.Video
{
    /// <summary>
    /// http://kodi.wiki/view/Advancedsettings.xml#video
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
            var originalName = name;

            try
            {
                var extension = Path.GetExtension(name) ?? string.Empty;
                // Check supported extensions
                if (!_options.VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) &&
                    !_options.AudioFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    // Dummy up a file extension because the expressions will fail without one
                    // This is tricky because we can't just check Path.GetExtension for empty
                    // If the input is "St. Vincent (2014)", it will produce ". Vincent (2014)" as the extension
                    name += ".mkv";
                }
            }
            catch (ArgumentException)
            {
                
            }

            var result = _options.CleanDateTimeRegexes.Select(i => Clean(name, i))
                .FirstOrDefault(i => i.HasChanged) ??
                new CleanDateTimeResult { Name = originalName };

            if (result.HasChanged)
            {
                return result;
            }

            // Make a second pass, running clean string first
            var cleanStringResult = new CleanStringParser().Clean(name, _options.CleanStringRegexes);

            if (!cleanStringResult.HasChanged)
            {
                return result;
            }

            return _options.CleanDateTimeRegexes.Select(i => Clean(cleanStringResult.Name, i))
                .FirstOrDefault(i => i.HasChanged) ??
                result;
        }

        private CleanDateTimeResult Clean(string name, Regex expression)
        {
            var result = new CleanDateTimeResult();

            var match = expression.Match(name);

            if (match.Success && match.Groups.Count == 4)
            {
                int year;
                if (match.Groups[1].Success && match.Groups[2].Success && int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out year))
                {
                    name = match.Groups[1].Value;
                    result.Year = year;
                    result.HasChanged = true;
                }
            }

            result.Name = name;
            return result;
        }
    }
}
