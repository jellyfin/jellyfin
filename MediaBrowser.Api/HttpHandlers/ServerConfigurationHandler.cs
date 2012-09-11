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

        public override TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromDays(7);
            }
        }

        protected override Task<DateTime?> GetLastDateModified()
        {
            return Task.FromResult<DateTime?>(File.GetLastWriteTimeUtc(Kernel.Instance.ApplicationPaths.SystemConfigurationFilePath));
        }
    }
}
