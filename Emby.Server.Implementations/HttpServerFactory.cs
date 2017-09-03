using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Text;
using ServiceStack.Text.Jsv;
using SocketHttpListener.Primitives;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Class ServerFactory
    /// </summary>
    public static class HttpServerFactory
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
            ICryptoProvider cryptoProvider,
            IJsonSerializer json,
            IXmlSerializer xml,
            IEnvironmentInfo environment,
            X509Certificate certificate,
            IFileSystem fileSystem,
            bool enableDualModeSockets)
        {
            var logger = logManager.GetLogger("HttpServer");

            return new HttpListenerHost(applicationHost,
                logger,
                config,
                serverName,
                defaultRedirectpath,
                networkmanager,
                streamProvider,
                textEncoding,
                socketFactory,
                cryptoProvider,
                json,
                xml,
                environment,
                certificate,
                GetParseFn,
                enableDualModeSockets,
                fileSystem);
        }

        private static Func<string, object> GetParseFn(Type propertyType)
        {
            return s => JsvReader.GetParseFn(propertyType)(s);
        }
    }
}
