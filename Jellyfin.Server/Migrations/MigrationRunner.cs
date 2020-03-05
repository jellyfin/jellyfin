using System;
using System.Linq;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// The class that knows how migrate between different Jellyfin versions.
    /// </summary>
    public sealed class MigrationRunner
    {
        /// <summary>
        /// The list of known migrations, in order of applicability.
        /// </summary>
        internal static readonly IUpdater[] Migrations =
        {
            new DisableTranscodingThrottling(),
            new DisableZealousLogging()
        };

        /// <summary>
        /// Run all needed migrations.
        /// </summary>
        /// <param name="host">CoreAppHost that hosts current version.</param>
        /// <param name="loggerFactory">Factory for making the logger.</param>
        public static void Run(CoreAppHost host, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<MigrationRunner>();
            var migrationOptions = ((IConfigurationManager)host.ServerConfigurationManager).GetConfiguration<MigrationOptions>("migrations");
            var applied = migrationOptions.Applied.ToList();

            for (var i = 0; i < Migrations.Length; i++)
            {
                var updater = Migrations[i];
                if (applied.Contains(updater.Name))
                {
                    logger.LogDebug("Skipping migration {0} as it is already applied", updater.Name);
                    continue;
                }

                try
                {
                    updater.Perform(host, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot apply migration {0}", updater.Name);
                    continue;
                }

                applied.Add(updater.Name);
            }

            if (applied.Count > migrationOptions.Applied.Length)
            {
                logger.LogInformation("Some migrations were run, saving the state");
                migrationOptions.Applied = applied.ToArray();
                host.ServerConfigurationManager.SaveConfiguration("migrations", migrationOptions);
            }
        }
    }
}
