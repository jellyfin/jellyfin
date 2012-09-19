using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    class ServerConfigurationHandler : BaseSerializationHandler<ServerConfiguration>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("serverconfiguration", request);
        }

        protected override Task<ServerConfiguration> GetObjectToSerialize()
        {
            return Task.FromResult(Kernel.Instance.Configuration);
        }

        protected override async Task<ResponseInfo> GetResponseInfo()
        {
            var info = await base.GetResponseInfo().ConfigureAwait(false);

            info.DateLastModified =
                File.GetLastWriteTimeUtc(Kernel.Instance.ApplicationPaths.SystemConfigurationFilePath);

            info.CacheDuration = TimeSpan.FromDays(7);

            return info;
        }
    }
}
