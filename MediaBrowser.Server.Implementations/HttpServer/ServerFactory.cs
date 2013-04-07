using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;

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
        /// <param name="logger">The logger.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <returns>IHttpServer.</returns>
        public static IHttpServer CreateServer(IApplicationHost applicationHost, ILogger logger, string serverName, string defaultRedirectpath)
        {
            return new HttpServer(applicationHost, logger, serverName, defaultRedirectpath);
        }
    }
}
