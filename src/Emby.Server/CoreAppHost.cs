using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Emby.Server.Core;
using Emby.Server.Implementations.FFMpeg;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using Emby.Server.Implementations;

namespace Emby.Server
{
    public class CoreAppHost : ApplicationHost
    {
        public CoreAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, IMemoryStreamFactory memoryStreamFactory, MediaBrowser.Common.Net.INetworkManager networkManager, Action<string, string> certificateGenerator, Func<string> defaultUsernameFactory)
            : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, memoryStreamFactory, networkManager, certificateGenerator, defaultUsernameFactory)
        {
        }

        protected override void RestartInternal()
        {
            Program.Restart();
        }

        protected override void ShutdownInternal()
        {
            Program.Shutdown();
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(GetType().GetTypeInfo().Assembly);

            return list;
        }

        public override bool CanSelfRestart
        {
            get
            {
                return Program.CanSelfRestart;
            }
        }

        public override bool CanSelfUpdate
        {
            get
            {
                return Program.CanSelfUpdate;
            }
        }
    }
}
