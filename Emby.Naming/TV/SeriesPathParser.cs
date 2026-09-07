using Emby.Naming.Common;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Used to parse information about series from paths containing more information that only the series name.
    /// Uses the same regular expressions as the EpisodePathParser but have different success criteria.
    /// </summary>
    public static class SeriesPathParser
    {
        /// <summary>
        /// Parses information about series from path.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing EpisodeExpressions and MultipleEpisodeExpressions.</param>
        /// <param name="path">Path.</param>
        /// <returns>Returns <see cref="SeriesPathParserResult"/> object.</returns>
        public static SeriesPathParserResult Parse(NamingOptions options, string path)
        {
            SeriesPathParserResult? result = null;

            foreach (var expression in options.EpisodeExpressions)
            {
                // Optimistic expressions (bare numbers, "01.blah", etc.) are only meant for
                // episode parsing and produce false series names on release folder names like
                // "Silo.S03.1080p.WEB-DL..." (e.g. reading "264" as S02E64). Skip them here.
                if (expression.IsOptimistic)
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

            if (result is not null)
            {
                if (!string.IsNullOrEmpty(result.SeriesName))
                {
                    result.SeriesName = result.SeriesName.Trim(' ', '_', '.', '-');
                }
            }

            return result ?? new SeriesPathParserResult();
        }

        private static SeriesPathParserResult Parse(string name, EpisodeExpression expression)
        {
            var result = new SeriesPathParserResult();

            var match = expression.Regex.Match(name);

            if (match.Success && match.Groups.Count >= 3)
            {
                if (expression.IsNamed)
                {
                    result.SeriesName = match.Groups["seriesname"].Value;
                    result.Success = !string.IsNullOrEmpty(result.SeriesName) && !match.Groups["seasonnumber"].ValueSpan.IsEmpty;
                }
            }

            return result;
        }
    }
}
