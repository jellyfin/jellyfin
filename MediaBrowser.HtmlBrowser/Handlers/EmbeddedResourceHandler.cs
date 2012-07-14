using System.IO;
using System.Reflection;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;

namespace MediaBrowser.HtmlBrowser.Handlers
{
    class EmbeddedResourceHandler : BaseEmbeddedResourceHandler
    {
        public EmbeddedResourceHandler(RequestContext ctx, string resourcePath)
            : base(ctx, resourcePath)
        {
      
        }

        protected override Stream GetEmbeddedResourceStream()
        {
            string path = ResourcePath.Replace("/", ".");

            return Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.HtmlBrowser.Html." + path);
        }
    }
}
