using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// ASS subtitle writer.
    /// </summary>
    public partial class AssWriter : ISubtitleWriter
    {
        [GeneratedRegex(@"\n", RegexOptions.IgnoreCase)]
        private static partial Regex NewLineRegex();

        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                var trackEvents = info.TrackEvents;
                var timeFormat = @"hh\:mm\:ss\.ff";

                // Write ASS header
                writer.WriteLine("[Script Info]");
                writer.WriteLine("Title: Jellyfin transcoded ASS subtitle");
                writer.WriteLine("ScriptType: v4.00+");
                writer.WriteLine();
                writer.WriteLine("[V4+ Styles]");
                writer.WriteLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
                writer.WriteLine("Style: Default,Arial,20,&H00FFFFFF,&H00FFFFFF,&H19333333,&H910E0807,0,0,0,0,100,100,0,0,0,1,0,2,10,10,10,1");
                writer.WriteLine();
                writer.WriteLine("[Events]");
                writer.WriteLine("Format: Layer, Start, End, Style, Text");

                for (int i = 0; i < trackEvents.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var trackEvent = trackEvents[i];
                    var startTime = TimeSpan.FromTicks(trackEvent.StartPositionTicks).ToString(timeFormat, CultureInfo.InvariantCulture);
                    var endTime = TimeSpan.FromTicks(trackEvent.EndPositionTicks).ToString(timeFormat, CultureInfo.InvariantCulture);
                    var text = NewLineRegex().Replace(trackEvent.Text, "\\n");

                    writer.WriteLine(
                        "Dialogue: 0,{0},{1},Default,{2}",
                        startTime,
                        endTime,
                        text);
                }
            }
        }
    }
}
