using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class VttWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine(string.Empty);
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    TimeSpan startTime = TimeSpan.FromTicks(trackEvent.StartPositionTicks);
                    TimeSpan endTime = TimeSpan.FromTicks(trackEvent.EndPositionTicks);

                    // make sure the start and end times are different and seqential
                    if (endTime.TotalMilliseconds <= startTime.TotalMilliseconds)
                    {
                        endTime = startTime.Add(TimeSpan.FromMilliseconds(1));
                    }

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff}", startTime, endTime);

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
