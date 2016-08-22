using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class UpdateLevelMigration : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _releaseAssetFilename;

        public UpdateLevelMigration(IServerConfigurationManager config, IServerApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer, string releaseAssetFilename)
        {
            _config = config;
            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _releaseAssetFilename = releaseAssetFilename;
        }

        public async void Run()
        {
            var lastVersion = _config.Configuration.LastVersion;
            var currentVersion = _appHost.ApplicationVersion;

            if (string.Equals(lastVersion, currentVersion.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var updateLevel = _config.Configuration.SystemUpdateLevel;

                // Go down a level
                if (updateLevel == PackageVersionClass.Release)
                {
                    updateLevel = PackageVersionClass.Beta;
                }
                else if (updateLevel == PackageVersionClass.Beta)
                {
                    updateLevel = PackageVersionClass.Dev;
                }
                else if (updateLevel == PackageVersionClass.Dev)
                {
                    // It's already dev, there's nothing to check
                    return;
                }

                await CheckVersion(currentVersion, updateLevel, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        private async Task CheckVersion(Version currentVersion, PackageVersionClass updateLevel, CancellationToken cancellationToken)
        {
            var result = await new GithubUpdater(_httpClient, _jsonSerializer, TimeSpan.FromMinutes(5))
                .CheckForUpdateResult("MediaBrowser", "Emby", currentVersion, PackageVersionClass.Beta, _releaseAssetFilename, "MBServer", "Mbserver.zip",
                    cancellationToken).ConfigureAwait(false);

            if (result != null && result.IsUpdateAvailable)
            {
                _config.Configuration.SystemUpdateLevel = updateLevel;
                _config.SaveConfiguration();
                return;
            }

            // Go down a level
            if (updateLevel == PackageVersionClass.Release)
            {
                updateLevel = PackageVersionClass.Beta;
            }
            else if (updateLevel == PackageVersionClass.Beta)
            {
                updateLevel = PackageVersionClass.Dev;
            }
            else
            {
                return;
            }

            await CheckVersion(currentVersion, updateLevel, cancellationToken).ConfigureAwait(false);
        }
    }
}
