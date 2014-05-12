using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class VttWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream) {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine(string.Empty);
                foreach (var trackEvent in info.TrackEvents)
                {
                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff}", TimeSpan.FromTicks(trackEvent.StartPositionTicks), TimeSpan.FromTicks(trackEvent.EndPositionTicks));
                    writer.WriteLine(trackEvent.Text);
                    writer.WriteLine(string.Empty);
                }
            }
        }
    }
}
