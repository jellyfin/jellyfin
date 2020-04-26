using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Disable metrics collections for all installations since it can be a security risk if not properly secured.
    /// </summary>
    internal class DisableMetricsCollection : IMigrationRoutine
    {
        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{4124C2CD-E939-4FFB-9BE9-9B311C413638}");

        /// <inheritdoc/>
        public string Name => "DisableMetricsCollection";

        /// <inheritdoc/>
        public void Perform(CoreAppHost host, ILogger logger)
        {
            // Set EnableMetrics to false since it can leak sensitive information if not properly secured
            var metrics = host.ServerConfigurationManager.Configuration.EnableMetrics;
            if (metrics)
            {
                logger.LogInformation("Disabling metrics collection during migration");
                metrics = false;

                host.ServerConfigurationManager.SaveConfiguration("false", metrics);
            }
        }
    }
}
