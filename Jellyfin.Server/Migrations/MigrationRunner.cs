using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;
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
            var migrationOptions = ((IConfigurationManager)host.ServerConfigurationManager).GetConfiguration<MigrationOptions>(MigrationsListStore.StoreKey);

            if (!host.ServerConfigurationManager.Configuration.IsStartupWizardCompleted && migrationOptions.Applied.Count == 0)
            {
                // If startup wizard is not finished, this is a fresh install.
                // Don't run any migrations, just mark all of them as applied.
                logger.LogInformation("Marking all known migrations as applied because this is a fresh install");
                migrationOptions.Applied.AddRange(migrations.Where(m => !m.PerformOnNewInstall).Select(m => (m.Id, m.Name)));
                host.ServerConfigurationManager.SaveConfiguration(MigrationsListStore.StoreKey, migrationOptions);
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

        /// <summary>
        /// Runs the network setting migration.
        /// </summary>
        /// <param name="appPaths">The <see cref="IServerApplicationPaths"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public static void RunNetworkSettingMigration(IServerApplicationPaths appPaths, ILogger logger)
        {
            const string NetworkRoutine = "909525D1-AC26-4DCF-9100-6E94742F3016";
            string routineComplete = "    <ValueTupleOfGuidString>\r\n      <Item1>" + NetworkRoutine + "</Item1>\r\n      <Item2>NetworkSettingMigration</Item2>\r\n    </ValueTupleOfGuidString>";

            // Cannot use ConfigurationManager as it overwrites the changes we need to see, so we need to emulate the
            // function of the MigrationsFactory.
            var migrateFile = Path.Combine(appPaths.ConfigurationDirectoryPath, "migrations.xml");
            bool needToRun = true;
            List<string>? completedMigrations = null;

            if (File.Exists(migrateFile))
            {
                completedMigrations = File.ReadLines(migrateFile, Encoding.UTF8).ToList();
                needToRun = string.IsNullOrEmpty(completedMigrations.FirstOrDefault(p => p.IndexOf(NetworkRoutine, StringComparison.Ordinal) != -1));
            }

            if (needToRun)
            {
                var destFile = Path.Combine(appPaths.ConfigurationDirectoryPath, "network.xml");
                NetworkConfiguration settings = new ();
                var settingsType = typeof(NetworkConfiguration);
                var props = settingsType.GetProperties().Where(x => x.CanWrite).ToList();

                // manually load source xml file.
                var serializer = new XmlSerializer(typeof(ServerConfiguration));
                serializer.UnknownElement += (object? sender, XmlElementEventArgs e) =>
                {
                    var p = props.Find(x => string.Equals(x.Name, e.Element.Name, StringComparison.Ordinal));
                    if (p != null)
                    {
                        if (p.PropertyType == typeof(bool))
                        {
                            bool.TryParse(e.Element.InnerText, out var boolVal);
                            p.SetValue(settings, boolVal);
                        }
                        else if (p.PropertyType == typeof(int))
                        {
                            int.TryParse(e.Element.InnerText, out var intVal);
                            p.SetValue(settings, intVal);
                        }
                        else if (p.PropertyType == typeof(string[]))
                        {
                            var items = new List<string>();
                            foreach (XmlNode el in e.Element.ChildNodes)
                            {
                                items.Add(el.InnerText);
                            }

                            p.SetValue(settings, items.ToArray());
                        }
                        else
                        {
                            try
                            {
                                p.SetValue(settings, e.Element.InnerText ?? string.Empty);
                            }
                            catch
                            {
                                logger.LogDebug(
                                    "Unable to migrate value {Name}. Unknown datatype {DataType}. Value {Value}.",
                                    e.Element.Name,
                                    p.PropertyType,
                                    e.Element.InnerText);
                            }
                        }
                    }
                };

                ServerConfiguration? deserialized;
                try
                {
                    using (StreamReader reader = new StreamReader(appPaths.SystemConfigurationFilePath))
                    {
                        deserialized = (ServerConfiguration?)serializer.Deserialize(reader);
                    }

                    var xmlSerializer = new MyXmlSerializer();
                    xmlSerializer.SerializeToFile(settings, destFile);

                    // Insert our completed state at the end of the migrations, but before the Applied element.
                    if (completedMigrations == null)
                    {
                        // Create the file.
                        File.WriteAllText(
                            migrateFile,
                            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<MigrationOptions xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Applied>\r\n" + routineComplete + "\r\n   </Applied>\r\n</MigrationOptions>\r\n");
                    }
                    else
                    {
                        // Append us to the end of the elements in the file. (the last 2 lines are the root and top element.)
                        completedMigrations.Insert(completedMigrations.Count - 2, routineComplete);
                        File.WriteAllLines(migrateFile, completedMigrations.ToArray());
                    }
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
