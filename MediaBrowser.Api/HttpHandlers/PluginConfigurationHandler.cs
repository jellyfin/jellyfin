using System;
using System.Linq;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class PluginConfigurationHandler : JsonHandler
    {
        protected override object ObjectToSerialize
        {
            get
            {
                string pluginName = QueryString["name"];

                return Kernel.Instance.PluginController.Plugins.First(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase)).Configuration;
            }
        }
    }
}
