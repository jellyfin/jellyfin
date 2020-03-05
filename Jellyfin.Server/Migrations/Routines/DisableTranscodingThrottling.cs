using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Updater that takes care of bringing configuration up to 10.5.0 standards.
    /// </summary>
    internal class DisableTranscodingThrottling : IUpdater
    {
        /// <inheritdoc/>
        public string Name => "DisableTranscodingThrottling";

        /// <inheritdoc/>
        public void Perform(CoreAppHost host, ILogger logger)
        {
            // Set EnableThrottling to false as it wasn't used before, and in 10.5.0 it may introduce issues
            var encoding = ((IConfigurationManager)host.ServerConfigurationManager).GetConfiguration<EncodingOptions>("encoding");
            if (encoding.EnableThrottling)
            {
                logger.LogInformation("Disabling transcoding throttling during migration");
                encoding.EnableThrottling = false;

                host.ServerConfigurationManager.SaveConfiguration("encoding", encoding);
            }
        }
    }
}
