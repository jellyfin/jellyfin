using System.IO;
using System.Text.Json;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// JSON subtitle writer.
    /// </summary>
    public class JsonWriter : ISubtitleWriter
    {
        /// <inheritdoc />
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new Utf8JsonWriter(stream))
            {
                var trackevents = info.TrackEvents;
                writer.WriteStartObject();
                writer.WriteStartArray("TrackEvents");

                for (int i = 0; i < trackevents.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var current = trackevents[i];
                    writer.WriteStartObject();

                    writer.WriteString("Id", current.Id);
                    writer.WriteString("Text", current.Text);
                    writer.WriteNumber("StartPositionTicks", current.StartPositionTicks);
                    writer.WriteNumber("EndPositionTicks", current.EndPositionTicks);

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();

                writer.Flush();
            }
        }
    }
}
