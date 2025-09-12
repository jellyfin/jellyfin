using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Used to parse information about episode from path.
    /// </summary>
    public class EpisodePathParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodePathParser"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing EpisodeExpressions and MultipleEpisodeExpressions.</param>
        public EpisodePathParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Parses information about episode from path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="isDirectory">Is path for a directory or file.</param>
        /// <param name="isNamed">Do we want to use IsNamed expressions.</param>
        /// <param name="isOptimistic">Do we want to use Optimistic expressions.</param>
        /// <param name="supportsAbsoluteNumbers">Do we want to use expressions supporting absolute episode numbers.</param>
        /// <param name="fillExtendedInfo">Should we attempt to retrieve extended information.</param>
        /// <returns>Returns <see cref="EpisodePathParserResult"/> object.</returns>
        public EpisodePathParserResult Parse(
            string path,
            bool isDirectory,
            bool? isNamed = null,
            bool? isOptimistic = null,
            bool? supportsAbsoluteNumbers = null,
            bool fillExtendedInfo = true)
        {
            // Added to be able to use regex patterns which require a file extension.
            // There were no failed tests without this block, but to be safe, we can keep it until
            // the regex which require file extensions are modified so that they don't need them.
            if (isDirectory)
            {
                path += ".mp4";
            }

            EpisodePathParserResult? result = null;

            foreach (var expression in _options.EpisodeExpressions)
            {
                if (supportsAbsoluteNumbers.HasValue
                    && expression.SupportsAbsoluteEpisodeNumbers != supportsAbsoluteNumbers.Value)
                {
                    continue;
                }

                if (isNamed.HasValue && expression.IsNamed != isNamed.Value)
                {
                    continue;
                }

                if (isOptimistic.HasValue && expression.IsOptimistic != isOptimistic.Value)
                {
                    continue;
                }

                var currentResult = Parse(path, expression);
                if (currentResult.Success)
                {
                    result = currentResult;
                    break;
                }
            }

            if (result is not null && fillExtendedInfo)
            {
                FillAdditional(path, result);

                if (!string.IsNullOrEmpty(result.SeriesName))
                {
                    result.SeriesName = result.SeriesName
                        .Trim()
                        .Trim('_', '.', '-')
                        .Trim();
                }
            }

            return result ?? new EpisodePathParserResult();
        }

        private static EpisodePathParserResult Parse(string name, EpisodeExpression expression)
        {
            var result = new EpisodePathParserResult();

            // This is a hack to handle wmc naming
            if (expression.IsByDate)
            {
                name = name.Replace('_', '-');
            }

            var match = expression.Regex.Match(name);

            // (Full)(Season)(Episode)(Extension)
            if (match.Success && match.Groups.Count >= 3)
            {
                if (expression.IsByDate)
                {
                    DateTime date;
                    if (expression.DateTimeFormats.Count > 0)
                    {
                        if (DateTime.TryParseExact(
                            match.Groups[0].ValueSpan,
                            expression.DateTimeFormats.ToArray(),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out date))
                        {
                            result.Year = date.Year;
                            result.Month = date.Month;
                            result.Day = date.Day;
                            result.Success = true;
                        }
                    }
                    else if (DateTime.TryParse(match.Groups[0].ValueSpan, out date))
                    {
                        result.Year = date.Year;
                        result.Month = date.Month;
                        result.Day = date.Day;
                        result.Success = true;
                    }

                    // TODO: Only consider success if date successfully parsed?
                    result.Success = true;
                }
                else if (expression.IsNamed)
                {
                    if (int.TryParse(match.Groups["seasonnumber"].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                    {
                        result.SeasonNumber = num;
                    }

                    if (int.TryParse(match.Groups["epnumber"].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                    {
                        result.EpisodeNumber = num;
                    }

                    var endingNumberGroup = match.Groups["endingepnumber"];
                    if (endingNumberGroup.Success)
                    {
                        // Will only set EndingEpisodeNumber if the captured number is not followed by additional numbers
                        // or a 'p' or 'i' as what you would get with a pixel resolution specification.
                        // It avoids erroneous parsing of something like "series-s09e14-1080p.mkv" as a multi-episode from E14 to E108
                        int nextIndex = endingNumberGroup.Index + endingNumberGroup.Length;
                        if (nextIndex >= name.Length
                            || !"0123456789iIpP".Contains(name[nextIndex], StringComparison.Ordinal))
                        {
                            if (int.TryParse(endingNumberGroup.ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                            {
                                result.EndingEpisodeNumber = num;
                            }
                        }
                    }

                    result.SeriesName = match.Groups["seriesname"].Value;
                    result.Success = result.EpisodeNumber.HasValue;
                }
                else
                {
                    if (int.TryParse(match.Groups[1].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                    {
                        result.SeasonNumber = num;
                    }

                    if (int.TryParse(match.Groups[2].ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                    {
                        result.EpisodeNumber = num;
                    }

                    result.Success = result.EpisodeNumber.HasValue;
                }

                // Invalidate match when the season is 200 through 1927 or above 2500
                // because it is an error unless the TV show is intentionally using false season numbers.
                // It avoids erroneous parsing of something like "Series Special (1920x1080).mkv" as being season 1920 episode 1080.
                if ((result.SeasonNumber >= 200 && result.SeasonNumber < 1928)
                    || result.SeasonNumber > 2500)
                {
                    result.Success = false;
                }

                result.IsByDate = expression.IsByDate;
            }

            return result;
        }

        private void FillAdditional(string path, EpisodePathParserResult info)
        {
            var expressions = _options.MultipleEpisodeExpressions.Where(i => i.IsNamed).ToList();

            if (string.IsNullOrEmpty(info.SeriesName))
            {
                expressions.InsertRange(0, _options.EpisodeExpressions.Where(i => i.IsNamed));
            }

            FillAdditional(path, info, expressions);
        }

        private void FillAdditional(string path, EpisodePathParserResult info, IEnumerable<EpisodeExpression> expressions)
        {
            foreach (var i in expressions)
            {
                var result = Parse(path, i);

                if (!result.Success)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(info.SeriesName))
                {
                    info.SeriesName = result.SeriesName;
                }

                if (!info.EndingEpisodeNumber.HasValue && info.EpisodeNumber.HasValue)
                {
                    info.EndingEpisodeNumber = result.EndingEpisodeNumber;
                }

                if (!string.IsNullOrEmpty(info.SeriesName)
                    && (!info.EpisodeNumber.HasValue || info.EndingEpisodeNumber.HasValue))
                {
                    break;
                }
            }
        }
    }
}
