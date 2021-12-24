using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// SubStation Alpha subtitle parser.
    /// </summary>
    public class SsaParser : SubtitleEditParser<SubStationAlpha>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SsaParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SsaParser(ILogger logger) : base(logger)
        {
        }
    }
}
