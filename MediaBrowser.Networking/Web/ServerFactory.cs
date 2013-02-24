using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Networking.Web
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
        /// <param name="kernel">The kernel.</param>
        /// <param name="protobufSerializer">The protobuf serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <returns>IHttpServer.</returns>
        public static IHttpServer CreateServer(IApplicationHost applicationHost, IKernel kernel, IProtobufSerializer protobufSerializer, ILogger logger, string serverName, string defaultRedirectpath)
        {
            return new HttpServer(applicationHost, kernel, protobufSerializer, logger, serverName, defaultRedirectpath);
        }
    }
}
