using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Interface IKernel
    /// </summary>
    public interface IKernel
    {
        /// <summary>
        /// Occurs when [application restart requested].
        /// </summary>
        event EventHandler ApplicationRestartRequested;

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        BaseApplicationPaths ApplicationPaths { get; }

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
        /// Gets the protobuf serializer.
        /// </summary>
        /// <value>The protobuf serializer.</value>
        DynamicProtobufSerializer ProtobufSerializer { get; }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Init(IIsoManager isoManager);

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Reload();

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
        /// Gets the scheduled tasks.
        /// </summary>
        /// <value>The scheduled tasks.</value>
        IEnumerable<IScheduledTask> ScheduledTasks { get; }

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
        /// Gets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        string LogFilePath { get; }

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
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        bool IsFirstRun { get; }

        /// <summary>
        /// Gets the TCP manager.
        /// </summary>
        /// <value>The TCP manager.</value>
        TcpManager TcpManager { get; }

        /// <summary>
        /// Gets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        TaskManager TaskManager { get; }

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
        /// Gets the assemblies.
        /// </summary>
        /// <value>The assemblies.</value>
        Assembly[] Assemblies { get; }

        /// <summary>
        /// Gets the rest services.
        /// </summary>
        /// <value>The rest services.</value>
        IEnumerable<IRestfulService> RestServices { get; }
    }
}
