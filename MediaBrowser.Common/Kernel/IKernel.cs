using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Interface IKernel
    /// </summary>
    public interface IKernel
    {
        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        IApplicationPaths ApplicationPaths { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        BaseApplicationConfiguration Configuration { get; }

        /// <summary>
        /// Gets the kernel context.
        /// </summary>
        /// <value>The kernel context.</value>
        KernelContext KernelContext { get; }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Init();

        /// <summary>
        /// Gets or sets a value indicating whether this instance has pending kernel reload.
        /// </summary>
        /// <value><c>true</c> if this instance has pending kernel reload; otherwise, <c>false</c>.</value>
        bool HasPendingRestart { get; }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        SystemInfo GetSystemInfo();

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        void ReloadLogger();

        /// <summary>
        /// Called when [application updated].
        /// </summary>
        /// <param name="newVersion">The new version.</param>
        void OnApplicationUpdated(Version newVersion);

        /// <summary>
        /// Gets the name of the web application.
        /// </summary>
        /// <value>The name of the web application.</value>
        string WebApplicationName { get; }

        /// <summary>
        /// Performs the pending restart.
        /// </summary>
        void PerformPendingRestart();

        /// <summary>
        /// Gets the plugins.
        /// </summary>
        /// <value>The plugins.</value>
        IEnumerable<IPlugin> Plugins { get; }

        /// <summary>
        /// Gets the UDP server port number.
        /// </summary>
        /// <value>The UDP server port number.</value>
        int UdpServerPortNumber { get; }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        string HttpServerUrlPrefix { get; }

        /// <summary>
        /// Gets the TCP manager.
        /// </summary>
        /// <value>The TCP manager.</value>
        IServerManager ServerManager { get; }

        /// <summary>
        /// Gets the web socket listeners.
        /// </summary>
        /// <value>The web socket listeners.</value>
        IEnumerable<IWebSocketListener> WebSocketListeners { get; }

        /// <summary>
        /// Occurs when [logger loaded].
        /// </summary>
        event EventHandler LoggerLoaded;

        /// <summary>
        /// Occurs when [reload completed].
        /// </summary>
        event EventHandler<EventArgs> ReloadCompleted;

        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        event EventHandler<EventArgs> ConfigurationUpdated;

        /// <summary>
        /// Notifies the pending restart.
        /// </summary>
        void NotifyPendingRestart();

        /// <summary>
        /// Gets the XML configuration.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.Object.</returns>
        object GetXmlConfiguration(Type type, string path);
    }
}
