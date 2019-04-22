using System.Collections.Generic;
using System.Reflection;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.HttpServer;
using Jellyfin.Model.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server
{
    public class CoreAppHost : ApplicationHost
    {
        public CoreAppHost(
            ServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            StartupOptions options,
            IFileSystem fileSystem,
            Jellyfin.Controller.Drawing.IImageEncoder imageEncoder,
            Jellyfin.Common.Net.INetworkManager networkManager,
            IConfiguration configuration)
            : base(
                applicationPaths,
                loggerFactory,
                options,
                fileSystem,
                imageEncoder,
                networkManager,
                configuration)
        {
        }

        public override bool CanSelfRestart => StartupOptions.RestartPath != null;

        protected override void RestartInternal() => Program.Restart();

        protected override IEnumerable<Assembly> GetAssembliesWithPartsInternal()
        {
            yield return typeof(CoreAppHost).Assembly;
        }

        protected override void ShutdownInternal() => Program.Shutdown();
    }
}
