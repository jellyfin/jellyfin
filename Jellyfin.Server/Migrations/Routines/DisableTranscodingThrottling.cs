using System;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Disable transcode throttling for all installations since it is currently broken for certain video formats.
    /// </summary>
    [JellyfinMigration("2025-04-20T05:00:00", nameof(DisableTranscodingThrottling), "4124C2CD-E939-4FFB-9BE9-9B311C413638")]
#pragma warning disable CS0618 // Type or member is obsolete
    internal class DisableTranscodingThrottling : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ILogger<DisableTranscodingThrottling> _logger;
        private readonly IConfigurationManager _configManager;

        public DisableTranscodingThrottling(ILogger<DisableTranscodingThrottling> logger, IConfigurationManager configManager)
        {
            _logger = logger;
            _configManager = configManager;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            // Set EnableThrottling to false since it wasn't used before and may introduce issues
            var encoding = _configManager.GetEncodingOptions();
            if (encoding.EnableThrottling)
            {
                _logger.LogInformation("Disabling transcoding throttling during migration");
                encoding.EnableThrottling = false;

                _configManager.SaveConfiguration("encoding", encoding);
            }
        }
    }
}
