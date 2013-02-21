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

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return GetEmbeddedResourceStream().CopyToAsync(stream);
        }

        protected abstract Stream GetEmbeddedResourceStream();
    }
}
