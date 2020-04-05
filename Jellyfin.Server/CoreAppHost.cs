using System.Collections.Generic;
using System.Reflection;
using Emby.Drawing;
using Emby.Server.Implementations;
using Jellyfin.Drawing.Skia;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server
{
    /// <summary>
    /// Implementation of the abstract <see cref="ApplicationHost" /> class.
    /// </summary>
    public class CoreAppHost : ApplicationHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreAppHost" /> class.
        /// </summary>
        /// <param name="applicationPaths">The <see cref="ServerApplicationPaths" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="options">The <see cref="StartupOptions" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="networkManager">The <see cref="INetworkManager" /> to be used by the <see cref="CoreAppHost" />.</param>
        public CoreAppHost(
            ServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            StartupOptions options,
            IFileSystem fileSystem,
            INetworkManager networkManager)
            : base(
                applicationPaths,
                loggerFactory,
                options,
                fileSystem,
                networkManager)
        {
        }

        /// <inheritdoc/>
        protected override void RegisterServices(IServiceCollection serviceCollection)
        {
            var imageEncoderType = SkiaEncoder.IsNativeLibAvailable()
                ? typeof(SkiaEncoder)
                : typeof(NullImageEncoder);
            serviceCollection.AddSingleton(typeof(IImageEncoder), imageEncoderType);

            base.RegisterServices(serviceCollection);
        }

        /// <inheritdoc />
        protected override void RestartInternal() => Program.Restart();

        /// <inheritdoc />
        protected override IEnumerable<Assembly> GetAssembliesWithPartsInternal()
        {
            yield return typeof(CoreAppHost).Assembly;
        }

        /// <inheritdoc />
        protected override void ShutdownInternal() => Program.Shutdown();
    }
}
