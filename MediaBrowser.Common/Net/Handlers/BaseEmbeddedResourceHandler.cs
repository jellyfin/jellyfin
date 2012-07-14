using System.IO;
using System.IO.Compression;
using System;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseEmbeddedResourceHandler : Response
    {
        public BaseEmbeddedResourceHandler(RequestContext ctx, string resourcePath)
            : base(ctx)
        {
            ResourcePath = resourcePath;

            Headers["Content-Encoding"] = "gzip";

            WriteStream = s =>
            {
                WriteReponse(s);
                s.Close();
            };
        }

        protected string ResourcePath { get; set; }

        public override string ContentType
        {
            get
            {
                string extension = Path.GetExtension(ResourcePath);

                if (extension.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) || extension.EndsWith("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    return "image/jpeg";
                }
                else if (extension.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                {
                    return "image/png";
                }
                else if (extension.EndsWith("ico", StringComparison.OrdinalIgnoreCase))
                {
                    return "image/ico";
                }
                else if (extension.EndsWith("js", StringComparison.OrdinalIgnoreCase))
                {
                    return "application/x-javascript";
                }
                else if (extension.EndsWith("css", StringComparison.OrdinalIgnoreCase))
                {
                    return "text/css";
                }
                else if (extension.EndsWith("html", StringComparison.OrdinalIgnoreCase))
                {
                    return "text/html; charset=utf-8";
                }

                return "text/plain; charset=utf-8";
            }
        }

        private void WriteReponse(Stream stream)
        {
            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Compress, false))
            {
                GetEmbeddedResourceStream().CopyTo(gzipStream);
            }
        }

        protected abstract Stream GetEmbeddedResourceStream();
    }
}
