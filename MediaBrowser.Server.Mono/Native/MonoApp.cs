using MediaBrowser.Common.Net;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Emby.Common.Implementations.Networking;
using Emby.Server.Core;
using Emby.Server.Core.Data;
using Emby.Server.Core.FFMpeg;
using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Mono.Native
{
    public class MonoApp : INativeApp
    {
        protected StartupOptions StartupOptions { get; private set; }
        protected ILogger Logger { get; private set; }
        private readonly MonoEnvironmentInfo _environment;

        public MonoApp(StartupOptions startupOptions, ILogger logger, MonoEnvironmentInfo environment)
        {
            StartupOptions = startupOptions;
            Logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public void Shutdown()
        {
            MainClass.Shutdown();
        }

        /// <summary>
        /// Determines whether this instance [can self restart].
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public bool CanSelfRestart
        {
            get
            {
                // A restart script must be provided
                return StartupOptions.ContainsOption("-restartpath");
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public void Restart(StartupOptions startupOptions)
        {
            MainClass.Restart(startupOptions);
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate
        {
            get
            {
                return false;
            }
        }

        public bool SupportsAutoRunAtStartup
        {
            get { return false; }
        }

        public List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            list.Add(GetType().Assembly);

            return list;
        }

        private IEnumerable<Assembly> GetLinuxAssemblies()
        {
            var list = new List<Assembly>();

            //list.Add(typeof(LinuxIsoManager).Assembly);

            return list;
        }

        public void AuthorizeServer(int udpPort, int httpServerPort, int httpsPort, string applicationPath, string tempDirectory)
        {
        }

        public bool SupportsRunningAsService
        {
            get
            {
                return false;
            }
        }

        public bool IsRunningAsService
        {
            get
            {
                return false;
            }
        }

        public void ConfigureAutoRun(bool autorun)
        {
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

        public FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            var info = new FFMpegInstallInfo();

            // Windows builds: http://ffmpeg.zeranoe.com/builds/
            // Linux builds: http://johnvansickle.com/ffmpeg/
            // OS X builds: http://ffmpegmac.net/
            // OS X x64: http://www.evermeet.cx/ffmpeg/

            if (_environment.IsBsd)
            {
                
            }
            else if (_environment.OperatingSystem == Model.System.OperatingSystem.Linux)
            {
                info.ArchiveType = "7z";
                info.Version = "20160215";
            }

            // No version available - user requirement
            info.DownloadUrls = new string[] { };

            return info;
        }

        public void LaunchUrl(string url)
        {
            throw new NotImplementedException();
        }

        public IDbConnector GetDbConnector()
        {
            return new DbConnector(Logger);
        }

        public void EnableLoopback(string appName)
        {

        }
    }
}
