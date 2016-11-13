using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Emby.Server.Core;
using Emby.Server.Core.Data;
using Emby.Server.Core.FFMpeg;
using Emby.Server.Data;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;

namespace Emby.Server
{
    public class CoreAppHost : ApplicationHost
    {
        public CoreAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, IMemoryStreamFactory memoryStreamFactory, MediaBrowser.Common.Net.INetworkManager networkManager, Action<string, string> certificateGenerator, Func<string> defaultUsernameFactory)
            : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, memoryStreamFactory, networkManager, certificateGenerator, defaultUsernameFactory)
        {
        }

        public override bool IsRunningAsService
        {
            get { return false; }
        }

        protected override void RestartInternal()
        {
            Program.Restart();
        }

        protected override void ShutdownInternal()
        {
            Program.Shutdown();
        }

        protected override FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            var info = new FFMpegInstallInfo();

            return info;
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(GetType().GetTypeInfo().Assembly);

            return list;
        }

        protected override void AuthorizeServer()
        {
        }

        protected override IDbConnector GetDbConnector()
        {
            return new DbConnector(Logger);
        }

        protected override void ConfigureAutoRunInternal(bool autorun)
        {
        }

        public override void LaunchUrl(string url)
        {
        }

        protected override void EnableLoopbackInternal(string appName)
        {
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
                return Program.CanSelfRestart;
            }
        }

        public override bool SupportsAutoRunAtStartup
        {
            get
            {
                return true;
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
