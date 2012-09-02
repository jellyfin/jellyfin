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
    }
}
