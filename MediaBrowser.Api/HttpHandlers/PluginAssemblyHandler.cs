using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    class PluginAssemblyHandler : BaseHandler
    {
        public override Task<string> GetContentType()
        {
            throw new NotImplementedException();
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            throw new NotImplementedException();
        }

        public override Task ProcessRequest(HttpListenerContext ctx)
        {
            string filename = ctx.Request.QueryString["assemblyfilename"];

            string path = Path.Combine(Kernel.Instance.ApplicationPaths.PluginsPath, filename);

            return new StaticFileHandler() { Path = path }.ProcessRequest(ctx);
        }
    }
}
