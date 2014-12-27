using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.ServerApplication.Networking;
using System.Collections.Generic;
using System.Reflection;

namespace MediaBrowser.ServerApplication.Native
{
    public class WindowsApp : INativeApp
    {
        public List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            list.Add(typeof(PismoIsoManager).Assembly);

            list.Add(GetType().Assembly);
            
            return list;
        }

        public void AuthorizeServer(int httpServerPort, string httpServerUrlPrefix, int udpPort, string tempDirectory)
        {
            ServerAuthorization.AuthorizeServer(httpServerPort, httpServerUrlPrefix, udpPort, tempDirectory);
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

        public void Restart()
        {
            MainStartup.Restart();
        }

        public void ConfigureAutoRun(bool autorun)
        {
            Autorun.Configure(autorun);
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

        public void PreventSystemStandby()
        {
            Standby.PreventSystemStandby();
        }
    }
}
