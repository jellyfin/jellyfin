using System;
using System.Collections.Generic;
using System.Reflection;
using Emby.Server.CinemaMode;
using Emby.Server.Connect;
using Emby.Server.Core;
using Emby.Server.Implementations;
using Emby.Server.Sync;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Sync;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Server.Startup.Common;

namespace MediaBrowser.Server.Mono
{
    public class MonoAppHost : ApplicationHost
    {
        public MonoAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, IMemoryStreamFactory memoryStreamFactory, MediaBrowser.Common.Net.INetworkManager networkManager, Action<string, string, string> certificateGenerator, Func<string> defaultUsernameFactory) : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, memoryStreamFactory, networkManager, certificateGenerator, defaultUsernameFactory)
        {
        }

        public override bool CanSelfRestart
        {
            get
            {
                // A restart script must be provided
                return StartupOptions.ContainsOption("-restartpath");
            }
        }

        protected override IConnectManager CreateConnectManager()
        {
            return new ConnectManager();
        }

        protected override ISyncManager CreateSyncManager()
        {
            return new SyncManager();
        }

        public override PackageVersionClass SystemUpdateLevel
        {
            get { return UpdateLevelHelper.GetSystemUpdateLevel(ConfigurationManager); }
        }

        protected override void RestartInternal()
        {
            MainClass.Restart(StartupOptions);
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(GetType().Assembly);
            list.AddRange(GetLinuxAssemblies());

            return list;
        }

        private IEnumerable<Assembly> GetLinuxAssemblies()
        {
            var list = new List<Assembly>();

            list.Add(typeof(DefaultIntroProvider).Assembly);
            list.Add(typeof(LinuxIsoManager).Assembly);
            list.Add(typeof(ConnectManager).Assembly);
            list.Add(typeof(SyncManager).Assembly);

            return list;
        }

        protected override void ShutdownInternal()
        {
            MainClass.Shutdown();
        }

        protected override bool SupportsDualModeSockets
        {
            get
            {
                return GetMonoVersion() >= new Version(4, 6);
            }
        }

        private static Version GetMonoVersion()
        {
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetTypeInfo().GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                var displayNameValue = displayName.Invoke(null, null).ToString().Trim().Split(' ')[0];

                Version version;
                if (Version.TryParse(displayNameValue, out version))
                {
                    return version;
                }
            }

            return new Version(1, 0);
        }
    }
}
