using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Updater that takes care of bringing configuration up to 10.5.0 standards.
    /// </summary>
    internal class Pre_10_5 : IUpdater
    {
        /// <inheritdoc/>
        public Version Maximum { get => Version.Parse("10.5.0"); }

        /// <inheritdoc/>
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
