using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class TtmlWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            // Example: https://github.com/zmalltalker/ttml2vtt/blob/master/data/sample.xml
            // Parser example: https://github.com/mozilla/popcorn-js/blob/master/parsers/parserTTML/popcorn.parserTTML.js

            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:tts=\"http://www.w3.org/2006/04/ttaf1#styling\" lang=\"no\">");

                writer.WriteLine("<head>");
                writer.WriteLine("<styling>");
                writer.WriteLine("<style id=\"italic\" tts:fontStyle=\"italic\" />");
                writer.WriteLine("<style id=\"left\" tts:textAlign=\"left\" />");
                writer.WriteLine("<style id=\"center\" tts:textAlign=\"center\" />");
                writer.WriteLine("<style id=\"right\" tts:textAlign=\"right\" />");
                writer.WriteLine("</styling>");
                writer.WriteLine("</head>");

                writer.WriteLine("<body>");
                writer.WriteLine("<div>");

                foreach (var trackEvent in info.TrackEvents)
                {
                    var text = trackEvent.Text;

                    text = Regex.Replace(text, @"\\n", "<br/>", RegexOptions.IgnoreCase);

                    writer.WriteLine("<p begin=\"{0}\" dur=\"{1}\">{2}</p>",
                        trackEvent.StartPositionTicks,
                        (trackEvent.EndPositionTicks - trackEvent.StartPositionTicks),
                        text);
                }

                writer.WriteLine("</div>");
                writer.WriteLine("</body>");

                writer.WriteLine("</tt>");
            }
        }
    }
}
