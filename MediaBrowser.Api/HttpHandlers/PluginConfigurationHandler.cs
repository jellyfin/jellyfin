using System;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PluginConfigurationHandler : BaseJsonHandler<BasePluginConfiguration>
    {
        protected override BasePluginConfiguration GetObjectToSerialize()
        {
            string pluginName = QueryString["name"];

            return Kernel.Instance.Plugins.First(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)).Configuration;
        }
    }
}
