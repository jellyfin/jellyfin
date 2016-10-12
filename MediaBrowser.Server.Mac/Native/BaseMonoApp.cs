using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Startup.Common.FFMpeg;
using System.Diagnostics;
using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Mac
{
    public abstract class BaseMonoApp : INativeApp
    {
        protected ILogger Logger { get; private set; }

        protected BaseMonoApp(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        /// Restarts this instance.
        /// </summary>
		public virtual void Restart(StartupOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether this instance [can self restart].
        /// </summary>
        /// <returns><c>true</c> if this instance [can self restart]; otherwise, <c>false</c>.</returns>
        public virtual bool CanSelfRestart
        {
            get
            {
                return false;
            }
        }

        public void PreventSystemStandby()
        {

        }

        public void AllowSystemStandby()
        {

        }
        
        public IDbConnector GetDbConnector()
        {
            return new DbConnector(Logger);
        }

		public virtual bool SupportsLibraryMonitor
		{
			get
			{
				return true;
			}
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

		public void AuthorizeServer(int udpPort, int httpServerPort, int httpsPort, string applicationPath, string tempDirectory)
        {
        }

        private NativeEnvironment _nativeEnvironment;
        public NativeEnvironment Environment
        {
            get { return _nativeEnvironment ?? (_nativeEnvironment = GetEnvironmentInfo()); }
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

        public void LaunchUrl(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url
                },

                EnableRaisingEvents = true,
            };

            process.Exited += ProcessExited;

			process.Start();
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }

        public FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            return GetInfo(Environment);
        }

        public static FFMpegInstallInfo GetInfo(NativeEnvironment environment)
        {
            var info = new FFMpegInstallInfo();

            info.ArchiveType = "7z";

            switch (environment.SystemArchitecture)
            {
                case Architecture.X64:
                    info.Version = "20160124";
                    break;
                case Architecture.X86:
                    info.Version = "20150110";
                    break;
            }

            info.DownloadUrls = GetDownloadUrls(environment);

            return info;
        }

        private static string[] GetDownloadUrls(NativeEnvironment environment)
        {
            switch (environment.SystemArchitecture)
            {
                case Architecture.X64:
                    return new[]
                    {
                                "https://github.com/MediaBrowser/Emby.Resources/raw/master/ffmpeg/osx/ffmpeg-x64-2.8.5.7z"
                            };
            }

            // No version available 
            return new string[] { };
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

		public void EnableLoopback(string appName)
		{

		}

		public bool PortsRequireAuthorization(string applicationPath)
		{
			return false;
		}

        private NativeEnvironment GetEnvironmentInfo()
        {
            var info = new NativeEnvironment
            {
                OperatingSystem = Startup.Common.OperatingSystem.Linux
            };

            var uname = GetUnixName();

            var sysName = uname.sysname ?? string.Empty;

			info.OperatingSystem = Startup.Common.OperatingSystem.Osx;

            var archX86 = new Regex("(i|I)[3-6]86");

            if (archX86.IsMatch(uname.machine))
            {
                info.SystemArchitecture = Architecture.X86;
            }
            else if (string.Equals(uname.machine, "x86_64", StringComparison.OrdinalIgnoreCase))
            {
                info.SystemArchitecture = Architecture.X64;
            }
            else if (uname.machine.StartsWith("arm", StringComparison.OrdinalIgnoreCase))
            {
                info.SystemArchitecture = Architecture.Arm;
            }

            info.OperatingSystemVersionString = string.IsNullOrWhiteSpace(sysName) ?
                System.Environment.OSVersion.VersionString :
                sysName;

            return info;
        }

        private Uname _unixName;
        private Uname GetUnixName()
        {
            if (_unixName == null)
            {
                var uname = new Uname();
                Utsname utsname;
                var callResult = Syscall.uname(out utsname);
                if (callResult == 0)
                {
                    uname.sysname = utsname.sysname;
                    uname.machine = utsname.machine;
                }

                _unixName = uname;
            }
            return _unixName;
        }

        private class Uname
        {
            public string sysname = string.Empty;
            public string machine = string.Empty;
        }
    }
}
