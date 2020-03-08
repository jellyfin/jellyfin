using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Disable transcode throttling for all installations since it is currently broken for certain video formats.
    /// </summary>
    internal class DisableTranscodingThrottling : IMigrationRoutine
    {
        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{4124C2CD-E939-4FFB-9BE9-9B311C413638}");

        /// <inheritdoc/>
        public string Name => "DisableTranscodingThrottling";

        /// <inheritdoc/>
        public void Perform(CoreAppHost host, ILogger logger)
        {
            // Set EnableThrottling to false since it wasn't used before and may introduce issues
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
