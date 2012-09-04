using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Api.HttpHandlers
{
    class ServerConfigurationHandler : BaseSerializationHandler<ServerConfiguration>
    {
        protected override Task<ServerConfiguration> GetObjectToSerialize()
        {
            return Task.FromResult<ServerConfiguration>(Kernel.Instance.Configuration);
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
