using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Reflection;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Startup.Common.FFMpeg;

namespace MediaBrowser.Server.Startup.Common
{
    public interface INativeApp
    {
        /// <summary>
        /// Gets the assemblies with parts.
        /// </summary>
        /// <returns>List&lt;Assembly&gt;.</returns>
        List<Assembly> GetAssembliesWithParts();

        /// <summary>
        /// Authorizes the server.
        /// </summary>
        /// <param name="udpPort">The UDP port.</param>
        /// <param name="httpServerPort">The HTTP server port.</param>
        /// <param name="httpsServerPort">The HTTPS server port.</param>
        /// <param name="tempDirectory">The temporary directory.</param>
        void AuthorizeServer(int udpPort, int httpServerPort, int httpsServerPort, string applicationPath, string tempDirectory);

        /// <summary>
        /// Gets the environment.
        /// </summary>
        /// <value>The environment.</value>
        NativeEnvironment Environment { get; }

        /// <summary>
        /// Gets a value indicating whether [supports running as service].
        /// </summary>
        /// <value><c>true</c> if [supports running as service]; otherwise, <c>false</c>.</value>
        bool SupportsRunningAsService { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is running as service.
        /// </summary>
        /// <value><c>true</c> if this instance is running as service; otherwise, <c>false</c>.</value>
        bool IsRunningAsService { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        bool CanSelfRestart { get; }

        /// <summary>
        /// Gets a value indicating whether [supports autorun at startup].
        /// </summary>
        /// <value><c>true</c> if [supports autorun at startup]; otherwise, <c>false</c>.</value>
        bool SupportsAutoRunAtStartup { get; }

        /// <summary>
        /// Gets a value indicating whether [supports library monitor].
        /// </summary>
        /// <value><c>true</c> if [supports library monitor]; otherwise, <c>false</c>.</value>
        bool SupportsLibraryMonitor { get; }
        
        /// <summary>
        /// Gets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        bool CanSelfUpdate { get; }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        void Restart(StartupOptions startupOptions);

        /// <summary>
        /// Configures the automatic run.
        /// </summary>
        /// <param name="autorun">if set to <c>true</c> [autorun].</param>
        void ConfigureAutoRun(bool autorun);

        /// <summary>
        /// Gets the network manager.
        /// </summary>
        /// <returns>INetworkManager.</returns>
        INetworkManager CreateNetworkManager(ILogger logger);

        /// <summary>
        /// Prevents the system stand by.
        /// </summary>
        void PreventSystemStandby();

        void AllowSystemStandby();

        FFMpegInstallInfo GetFfmpegInstallInfo();

        void LaunchUrl(string url);

        IDbConnector GetDbConnector();

        void EnableLoopback(string appName);
    }
}
