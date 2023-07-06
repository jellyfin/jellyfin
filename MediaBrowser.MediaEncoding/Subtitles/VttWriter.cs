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
        [GeneratedRegex(@"\\n", RegexOptions.IgnoreCase)]
        private static partial Regex NewlineEscapeRegex();

        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine();
                writer.WriteLine("Region: id:subtitle width:80% lines:3 regionanchor:50%,100% viewportanchor:50%,90%");
                writer.WriteLine();
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = TimeSpan.FromTicks(trackEvent.StartPositionTicks);
                    var endTime = TimeSpan.FromTicks(trackEvent.EndPositionTicks);

                    // make sure the start and end times are different and sequential
                    if (endTime.TotalMilliseconds <= startTime.TotalMilliseconds)
                    {
                        endTime = startTime.Add(TimeSpan.FromMilliseconds(1));
                    }

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff} region:subtitle line:90%", startTime, endTime);

                    var text = trackEvent.Text;

                    // TODO: Not sure how to handle these
                    text = NewlineEscapeRegex().Replace(text, " ");

                    writer.WriteLine(text);
                    writer.WriteLine();
                }
            }
        }
    }
}
