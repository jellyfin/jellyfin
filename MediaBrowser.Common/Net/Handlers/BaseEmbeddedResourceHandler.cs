using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseEmbeddedResourceHandler : BaseHandler
    {
        protected BaseEmbeddedResourceHandler(string resourcePath)
            : base()
        {
            ResourcePath = resourcePath;
        }

        protected string ResourcePath { get; set; }

        public override Task<string> GetContentType()
        {
            return Task.FromResult(MimeTypes.GetMimeType(ResourcePath));
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return GetEmbeddedResourceStream().CopyToAsync(stream);
        }

        protected abstract Stream GetEmbeddedResourceStream();
    }
}
