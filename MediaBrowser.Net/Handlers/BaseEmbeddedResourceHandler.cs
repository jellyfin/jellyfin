using System;
using System.IO;

namespace MediaBrowser.Net.Handlers
{
    public abstract class BaseEmbeddedResourceHandler : BaseHandler
    {
        public BaseEmbeddedResourceHandler(string resourcePath)
            : base()
        {
            ResourcePath = resourcePath;
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

        protected override void WriteResponseToOutputStream(Stream stream)
        {
            GetEmbeddedResourceStream().CopyTo(stream);
        }

        protected abstract Stream GetEmbeddedResourceStream();
    }
}
