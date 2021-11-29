using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Advanced SubStation Alpha subtitle parser.
    /// </summary>
    public class AssParser : SubtitleEditParser<AdvancedSubStationAlpha>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public AssParser(ILogger logger) : base(logger)
        {
        }
    }
}
