using System.IO;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Legacy.V10v7
{
    /// <summary>
    /// Defines the settings migration for version 10.7.
    /// </summary>
    public class RunMigration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunMigration"/> class.
        /// </summary>
        /// <param name="appPaths">The <see cref="IServerApplicationPaths"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public RunMigration(IServerApplicationPaths appPaths, ILogger logger)
        {
            var destFile = Path.Combine(appPaths.ConfigurationDirectoryPath, "network.xml");
            if (File.Exists(destFile))
            {
                // Migrate version 10.7 network configuration.
                MigrationRunner.RunSettingMigration<ServerConfiguration, NetworkConfiguration>(logger, appPaths.SystemConfigurationFilePath, destFile);
                logger.LogDebug("Migrated to network configuration 7.0.3");
            }
        }
    }
}
