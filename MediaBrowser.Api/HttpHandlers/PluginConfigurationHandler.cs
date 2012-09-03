using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PluginConfigurationHandler : BaseSerializationHandler<BasePluginConfiguration>
    {
        protected override Task<BasePluginConfiguration> GetObjectToSerialize()
        {
            string name = QueryString["assemblyfilename"];

            BasePluginConfiguration config = Kernel.Instance.Plugins.First(p => p.AssemblyFileName.Equals(name, StringComparison.OrdinalIgnoreCase)).Configuration;

            return Task.FromResult<BasePluginConfiguration>(config);
        }
    }
}
