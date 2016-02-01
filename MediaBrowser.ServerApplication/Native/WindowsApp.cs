using MediaBrowser.Common.Net;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.ServerApplication.Networking;
using System.Collections.Generic;
using System.Reflection;
using CommonIO;
using MediaBrowser.Controller.Power;

namespace MediaBrowser.ServerApplication.Native
{
    public class WindowsApp : INativeApp
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public WindowsApp(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            if (!System.Environment.Is64BitProcess)
            {
                //list.Add(typeof(PismoIsoManager).Assembly);
            }

            list.Add(GetType().Assembly);
            
            return list;
        }

        public void AuthorizeServer(int udpPort, int httpServerPort, int httpsPort, string tempDirectory)
        {
            ServerAuthorization.AuthorizeServer(udpPort, httpServerPort, httpsPort, tempDirectory);
        }

        public NativeEnvironment Environment
        {
            get
            {
                return new NativeEnvironment
                {
                    OperatingSystem = OperatingSystem.Windows,
                    SystemArchitecture = System.Environment.Is64BitOperatingSystem ? Architecture.X86_X64 : Architecture.X86,
                    OperatingSystemVersionString = System.Environment.OSVersion.VersionString
                };
            }
        }

        public bool SupportsLibraryMonitor
        {
            get { return true; }
        }

        public bool SupportsRunningAsService
        {
            get
            {
                return true;
            }
        }

        public bool IsRunningAsService
        {
            get;
            set;
        }

        public bool CanSelfRestart
        {
            get
            {
                return MainStartup.CanSelfRestart;
            }
        }

        public bool SupportsAutoRunAtStartup
        {
            get
            {
                return true;
            }
        }

        public bool CanSelfUpdate
        {
            get
            {
                return MainStartup.CanSelfUpdate;
            }
        }

        public void Shutdown()
        {
            MainStartup.Shutdown();
        }

        public void Restart(StartupOptions startupOptions)
        {
            MainStartup.Restart();
        }

        public void ConfigureAutoRun(bool autorun)
        {
            Autorun.Configure(autorun, _fileSystem);
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

        public void PreventSystemStandby()
        {
            Standby.PreventSystemStandby();
        }

        public IPowerManagement GetPowerManagement()
        {
            return new WindowsPowerManagement(_logger);
        }
    }
}
