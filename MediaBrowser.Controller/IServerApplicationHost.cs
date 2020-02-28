using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Http;

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
        Task<SystemInfo> GetSystemInfo(CancellationToken cancellationToken);

        Task<PublicSystemInfo> GetPublicSystemInfo(CancellationToken cancellationToken);

        bool CanLaunchWebBrowser { get; }

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
        Task<List<IPAddress>> GetLocalIpAddresses(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the local API URL.
        /// </summary>
        /// <value>The local API URL.</value>
        Task<string> GetLocalApiUrl(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the local API URL.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <returns>The local API URL.</returns>
        string GetLocalApiUrl(ReadOnlySpan<char> hostname);

        /// <summary>
        /// Gets the local API URL.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>The local API URL.</returns>
        string GetLocalApiUrl(IPAddress address);

        void LaunchUrl(string url);

        void EnableLoopback(string appName);

        IEnumerable<WakeOnLanInfo> GetWakeOnLanInfo();

        string ExpandVirtualPath(string path);
        string ReverseVirtualPath(string path);

        Task ExecuteHttpHandlerAsync(HttpContext context, Func<Task> next);

        Task ExecuteWebsocketHandlerAsync(HttpContext context, Func<Task> next);
    }
}
