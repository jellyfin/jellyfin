using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Implementations;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Configuration;
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
        /// <param name="imageEncoder">The <see cref="IImageEncoder" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="networkManager">The <see cref="INetworkManager" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="configuration">The <see cref="IConfiguration" /> to be used by the <see cref="CoreAppHost" />.</param>
        public CoreAppHost(
            ServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            StartupOptions options,
            IFileSystem fileSystem,
            IImageEncoder imageEncoder,
            INetworkManager networkManager,
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

        /// <inheritdoc />
        public override bool CanSelfRestart => StartupOptions.RestartPath != null;

        /// <inheritdoc />
        protected override void RestartInternal() => Program.Restart();

        /// <inheritdoc />
        protected override IEnumerable<Assembly> GetAssembliesWithPartsInternal()
        {
            yield return typeof(CoreAppHost).Assembly;
        }

        /// <inheritdoc />
        protected override void ShutdownInternal() => Program.Shutdown();

        /// <summary>
        /// Runs the migration routines if necessary.
        /// </summary>
        public void TryMigrate()
        {
            var previousVersion = ConfigurationManager.CommonConfiguration.PreviousVersion;
            switch (ApplicationVersion.CompareTo(previousVersion))
            {
                case 1:
                    Logger.LogWarning("Version check shows Jellyfin was updated: previous version={0}, current version={1}", previousVersion, ApplicationVersion);

                    Migrations.Run(this, Logger);

                    ConfigurationManager.CommonConfiguration.PreviousVersion = ApplicationVersion;
                    ConfigurationManager.SaveConfiguration();
                    break;
                case 0:
                    // nothing to do, versions match
                    break;
                case -1:
                    Logger.LogWarning("Version check shows Jellyfin was rolled back, use at your own risk: previous version={0}, current version={1}", previousVersion, ApplicationVersion);
                    // no "rollback" routines for now
                    ConfigurationManager.CommonConfiguration.PreviousVersion = ApplicationVersion;
                    ConfigurationManager.SaveConfiguration();
                    break;
            }
        }
    }
}
