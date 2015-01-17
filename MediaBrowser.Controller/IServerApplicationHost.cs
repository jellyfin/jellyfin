using MediaBrowser.Common;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface IServerApplicationHost
    /// </summary>
    public interface IServerApplicationHost : IApplicationHost
    {
        event EventHandler HasUpdateAvailableChanged;
        
        /// <summary>
        /// Gets the system info.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        SystemInfo GetSystemInfo();

        /// <summary>
        /// Gets a value indicating whether [supports automatic run at startup].
        /// </summary>
        /// <value><c>true</c> if [supports automatic run at startup]; otherwise, <c>false</c>.</value>
        bool SupportsAutoRunAtStartup { get; }

        /// <summary>
        /// Gets the HTTP server port.
        /// </summary>
        /// <value>The HTTP server port.</value>
        int HttpServerPort { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has update available.
        /// </summary>
        /// <value><c>true</c> if this instance has update available; otherwise, <c>false</c>.</value>
        bool HasUpdateAvailable { get; }

        /// <summary>
        /// Gets the name of the friendly.
        /// </summary>
        /// <value>The name of the friendly.</value>
        string FriendlyName { get; }

        /// <summary>
        /// Gets the HTTP server ip addresses.
        /// </summary>
        /// <value>The HTTP server ip addresses.</value>
        IEnumerable<string> HttpServerIpAddresses { get; }
    }
}
