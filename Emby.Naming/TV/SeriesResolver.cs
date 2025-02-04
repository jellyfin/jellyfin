using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Used to resolve information about series from path.
    /// </summary>
    public static partial class SeriesResolver
    {
        /// <summary>
        /// Regex that matches strings of at least 2 characters separated by a dot or underscore.
        /// Used for removing separators between words, i.e turns "The_show" into "The show" while
        /// preserving names like "S.H.O.W".
        /// </summary>
        [GeneratedRegex(@"((?<a>[^\._]{2,})[\._]*)|([\._](?<b>[^\._]{2,}))")]
        private static partial Regex SeriesNameRegex();

        /// <summary>
        /// Resolve information about series from path.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object passed to <see cref="SeriesPathParser"/>.</param>
        /// <param name="path">Path to series.</param>
        /// <returns>SeriesInfo.</returns>
        public static SeriesInfo Resolve(NamingOptions options, string path)
        {
            string seriesName = Path.GetFileName(path);

            SeriesPathParserResult result = SeriesPathParser.Parse(options, path);
            if (result.Success)
            {
                if (!string.IsNullOrEmpty(result.SeriesName))
                {
                    seriesName = result.SeriesName;
                }
            }

            if (!string.IsNullOrEmpty(seriesName))
            {
                seriesName = SeriesNameRegex().Replace(seriesName, "${a} ${b}").Trim();
            }

            return new SeriesInfo(path)
            {
                Name = seriesName
            };
        }
    }
}
