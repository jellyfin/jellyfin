using System;
using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Core;
using Emby.Server.Implementations;
using Emby.Server.Implementations.FFMpeg;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using Emby.Server.Mac.Native;
using System.Diagnostics;
using MediaBrowser.Controller.Connect;
using Emby.Server.Connect;
using Emby.Server.Sync;
using MediaBrowser.Controller.Sync;

namespace MediaBrowser.Server.Mac
{
	public class MacAppHost : ApplicationHost
	{
        public MacAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, IMemoryStreamFactory memoryStreamFactory, MediaBrowser.Common.Net.INetworkManager networkManager, Action<string, string> certificateGenerator, Func<string> defaultUsernameFactory) : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, memoryStreamFactory, networkManager, certificateGenerator, defaultUsernameFactory)
        {
        }

        public override bool CanSelfRestart
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
                return false;
            }
        }

		protected override bool SupportsDualModeSockets
		{
			get
			{
				return true;
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

        protected override void RestartInternal()
        {
            MainClass.Restart();
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(GetType().Assembly);
			list.Add(typeof(ConnectManager).Assembly);
			list.Add(typeof(SyncManager).Assembly);

            return list;
        }

        protected override void ShutdownInternal()
        {
            MainClass.Shutdown();
        }

        protected override void AuthorizeServer()
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureAutoRunInternal(bool autorun)
        {
            throw new NotImplementedException();
        }

        public override bool SupportsRunningAsService
        {
            get
            {
                return false;
            }
        }

        public override bool SupportsAutoRunAtStartup
        {
            get { return false; }
        }

        public override bool IsRunningAsService
        {
            get
            {
                return false;
            }
        }
    }
}
