using System;
using System.IO;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class VttWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            var writer = new StreamWriter(stream);

            try
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine(string.Empty);
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff}", TimeSpan.FromTicks(trackEvent.StartPositionTicks), TimeSpan.FromTicks(trackEvent.EndPositionTicks));
                    writer.WriteLine(trackEvent.Text.Replace("<br />", "\r\n"));
                    writer.WriteLine(string.Empty);
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
