using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PluginConfigurationHandler : BaseJsonHandler<BasePluginConfiguration>
    {
        protected override Task<BasePluginConfiguration> GetObjectToSerialize()
        {
            return Task.Run(() =>
            {
                string pluginName = QueryString["name"];

                return Kernel.Instance.Plugins.First(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)).Configuration;
            });
        }
    }
}
