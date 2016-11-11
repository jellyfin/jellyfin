using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Core;
using Emby.Server.Core.Data;
using Emby.Server.Core.FFMpeg;

namespace Emby.Server.Core
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
        void AuthorizeServer(int udpPort, int httpServerPort, int httpsServerPort, string applicationPath, string tempDirectory);

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

        FFMpegInstallInfo GetFfmpegInstallInfo();

        void LaunchUrl(string url);

        IDbConnector GetDbConnector();

        void EnableLoopback(string appName);
    }
}
