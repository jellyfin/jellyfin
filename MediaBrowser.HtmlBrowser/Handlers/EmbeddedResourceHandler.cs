using System.IO;
using System.Reflection;
using MediaBrowser.Net;
using MediaBrowser.Net.Handlers;

namespace MediaBrowser.HtmlBrowser.Handlers
{
    class EmbeddedResourceHandler : BaseEmbeddedResourceHandler
    {
        public EmbeddedResourceHandler(string resourcePath)
            : base(resourcePath)
        {
      
        }

        protected override Stream GetEmbeddedResourceStream()
        {
            string path = ResourcePath.Replace("/", ".");

            return Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.HtmlBrowser.Html." + path);
        }
    }
}
