using MediaBrowser.Common;
using MediaBrowser.Model.System;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface IServerApplicationHost
    /// </summary>
    public interface IServerApplicationHost : IApplicationHost
    {
        /// <summary>
        /// Gets the system info.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        SystemInfo GetSystemInfo();

        /// <summary>
        /// Gets the name of the web application.
        /// </summary>
        /// <value>The name of the web application.</value>
        string WebApplicationName { get; }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        string HttpServerUrlPrefix { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is background service.
        /// </summary>
        /// <value><c>true</c> if this instance is background service; otherwise, <c>false</c>.</value>
        bool IsBackgroundService { get; }
    }
}
