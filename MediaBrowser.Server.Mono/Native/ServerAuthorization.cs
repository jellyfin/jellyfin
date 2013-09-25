using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class Authorization
    /// </summary>
    public static class ServerAuthorization
    {
        /// <summary>
        /// Authorizes the server.
        /// </summary>
        /// <param name="httpServerPort">The HTTP server port.</param>
        /// <param name="httpServerUrlPrefix">The HTTP server URL prefix.</param>
        /// <param name="webSocketPort">The web socket port.</param>
        /// <param name="udpPort">The UDP port.</param>
        /// <param name="tempDirectory">The temp directory.</param>
        public static void AuthorizeServer(int httpServerPort, string httpServerUrlPrefix, int webSocketPort, int udpPort, string tempDirectory)
        {

        }
    }
}
