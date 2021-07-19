using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private const string MigrationNamespace = "Jellyfin.Server.Migrations.Legacy.V";

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
        /// Performs the setting migrations.
        ///
        /// Namespaces with the prefix 'Jellyfin.Server.Migrations.Legacy.V{x}v{y}[r{y}] should contain a method called RunMigration.
        /// This method is responsible for each individual migration.
        /// Each migrations is applied in version order, starting with the earliest.
        ///
        /// </summary>
        /// <param name="appPaths">The <see cref="IServerApplicationPaths"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public static void RunSettingsMigration(IServerApplicationPaths appPaths, ILogger logger)
        {
            var previous = Array.Empty<string>();
            var migrationHistory = Path.Combine(appPaths.ConfigurationDirectoryPath, "settingsMigrations");
            if (File.Exists(migrationHistory))
            {
                try
                {
                    previous = File.ReadAllLines(migrationHistory);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to read migration history. Re-applying all.");
                }
            }

            var completedMigrations = new List<string>(previous);

            var migrations = GetMigrationClasses(MigrationNamespace);
            bool changed = false;
            foreach (string migration in migrations)
            {
                var method = migration + ".RunMigration";
                if (!completedMigrations.Contains(migration))
                {
                    // Each migration namespace needs has a migrateSettings class.
                    var migrationMethodType = Type.GetType(method);
                    if (migrationMethodType != null)
                    {
                        // Execute migration.
                        _ = Activator.CreateInstance(migrationMethodType, appPaths, logger);
                        completedMigrations.Add(migration);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                try
                {
                    File.WriteAllLines(migrationHistory, completedMigrations);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to store migration history.");
                }
            }
        }

        /// <summary>
        /// Performs a generic setting migration, which has to be done before settings.xml is loaded.
        /// </summary>
        /// <typeparam name="T">Source class.</typeparam>
        /// <typeparam name="TU">Destination class.</typeparam>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="sourceFile">Source filename.</param>
        /// <param name="destFile">Destination filename.</param>
        public static void RunSettingMigration<T, TU>(ILogger logger, string sourceFile, string destFile)
            where TU : new()
        {
            try
            {
                var xmlSerializer = new MyXmlSerializer();
                var source = xmlSerializer.DeserializeFromFile(typeof(T), sourceFile);

                var target = new TU();
                var tprops = typeof(TU).GetProperties();
                tprops.Where(x => x.CanWrite == true).ToList().ForEach(prop =>
                {
                    var sp = source.GetType().GetProperty(prop.Name);
                    if (sp != null)
                    {
                        var value = sp.GetValue(source, null);
                        try
                        {
                            target.GetType().GetProperty(prop.Name)?.SetValue(target, value, null);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(ex, "Unable to migrate property {Name}", prop.Name);
                        }
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

        private static string GetNamespace(string ns)
        {
            var i = ns.LastIndexOf('.');
            if (i != -1)
            {
                return ns.Substring(0, i);
            }

            return ns;
        }

        private static Version GetVersion(string ns)
        {
            // extrapolate version number from the namespace.
            return Version.Parse(ns[MigrationNamespace.Length..]
                .Replace("v", ".", StringComparison.Ordinal)
                .Replace("r", ".", StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns a list of namespace classes in the format 'Legacy.V{x}r{y}'.
        /// </summary>
        /// <param name="nameSpace">Namespace to enumerate.</param>
        /// <returns>List of namespaces.</returns>
        private static IEnumerable<string> GetMigrationClasses(string nameSpace)
        {
            var asm = Assembly.GetEntryAssembly();
            return asm!.GetTypes()
                .Where(type => type.Namespace != null
                    && type.Namespace!.StartsWith(nameSpace, StringComparison.OrdinalIgnoreCase))
                .Select(type => GetNamespace(type.FullName!))
                .Distinct()
                .OrderBy(fn => GetVersion(fn));
        }
    }
}
