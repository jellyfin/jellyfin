using MediaBrowser.Common;
using MediaBrowser.Controller.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.WebDashboard
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// Gets the list of plugin configuration pages
        /// </summary>
        /// <value>The configuration pages.</value>
        public List<IPluginConfigurationPage> PluginConfigurationPages { get; private set; }

        private readonly IApplicationHost _appHost;

        public static ServerEntryPoint Instance { get; private set; }

        public ServerEntryPoint(IApplicationHost appHost)
        {
            _appHost = appHost;
            Instance = this;
        }

        public void Run()
        {
            PluginConfigurationPages = _appHost.GetExports<IPluginConfigurationPage>().ToList();
        }

        public void Dispose()
        {
        }
    }
}
