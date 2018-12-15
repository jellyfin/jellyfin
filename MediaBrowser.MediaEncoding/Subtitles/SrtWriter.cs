using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SrtWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                var index = 1;

                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteLine(index.ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine(@"{0:hh\:mm\:ss\,fff} --> {1:hh\:mm\:ss\,fff}", TimeSpan.FromTicks(trackEvent.StartPositionTicks), TimeSpan.FromTicks(trackEvent.EndPositionTicks));

                    var text = trackEvent.Text;

                    // TODO: Not sure how to handle these
                    text = Regex.Replace(text, @"\\n", " ", RegexOptions.IgnoreCase);

                    writer.WriteLine(text);
                    writer.WriteLine(string.Empty);

                    index++;
                }
            }
        }
    }
}
