using System.IO;
using System.Text;
using System.Threading;
using Jellyfin.Model.MediaInfo;
using Jellyfin.Model.Serialization;

namespace Jellyfin.MediaEncoding.Subtitles
{
    public class JsonWriter : ISubtitleWriter
    {
        private readonly IJsonSerializer _json;

        public JsonWriter(IJsonSerializer json)
        {
            _json = json;
        }

        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                var json = _json.SerializeToString(info);

                writer.Write(json);
            }
        }
    }
}
