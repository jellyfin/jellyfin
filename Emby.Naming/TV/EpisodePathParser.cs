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
            return EpisodePathParserRustInterop.Parse(
                _options,
                path,
                isDirectory,
                isNamed,
                isOptimistic,
                supportsAbsoluteNumbers,
                fillExtendedInfo);
        }
    }
}
