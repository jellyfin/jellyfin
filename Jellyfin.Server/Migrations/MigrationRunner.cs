using System;
using System.Linq;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// The class that knows which migrations to apply and how to apply them.
    /// </summary>
    public sealed class MigrationRunner
    {
        /// <summary>
        /// The list of known migrations, in order of applicability.
        /// </summary>
        internal static readonly IMigrationRoutine[] Migrations =
        {
            new Routines.DisableTranscodingThrottling(),
            new Routines.CreateUserLoggingConfigFile()
        };

        /// <summary>
        /// Run all needed migrations.
        /// </summary>
        /// <param name="host">CoreAppHost that hosts current version.</param>
        /// <param name="loggerFactory">Factory for making the logger.</param>
        public static void Run(CoreAppHost host, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<MigrationRunner>();
            var migrationOptions = ((IConfigurationManager)host.ServerConfigurationManager).GetConfiguration<MigrationOptions>(MigrationsListStore.StoreKey);

            if (!host.ServerConfigurationManager.Configuration.IsStartupWizardCompleted && migrationOptions.Applied.Length == 0)
            {
                // If startup wizard is not finished, this is a fresh install.
                // Don't run any migrations, just mark all of them as applied.
                logger.LogInformation("Marking all known migrations as applied because this is fresh install");
                migrationOptions.Applied = Migrations.Select(m => m.Name).ToArray();
                host.ServerConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
                return;
            }

            var applied = migrationOptions.Applied.ToList();

            for (var i = 0; i < Migrations.Length; i++)
            {
                var migrationRoutine = Migrations[i];
                if (applied.Contains(migrationRoutine.Name))
                {
                    logger.LogDebug("Skipping migration {Name} as it is already applied", migrationRoutine.Name);
                    continue;
                }

                logger.LogInformation("Applying migration {Name}", migrationRoutine.Name);

                try
                {
                    migrationRoutine.Perform(host, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not apply migration {Name}", migrationRoutine.Name);
                    continue;
                }

                logger.LogInformation("Migration {Name} applied successfully", migrationRoutine.Name);
                applied.Add(migrationRoutine.Name);
            }

            if (applied.Count > migrationOptions.Applied.Length)
            {
                logger.LogInformation("Some migrations were run, saving the state");
                migrationOptions.Applied = applied.ToArray();
                host.ServerConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
            }
        }
    }
}
