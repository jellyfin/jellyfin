using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    class PluginAssemblyHandler : BaseHandler
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("pluginassembly", request);
        }

        protected override Task<ResponseInfo> GetResponseInfo()
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

            return new StaticFileHandler { Path = path }.ProcessRequest(ctx);
        }
    }
}
