using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Server.Helpers;
using MediaBrowser.Common.Configuration;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to update logging.default.json to insert suppressions for unnecessary warnings.
    /// Checks for old default logging.default.json without the suppressions and replaces if not present.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [JellyfinMigration("2026-11-06T16:00:00", nameof(MigrateLoggingDefault), "E5670d16-5795-49f5-98FF-0EfC5AFFB8A7")]
    internal class MigrateLoggingDefault : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly IApplicationPaths _appPaths;

        public MigrateLoggingDefault(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            var configDirectory = _appPaths.ConfigurationDirectoryPath;
            var existingConfigPath = Path.Combine(configDirectory, "logging.default.json");

            // If the existing logging.default.json config file is an older version, then 'reset' it by moving it to 'logging.default.old.json'.
            // Migrations happen after CreateAppConfiguration inits the config directory.  Call StartupHelpers to re-init logging.default.json.
            // NOTE: This config file has 'reloadOnChange: true', so this change will take effect immediately even though it has already been loaded
            if (File.Exists(existingConfigPath) && OldDefaultLogging(existingConfigPath))
            {
                File.Move(existingConfigPath, Path.Combine(configDirectory, "logging.default.old.json"), overwrite: true);
                StartupHelpers.InitLoggingConfigFile(_appPaths).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Check if the existing logging.default.json contains the keys added in the new default.
        /// </summary>
        /// <exception cref="IOException"><paramref name="oldConfigPath"/> does not exist or could not be read.</exception>
        private bool OldDefaultLogging(string oldConfigPath)
        {
            try
            {
                var existingConfigJson = JToken.Parse(File.ReadAllText(oldConfigPath));
                var overrides = existingConfigJson["Serilog"]?["MinimumLevel"]?["Override"];

                if (overrides == null)
                {
                    return true;
                }

                var xmlManager = overrides["Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager"]?.ToString();
                var ephemeralXmlRepository = overrides["Microsoft.AspNetCore.DataProtection.Repositories.EphemeralXmlRepository"]?.ToString();
                var staticFiles = overrides["Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware"]?.ToString();

                // If these keys are missing or not set to "Error", the file does not match the new default
                return xmlManager != "Error" || ephemeralXmlRepository != "Error" || staticFiles != "Error";
            }
            catch
            {
                // If the file is unreadable or malformed JSON, it does not match and should be replaced
                return true;
            }
        }
    }
}
