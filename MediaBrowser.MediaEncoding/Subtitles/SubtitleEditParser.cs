#nullable enable

using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.MediaInfo;
using Nikse.SubtitleEdit.Core;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// SubStation Alpha subtitle parser.
    /// </summary>
    /// <typeparam name="T">The <see cref="Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat" />.</typeparam>
    public abstract class SubtitleEditParser<T> : ISubtitleParser
        where T : Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat, new()
    {
        /// <inheritdoc />
        public SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken)
        {
            var subtitle = new Subtitle();
            var subRip = new T();
            var lines = stream.ReadAllLines().ToList();
            subRip.LoadSubtitle(subtitle, lines, "untitled");

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
