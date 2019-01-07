using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Implementations;
using Emby.Server.Implementations.HttpServer;
using Jellyfin.SocketSharp;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server
{
    public class CoreAppHost : ApplicationHost
    {
        public CoreAppHost(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory, StartupOptions options, IFileSystem fileSystem, IEnvironmentInfo environmentInfo, MediaBrowser.Controller.Drawing.IImageEncoder imageEncoder, ISystemEvents systemEvents, MediaBrowser.Common.Net.INetworkManager networkManager)
            : base(applicationPaths, loggerFactory, options, fileSystem, environmentInfo, imageEncoder, systemEvents, networkManager)
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

        protected override void RestartInternal() => Program.Restart();

        protected override IEnumerable<Assembly> GetAssembliesWithPartsInternal()
            => new [] { typeof(CoreAppHost).Assembly };

        protected override void ShutdownInternal() => Program.Shutdown();

        protected override bool SupportsDualModeSockets
        {
            get
            {
                return true;
            }
        }

        protected override IHttpListener CreateHttpListener()
            => new WebSocketSharpListener(
                Logger,
                Certificate,
                StreamHelper,
                TextEncoding,
                NetworkManager,
                SocketFactory,
                CryptographyProvider,
                SupportsDualModeSockets,
                FileSystemManager,
                EnvironmentInfo
            );
    }
}
