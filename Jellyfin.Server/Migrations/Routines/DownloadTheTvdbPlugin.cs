using System;
using System.Linq;
using MediaBrowser.Common.Updates;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Download TheTvdb plugin after update.
    /// </summary>
    public class DownloadTheTvdbPlugin : IMigrationRoutine
    {
        private readonly Guid _tvdbPluginId = new Guid("a677c0da-fac5-4cde-941a-7134223f14c8");
        private readonly IInstallationManager _installationManager;
        private readonly ILogger<DownloadTheTvdbPlugin> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadTheTvdbPlugin"/> class.
        /// </summary>
        /// <param name="installationManager">Instance of the <see cref="IInstallationManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{DownloadTvdbPlugin}"/> interface.</param>
        public DownloadTheTvdbPlugin(IInstallationManager installationManager, ILogger<DownloadTheTvdbPlugin> logger)
        {
            _installationManager = installationManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public Guid Id => new Guid("42E45BB4-5D78-4EE2-8C45-9095216D4769");

        /// <inheritdoc />
        public string Name => "DownloadTheTvdbPlugin";

        /// <inheritdoc />
        public bool PerformOnNewInstall => false;

        /// <inheritdoc />
        public void Perform()
        {
            try
            {
                var packages = _installationManager.GetAvailablePackages().GetAwaiter().GetResult();
                var package = _installationManager.GetCompatibleVersions(
                        packages,
                        guid: _tvdbPluginId)
                    .FirstOrDefault();

                if (package == null)
                {
                    _logger.LogWarning("TheTVDB Plugin not found, skipping migration.");
                    return;
                }

                _installationManager.InstallPackage(package).GetAwaiter().GetResult();
                _logger.LogInformation("TheTVDB Plugin installed, please restart Jellyfin.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to install TheTVDB Plugin.");
            }
        }
    }
}