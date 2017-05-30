using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using Emby.Common.Implementations.IO;
using Emby.Server.CinemaMode;
using Emby.Server.Connect;
using Emby.Server.Core;
using Emby.Server.Implementations;
using Emby.Server.Implementations.EntryPoints;
using Emby.Server.Implementations.FFMpeg;
using Emby.Server.Sync;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.ServerApplication.Native;

namespace MediaBrowser.ServerApplication
{
    public class WindowsAppHost : ApplicationHost
    {
        public WindowsAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, IMemoryStreamFactory memoryStreamFactory, MediaBrowser.Common.Net.INetworkManager networkManager, Action<string, string, string> certificateGenerator, Func<string> defaultUsernameFactory)
            : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, memoryStreamFactory, networkManager, certificateGenerator, defaultUsernameFactory)
        {
        }

        public override bool IsRunningAsService
        {
            get { return MainStartup.IsRunningAsService; }
        }

        protected override IConnectManager CreateConnectManager()
        {
            return new ConnectManager();
        }

        protected override ISyncManager CreateSyncManager()
        {
            return new SyncManager();
        }

        protected override void RestartInternal()
        {
            MainStartup.Restart();
        }

        public override void EnableLoopback(string appName)
        {
            LoopUtil.Run(appName);
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(typeof(DefaultIntroProvider).Assembly);
            list.Add(typeof(ConnectManager).Assembly);
            list.Add(typeof(SyncManager).Assembly);
            list.Add(GetType().Assembly);

            return list;
        }

        protected override void ShutdownInternal()
        {
            MainStartup.Shutdown();
        }

        protected override void AuthorizeServer()
        {
            ServerAuthorization.AuthorizeServer(UdpServerEntryPoint.PortNumber,
                    ServerConfigurationManager.Configuration.HttpServerPortNumber,
                    ServerConfigurationManager.Configuration.HttpsPortNumber,
                    MainStartup.ApplicationPath,
                    ConfigurationManager.CommonApplicationPaths.TempDirectory);
        }

        protected override void ConfigureAutoRunInternal(bool autorun)
        {
            var startupPath = Environment.GetFolderPath(System.Environment.SpecialFolder.Startup);

            if (autorun && !MainStartup.IsRunningAsService)
            {
                //Copy our shortut into the startup folder for this user
                var targetPath = Path.Combine(startupPath, "Emby Server.lnk");

                IShellLinkW link = (IShellLinkW)new ShellLink();

                var appPath = Process.GetCurrentProcess().MainModule.FileName;

                // setup shortcut information
                link.SetDescription(Name);
                link.SetPath(appPath);
                link.SetWorkingDirectory(Path.GetDirectoryName(appPath));

                // save it
                IPersistFile file = (IPersistFile)link;
                file.Save(targetPath, true);
            }
            else
            {
                //Remove our shortcut from the startup folder for this user
                FileSystemManager.DeleteFile(Path.Combine(startupPath, "Emby Server.lnk"));
            }
        }

        public override bool SupportsRunningAsService
        {
            get
            {
                return true;
            }
        }

        public override bool CanSelfRestart
        {
            get
            {
                return MainStartup.CanSelfRestart;
            }
        }

        public override bool CanSelfUpdate
        {
            get
            {
                return MainStartup.CanSelfUpdate;
            }
        }
    }
}
