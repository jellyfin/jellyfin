using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
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
        public static IHttpServer CreateServer(IServerApplicationHost applicationHost,
            ILogManager logManager,
            IServerConfigurationManager config, 
            INetworkManager networkmanager,
            IMemoryStreamProvider streamProvider,
            string serverName, 
            string defaultRedirectpath)
        {
            LogManager.LogFactory = new ServerLogFactory(logManager);

            return new HttpListenerHost(applicationHost, logManager, config, serverName, defaultRedirectpath, networkmanager, streamProvider);
        }
    }
}
