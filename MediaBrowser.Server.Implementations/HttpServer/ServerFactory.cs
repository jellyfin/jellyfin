using MediaBrowser.Common;
using MediaBrowser.Common.IO;
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
        /// <returns>IHttpServer.</returns>
        public static IHttpServer CreateServer(IApplicationHost applicationHost,
            ILogManager logManager,
            IServerConfigurationManager config, 
            INetworkManager _networkmanager,
            IMemoryStreamProvider streamProvider,
            string serverName, 
            string defaultRedirectpath)
        {
            LogManager.LogFactory = new ServerLogFactory(logManager);

            return new HttpListenerHost(applicationHost, logManager, config, serverName, defaultRedirectpath, _networkmanager, streamProvider);
        }
    }
}
