using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// SubRip subtitle parser.
    /// </summary>
    public class SrtParser : SubtitleEditParser<SubRip>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SrtParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SrtParser(ILogger logger) : base(logger)
        {
        }
    }
}
