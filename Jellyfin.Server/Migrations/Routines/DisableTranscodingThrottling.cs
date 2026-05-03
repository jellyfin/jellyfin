using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Disable transcode throttling for all installations since it is currently broken for certain video formats.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [JellyfinMigration("2025-04-20T05:00:00", nameof(DisableTranscodingThrottling), "4124C2CD-E939-4FFB-9BE9-9B311C413638")]
    internal class DisableTranscodingThrottling : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ILogger<DisableTranscodingThrottling> _logger;
        private readonly IWritableOptions<EncodingOptions> _encodingOptions;

        public DisableTranscodingThrottling(ILogger<DisableTranscodingThrottling> logger, IWritableOptions<EncodingOptions> encodingOptions)
        {
            _logger = logger;
            _encodingOptions = encodingOptions;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            // Set EnableThrottling to false since it wasn't used before and may introduce issues
            if (_encodingOptions.Value.EnableThrottling)
            {
                _logger.LogInformation("Disabling transcoding throttling during migration");
                _encodingOptions.Update(o => o.EnableThrottling = false);
            }
        }
    }
}
