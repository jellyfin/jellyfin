using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SrtWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            var writer = new StreamWriter(stream);

            try
            {
                var index = 1;

                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteLine(index.ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff}", TimeSpan.FromTicks(trackEvent.StartPositionTicks), TimeSpan.FromTicks(trackEvent.EndPositionTicks));

                    var text = trackEvent.Text;

                    // TODO: Not sure how to handle these
                    text = Regex.Replace(text, @"\\N", " ", RegexOptions.IgnoreCase);

                    writer.WriteLine(text);
                    writer.WriteLine(string.Empty);

                    index++;
                }
            }
            catch
            {
                writer.Dispose();

                throw;
            }
        }
    }
}
