using MediaBrowser.Common;
using MediaBrowser.Controller.Net;
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
        /// <param name="logManager">The log manager.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="handlerPath">The handler path.</param>
        /// <param name="defaultRedirectpath">The default redirectpath.</param>
        /// <returns>IHttpServer.</returns>
        public static IHttpServer CreateServer(IApplicationHost applicationHost, ILogManager logManager, string serverName, string handlerPath, string defaultRedirectpath)
        {
            return new HttpListenerHost(applicationHost, logManager, serverName, handlerPath, defaultRedirectpath);
        }
    }
}
