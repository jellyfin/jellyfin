using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server
{
    /// <summary>
    /// The class that knows how migrate between different Jellyfin versions.
    /// </summary>
    internal static class Migrations
    {
        private static readonly IUpdater[] _migrations =
        {
            new Pre10_5()
        };

        /// <summary>
        /// Interface that descibes a migration routine.
        /// </summary>
        private interface IUpdater
        {
            /// <summary>
            /// Gets maximum version this Updater applies to.
            /// If current version is greater or equal to it, skip the updater.
            /// </summary>
            public abstract Version Maximum { get; }

            /// <summary>
            /// Execute the migration from version "from".
            /// </summary>
            /// <param name="host">Host that hosts current version.</param>
            /// <param name="logger">Host logger.</param>
            /// <param name="from">Version to migrate from.</param>
            /// <returns>Whether configuration was changed.</returns>
            public abstract bool Perform(CoreAppHost host, ILogger logger, Version from);
        }

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

        private class Pre10_5 : IUpdater
        {
            public Version Maximum { get => Version.Parse("10.5.0"); }

            public bool Perform(CoreAppHost host, ILogger logger, Version from)
            {
                // Set EnableThrottling to false as it wasn't used before, and in 10.5.0 it may introduce issues
                var encoding = ((IConfigurationManager)host.ServerConfigurationManager).GetConfiguration<EncodingOptions>("encoding");
                if (encoding.EnableThrottling)
                {
                    logger.LogInformation("Disabling transcoding throttling during migration");
                    encoding.EnableThrottling = false;

                    host.ServerConfigurationManager.SaveConfiguration("encoding", encoding);
                    return true;
                }

                return false;
            }
        }
    }
}
