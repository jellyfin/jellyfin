using System.IO;
using System.IO.Compression;
using MediaBrowser.Common.Json;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class JsonHandler : Response
    {
        public JsonHandler(RequestContext ctx)
            : base(ctx)
        {
            Headers["Content-Encoding"] = "gzip";

            WriteStream = s =>
            {
                WriteReponse(s);
                s.Close();
            };
        }

        public override string ContentType
        {
            get { return "application/json"; }
        }

        protected abstract object ObjectToSerialize { get; }

        private void WriteReponse(Stream stream)
        {
            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Compress, false))
            {
                JsonSerializer.SerializeToStream(ObjectToSerialize, gzipStream);
            }
        }
    }
}
