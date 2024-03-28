using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Migrations
{
    /// <summary>
    /// The class that knows which migrations to apply and how to apply them.
    /// </summary>
    public sealed class MigrationRunner
    {
        /// <summary>
        /// Gets types implementing migration routine interface.
        /// </summary>
        /// <param name="preStartupRoutines">Boolean, true when getting routines to be performed before server start.</param>
        /// <returns>Enumerable of types.</returns>
        public static IEnumerable<Type> GetMigrationTypes(bool preStartupRoutines)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetExportedTypes())
                .Where(p => p.IsClass && !p.IsAbstract && p.IsAssignableTo(preStartupRoutines ? typeof(IPreStartupMigrationRoutine) : typeof(IPostStartupMigrationRoutine)));
        }

        /// <summary>
        /// Run all needed migrations.
        /// </summary>
        /// <param name="serviceProvider">ServiceProvider for dependency injection.</param>
        /// <param name="configurationManager">ServerConfigurationManager for updating configuration.</param>
        /// <param name="loggerFactory">Factory for making the logger.</param>
        public static void Run(IServiceProvider serviceProvider, ServerConfigurationManager configurationManager, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<MigrationRunner>();
            var migrations = GetMigrationTypes(false)
                .Select(m => ActivatorUtilities.CreateInstance(serviceProvider, m))
                .OfType<IMigrationRoutine>()
                .OrderBy(m => m.Timestamp)
                .ToArray();
            var migrationOptions = configurationManager.GetConfiguration<MigrationOptions>(MigrationsListStore.StoreKey);
            HandleStartupWizardCondition(migrations, migrationOptions, configurationManager.Configuration.IsStartupWizardCompleted, logger);
            PerformMigrations(migrations, migrationOptions, options => configurationManager.SaveConfiguration(MigrationsListStore.StoreKey, options), logger);
        }

        /// <summary>
        /// Run all needed pre-startup migrations.
        /// </summary>
        /// <param name="appPaths">Application paths.</param>
        /// <param name="loggerFactory">Factory for making the logger.</param>
        public static void RunPreStartup(ServerApplicationPaths appPaths, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<MigrationRunner>();
            var migrations = GetMigrationTypes(true)
                .Select(m => Activator.CreateInstance(m, appPaths, loggerFactory))
                .OfType<IMigrationRoutine>()
                .OrderBy(m => m.Timestamp)
                .ToArray();

            var xmlSerializer = new MyXmlSerializer();
            var migrationConfigPath = Path.Join(appPaths.ConfigurationDirectoryPath, MigrationsListStore.StoreKey.ToLowerInvariant() + ".xml");
            var migrationOptions = File.Exists(migrationConfigPath)
                 ? (MigrationOptions)xmlSerializer.DeserializeFromFile(typeof(MigrationOptions), migrationConfigPath)!
                 : new MigrationOptions();

            // We have to deserialize it manually since the configuration manager may overwrite it
            var serverConfig = File.Exists(appPaths.SystemConfigurationFilePath)
                ? (ServerConfiguration)xmlSerializer.DeserializeFromFile(typeof(ServerConfiguration), appPaths.SystemConfigurationFilePath)!
                : new ServerConfiguration();

            HandleStartupWizardCondition(migrations, migrationOptions, serverConfig.IsStartupWizardCompleted, logger);
            PerformMigrations(migrations, migrationOptions, options => xmlSerializer.SerializeToFile(options, migrationConfigPath), logger);
        }

        private static void HandleStartupWizardCondition(IEnumerable<IMigrationRoutine> migrations, MigrationOptions migrationOptions, bool isStartWizardCompleted, ILogger logger)
        {
            if (isStartWizardCompleted)
            {
                return;
            }

            // If startup wizard is not finished, this is a fresh install.
            var onlyOldInstalls = migrations.Where(m => !m.PerformOnNewInstall).ToArray();
            logger.LogInformation(
                "Marking following migrations as applied because this is a fresh install: {OnlyOldInstalls}",
                onlyOldInstalls.Select(m => string.Format(CultureInfo.InvariantCulture, "{0} from {1}", m.Name, m.GetType().Namespace)));
            migrationOptions.Applied.AddRange(onlyOldInstalls.Select(m => (m.Id, m.Name)));
        }

        private static void PerformMigrations(IMigrationRoutine[] migrations, MigrationOptions migrationOptions, Action<MigrationOptions> saveConfiguration, ILogger logger)
        {
            // save already applied migrations, and skip them thereafter
            saveConfiguration(migrationOptions);
            var appliedMigrationIds = migrationOptions.Applied.Select(m => m.Id).ToHashSet();

            for (var i = 0; i < migrations.Length; i++)
            {
                var migrationRoutine = migrations[i];
                if (appliedMigrationIds.Contains(migrationRoutine.Id))
                {
                    logger.LogDebug("Skipping migration '{Name}' from '{Namespace}' since it is already applied", migrationRoutine.Name, migrationRoutine.GetType().Namespace);
                    continue;
                }

                logger.LogInformation("Applying migration '{Name}' from '{Namespace}'", migrationRoutine.Name, migrationRoutine.GetType().Namespace);

                try
                {
                    migrationRoutine.Perform();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not apply migration '{Name}' from '{Namespace}'", migrationRoutine.Name, migrationRoutine.GetType().Namespace);
                    throw;
                }

                // Mark the migration as completed
                logger.LogInformation("Migration '{Name}' from '{Namespace}' applied successfully", migrationRoutine.Name, migrationRoutine.GetType().Namespace);
                migrationOptions.Applied.Add((migrationRoutine.Id, migrationRoutine.Name));
                saveConfiguration(migrationOptions);
                logger.LogDebug("Migration '{Name}' from '{Namespace}' marked as applied in configuration.", migrationRoutine.Name, migrationRoutine.GetType().Namespace);
            }
        }
    }
}
