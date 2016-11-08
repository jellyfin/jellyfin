using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Text;

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
            IMemoryStreamFactory streamProvider,
            string serverName, 
            string defaultRedirectpath,
            ITextEncoding textEncoding,
            ISocketFactory socketFactory,
            ICryptoProvider cryptoProvider)
        {
            return new HttpListenerHost(applicationHost, logManager, config, serverName, defaultRedirectpath, networkmanager, streamProvider, textEncoding, socketFactory, cryptoProvider);
        }
    }
}
