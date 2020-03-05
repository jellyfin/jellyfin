using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// The class that knows how migrate between different Jellyfin versions.
    /// </summary>
    public static class MigrationRunner
    {
        private static readonly IUpdater[] _migrations =
        {
            new Pre_10_5()
        };

        /// <summary>
        /// Run all needed migrations.
        /// </summary>
        /// <param name="host">CoreAppHost that hosts current version.</param>
        /// <param name="logger">AppHost logger.</param>
        /// <returns>Whether anything was changed.</returns>
        public static bool Run(CoreAppHost host, ILogger logger)
        {
            bool updated = false;
            var version = host.ServerConfigurationManager.CommonConfiguration.PreviousVersion;

            for (var i = 0; i < _migrations.Length; i++)
            {
                var updater = _migrations[i];
                if (version.CompareTo(updater.Maximum) >= 0)
                {
                    logger.LogDebug("Skipping updater {0} as current version {1} >= its maximum applicable version {2}", updater, version, updater.Maximum);
                    continue;
                }

                if (updater.Perform(host, logger, version))
                {
                    updated = true;
                }

                version = updater.Maximum;
            }

            return updated;
        }
    }
}
