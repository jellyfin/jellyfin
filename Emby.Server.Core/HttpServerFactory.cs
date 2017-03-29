using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Emby.Common.Implementations.Net;
using Emby.Server.Implementations.HttpServer;
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

namespace Emby.Server.Core
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
            ICertificate certificate,
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
                new StreamFactory(),
                GetParseFn,
                enableDualModeSockets,
                fileSystem);
        }

        private static Func<string, object> GetParseFn(Type propertyType)
        {
            return s => JsvReader.GetParseFn(propertyType)(s);
        }
    }

    public class StreamFactory : IStreamFactory
    {
        public Stream CreateNetworkStream(IAcceptSocket acceptSocket, bool ownsSocket)
        {
            var netSocket = (NetAcceptSocket)acceptSocket;

            return new NetworkStream(netSocket.Socket, ownsSocket);
        }

        public Task AuthenticateSslStreamAsServer(Stream stream, ICertificate certificate)
        {
            var sslStream = (SslStream)stream;
            var cert = (Certificate)certificate;

            return sslStream.AuthenticateAsServerAsync(cert.X509Certificate);
        }

        public Stream CreateSslStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            return new SslStream(innerStream, leaveInnerStreamOpen);
        }
    }

    public class Certificate : ICertificate
    {
        public Certificate(X509Certificate x509Certificate)
        {
            X509Certificate = x509Certificate;
        }

        public X509Certificate X509Certificate { get; private set; }
    }
}
