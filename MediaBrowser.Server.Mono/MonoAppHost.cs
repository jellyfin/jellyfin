using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
//using Emby.Server.CinemaMode;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.Net;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Sync;
using IsoMounter;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Mono
{
    public class MonoAppHost : ApplicationHost
    {
        public MonoAppHost(ServerApplicationPaths applicationPaths, ILogManager logManager, StartupOptions options, IFileSystem fileSystem, IPowerManagement powerManagement, string releaseAssetFilename, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, MediaBrowser.Common.Net.INetworkManager networkManager) : base(applicationPaths, logManager, options, fileSystem, powerManagement, releaseAssetFilename, environmentInfo, imageEncoder, systemEvents, networkManager)
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

        //protected override ISyncManager CreateSyncManager()
        //{
        //    return new SyncManager();
        //}

        protected override void RestartInternal()
        {
            MainClass.Restart();
        }

        protected override List<Assembly> GetAssembliesWithPartsInternal()
        {
            var list = new List<Assembly>();

            list.Add(GetType().Assembly);

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

        protected override IHttpListener CreateHttpListener()
        {
            return new EmbyServer.SocketSharp.WebSocketSharpListener(LogManager.GetLogger("HttpServer"),
                Certificate,
                StreamHelper,
                TextEncoding,
                NetworkManager,
                SocketFactory,
                CryptographyProvider,
                SupportsDualModeSockets,
                FileSystemManager,
                EnvironmentInfo);
        }

    }
}
