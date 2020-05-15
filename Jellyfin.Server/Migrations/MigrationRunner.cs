using System;
using System.Linq;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private static readonly Type[] _migrationTypes =
        {
            typeof(Routines.DisableTranscodingThrottling),
            typeof(Routines.CreateUserLoggingConfigFile),
            typeof(Routines.MigrateActivityLogDb),
            typeof(Routines.RemoveDuplicateExtras)
        };

        /// <summary>
        /// Run all needed migrations.
        /// </summary>
        /// <param name="host">CoreAppHost that hosts current version.</param>
        /// <param name="loggerFactory">Factory for making the logger.</param>
        public static void Run(CoreAppHost host, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<MigrationRunner>();
            var migrations = _migrationTypes
                .Select(m => ActivatorUtilities.CreateInstance(host.ServiceProvider, m))
                .OfType<IMigrationRoutine>()
                .ToArray();
            var migrationOptions = ((IConfigurationManager)host.ServerConfigurationManager).GetConfiguration<MigrationOptions>(MigrationsListStore.StoreKey);

            if (!host.ServerConfigurationManager.Configuration.IsStartupWizardCompleted && migrationOptions.Applied.Count == 0)
            {
                // If startup wizard is not finished, this is a fresh install.
                // Don't run any migrations, just mark all of them as applied.
                logger.LogInformation("Marking all known migrations as applied because this is a fresh install");
                migrationOptions.Applied.AddRange(migrations.Select(m => (m.Id, m.Name)));
                host.ServerConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
                return;
            }

            var appliedMigrationIds = migrationOptions.Applied.Select(m => m.Id).ToHashSet();

            for (var i = 0; i < migrations.Length; i++)
            {
                var migrationRoutine = migrations[i];
                if (appliedMigrationIds.Contains(migrationRoutine.Id))
                {
                    logger.LogDebug("Skipping migration '{Name}' since it is already applied", migrationRoutine.Name);
                    continue;
                }

                logger.LogInformation("Applying migration '{Name}'", migrationRoutine.Name);

                try
                {
                    migrationRoutine.Perform();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not apply migration '{Name}'", migrationRoutine.Name);
                    throw;
                }

                // Mark the migration as completed
                logger.LogInformation("Migration '{Name}' applied successfully", migrationRoutine.Name);
                migrationOptions.Applied.Add((migrationRoutine.Id, migrationRoutine.Name));
                host.ServerConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
                logger.LogDebug("Migration '{Name}' marked as applied in configuration.", migrationRoutine.Name);
            }
        }
    }
}
