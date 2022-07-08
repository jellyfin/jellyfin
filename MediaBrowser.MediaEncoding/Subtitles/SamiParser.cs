using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Synchronized Accessible Media Interchange subtitle parser.
    /// </summary>
    public class SamiParser : SubtitleEditParser<Sami>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamiParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SamiParser(ILogger logger) : base(logger)
        {
        }
    }
}
