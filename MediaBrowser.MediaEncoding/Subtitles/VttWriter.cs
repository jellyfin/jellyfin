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
    public class VttWriter : ISubtitleWriter
    {
        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine(string.Empty);
                writer.WriteLine("REGION");
                writer.WriteLine("id:subtitle");
                writer.WriteLine("width:80%");
                writer.WriteLine("lines:3");
                writer.WriteLine("regionanchor:50%,100%");
                writer.WriteLine("viewportanchor:50%,90%");
                writer.WriteLine(string.Empty);
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

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff} region:subtitle", startTime, endTime);

                    var text = trackEvent.Text;

                    // TODO: Not sure how to handle these
                    text = Regex.Replace(text, @"\\n", " ", RegexOptions.IgnoreCase);

                    writer.WriteLine(text);
                    writer.WriteLine(string.Empty);
                }
            }
        }
    }
}
