using MediaBrowser.Common;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

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
        Task<SystemInfo> GetSystemInfo();

        /// <summary>
        /// Gets a value indicating whether this instance is running as service.
        /// </summary>
        /// <value><c>true</c> if this instance is running as service; otherwise, <c>false</c>.</value>
        bool IsRunningAsService { get; }

        /// <summary>
        /// Gets a value indicating whether [supports automatic run at startup].
        /// </summary>
        /// <value><c>true</c> if [supports automatic run at startup]; otherwise, <c>false</c>.</value>
        bool SupportsAutoRunAtStartup { get; }
        
        /// <summary>
        /// Gets the HTTP server port.
        /// </summary>
        /// <value>The HTTP server port.</value>
        int HttpPort { get; }

        /// <summary>
        /// Gets the HTTPS port.
        /// </summary>
        /// <value>The HTTPS port.</value>
        int HttpsPort { get; }
        
        /// <summary>
        /// Gets a value indicating whether [supports HTTPS].
        /// </summary>
        /// <value><c>true</c> if [supports HTTPS]; otherwise, <c>false</c>.</value>
        bool EnableHttps { get; }

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
        /// Gets the local ip address.
        /// </summary>
        /// <value>The local ip address.</value>
        Task<List<IpAddressInfo>> GetLocalIpAddresses();

        /// <summary>
        /// Gets the local API URL.
        /// </summary>
        /// <value>The local API URL.</value>
        Task<string> GetLocalApiUrl();

        /// <summary>
        /// Gets the local API URL.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>System.String.</returns>
        string GetLocalApiUrl(string host);

        /// <summary>
        /// Gets the local API URL.
        /// </summary>
        string GetLocalApiUrl(IpAddressInfo address);

        void LaunchUrl(string url);

        void EnableLoopback(string appName);
    }
}
