using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using ServiceStack.Logging;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class ServerFactory
    /// </summary>
    public static class ServerFactory
    {
        /// <summary>
        /// Creates the server.
        /// </summary>
        /// <param name="applicationHost">The application host.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="_networkmanager">The _networkmanager.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <returns>IHttpServer.</returns>
        public static IHttpServer CreateServer(IApplicationHost applicationHost,
            ILogManager logManager,
            IServerConfigurationManager config, 
            INetworkManager _networkmanager,
            string serverName, 
            string defaultRedirectpath)
        {
            LogManager.LogFactory = new ServerLogFactory(logManager);

            return new HttpListenerHost(applicationHost, logManager, config, serverName, defaultRedirectpath, _networkmanager);
        }
    }
}
