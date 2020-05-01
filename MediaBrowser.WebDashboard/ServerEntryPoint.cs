#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.WebDashboard
{
    public sealed class ServerEntryPoint : IServerEntryPoint
    {
        private readonly IApplicationHost _appHost;

        public ServerEntryPoint(IApplicationHost appHost)
        {
            _appHost = appHost;
            Instance = this;
        }

        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// Gets the list of plugin configuration pages.
        /// </summary>
        /// <value>The configuration pages.</value>
        public List<IPluginConfigurationPage> PluginConfigurationPages { get; private set; }

        /// <inheritdoc />
        public Task RunAsync()
        {
            PluginConfigurationPages = _appHost.GetExports<IPluginConfigurationPage>().ToList();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
