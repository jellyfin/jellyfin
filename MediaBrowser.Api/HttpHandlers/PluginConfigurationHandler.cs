using System;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PluginConfigurationHandler : BaseJsonHandler
    {
        protected override object GetObjectToSerialize()
        {
            string pluginName = QueryString["name"];

            return Kernel.Instance.Plugins.First(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)).Configuration;
        }
    }
}
