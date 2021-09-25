using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Extensions;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using SubtitleFormat = Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// SubStation Alpha subtitle parser.
    /// </summary>
    /// <typeparam name="T">The <see cref="SubtitleFormat" />.</typeparam>
    public abstract class SubtitleEditParser<T> : ISubtitleParser
        where T : SubtitleFormat, new()
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleEditParser{T}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        protected SubtitleEditParser(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var subtitle = new Subtitle();
            var subRip = new T();
            var lines = stream.ReadAllLines().ToList();
            subRip.LoadSubtitle(subtitle, lines, "untitled");
            if (subRip.ErrorCount > 0)
            {
                _logger.LogError("{ErrorCount} errors encountered while parsing subtitle", subRip.ErrorCount);
            }

            var trackInfo = new SubtitleTrackInfo();
            int len = subtitle.Paragraphs.Count;
            var trackEvents = new SubtitleTrackEvent[len];
            for (int i = 0; i < len; i++)
            {
                var p = subtitle.Paragraphs[i];
                trackEvents[i] = new SubtitleTrackEvent(p.Number.ToString(CultureInfo.InvariantCulture), p.Text)
                {
                    StartPositionTicks = p.StartTime.TimeSpan.Ticks,
                    EndPositionTicks = p.EndTime.TimeSpan.Ticks
                };
            }

            trackInfo.TrackEvents = trackEvents;
            return trackInfo;
        }
    }
}
