using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Subtitle writer for the WebVTT format.
    /// </summary>
    public partial class VttWriter : ISubtitleWriter
    {
        /// <summary>
        /// Matches ASS alignment tags (e.g. {\an8}) introduced by libse during VTT parsing.
        /// These are stripped from the text since position is already preserved in VttCueSettings.
        /// </summary>
        [GeneratedRegex(@"^\{\\an\d\}")]
        private static partial Regex AssAlignmentTagRegex();

        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine();
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = TimeSpan.FromTicks(trackEvent.StartPositionTicks);
                    var endTime = TimeSpan.FromTicks(trackEvent.EndPositionTicks);

                    if (endTime.TotalMilliseconds <= startTime.TotalMilliseconds)
                    {
                        endTime = startTime.Add(TimeSpan.FromMilliseconds(1));
                    }

                    // Use cue settings from the original VTT file if available, otherwise default to bottom center.
                    var cueSettings = string.IsNullOrEmpty(trackEvent.VttCueSettings)
                        ? "line:90%,end"
                        : trackEvent.VttCueSettings;

                    var text = trackEvent.Text;

                    // Strip {\anN} tags introduced by libse during VTT parsing only when cue settings
                    // are present — position is already preserved in VttCueSettings.
                    if (!string.IsNullOrEmpty(trackEvent.VttCueSettings))
                    {
                        text = AssAlignmentTagRegex().Replace(text, string.Empty);
                    }

                    // Preserve existing newlines as-is - the SubtitleEdit parser already handled them correctly
                    // No newline transformation is needed here

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff} {2}", startTime, endTime, cueSettings);
                    writer.WriteLine(text);
                    writer.WriteLine();
                }
            }
        }
    }
}
