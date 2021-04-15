using System;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
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
            typeof(Routines.RemoveDuplicateExtras),
            typeof(Routines.AddDefaultPluginRepository),
            typeof(Routines.MigrateUserDb),
            typeof(Routines.ReaddDefaultPluginRepository),
            typeof(Routines.MigrateDisplayPreferencesDb),
            typeof(Routines.RemoveDownloadImagesInAdvance),
            typeof(Routines.AddPeopleQueryIndex)
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
            var migrationOptions = ((IConfigurationManager)host.ConfigurationManager).GetConfiguration<MigrationOptions>(MigrationsListStore.StoreKey);

            if (!host.ConfigurationManager.Configuration.IsStartupWizardCompleted && migrationOptions.Applied.Count == 0)
            {
                // If startup wizard is not finished, this is a fresh install.
                // Don't run any migrations, just mark all of them as applied.
                logger.LogInformation("Marking all known migrations as applied because this is a fresh install");
                migrationOptions.Applied.AddRange(migrations.Where(m => !m.PerformOnNewInstall).Select(m => (m.Id, m.Name)));
                host.ConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
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
                host.ConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
                logger.LogDebug("Migration '{Name}' marked as applied in configuration.", migrationRoutine.Name);
            }
        }

        /// <summary>
        /// Runs the network setting migration, which has to be done before settings.xml is loaded.
        /// </summary>
        /// <param name="appPaths">The <see cref="IServerApplicationPaths"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public static void RunNetworkSettingMigration(IServerApplicationPaths appPaths, ILogger logger)
        {
            var destFile = Path.Combine(appPaths.ConfigurationDirectoryPath, "network.xml");
            if (!File.Exists(destFile))
            {
                try
                {
                    var xmlSerializer = new MyXmlSerializer();
                    var source = xmlSerializer.DeserializeFromFile(typeof(Legacy.ServerConfiguration), appPaths.SystemConfigurationFilePath);

                    var target = new NetworkConfiguration();
                    var tprops = typeof(NetworkConfiguration).GetProperties();
                    tprops.Where(x => x.CanWrite == true).ToList().ForEach(prop =>
                    {
                        var sp = source.GetType().GetProperty(prop.Name);
                        if (sp != null)
                        {
                            var value = sp.GetValue(source, null);
                            target.GetType().GetProperty(prop.Name)?.SetValue(target, value, null);
                        }
                    });

                    xmlSerializer.SerializeToFile(target, destFile);
                }
                catch (Exception ex)
                {
                    // Catch everything, so we don't bomb out JF.
                    logger.LogDebug(ex, "Exception occurred migrating settings.");
                }
            }
        }
    }
}
