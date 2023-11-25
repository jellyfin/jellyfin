using System;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Disable transcode throttling for all installations since it is currently broken for certain video formats.
    /// </summary>
    public partial class DisableTranscodingThrottling : IPostStartupMigrationRoutine
    {
        private readonly ILogger<DisableTranscodingThrottling> _logger;
        private readonly IConfigurationManager _configManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableTranscodingThrottling"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configManager">The application paths.</param>
        public DisableTranscodingThrottling(ILogger<DisableTranscodingThrottling> logger, IConfigurationManager configManager)
        {
            _logger = logger;
            _configManager = configManager;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{4124C2CD-E939-4FFB-9BE9-9B311C413638}");

        /// <inheritdoc/>
        public string Name => "DisableTranscodingThrottling";

        /// <inheritdoc />
        public string Timestamp => "20231125220003";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

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
