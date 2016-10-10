using MediaBrowser.Common.Net;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Mono.Networking;
using MediaBrowser.Server.Startup.Common;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using MediaBrowser.Model.System;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Startup.Common.FFMpeg;
using OperatingSystem = MediaBrowser.Server.Startup.Common.OperatingSystem;

namespace MediaBrowser.Server.Mono.Native
{
    public abstract class BaseMonoApp : INativeApp
    {
        protected StartupOptions StartupOptions { get; private set; }
        protected ILogger Logger { get; private set; }

        protected BaseMonoApp(StartupOptions startupOptions, ILogger logger)
        {
            StartupOptions = startupOptions;
            Logger = logger;
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public abstract void Shutdown();

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public virtual void Restart(StartupOptions startupOptions)
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

        public void PreventSystemStandby()
        {

        }

        public void AllowSystemStandby()
        {

        }

        public List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            if (Environment.OperatingSystem == Startup.Common.OperatingSystem.Linux)
            {
                list.AddRange(GetLinuxAssemblies());
            }

            list.Add(GetType().Assembly);

            return list;
        }

        private IEnumerable<Assembly> GetLinuxAssemblies()
        {
            var list = new List<Assembly>();

            list.Add(typeof(LinuxIsoManager).Assembly);

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

        public bool SupportsLibraryMonitor
        {
            get
            {
                return Environment.OperatingSystem != Startup.Common.OperatingSystem.Osx;
            }
        }

        public void ConfigureAutoRun(bool autorun)
        {
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

        private NativeEnvironment GetEnvironmentInfo()
        {
            var info = new NativeEnvironment
            {
                OperatingSystem = Startup.Common.OperatingSystem.Linux
            };

            var uname = GetUnixName();

            var sysName = uname.sysname ?? string.Empty;

            if (string.Equals(sysName, "Darwin", StringComparison.OrdinalIgnoreCase))
            {
                info.OperatingSystem = Startup.Common.OperatingSystem.Osx;
            }
            else if (string.Equals(sysName, "Linux", StringComparison.OrdinalIgnoreCase))
            {
                info.OperatingSystem = Startup.Common.OperatingSystem.Linux;
            }
            else if (string.Equals(sysName, "BSD", StringComparison.OrdinalIgnoreCase))
            {
                info.OperatingSystem = Startup.Common.OperatingSystem.Bsd;
            }

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
            else if (System.Environment.Is64BitOperatingSystem)
            {
                info.SystemArchitecture = Architecture.X64;
            }
            else
            {
                info.SystemArchitecture = Architecture.X86;
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
                try
                {
                    Utsname utsname;
                    var callResult = Syscall.uname(out utsname);
                    if (callResult == 0)
                    {
                        uname.sysname = utsname.sysname ?? string.Empty;
                        uname.machine = utsname.machine ?? string.Empty;
                    }

                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error getting unix name", ex);
                }
                _unixName = uname;
            }
            return _unixName;
        }

        public class Uname
        {
            public string sysname = string.Empty;
            public string machine = string.Empty;
        }

        public FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            return GetInfo(Environment);
        }

        public void LaunchUrl(string url)
        {
            throw new NotImplementedException();
        }

        public IDbConnector GetDbConnector()
        {
            return new DbConnector(Logger);
        }

        public static FFMpegInstallInfo GetInfo(NativeEnvironment environment)
        {
            var info = new FFMpegInstallInfo();

            // Windows builds: http://ffmpeg.zeranoe.com/builds/
            // Linux builds: http://johnvansickle.com/ffmpeg/
            // OS X builds: http://ffmpegmac.net/
            // OS X x64: http://www.evermeet.cx/ffmpeg/

            switch (environment.OperatingSystem)
            {
                case OperatingSystem.Osx:
                case OperatingSystem.Bsd:
                    break;
                case OperatingSystem.Linux:

                    info.ArchiveType = "7z";
                    info.Version = "20160215";
                    break;
            }

            // No version available - user requirement
            info.DownloadUrls = new string[] { };

            return info;
        }

        public void EnableLoopback(string appName)
        {

        }
    }
}
